-- Catálogo comunitario de referencias IGDB confirmadas.
-- Las confirmaciones conservan la primera propuesta y se ejecutan desde el servicio.
-- Only text metadata, no images, URLs, user identity or public direct writes.
BEGIN;

CREATE TABLE IF NOT EXISTS checkpoint_steam.community_covers (
  id text PRIMARY KEY CHECK (id ~ '^[a-f0-9]{64}$'),
  title text NOT NULL CHECK (length(title) BETWEEN 1 AND 140),
  platform text NOT NULL CHECK (length(platform) BETWEEN 1 AND 80),
  candidate jsonb NOT NULL CHECK (pg_column_size(candidate) <= 2048),
  created_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE checkpoint_steam.community_covers enable ROW level security;

REVOKE ALL ON checkpoint_steam.community_covers
FROM
  public,
  anon,
  authenticated;

CREATE OR REPLACE FUNCTION public.cp_community_cover (
  p_action text,
  p_id text,
  p_value jsonb DEFAULT '{}'::jsonb
) returns jsonb language plpgsql security definer
SET
  search_path = '' AS $$
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

REVOKE ALL ON function public.cp_community_cover (text, text, jsonb)
FROM
  public,
  anon,
  authenticated;

GRANT
EXECUTE ON function public.cp_community_cover (text, text, jsonb) TO service_role;

COMMIT;
