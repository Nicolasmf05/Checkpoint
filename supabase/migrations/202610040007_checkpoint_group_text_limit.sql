-- Bound stored collaborative achievement text and validate owner-only goal removal.
BEGIN;

ALTER TABLE public.cp_groups
ADD CONSTRAINT cp_group_text_size CHECK (octet_length(goals::text) <= 150000);

CREATE OR REPLACE FUNCTION public.cp_create_group (
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
 IF p_name IS NULL OR length(btrim(p_name)) NOT BETWEEN 1 AND 50 OR p_game_title IS NULL OR length(btrim(p_game_title)) NOT BETWEEN 1 AND 140 OR p_steam_app_id IS NOT NULL AND p_steam_app_id<=0 OR p_goals IS NULL OR jsonb_typeof(p_goals)<>'array' OR jsonb_array_length(p_goals)>200 OR octet_length(p_goals::text)>150000 THEN RAISE EXCEPTION 'Datos del grupo no válidos' USING errcode='22023'; END IF;
 IF EXISTS(SELECT 1 FROM jsonb_array_elements(p_goals) g WHERE jsonb_typeof(g)<>'object' OR NOT(g ?& array['id','name','description','completed']) OR (g-'id'-'name'-'description'-'completed')<>'{}'::jsonb OR jsonb_typeof(g->'id')<>'string' OR length(g->>'id') NOT BETWEEN 1 AND 100 OR jsonb_typeof(g->'name')<>'string' OR length(g->>'name') NOT BETWEEN 1 AND 250 OR jsonb_typeof(g->'description')<>'string' OR length(g->>'description')>2000 OR jsonb_typeof(g->'completed')<>'boolean') THEN RAISE EXCEPTION 'Logros del grupo no válidos' USING errcode='22023'; END IF;
 INSERT INTO public.cp_groups(name,owner_id,game_title,steam_app_id,goals) VALUES(btrim(p_name),actor,btrim(p_game_title),p_steam_app_id,p_goals) RETURNING id INTO gid;
 INSERT INTO public.cp_group_members(group_id,user_id) VALUES(gid,actor);
 RETURN gid;
END; $$;

CREATE OR REPLACE FUNCTION public.cp_add_group_goal (
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
 IF octet_length((current_goals||jsonb_build_array(jsonb_build_object('id',p_goal_id,'name',btrim(p_name),'description',p_description,'completed',false)))::text)>150000 THEN RAISE EXCEPTION 'El grupo alcanzó el límite de almacenamiento de texto' USING errcode='22023'; END IF;
 UPDATE public.cp_groups SET goals=current_goals||jsonb_build_array(jsonb_build_object('id',p_goal_id,'name',btrim(p_name),'description',p_description,'completed',false)) WHERE id=p_group_id;
 RETURN true;
END; $$;

CREATE OR REPLACE FUNCTION public.cp_remove_group_goal (p_group_id uuid, p_goal_id text) RETURNS boolean LANGUAGE plpgsql SECURITY DEFINER
SET
  search_path = '' AS $$
DECLARE current_goals jsonb;
BEGIN
 IF p_goal_id IS NULL OR length(p_goal_id) NOT BETWEEN 1 AND 100 THEN RAISE EXCEPTION 'Logro no válido' USING errcode='22023'; END IF;
 SELECT g.goals INTO current_goals FROM public.cp_groups g WHERE g.id=p_group_id AND g.owner_id=auth.uid() FOR UPDATE;
 IF current_goals IS NULL THEN RAISE EXCEPTION 'Solo quien creó el grupo puede quitar objetivos' USING errcode='42501'; END IF;
 UPDATE public.cp_groups SET goals=(SELECT coalesce(jsonb_agg(goal),'[]'::jsonb) FROM jsonb_array_elements(current_goals) goal WHERE goal->>'id'<>p_goal_id) WHERE id=p_group_id;
 RETURN true;
END; $$;

COMMIT;
