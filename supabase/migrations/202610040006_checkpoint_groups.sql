-- Collaborative Checkpoint achievement groups. Progress is a shared checklist, never a provider unlock.
BEGIN;

CREATE TABLE public.cp_groups (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name text NOT NULL CHECK (length(name) BETWEEN 1 AND 50),
  owner_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  game_title text NOT NULL CHECK (length(game_title) BETWEEN 1 AND 140),
  steam_app_id integer CHECK (
    steam_app_id IS NULL
    OR steam_app_id > 0
  ),
  goals jsonb NOT NULL DEFAULT '[]'::jsonb CHECK (
    jsonb_typeof(goals) = 'array'
    AND jsonb_array_length(goals) <= 200
  ),
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE public.cp_group_members (
  group_id uuid NOT NULL REFERENCES public.cp_groups (id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  joined_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (group_id, user_id)
);

CREATE TABLE public.cp_group_invites (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  group_id uuid NOT NULL REFERENCES public.cp_groups (id) ON DELETE CASCADE,
  inviter_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  invitee_id uuid NOT NULL REFERENCES public.cp_profiles (user_id) ON DELETE CASCADE,
  status text NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'accepted', 'rejected')),
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (group_id, invitee_id)
);

CREATE INDEX cp_group_members_user ON public.cp_group_members (user_id);

CREATE INDEX cp_group_invites_recipient ON public.cp_group_invites (invitee_id, status);

ALTER TABLE public.cp_groups ENABLE ROW LEVEL SECURITY;

ALTER TABLE public.cp_group_members ENABLE ROW LEVEL SECURITY;

ALTER TABLE public.cp_group_invites ENABLE ROW LEVEL SECURITY;

CREATE FUNCTION checkpoint_private.is_group_member (target uuid) RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER
SET
  search_path = '' AS $$ SELECT EXISTS(SELECT 1 FROM public.cp_group_members WHERE group_id=target AND user_id=auth.uid()); $$;

REVOKE ALL ON FUNCTION checkpoint_private.is_group_member (uuid)
FROM
  PUBLIC,
  anon;

GRANT
EXECUTE ON FUNCTION checkpoint_private.is_group_member (uuid) TO authenticated;

REVOKE ALL ON public.cp_groups,
public.cp_group_members,
public.cp_group_invites
FROM
  anon,
  authenticated;

GRANT
SELECT
  ON public.cp_groups,
  public.cp_group_members,
  public.cp_group_invites TO authenticated;

CREATE POLICY cp_group_read ON public.cp_groups FOR
SELECT
  TO authenticated USING (checkpoint_private.is_group_member (id));

CREATE POLICY cp_group_members_read ON public.cp_group_members FOR
SELECT
  TO authenticated USING (checkpoint_private.is_group_member (group_id));

CREATE POLICY cp_group_invites_read ON public.cp_group_invites FOR
SELECT
  TO authenticated USING (
    invitee_id = (
      SELECT
        auth.uid ()
    )
    OR checkpoint_private.is_group_member (group_id)
  );

CREATE FUNCTION public.cp_create_group (
  p_name text,
  p_game_title text,
  p_steam_app_id integer,
  p_goals jsonb
) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER
SET
  search_path = '' AS $$
DECLARE actor uuid:=auth.uid(); gid uuid;
BEGIN
 IF actor IS NULL THEN RAISE EXCEPTION 'Inicia sesión' USING errcode='42501'; END IF;
 IF p_name IS NULL OR length(btrim(p_name)) NOT BETWEEN 1 AND 50 OR p_game_title IS NULL OR length(btrim(p_game_title)) NOT BETWEEN 1 AND 140 OR p_steam_app_id IS NOT NULL AND p_steam_app_id<=0 OR p_goals IS NULL OR jsonb_typeof(p_goals)<>'array' OR jsonb_array_length(p_goals)>200 OR octet_length(p_goals::text)>400000 THEN RAISE EXCEPTION 'Datos del grupo no válidos' USING errcode='22023'; END IF;
 IF EXISTS(SELECT 1 FROM jsonb_array_elements(p_goals) g WHERE jsonb_typeof(g)<>'object' OR NOT(g ?& array['id','name','description','completed']) OR (g-'id'-'name'-'description'-'completed')<>'{}'::jsonb OR jsonb_typeof(g->'id')<>'string' OR length(g->>'id') NOT BETWEEN 1 AND 100 OR jsonb_typeof(g->'name')<>'string' OR length(g->>'name') NOT BETWEEN 1 AND 250 OR jsonb_typeof(g->'description')<>'string' OR length(g->>'description')>2000 OR jsonb_typeof(g->'completed')<>'boolean') THEN RAISE EXCEPTION 'Logros del grupo no válidos' USING errcode='22023'; END IF;
 INSERT INTO public.cp_groups(name,owner_id,game_title,steam_app_id,goals) VALUES(btrim(p_name),actor,btrim(p_game_title),p_steam_app_id,p_goals) RETURNING id INTO gid;
 INSERT INTO public.cp_group_members(group_id,user_id) VALUES(gid,actor);
 RETURN gid;
END; $$;

CREATE FUNCTION public.cp_invite_group_friend (p_group_id uuid, p_friend_id uuid) RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER
SET
  search_path = '' AS $$
DECLARE actor uuid:=auth.uid(); invitation uuid;
BEGIN
 IF actor IS NULL THEN RAISE EXCEPTION 'Inicia sesión' USING errcode='42501'; END IF;
 IF NOT EXISTS(SELECT 1 FROM public.cp_group_members WHERE group_id=p_group_id AND user_id=actor) THEN RAISE EXCEPTION 'Solo integrantes del grupo pueden invitar' USING errcode='42501'; END IF;
 IF NOT checkpoint_private.is_friend(p_friend_id) OR p_friend_id=actor THEN RAISE EXCEPTION 'Solo puedes invitar a tus amigos de Checkpoint' USING errcode='42501'; END IF;
 INSERT INTO public.cp_group_invites(group_id,inviter_id,invitee_id) VALUES(p_group_id,actor,p_friend_id) ON CONFLICT(group_id,invitee_id) DO UPDATE SET inviter_id=EXCLUDED.inviter_id,status='pending',created_at=now() WHERE public.cp_group_invites.status<>'accepted' RETURNING id INTO invitation;
 IF invitation IS NULL THEN RAISE EXCEPTION 'La persona ya pertenece a este grupo' USING errcode='22023'; END IF;
 RETURN invitation;
END; $$;

CREATE FUNCTION public.cp_answer_group_invite (p_invite_id uuid, p_accept boolean) RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER
SET
  search_path = '' AS $$
DECLARE actor uuid:=auth.uid(); invitation public.cp_group_invites;
BEGIN
 SELECT * INTO invitation FROM public.cp_group_invites WHERE id=p_invite_id AND invitee_id=actor AND status='pending' FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'La invitación ya no está pendiente' USING errcode='22023'; END IF;
 IF p_accept AND NOT checkpoint_private.is_friend(invitation.inviter_id) THEN RAISE EXCEPTION 'La invitación ya no es válida para esta amistad' USING errcode='42501'; END IF;
 UPDATE public.cp_group_invites SET status=CASE WHEN p_accept THEN 'accepted' ELSE 'rejected' END WHERE id=p_invite_id;
 IF p_accept THEN INSERT INTO public.cp_group_members(group_id,user_id) VALUES(invitation.group_id,actor) ON CONFLICT DO NOTHING; END IF;
 RETURN true;
END; $$;

CREATE FUNCTION public.cp_update_group_goal (
  p_group_id uuid,
  p_goal_id text,
  p_completed boolean
) RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER
SET
  search_path = '' AS $$
DECLARE updated_goals jsonb;
BEGIN
 IF auth.uid() IS NULL OR p_goal_id IS NULL OR length(p_goal_id)>100 OR p_completed IS NULL THEN RAISE EXCEPTION 'Solicitud no válida' USING errcode='22023'; END IF;
 PERFORM 1 FROM public.cp_groups g WHERE g.id=p_group_id AND EXISTS(SELECT 1 FROM public.cp_group_members m WHERE m.group_id=g.id AND m.user_id=auth.uid()) FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'No perteneces a este grupo' USING errcode='42501'; END IF;
 SELECT jsonb_agg(CASE WHEN goal->>'id'=p_goal_id THEN goal||jsonb_build_object('completed',p_completed) ELSE goal END ORDER BY ordinality) INTO updated_goals FROM jsonb_array_elements((SELECT goals FROM public.cp_groups WHERE id=p_group_id)) WITH ORDINALITY AS entry(goal,ordinality);
 IF NOT EXISTS(SELECT 1 FROM jsonb_array_elements((SELECT goals FROM public.cp_groups WHERE id=p_group_id)) goal WHERE goal->>'id'=p_goal_id) THEN RAISE EXCEPTION 'No se encontró ese logro' USING errcode='22023'; END IF;
 UPDATE public.cp_groups SET goals=updated_goals WHERE id=p_group_id;
 RETURN true;
END; $$;

CREATE FUNCTION public.cp_add_group_goal (
  p_group_id uuid,
  p_goal_id text,
  p_name text,
  p_description text
) RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER
SET
  search_path = '' AS $$
DECLARE current_goals jsonb;
BEGIN
 IF auth.uid() IS NULL OR p_goal_id IS NULL OR length(p_goal_id) NOT BETWEEN 1 AND 100 OR p_name IS NULL OR length(btrim(p_name)) NOT BETWEEN 1 AND 250 OR p_description IS NULL OR length(p_description)>2000 THEN RAISE EXCEPTION 'Logro no válido' USING errcode='22023'; END IF;
 SELECT g.goals INTO current_goals FROM public.cp_groups g WHERE g.id=p_group_id AND EXISTS(SELECT 1 FROM public.cp_group_members m WHERE m.group_id=g.id AND m.user_id=auth.uid()) FOR UPDATE;
 IF current_goals IS NULL THEN RAISE EXCEPTION 'No perteneces a este grupo' USING errcode='42501'; END IF;
 IF jsonb_array_length(current_goals)>=200 OR EXISTS(SELECT 1 FROM jsonb_array_elements(current_goals) goal WHERE goal->>'id'=p_goal_id) THEN RAISE EXCEPTION 'Límite alcanzado o logro repetido' USING errcode='22023'; END IF;
 UPDATE public.cp_groups SET goals=current_goals||jsonb_build_array(jsonb_build_object('id',p_goal_id,'name',btrim(p_name),'description',p_description,'completed',false)) WHERE id=p_group_id;
 RETURN true;
END; $$;

CREATE FUNCTION public.cp_remove_group_goal (p_group_id uuid, p_goal_id text) RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER
SET
  search_path = '' AS $$
DECLARE current_goals jsonb;
BEGIN
 SELECT g.goals INTO current_goals FROM public.cp_groups g WHERE g.id=p_group_id AND g.owner_id=auth.uid() FOR UPDATE;
 IF current_goals IS NULL THEN RAISE EXCEPTION 'Solo quien creó el grupo puede quitar objetivos' USING errcode='42501'; END IF;
 UPDATE public.cp_groups SET goals=(SELECT coalesce(jsonb_agg(goal),'[]'::jsonb) FROM jsonb_array_elements(current_goals) goal WHERE goal->>'id'<>p_goal_id) WHERE id=p_group_id;
 RETURN true;
END; $$;

REVOKE ALL ON FUNCTION public.cp_create_group (text, text, integer, jsonb),
public.cp_invite_group_friend (uuid, uuid),
public.cp_answer_group_invite (uuid, boolean),
public.cp_update_group_goal (uuid, text, boolean),
public.cp_add_group_goal (uuid, text, text, text),
public.cp_remove_group_goal (uuid, text)
FROM
  PUBLIC,
  anon;

GRANT
EXECUTE ON FUNCTION public.cp_create_group (text, text, integer, jsonb),
public.cp_invite_group_friend (uuid, uuid),
public.cp_answer_group_invite (uuid, boolean),
public.cp_update_group_goal (uuid, text, boolean),
public.cp_add_group_goal (uuid, text, text, text),
public.cp_remove_group_goal (uuid, text) TO authenticated;

COMMIT;
