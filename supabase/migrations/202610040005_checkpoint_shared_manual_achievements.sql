-- Share only text and completion of manual achievements; keep existing friend access controls.
BEGIN;

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
    if p_game is not null and (jsonb_typeof(p_game) <> 'object' or octet_length(p_game::text) > 700000) then
        raise exception 'Publicación no válida' using errcode = '22023';
    end if;
    if p_game is not null and exists (select 1 from jsonb_object_keys(p_game) k where k not in
        ('title','platform','status','goalKind','goalText','storyPercent','tasksDone','tasksTotal',
         'achievementsUnlocked','achievementsTotal','steamAppId','coverPath','finishedAt','manualAchievementsJson')) then
        raise exception 'Campo no permitido. Las notas y las etiquetas de tareas son privadas' using errcode = '22023';
    end if;
    if p_game is not null and exists (
        select 1 from jsonb_each(p_game) as entry(key, value)
        where (key in ('title','platform','status','goalKind','goalText','coverPath','finishedAt','manualAchievementsJson')
                and jsonb_typeof(value) not in ('string','null'))
           or (key in ('storyPercent','tasksDone','tasksTotal','achievementsUnlocked','achievementsTotal','steamAppId')
                and jsonb_typeof(value) <> 'null'
                and (jsonb_typeof(value) <> 'number' or value::text !~ '^-?[0-9]+$'))
    ) then
        raise exception 'Los campos tienen un tipo no válido' using errcode = '22023';
    end if;
    if p_game->>'manualAchievementsJson' is not null then
        if octet_length(p_game->>'manualAchievementsJson') > 600000 or jsonb_typeof((p_game->>'manualAchievementsJson')::jsonb) <> 'array' then
            raise exception 'Logros manuales no válidos' using errcode = '22023';
        end if;
        if jsonb_array_length((p_game->>'manualAchievementsJson')::jsonb) > 200 then
            raise exception 'Logros manuales no válidos' using errcode = '22023';
        end if;
        if exists (select 1 from jsonb_array_elements((p_game->>'manualAchievementsJson')::jsonb) a
            where jsonb_typeof(a) <> 'object'
               or not (a ?& array['name','description','completed'])
               or (a - 'name' - 'description' - 'completed') <> '{}'::jsonb
               or jsonb_typeof(a->'name') <> 'string' or length(a->>'name') not between 1 and 250
               or jsonb_typeof(a->'description') <> 'string' or length(a->>'description') > 2000
               or jsonb_typeof(a->'completed') <> 'boolean') then
            raise exception 'Logros manuales no válidos' using errcode = '22023';
        end if;
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
