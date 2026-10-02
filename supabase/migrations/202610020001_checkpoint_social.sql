-- Checkpoint accounts belong to Supabase Auth, independently of Steam.
-- Apply once to a dedicated project. No existing tables are replaced.
begin;

create schema if not exists checkpoint_private;
revoke all on schema checkpoint_private from public, anon;
grant usage on schema checkpoint_private to authenticated;

create table public.cp_profiles (
    user_id uuid primary key references auth.users(id) on delete cascade,
    friend_code text not null unique default ('cp-' || substr(replace(gen_random_uuid()::text, '-', ''), 1, 12)),
    display_name text not null default 'Jugador de Checkpoint',
    avatar_path text,
    created_at timestamptz not null default now(),
    constraint cp_profile_name check (length(btrim(display_name)) between 1 and 50 and display_name !~ '[[:cntrl:]]'),
    constraint cp_avatar_path check (avatar_path is null or avatar_path ~ ('^' || user_id::text || '/avatars/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$'))
);
create table public.cp_friend_requests (
    id uuid primary key default gen_random_uuid(),
    sender_id uuid not null references public.cp_profiles(user_id) on delete cascade,
    recipient_id uuid not null references public.cp_profiles(user_id) on delete cascade,
    status text not null default 'pending' check (status in ('pending', 'accepted', 'rejected')),
    created_at timestamptz not null default now(),
    check (sender_id <> recipient_id)
);
create unique index cp_pending_request on public.cp_friend_requests(sender_id, recipient_id) where status = 'pending';
create index cp_request_recipient on public.cp_friend_requests(recipient_id, status);
create index cp_request_sender on public.cp_friend_requests(sender_id, created_at);
-- Separate events keep cancellation from resetting the request rate limit.
create table checkpoint_private.request_events (
    sender_id uuid not null references public.cp_profiles(user_id) on delete cascade,
    created_at timestamptz not null default now()
);
create index cp_request_event_sender on checkpoint_private.request_events(sender_id, created_at);
revoke all on checkpoint_private.request_events from public, anon, authenticated;
alter table checkpoint_private.request_events enable row level security;
create table public.cp_friendships (
    user_low uuid not null references public.cp_profiles(user_id) on delete cascade,
    user_high uuid not null references public.cp_profiles(user_id) on delete cascade,
    created_at timestamptz not null default now(),
    primary key (user_low, user_high),
    check (user_low < user_high)
);
create index cp_friendship_high on public.cp_friendships(user_high);
create table public.cp_blocks (
    blocker_id uuid not null references public.cp_profiles(user_id) on delete cascade,
    blocked_id uuid not null references public.cp_profiles(user_id) on delete cascade,
    created_at timestamptz not null default now(),
    primary key (blocker_id, blocked_id),
    check (blocker_id <> blocked_id)
);
create index cp_blocked_user on public.cp_blocks(blocked_id);

-- A withdrawn publication keeps only a revision, so stale clients cannot silently
-- republish deleted data. Personal notes and task labels have no columns here.
create table public.cp_game_publications (
    owner_id uuid not null references public.cp_profiles(user_id) on delete cascade,
    game_id uuid not null,
    revision bigint not null default 0 check (revision >= 0),
    operation_id uuid,
    operation_payload jsonb,
    is_shared boolean not null default false,
    title text check (length(title) between 1 and 140),
    platform text check (length(platform) between 1 and 60),
    game_status text check (game_status in ('pending', 'playing', 'paused', 'finished', 'abandoned')),
    goal_kind text check (goal_kind in ('story', 'achievements', 'custom')),
    goal_text text check (length(goal_text) <= 500),
    story_percent integer check (story_percent between 0 and 100),
    tasks_done integer check (tasks_done between 0 and 200),
    tasks_total integer check (tasks_total between 1 and 200),
    achievements_unlocked integer check (achievements_unlocked between 0 and 100000),
    achievements_total integer check (achievements_total between 1 and 100000),
    steam_app_id integer check (steam_app_id > 0),
    cover_path text,
    finished_at timestamptz,
    updated_at timestamptz not null default now(),
    primary key (owner_id, game_id),
    check ((is_shared and operation_payload is not null and jsonb_typeof(operation_payload) = 'object' and octet_length(operation_payload::text) <= 4096) or (not is_shared and operation_payload is null)),
    check ((tasks_done is null and tasks_total is null) or (tasks_done is not null and tasks_total is not null and tasks_done <= tasks_total)),
    check ((achievements_unlocked is null and achievements_total is null) or (achievements_unlocked is not null and achievements_total is not null and achievements_unlocked <= achievements_total)),
    check (cover_path is null or cover_path ~ ('^' || owner_id::text || '/covers/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$')),
    check (not is_shared or (title is not null and platform is not null and game_status is not null and goal_kind is not null)),
    check (is_shared or (title is null and platform is null and game_status is null and goal_kind is null and goal_text is null and story_percent is null and tasks_done is null and tasks_total is null and achievements_unlocked is null and achievements_total is null and steam_app_id is null and cover_path is null and finished_at is null))
);

create function checkpoint_private.is_blocked(target uuid) returns boolean
language sql stable security definer set search_path = '' as $$
    select exists (select 1 from public.cp_blocks b where
        (b.blocker_id = (select auth.uid()) and b.blocked_id = target) or
        (b.blocker_id = target and b.blocked_id = (select auth.uid())));
$$;
create function checkpoint_private.is_friend(target uuid) returns boolean
language sql stable security definer set search_path = '' as $$
    select not checkpoint_private.is_blocked(target) and exists (
        select 1 from public.cp_friendships f where
        f.user_low = least((select auth.uid()), target) and
        f.user_high = greatest((select auth.uid()), target));
$$;
revoke all on function checkpoint_private.is_blocked(uuid), checkpoint_private.is_friend(uuid) from public, anon;
grant execute on function checkpoint_private.is_blocked(uuid), checkpoint_private.is_friend(uuid) to authenticated;

alter table public.cp_profiles enable row level security;
alter table public.cp_friend_requests enable row level security;
alter table public.cp_friendships enable row level security;
alter table public.cp_blocks enable row level security;
alter table public.cp_game_publications enable row level security;
revoke all on public.cp_profiles, public.cp_friend_requests, public.cp_friendships, public.cp_blocks, public.cp_game_publications from anon, authenticated;
grant select on public.cp_profiles, public.cp_friend_requests, public.cp_friendships, public.cp_blocks, public.cp_game_publications to authenticated;
grant update(display_name, avatar_path) on public.cp_profiles to authenticated;
grant insert(sender_id, recipient_id), update(status), delete on public.cp_friend_requests to authenticated;
grant delete on public.cp_friendships to authenticated;
grant insert(blocker_id, blocked_id), delete on public.cp_blocks to authenticated;

create policy cp_profile_read on public.cp_profiles for select to authenticated using (
    user_id = (select auth.uid()) or (not checkpoint_private.is_blocked(user_id) and
        (checkpoint_private.is_friend(user_id) or exists (select 1 from public.cp_friend_requests r
            where r.status = 'pending' and ((r.sender_id = (select auth.uid()) and r.recipient_id = user_id) or
            (r.recipient_id = (select auth.uid()) and r.sender_id = user_id))))));
create policy cp_profile_update on public.cp_profiles for update to authenticated
    using (user_id = (select auth.uid())) with check (user_id = (select auth.uid()));
create policy cp_request_read on public.cp_friend_requests for select to authenticated
    using ((select auth.uid()) in (sender_id, recipient_id));
create policy cp_request_insert on public.cp_friend_requests for insert to authenticated
    with check (sender_id = (select auth.uid()) and status = 'pending');
create policy cp_request_answer on public.cp_friend_requests for update to authenticated
    using (recipient_id = (select auth.uid()) and status = 'pending')
    with check (recipient_id = (select auth.uid()) and status in ('accepted', 'rejected'));
create policy cp_request_delete on public.cp_friend_requests for delete to authenticated
    using ((select auth.uid()) in (sender_id, recipient_id));
create policy cp_friendship_read on public.cp_friendships for select to authenticated
    using ((select auth.uid()) in (user_low, user_high) and not checkpoint_private.is_blocked(
        case when user_low = (select auth.uid()) then user_high else user_low end));
create policy cp_friendship_delete on public.cp_friendships for delete to authenticated
    using ((select auth.uid()) in (user_low, user_high));
create policy cp_block_read on public.cp_blocks for select to authenticated using (blocker_id = (select auth.uid()));
create policy cp_block_insert on public.cp_blocks for insert to authenticated with check (blocker_id = (select auth.uid()));
create policy cp_block_delete on public.cp_blocks for delete to authenticated using (blocker_id = (select auth.uid()));
create policy cp_publication_read on public.cp_game_publications for select to authenticated
    using (owner_id = (select auth.uid()) or (is_shared and checkpoint_private.is_friend(owner_id)));

create function checkpoint_private.create_profile() returns trigger
language plpgsql security definer set search_path = '' as $$
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
create trigger cp_new_user after insert on auth.users for each row execute function checkpoint_private.create_profile();

create function checkpoint_private.guard_request() returns trigger
language plpgsql security definer set search_path = '' as $$
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
create trigger cp_request_guard before insert or update on public.cp_friend_requests for each row execute function checkpoint_private.guard_request();

create function checkpoint_private.apply_block() returns trigger
language plpgsql security definer set search_path = '' as $$
begin
    perform pg_advisory_xact_lock(hashtextextended(
        least(new.blocker_id, new.blocked_id)::text || ':' ||
        greatest(new.blocker_id, new.blocked_id)::text, 1));
    delete from public.cp_friendships where user_low = least(new.blocker_id, new.blocked_id) and user_high = greatest(new.blocker_id, new.blocked_id);
    delete from public.cp_friend_requests where (sender_id = new.blocker_id and recipient_id = new.blocked_id) or (sender_id = new.blocked_id and recipient_id = new.blocker_id);
    return new;
end;
$$;
create trigger cp_apply_block after insert on public.cp_blocks for each row execute function checkpoint_private.apply_block();

create function public.cp_find_friend(p_code text) returns table(user_id uuid, display_name text, friend_code text)
language sql stable security definer set search_path = '' as $$
    select p.user_id, p.display_name, p.friend_code from public.cp_profiles p
    where auth.uid() is not null and length(p_code) = 15 and p.friend_code = lower(btrim(p_code))
    and p.user_id <> auth.uid() and not checkpoint_private.is_blocked(p.user_id);
$$;

create function public.cp_publish_game(p_game_id uuid, p_expected_revision bigint, p_operation_id uuid, p_game jsonb)
returns bigint language plpgsql security definer set search_path = '' as $$
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
revoke all on all functions in schema checkpoint_private from public, anon, authenticated;
grant execute on function checkpoint_private.is_blocked(uuid), checkpoint_private.is_friend(uuid) to authenticated;
revoke all on function public.cp_find_friend(text), public.cp_publish_game(uuid,bigint,uuid,jsonb) from public, anon;
grant execute on function public.cp_find_friend(text), public.cp_publish_game(uuid,bigint,uuid,jsonb) to authenticated;

-- Private images: own uploads; friends can read only referenced shared covers
-- or avatars from profiles that their current permissions allow them to see.
insert into storage.buckets(id, name, public, file_size_limit, allowed_mime_types)
    values ('checkpoint-assets', 'checkpoint-assets', false, 2097152, array['image/png','image/jpeg','image/webp']);
create policy cp_asset_read on storage.objects for select to authenticated using (
    bucket_id = 'checkpoint-assets' and ((storage.foldername(name))[1] = (select auth.uid())::text
        or exists (select 1 from public.cp_game_publications g where g.is_shared and g.cover_path = name)
        or exists (select 1 from public.cp_profiles p where p.avatar_path = name)));
create policy cp_asset_insert on storage.objects for insert to authenticated with check (
    bucket_id = 'checkpoint-assets' and name ~ ('^' || (select auth.uid())::text || '/(avatars|covers)/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$'));
create policy cp_asset_update on storage.objects for update to authenticated
    using (bucket_id = 'checkpoint-assets' and (storage.foldername(name))[1] = (select auth.uid())::text)
    with check (bucket_id = 'checkpoint-assets' and name ~ ('^' || (select auth.uid())::text || '/(avatars|covers)/[a-zA-Z0-9_-]+\.(png|jpg|jpeg|webp)$'));
create policy cp_asset_delete on storage.objects for delete to authenticated
    using (bucket_id = 'checkpoint-assets' and (storage.foldername(name))[1] = (select auth.uid())::text);

commit;
