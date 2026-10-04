import { readFile } from 'node:fs/promises';

// Uses the distributable publishable key only. No sessions, signup or writes.
const project = JSON.parse(await readFile(new URL('../supabase/project.json', import.meta.url), 'utf8'));
const origin = new URL(project.url);
if (origin.protocol !== 'https:' || origin.hostname !== `${project.projectRef}.supabase.co`
    || origin.pathname !== '/' || origin.search || origin.hash || origin.username || origin.password
    || !project.publishableKey.startsWith('sb_publishable_')) throw new Error('Invalid public project configuration');
const checks = [
  {name:'Anonymous community-cover state RPC',path:'rest/v1/rpc/cp_community_cover',body:{p_action:'get',p_id:'f'.repeat(64),p_value:{}}},
  { name: 'Anonymous Steam state RPC', path: 'rest/v1/rpc/cp_steam_state', body: {
    p_action: 'get', p_kind: 'session', p_id: 'public-permission-check', p_value: {}, p_ttl: 600
  } },
  ...['cp_profiles','cp_friend_requests','cp_friendships','cp_blocks','cp_game_publications']
    .map(table => ({ name: `Anonymous ${table}`, path: `rest/v1/${table}?select=*&limit=1` })),
  { name: 'Anonymous friend lookup', path: 'rest/v1/rpc/cp_find_friend', body: { p_code: 'cp-000000000000' } },
  { name: 'Anonymous publication', path: 'rest/v1/rpc/cp_publish_game', body: {
    p_game_id: '92000000-0000-4000-8000-000000000001', p_expected_revision: 0,
    p_operation_id: '92000000-0000-4000-8000-000000000002', p_game: null
  } }
];
for (const check of checks) {
  const response = await fetch(new URL(check.path, origin), {
    method: check.body ? 'POST' : 'GET', redirect: 'error', signal: AbortSignal.timeout(20000),
    headers: { apikey: project.publishableKey, ...(check.body ? { 'Content-Type': 'application/json' } : {}) },
    body: check.body ? JSON.stringify(check.body) : undefined
  });
  const error = await response.json();
  if (![401,403].includes(response.status) || error.code !== '42501')
    throw new Error(`${check.name}: expected permission denial, received ${response.status}`);
  console.log(`PASS ${check.name} (${response.status})`);
}
console.log(`${checks.length} public API permission checks passed`);
