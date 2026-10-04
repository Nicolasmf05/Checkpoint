-- Esquema social inicial: perfiles, solicitudes, amistades, bloqueos y progreso compartido.
-- Las políticas RLS y los RPC delimitan qué puede leer y modificar cada usuario.
-- Checkpoint accounts belong to Supabase Auth, independently of Steam.
-- Apply once to a dedicated project. No existing tables are replaced.
BEGIN;

CREATE SCHEMA if NOT EXISTS checkpoint_private;

REVOKE ALL ON schema checkpoint_private
FROM
  public,
  anon;

GRANT usage ON schema checkpoint_private TO authenticated;

-- Los perfiles almacenan identidad pública; el contenido de la biblioteca se publica por separado.
CREATE TABLE public.cp_profiles (
  user_id uuid PRIMARY KEY REFERENCES auth.users (id) ON DELETE CASCADE,
  friend_code text NOT NULL UNIQUE DEFAULT (
    'cp-' || substr(replace(gen_random_uuid()::text, '-', ''), 1, 12)
  ),
  display_name text NOT NULL DEFAULT 'Jugador de Checkpoint',
  avatar_path text,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT cp_profile_name CHECK (
    length(btrim(display_name)) BETWEEN 1 AND 50
    AND display_name !~ '[[:cntrl:]]'
  ),
  CONSTRAINT cp_avatar_path CHECK (
    avatar_path IS NULL
    OR avatar_path ~ (
      '^' || user_id::text || '/avatars/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$'
    )
  )
);

CREATE TABLE public.cp_friend_requests (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  recipient_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  status text NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'accepted', 'rejected')),
  created_at timestamptz NOT NULL DEFAULT now(),
  CHECK (sender_id <> recipient_id)
);

CREATE UNIQUE INDEX cp_pending_request ON public.cp_friend_requests (sender_id, recipient_id)
WHERE
  status = 'pending';

CREATE INDEX cp_request_recipient ON public.cp_friend_requests (recipient_id, status);

CREATE INDEX cp_request_sender ON public.cp_friend_requests (sender_id, created_at);

-- Separate events keep cancellation from resetting the request rate limit.
CREATE TABLE checkpoint_private.request_events (
  sender_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX cp_request_event_sender ON checkpoint_private.request_events (sender_id, created_at);

REVOKE ALL ON checkpoint_private.request_events
FROM
  public,
  anon,
  authenticated;

ALTER TABLE checkpoint_private.request_events enable ROW level security;

CREATE TABLE public.cp_friendships (
  user_low uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  user_high uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (user_low, user_high),
  CHECK (user_low < user_high)
);

CREATE INDEX cp_friendship_high ON public.cp_friendships (user_high);

CREATE TABLE public.cp_blocks (
  blocker_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  blocked_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (blocker_id, blocked_id),
  CHECK (blocker_id <> blocked_id)
);

CREATE INDEX cp_blocked_user ON public.cp_blocks (blocked_id);

-- A withdrawn publication keeps only a revision, so stale clients cannot silently
-- republish deleted data. Personal notes and task labels have no columns here.
CREATE TABLE public.cp_game_publications (
  owner_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  game_id uuid NOT NULL,
  revision bigint NOT NULL DEFAULT 0 CHECK (revision >= 0),
  operation_id uuid,
  operation_payload jsonb,
  is_shared boolean NOT NULL DEFAULT FALSE,
  title text CHECK (length(title) BETWEEN 1 AND 140),
  platform text CHECK (length(platform) BETWEEN 1 AND 60),
  game_status text CHECK (
    game_status IN (
      'pending',
      'playing',
      'paused',
      'finished',
      'abandoned'
    )
  ),
  goal_kind text CHECK (goal_kind IN ('story', 'achievements', 'custom')),
  goal_text text CHECK (length(goal_text) <= 500),
  story_percent integer CHECK (story_percent BETWEEN 0 AND 100),
  tasks_done integer CHECK (tasks_done BETWEEN 0 AND 200),
  tasks_total integer CHECK (tasks_total BETWEEN 1 AND 200),
  achievements_unlocked integer CHECK (achievements_unlocked BETWEEN 0 AND 100000),
  achievements_total integer CHECK (achievements_total BETWEEN 1 AND 100000),
  steam_app_id integer CHECK (steam_app_id > 0),
  cover_path text,
  finished_at timestamptz,
  updated_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (owner_id, game_id),
  CHECK (
    (
      is_shared
      AND operation_payload IS NOT NULL
      AND jsonb_typeof(operation_payload) = 'object'
      AND octet_length(operation_payload::text) <= 4096
    )
    OR (
      NOT is_shared
      AND operation_payload IS NULL
    )
  ),
  CHECK (
    (
      tasks_done IS NULL
      AND tasks_total IS NULL
    )
    OR (
      tasks_done IS NOT NULL
      AND tasks_total IS NOT NULL
      AND tasks_done <= tasks_total
    )
  ),
  CHECK (
    (
      achievements_unlocked IS NULL
      AND achievements_total IS NULL
    )
    OR (
      achievements_unlocked IS NOT NULL
      AND achievements_total IS NOT NULL
      AND achievements_unlocked <= achievements_total
    )
  ),
  CHECK (
    cover_path IS NULL
    OR cover_path ~ (
      '^' || owner_id::text || '/covers/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$'
    )
  ),
  CHECK (
    NOT is_shared
    OR (
      title IS NOT NULL
      AND platform IS NOT NULL
      AND game_status IS NOT NULL
      AND goal_kind IS NOT NULL
    )
  ),
  CHECK (
    is_shared
    OR (
      title IS NULL
      AND platform IS NULL
      AND game_status IS NULL
      AND goal_kind IS NULL
      AND goal_text IS NULL
      AND story_percent IS NULL
      AND tasks_done IS NULL
      AND tasks_total IS NULL
      AND achievements_unlocked IS NULL
      AND achievements_total IS NULL
      AND steam_app_id IS NULL
      AND cover_path IS NULL
      AND finished_at IS NULL
    )
  )
);

CREATE FUNCTION checkpoint_private.is_blocked (target uuid) returns boolean language sql stable security definer
SET
  search_path = '' AS $$
    select exists (select 1 from public.cp_blocks b where
        (b.blocker_id = (select auth.uid()) and b.blocked_id = target) or
        (b.blocker_id = target and b.blocked_id = (select auth.uid())));
$$;

CREATE FUNCTION checkpoint_private.is_friend (target uuid) returns boolean language sql stable security definer
SET
  search_path = '' AS $$
    select not checkpoint_private.is_blocked(target) and exists (
        select 1 from public.cp_friendships f where
        f.user_low = least((select auth.uid()), target) and
        f.user_high = greatest((select auth.uid()), target));
$$;

REVOKE ALL ON function checkpoint_private.is_blocked (uuid),
checkpoint_private.is_friend (uuid)
FROM
  public,
  anon;

GRANT
EXECUTE ON function checkpoint_private.is_blocked (uuid),
checkpoint_private.is_friend (uuid) TO authenticated;

ALTER TABLE public.cp_profiles enable ROW level security;

ALTER TABLE public.cp_friend_requests enable ROW level security;

ALTER TABLE public.cp_friendships enable ROW level security;

ALTER TABLE public.cp_blocks enable ROW level security;

ALTER TABLE public.cp_game_publications enable ROW level security;

REVOKE ALL ON public.cp_profiles,
public.cp_friend_requests,
public.cp_friendships,
public.cp_blocks,
public.cp_game_publications
FROM
  anon,
  authenticated;

GRANT
SELECT
  ON public.cp_profiles,
  public.cp_friend_requests,
  public.cp_friendships,
  public.cp_blocks,
  public.cp_game_publications TO authenticated;

GRANT
UPDATE (display_name, avatar_path) ON public.cp_profiles TO authenticated;

GRANT insert (sender_id, recipient_id),
UPDATE (status),
delete ON public.cp_friend_requests TO authenticated;

GRANT delete ON public.cp_friendships TO authenticated;

GRANT insert (blocker_id, blocked_id),
delete ON public.cp_blocks TO authenticated;

CREATE POLICY cp_profile_read ON public.cp_profiles FOR
SELECT
  TO authenticated USING (
    user_id = (
      SELECT
        auth.uid ()
    )
    OR (
      NOT checkpoint_private.is_blocked (user_id)
      AND (
        checkpoint_private.is_friend (user_id)
        OR EXISTS (
          SELECT
            1
          FROM
            public.cp_friend_requests r
          WHERE
            r.status = 'pending'
            AND (
              (
                r.sender_id = (
                  SELECT
                    auth.uid ()
                )
                AND r.recipient_id = user_id
              )
              OR (
                r.recipient_id = (
                  SELECT
                    auth.uid ()
                )
                AND r.sender_id = user_id
              )
            )
        )
      )
    )
  );

CREATE POLICY cp_profile_update ON public.cp_profiles
FOR UPDATE
  TO authenticated USING (
    user_id = (
      SELECT
        auth.uid ()
    )
  )
WITH
  CHECK (
    user_id = (
      SELECT
        auth.uid ()
    )
  );

CREATE POLICY cp_request_read ON public.cp_friend_requests FOR
SELECT
  TO authenticated USING (
    (
      SELECT
        auth.uid ()
    ) IN (sender_id, recipient_id)
  );

CREATE POLICY cp_request_insert ON public.cp_friend_requests FOR insert TO authenticated
WITH
  CHECK (
    sender_id = (
      SELECT
        auth.uid ()
    )
    AND status = 'pending'
  );

CREATE POLICY cp_request_answer ON public.cp_friend_requests
FOR UPDATE
  TO authenticated USING (
    recipient_id = (
      SELECT
        auth.uid ()
    )
    AND status = 'pending'
  )
WITH
  CHECK (
    recipient_id = (
      SELECT
        auth.uid ()
    )
    AND status IN ('accepted', 'rejected')
  );

CREATE POLICY cp_request_delete ON public.cp_friend_requests FOR delete TO authenticated USING (
  (
    SELECT
      auth.uid ()
  ) IN (sender_id, recipient_id)
);

CREATE POLICY cp_friendship_read ON public.cp_friendships FOR
SELECT
  TO authenticated USING (
    (
      SELECT
        auth.uid ()
    ) IN (user_low, user_high)
    AND NOT checkpoint_private.is_blocked (
      CASE
        WHEN user_low = (
          SELECT
            auth.uid ()
        ) THEN user_high
        ELSE user_low
      END
    )
  );

CREATE POLICY cp_friendship_delete ON public.cp_friendships FOR delete TO authenticated USING (
  (
    SELECT
      auth.uid ()
  ) IN (user_low, user_high)
);

CREATE POLICY cp_block_read ON public.cp_blocks FOR
SELECT
  TO authenticated USING (
    blocker_id = (
      SELECT
        auth.uid ()
    )
  );

CREATE POLICY cp_block_insert ON public.cp_blocks FOR insert TO authenticated
WITH
  CHECK (
    blocker_id = (
      SELECT
        auth.uid ()
    )
  );

CREATE POLICY cp_block_delete ON public.cp_blocks FOR delete TO authenticated USING (
  blocker_id = (
    SELECT
      auth.uid ()
  )
);

CREATE POLICY cp_publication_read ON public.cp_game_publications FOR
SELECT
  TO authenticated USING (
    owner_id = (
      SELECT
        auth.uid ()
    )
    OR (
      is_shared
      AND checkpoint_private.is_friend (owner_id)
    )
  );

CREATE FUNCTION checkpoint_private.create_profile () returns trigger language plpgsql security definer
SET
  search_path = '' AS $$
declare candidate text;
begin
    candidate := new.raw_user_meta_data->>'display_name';
    if candidate is null or length(btrim(candidate)) not between 1 and 50 or candidate ~ '[[:cntrl:]]' then
        candidate := 'Jugador de Checkpoint';
    end if;
    insert into public.cp_profiles(user_id, display_name) values (new.id, btrim(candidate));
    return new;
end;
$$;

CREATE TRIGGER cp_new_user
AFTER INSERT ON auth.users FOR EACH ROW
EXECUTE FUNCTION checkpoint_private.create_profile ();

CREATE FUNCTION checkpoint_private.guard_request () returns trigger language plpgsql security definer
SET
  search_path = '' AS $$
begin
    -- Serialize crossed invitations, acceptance and blocking for this pair.
    perform pg_advisory_xact_lock(hashtextextended(
        least(new.sender_id, new.recipient_id)::text || ':' ||
        greatest(new.sender_id, new.recipient_id)::text, 1));
    if tg_op = 'INSERT' then
        if new.sender_id is distinct from auth.uid() then raise exception 'Solicitud no autorizada' using errcode = '42501'; end if;
        perform pg_advisory_xact_lock(hashtextextended(new.sender_id::text, 0));
        if checkpoint_private.is_blocked(new.recipient_id) or checkpoint_private.is_friend(new.recipient_id) then
            raise exception 'Solicitud no disponible';
        end if;
        if exists (select 1 from public.cp_friend_requests where sender_id = new.recipient_id and recipient_id = new.sender_id and status = 'pending') then
            raise exception 'Ya hay una solicitud pendiente';
        end if;
        if (select count(*) from public.cp_friend_requests where sender_id = new.sender_id and status = 'pending') >= 50 or
           (select count(*) from checkpoint_private.request_events where sender_id = new.sender_id and created_at > now() - interval '1 minute') >= 10 then
            raise exception 'Demasiadas solicitudes. Espera antes de volver a intentarlo';
        end if;
        new.status := 'pending'; new.created_at := now();
        delete from checkpoint_private.request_events where sender_id = new.sender_id and created_at < now() - interval '1 day';
        insert into checkpoint_private.request_events(sender_id) values (new.sender_id);
    else
        if old.status <> 'pending' or new.recipient_id is distinct from auth.uid() or
           new.status not in ('accepted', 'rejected') or checkpoint_private.is_blocked(new.sender_id) then
            raise exception 'Respuesta no autorizada' using errcode = '42501';
        end if;
        if new.status = 'accepted' then
            insert into public.cp_friendships(user_low, user_high)
                values (least(new.sender_id, new.recipient_id), greatest(new.sender_id, new.recipient_id)) on conflict do nothing;
        end if;
    end if;
    return new;
end;
$$;

CREATE TRIGGER cp_request_guard
BEFORE INSERT OR UPDATE ON public.cp_friend_requests FOR EACH ROW
EXECUTE FUNCTION checkpoint_private.guard_request ();

CREATE FUNCTION checkpoint_private.apply_block () returns trigger language plpgsql security definer
SET
  search_path = '' AS $$
begin
    perform pg_advisory_xact_lock(hashtextextended(
        least(new.blocker_id, new.blocked_id)::text || ':' ||
        greatest(new.blocker_id, new.blocked_id)::text, 1));
    delete from public.cp_friendships where user_low = least(new.blocker_id, new.blocked_id) and user_high = greatest(new.blocker_id, new.blocked_id);
    delete from public.cp_friend_requests where (sender_id = new.blocker_id and recipient_id = new.blocked_id) or (sender_id = new.blocked_id and recipient_id = new.blocker_id);
    return new;
end;
$$;

CREATE TRIGGER cp_apply_block
AFTER INSERT ON public.cp_blocks FOR EACH ROW
EXECUTE FUNCTION checkpoint_private.apply_block ();

CREATE FUNCTION public.cp_find_friend (p_code text) returns TABLE (user_id uuid, display_name text, friend_code text) language sql stable security definer
SET
  search_path = '' AS $$
    select p.user_id, p.display_name, p.friend_code from public.cp_profiles p
    where auth.uid() is not null and length(p_code) = 15 and p.friend_code = lower(btrim(p_code))
    and p.user_id <> auth.uid() and not checkpoint_private.is_blocked(p.user_id);
$$;

-- La revisión esperada detecta conflictos y el identificador de operación hace idempotentes los reintentos.
CREATE FUNCTION public.cp_publish_game (
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
        if current_row.operation_payload is distinct from p_game then
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

-- Functions are not executable by anonymous clients or by PUBLIC.
REVOKE ALL ON ALL functions IN schema checkpoint_private
FROM
  public,
  anon,
  authenticated;

GRANT
EXECUTE ON function checkpoint_private.is_blocked (uuid),
checkpoint_private.is_friend (uuid) TO authenticated;

REVOKE ALL ON function public.cp_find_friend (text),
public.cp_publish_game (uuid, bigint, uuid, jsonb)
FROM
  public,
  anon;

GRANT
EXECUTE ON function public.cp_find_friend (text),
public.cp_publish_game (uuid, bigint, uuid, jsonb) TO authenticated;

-- Private images: own uploads; friends can read only referenced shared covers
-- or avatars from profiles that their current permissions allow them to see.
INSERT INTO
  storage.buckets (
    id,
    name,
    public,
    file_size_limit,
    allowed_mime_types
  )
VALUES
  (
    'checkpoint-assets',
    'checkpoint-assets',
    FALSE,
    2097152,
    ARRAY['image/png', 'image/jpeg', 'image/webp']
  );

CREATE POLICY cp_asset_read ON storage.objects FOR
SELECT
  TO authenticated USING (
    bucket_id = 'checkpoint-assets'
    AND (
      (storage.foldername (name)) [1] = (
        SELECT
          auth.uid ()
      )::text
      OR EXISTS (
        SELECT
          1
        FROM
          public.cp_game_publications g
        WHERE
          g.is_shared
          AND g.cover_path = name
      )
      OR EXISTS (
        SELECT
          1
        FROM
          public.cp_profiles p
        WHERE
          p.avatar_path = name
      )
    )
  );

CREATE POLICY cp_asset_insert ON storage.objects FOR insert TO authenticated
WITH
  CHECK (
    bucket_id = 'checkpoint-assets'
    AND name ~ (
      '^' || (
        SELECT
          auth.uid ()
      )::text || '/(avatars|covers)/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$'
    )
  );

CREATE POLICY cp_asset_update ON storage.objects
FOR UPDATE
  TO authenticated USING (
    bucket_id = 'checkpoint-assets'
    AND (storage.foldername (name)) [1] = (
      SELECT
        auth.uid ()
    )::text
  )
WITH
  CHECK (
    bucket_id = 'checkpoint-assets'
    AND name ~ (
      '^' || (
        SELECT
          auth.uid ()
      )::text || '/(avatars|covers)/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$'
    )
  );

CREATE POLICY cp_asset_delete ON storage.objects FOR delete TO authenticated USING (
  bucket_id = 'checkpoint-assets'
  AND (storage.foldername (name)) [1] = (
    SELECT
      auth.uid ()
  )::text
);

COMMIT;
