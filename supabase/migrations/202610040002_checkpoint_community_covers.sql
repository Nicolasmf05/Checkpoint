-- Only text metadata, no images, URLs, user identity or public direct writes.
begin;
create table if not exists checkpoint_steam.community_covers (
  id text primary key check(id ~ '^[a-f0-9]{64}$'),
  title text not null check(length(title) between 1 and 140),
  platform text not null check(length(platform) between 1 and 80),
  candidate jsonb not null check(pg_column_size(candidate)<=2048),
  created_at timestamptz not null default now()
);
alter table checkpoint_steam.community_covers enable row level security;
revoke all on checkpoint_steam.community_covers from public,anon,authenticated;
create or replace function public.cp_community_cover(p_action text,p_id text,p_value jsonb default '{}'::jsonb)
returns jsonb language plpgsql security definer set search_path='' as $$
declare result jsonb;
begin
  if p_id !~ '^[a-f0-9]{64}$' then raise exception 'Invalid cover identity'; end if;
  if p_action='get' then
    select candidate into result from checkpoint_steam.community_covers where id=p_id;
    return result;
  elsif p_action='confirm' then
    if coalesce(p_value->'candidate'->>'imageId','') !~ '^[A-Za-z0-9_-]{1,80}$' or
       jsonb_typeof(p_value->'candidate'->'id') is distinct from 'number' or
       length(p_value->'candidate'->>'name') not between 1 and 250 or
       pg_column_size(p_value)>4096 then raise exception 'Invalid cover metadata'; end if;
    -- Serializes the first choice and caps total permanent metadata growth.
    perform pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('cp-community-covers',0));
    select candidate into result from checkpoint_steam.community_covers where id=p_id;
    if result is not null then return result; end if;
    if (select count(*) from checkpoint_steam.community_covers)>=10000 then raise exception 'Cover metadata capacity reached'; end if;
    insert into checkpoint_steam.community_covers(id,title,platform,candidate)
      values(p_id,p_value->>'title',p_value->>'platform',p_value->'candidate');
    return p_value->'candidate';
  end if;
  raise exception 'Unsupported community cover action';
end $$;
revoke all on function public.cp_community_cover(text,text,jsonb) from public,anon,authenticated;
grant execute on function public.cp_community_cover(text,text,jsonb) to service_role;
commit;
