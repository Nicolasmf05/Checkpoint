-- Metadata-only test. No image upload or email; all synthetic data rolls back.
begin;
create temporary table cp_storage_results(test text primary key);
grant insert, select on cp_storage_results to authenticated;
insert into auth.users(id,email,raw_user_meta_data) values
 ('91000000-0000-4000-8000-000000000001','cp-storage-a@example.invalid','{}'),
 ('91000000-0000-4000-8000-000000000002','cp-storage-b@example.invalid','{}'),
 ('91000000-0000-4000-8000-000000000003','cp-storage-c@example.invalid','{}');
insert into storage.objects(bucket_id,name) values
 ('checkpoint-assets','91000000-0000-4000-8000-000000000001/covers/shared.png'),
 ('checkpoint-assets','91000000-0000-4000-8000-000000000001/covers/private.png'),
 ('checkpoint-assets','91000000-0000-4000-8000-000000000001/avatars/avatar.png');
set local role authenticated;
select set_config('request.jwt.claim.sub','91000000-0000-4000-8000-000000000001',true);
do $$ begin
 update public.cp_profiles set avatar_path = '91000000-0000-4000-8000-000000000001/avatars/avatar.png';
 perform public.cp_publish_game('91000000-0000-4000-8000-000000000010',0,'91000000-0000-4000-8000-000000000011',
   '{"title":"Cover test","platform":"PC","status":"playing","goalKind":"story","coverPath":"91000000-0000-4000-8000-000000000001/covers/shared.png"}');
 if (select count(*) from storage.objects where bucket_id = 'checkpoint-assets') <> 3 then
   raise exception 'Owner must see own three assets'; end if;
 insert into public.cp_friend_requests(sender_id,recipient_id) values(auth.uid(),'91000000-0000-4000-8000-000000000002');
 insert into cp_storage_results values ('Owner reads own assets');
end $$;
select set_config('request.jwt.claim.sub','91000000-0000-4000-8000-000000000002',true);
do $$ begin
 if (select count(*) from storage.objects where bucket_id = 'checkpoint-assets') <> 1 then
   raise exception 'Pending participant may see avatar only'; end if;
 update public.cp_friend_requests set status = 'accepted';
 if (select count(*) from storage.objects where bucket_id = 'checkpoint-assets') <> 2 or
    exists(select 1 from storage.objects where name like '%/private.png') then
   raise exception 'Friend must see shared cover/avatar but not private cover'; end if;
 begin
   insert into storage.objects(bucket_id,name) values('checkpoint-assets','91000000-0000-4000-8000-000000000001/covers/forged.png');
   raise exception 'Cannot upload under another owner';
 exception when insufficient_privilege then null; end;
 insert into cp_storage_results values ('Pending avatar only'), ('Shared cover visible'), ('Unreferenced cover hidden'), ('Foreign upload denied');
end $$;
select set_config('request.jwt.claim.sub','91000000-0000-4000-8000-000000000003',true);
do $$ begin
 if exists(select 1 from storage.objects where bucket_id = 'checkpoint-assets') then raise exception 'Third party cannot read images'; end if;
 insert into cp_storage_results values ('Third party images hidden');
end $$;
select set_config('request.jwt.claim.sub','91000000-0000-4000-8000-000000000002',true);
do $$ begin
 insert into public.cp_blocks(blocker_id,blocked_id) values(auth.uid(),'91000000-0000-4000-8000-000000000001');
 if exists(select 1 from storage.objects where bucket_id = 'checkpoint-assets') then raise exception 'Block must revoke image access'; end if;
 delete from public.cp_blocks;
 if exists(select 1 from storage.objects where bucket_id = 'checkpoint-assets') then raise exception 'Unblock does not restore images'; end if;
 insert into cp_storage_results values ('Block revokes images'), ('Unblock does not restore images');
end $$;
select set_config('request.jwt.claim.sub','91000000-0000-4000-8000-000000000001',true);
do $$ declare i integer; begin
 -- The original invitation already consumed one request; cancel nine more.
 for i in 1..9 loop
   insert into public.cp_friend_requests(sender_id,recipient_id) values(auth.uid(),'91000000-0000-4000-8000-000000000002');
   delete from public.cp_friend_requests where status = 'pending';
 end loop;
 begin
   insert into public.cp_friend_requests(sender_id,recipient_id) values(auth.uid(),'91000000-0000-4000-8000-000000000002');
   raise exception 'Cancellation must not bypass request rate limit' using errcode = 'P0004';
 exception when raise_exception then
   if sqlerrm not like 'Demasiadas solicitudes%' then raise; end if;
 end;
 insert into cp_storage_results values ('Cancelled requests still rate limited');
end $$;
reset role;
do $$ begin
 if (select public from storage.buckets where id = 'checkpoint-assets') is distinct from false then raise exception 'Bucket must be private'; end if;
 insert into cp_storage_results values ('Private bucket');
end $$;
select count(*) as passed_checks, array_agg(test order by test) as checks from cp_storage_results;
rollback;
