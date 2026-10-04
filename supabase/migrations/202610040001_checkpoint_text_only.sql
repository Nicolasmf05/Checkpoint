-- Migra las publicaciones a progreso sin imágenes y retira los recursos de Storage anteriores.
-- Checkpoint stores only bounded JSON/text metadata in Supabase.
-- Does not delete existing files, accounts, progress or image references.
-- Remove actual files with the Storage API after backing them up and approval.
BEGIN;

-- Restrictive rules also deny uploads if another permissive policy is added later.
DROP POLICY if EXISTS cp_asset_insert ON storage.objects;

DROP POLICY if EXISTS cp_asset_update ON storage.objects;

CREATE POLICY cp_text_only_insert ON storage.objects AS restrictive FOR insert TO anon,
authenticated
WITH
  CHECK (FALSE);

CREATE POLICY cp_text_only_update ON storage.objects AS restrictive
FOR UPDATE
  TO anon,
  authenticated USING (FALSE)
WITH
  CHECK (FALSE);

-- Storage service credentials bypass RLS, but not this upload guard.
CREATE FUNCTION checkpoint_private.reject_file_storage () returns trigger language plpgsql
SET
  search_path = '' AS $$
begin
    raise exception 'Checkpoint stores text only; file uploads are disabled' using errcode = '42501';
end;
$$;

REVOKE ALL ON function checkpoint_private.reject_file_storage ()
FROM
  public,
  anon,
  authenticated;

CREATE TRIGGER cp_text_only_storage_guard
BEFORE INSERT OR UPDATE ON storage.objects FOR EACH ROW
EXECUTE FUNCTION checkpoint_private.reject_file_storage ();

-- Legacy rows can still be read. Any new/updated publication is text-only.
ALTER TABLE public.cp_game_publications
ADD CONSTRAINT cp_text_only_publication CHECK (
  cover_path IS NULL
  AND (
    operation_payload IS NULL
    OR (
      operation_payload ->> 'coverPath' IS NULL
      AND operation_payload::text !~* 'data:[^" ]*;base64,'
    )
  )
) NOT valid;

ALTER TABLE public.cp_profiles
ADD CONSTRAINT cp_text_only_profile CHECK (avatar_path IS NULL) NOT valid;

ALTER TABLE checkpoint_steam.entries
ADD CONSTRAINT cp_text_only_state CHECK (
  octet_length(value::text) <= 2097152
  AND value::text !~* 'data:[^" ]*;base64,'
) NOT valid;

CREATE OR REPLACE FUNCTION public.cp_publish_game (
  p_game_id uuid,
  p_expected_revision bigint,
  p_operation_id uuid,
  p_game jsonb
) returns bigint language plpgsql security definer
SET
  search_path = '' AS $$
declare actor uuid := auth.uid(); current_row public.cp_game_publications; result_revision bigint;
begin
    if actor is null then raise exception 'Inicia sesión' using errcode = '42501'; end if;
    if p_game_id is null or p_operation_id is null or p_expected_revision is null or p_expected_revision < 0 then
        raise exception 'Publicación no válida' using errcode = '22023';
    end if;
    -- Legacy clients may include a cover path; it is ignored rather than stored.
    if p_game is not null and jsonb_typeof(p_game) = 'object' then
        p_game := p_game || jsonb_build_object('coverPath',null);
    end if;
    if p_game is not null and (jsonb_typeof(p_game) <> 'object' or octet_length(p_game::text) > 4096) then
        raise exception 'Publicación no válida' using errcode = '22023';
    end if;
    if p_game is not null and exists (select 1 from jsonb_object_keys(p_game) k where k not in
        ('title','platform','status','goalKind','goalText','storyPercent','tasksDone','tasksTotal',
         'achievementsUnlocked','achievementsTotal','steamAppId','coverPath','finishedAt')) then
        raise exception 'Campo no permitido. Las notas y las etiquetas de tareas son privadas' using errcode = '22023';
    end if;
    if p_game is not null and exists (
        select 1 from jsonb_each(p_game) as entry(key, value)
        where (key in ('title','platform','status','goalKind','goalText','coverPath','finishedAt')
                and jsonb_typeof(value) not in ('string','null'))
           or (key in ('storyPercent','tasksDone','tasksTotal','achievementsUnlocked','achievementsTotal','steamAppId')
                and jsonb_typeof(value) <> 'null'
                and (jsonb_typeof(value) <> 'number' or value::text !~ '^-?[0-9]+$'))
    ) then
        raise exception 'Los campos tienen un tipo no válido' using errcode = '22023';
    end if;
    insert into public.cp_game_publications(owner_id, game_id) values (actor, p_game_id) on conflict do nothing;
    select * into current_row from public.cp_game_publications where owner_id = actor and game_id = p_game_id for update;
    if current_row.operation_id = p_operation_id then
        if (current_row.operation_payload - 'coverPath') is distinct from (p_game - 'coverPath') then
            raise exception 'La operación ya se usó con otro contenido' using errcode = '22023';
        end if;
        return current_row.revision;
    end if;
    if current_row.revision <> p_expected_revision then
        raise exception 'El progreso cambió. Actualiza antes de publicar' using errcode = '40001';
    end if;
    update public.cp_game_publications set
        revision = revision + 1, operation_id = p_operation_id, operation_payload = p_game, is_shared = p_game is not null,
        title = p_game->>'title', platform = p_game->>'platform', game_status = p_game->>'status',
        goal_kind = p_game->>'goalKind', goal_text = p_game->>'goalText',
        story_percent = (p_game->>'storyPercent')::integer,
        tasks_done = (p_game->>'tasksDone')::integer, tasks_total = (p_game->>'tasksTotal')::integer,
        achievements_unlocked = (p_game->>'achievementsUnlocked')::integer,
        achievements_total = (p_game->>'achievementsTotal')::integer,
        steam_app_id = (p_game->>'steamAppId')::integer, cover_path = p_game->>'coverPath',
        finished_at = (p_game->>'finishedAt')::timestamptz, updated_at = now()
    where owner_id = actor and game_id = p_game_id returning revision into result_revision;
    return result_revision;
end;
$$;

COMMIT;
