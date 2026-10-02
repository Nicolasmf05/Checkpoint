-- Steam state is private and accessed exclusively by the Edge Function.
begin;
create schema if not exists checkpoint_steam;
revoke all on schema checkpoint_steam from public, anon, authenticated;
create table if not exists checkpoint_steam.entries (
  kind text not null check (kind in ('flow','session','nonce','cache','limit')),
  id text not null,
  value jsonb not null,
  expires timestamptz not null,
  primary key(kind,id)
);
alter table checkpoint_steam.entries enable row level security;
revoke all on checkpoint_steam.entries from public, anon, authenticated;
create index if not exists cp_steam_expiry on checkpoint_steam.entries(expires);

create or replace function public.cp_steam_state(p_action text, p_kind text, p_id text,
  p_value jsonb default '{}'::jsonb, p_ttl integer default 600)
returns jsonb language plpgsql security definer set search_path = '' as $$
declare v jsonb; n integer; expiry timestamptz; inserted integer;
begin
  if p_kind not in ('flow','session','nonce','cache','limit') or length(p_id) > 200
     or length(p_id) = 0 or p_ttl < 1 or p_ttl > 604800 then
    raise exception 'Invalid Steam state request';
  end if;
  -- Serialize same-key mutations across all function instances.
  perform pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('cp-steam:'||p_kind||':'||p_id,0));
  delete from checkpoint_steam.entries where (kind,id) in
    (select kind,id from checkpoint_steam.entries where expires <= now() order by expires,kind,id limit 200 for update skip locked);
  select value,expires into v,expiry from checkpoint_steam.entries
    where kind=p_kind and id=p_id and expires>now() for update;
  if p_action='get' then return v;
  elsif p_action='put' then
    insert into checkpoint_steam.entries values(p_kind,p_id,p_value,now()+pg_catalog.make_interval(secs=>p_ttl))
      on conflict(kind,id) do update set value=excluded.value,expires=excluded.expires;
    return 'true'::jsonb;
  elsif p_action='delete' then
    delete from checkpoint_steam.entries where kind=p_kind and id=p_id;
    return 'true'::jsonb;
  elsif p_action='limit' and p_kind='limit' then
    n=coalesce((v->>'count')::integer,0)+1;
    insert into checkpoint_steam.entries values(p_kind,p_id,jsonb_build_object('count',n),coalesce(expiry,now()+pg_catalog.make_interval(secs=>p_ttl)))
      on conflict(kind,id) do update set value=excluded.value,expires=excluded.expires;
    return to_jsonb(n <= (p_value->>'max')::integer);
  elsif p_action='claim' and p_kind='flow' then
    if v is null or v ? 'steamId' then return 'false'::jsonb; end if;
    insert into checkpoint_steam.entries values('nonce',p_value->>'nonceHash','{}',now()+interval '10 minutes')
      on conflict do nothing;
    get diagnostics inserted = row_count;
    if inserted=0 then return 'false'::jsonb; end if;
    update checkpoint_steam.entries set value=v||jsonb_build_object('steamId',p_value->>'steamId')
      where kind=p_kind and id=p_id;
    return 'true'::jsonb;
  elsif p_action='poll' and p_kind='flow' then
    if v is null or v->>'pollHash' is distinct from p_value->>'pollHash' then return null; end if;
    if not (v ? 'steamId') then return jsonb_build_object('status','pending'); end if;
    insert into checkpoint_steam.entries values('session',p_value->>'tokenHash',
      jsonb_build_object('steamId',v->>'steamId'),now()+interval '7 days');
    delete from checkpoint_steam.entries where kind=p_kind and id=p_id;
    return jsonb_build_object('status','complete','steamId',v->>'steamId');
  elsif p_action='logout' and p_kind='session' then
    delete from checkpoint_steam.entries where kind=p_kind and id=p_id;
    if v is not null then
      delete from checkpoint_steam.entries where kind='cache' and
        (id='library:'||(v->>'steamId') or id like 'achievements:'||(v->>'steamId')||':%');
    end if;
    return 'true'::jsonb;
  end if;
  raise exception 'Unsupported Steam state action';
end $$;
revoke all on function public.cp_steam_state(text,text,text,jsonb,integer) from public,anon,authenticated;
grant execute on function public.cp_steam_state(text,text,text,jsonb,integer) to service_role;
commit;
