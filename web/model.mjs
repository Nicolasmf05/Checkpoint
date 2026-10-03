export const statuses=['Pending','Playing','Paused','Finished','Abandoned'];
export const goals=['Story','Achievements','Custom'];
export const themes=['dark','light','midnight','ocean','forest','plum','amber','contrast'];
const text=(v,n)=>typeof v==='string'?v.slice(0,n):'';
const integer=(v,max)=>Number.isInteger(Number(v))&&Number(v)>=0&&Number(v)<=max?Number(v):0;
const enumeration=(value,values)=>typeof value==='number'&&values[value]?value:Math.max(0,values.findIndex(x=>x.toLowerCase()===String(value).toLowerCase()));
export const uuid=v=>typeof v==='string'&&/^[a-f\d]{8}(-[a-f\d]{4}){3}-[a-f\d]{12}$/i.test(v);
export function normalize(raw) {
  if(!raw||typeof raw!=='object'||!text(raw.title,140).trim())throw new Error('invalid-game');
  if(raw.friendsPrivate!=null&&typeof raw.friendsPrivate!=='boolean')throw new Error('invalid-game');
  const steam=Number(raw.steamAppId);
  return {id:uuid(raw.id)?raw.id:crypto.randomUUID(),title:text(raw.title,140).trim(),platform:text(raw.platform,40)||'PC',
    steamAppId:Number.isInteger(steam)&&steam>0&&steam<=2147483647?steam:null,
    status:enumeration(raw.status,statuses),goal:enumeration(raw.goal,goals),customGoal:text(raw.customGoal,250),
    storyPercent:raw.storyPercent==null||raw.storyPercent===''?null:Math.min(100,integer(raw.storyPercent,100)),
    friendsPrivate:typeof raw.friendsPrivate==='boolean'?raw.friendsPrivate:null,lists:normalizeLists(raw.lists),notes:text(raw.notes,20000),tracked:raw.tracked!==false,favorite:raw.favorite===true,sortOrder:integer(raw.sortOrder,100000),
    playtimeMinutes:integer(raw.playtimeMinutes,2147483647),addedAt:raw.addedAt||new Date().toISOString(),
    finishedAt:raw.status===3||String(raw.status).toLowerCase()==='finished'?raw.finishedAt||new Date().toISOString():null,
    syncedAt:typeof raw.syncedAt==='string'?raw.syncedAt:null,
    customCover:typeof raw.customCover==='string'&&/^data:image\/(png|jpeg|webp);base64,[a-z\d+/=]+$/i.test(raw.customCover)&&raw.customCover.length<400000?raw.customCover:null,
    tasks:(Array.isArray(raw.tasks)?raw.tasks:[]).slice(0,200).filter(t=>text(t?.title,250).trim()).map(t=>({id:uuid(t.id)?t.id:crypto.randomUUID(),title:text(t.title,250).trim(),done:t.done===true})),
    achievements:Array.isArray(raw.achievements)?raw.achievements.slice(0,10000).map(a=>({id:text(a?.id,250),name:text(a?.name,250),description:text(a?.description,2000),hidden:a?.hidden===true,unlocked:a?.unlocked===true,unlockedAt:typeof a?.unlockedAt==='string'?a.unlockedAt:null})):null};
}
export function mergeLibrary(games,incoming) {
  const map=new Map(games.filter(g=>g.steamAppId).map(g=>[g.steamAppId,g]));let added=0;
  for(const remote of incoming.slice(0,10000)) {
    if(!Number.isInteger(remote.appId)||remote.appId<=0||remote.appId>2147483647||typeof remote.name!=='string'||!remote.name.trim())continue;
    const minutes=integer(remote.playtimeMinutes,2147483647),old=map.get(remote.appId);
    if(old){old.playtimeMinutes=minutes;continue;}
    const game=normalize({title:remote.name,steamAppId:remote.appId,playtimeMinutes:minutes,platform:'Steam',tracked:false,sortOrder:games.length});
    games.push(game);map.set(remote.appId,game);added++;
  } return added;
}
export function importBackup(games,backup) {
  const source=Array.isArray(backup)?backup:backup?.games;if(!Array.isArray(source)||source.length>10000)throw new Error('invalid-backup');
  const ids=new Set(games.map(g=>g.id)),apps=new Set(games.filter(g=>g.steamAppId).map(g=>g.steamAppId));let added=0;
  // Validate first so an invalid backup never produces a partial import.
  const prepared=source.map(normalize);
  for(const game of prepared){if(ids.has(game.id)||game.steamAppId&&apps.has(game.steamAppId))continue;
    const cover=backup?.covers?.[game.id];if(typeof cover==='string'&&cover.length<400000&&/^[a-z\d+/=]+$/i.test(cover))game.customCover='data:image/png;base64,'+cover;
    game.sortOrder=games.length;games.push(game);ids.add(game.id);if(game.steamAppId)apps.add(game.steamAppId);added++;}return added;
}
export function payload(game) {
  return {title:game.title,platform:game.platform,status:statuses[game.status].toLowerCase(),goalKind:goals[game.goal].toLowerCase(),goalText:game.goal===2?game.customGoal:null,
    storyPercent:game.storyPercent,tasksDone:game.tasks.length?game.tasks.filter(t=>t.done).length:null,tasksTotal:game.tasks.length||null,
    achievementsUnlocked:game.achievements?.length?game.achievements.filter(a=>a.unlocked).length:null,achievementsTotal:game.achievements?.length||null,
    steamAppId:game.steamAppId,coverPath:null,finishedAt:game.finishedAt};
}
export function friendCode(code,service=false) {
  const suffix=String(code).trim().toLowerCase().replace(/^(checkpoint-|cp-)/,'');
  if(!/^[a-f\d]{12}$/.test(suffix)||!/^\s*(checkpoint-|cp-)/i.test(code))throw new Error('invalid-code');return (service?'cp-':'checkpoint-')+suffix;
}
export function account(username){const name=String(username).trim().toLowerCase();if(!/^[a-z\d_]{3,24}$/.test(name))throw new Error('invalid-user');return name+'@accounts.checkpoint.invalid';}


export function normalizeLists(names){const map=new Map();for(const n of Array.isArray(names)?names:[]){if(typeof n!=='string'||!n.trim()||n.trim().length>40||/[\x00-\x1f\x7f]/.test(n))continue;const key=n.trim().toLocaleLowerCase();if(!map.has(key))map.set(key,n.trim());}return [...map.values()].slice(0,30);}
export function listName(name,existing,previous){name=String(name||'').trim();if(!name||name.length>40||/[\x00-\x1f\x7f]/.test(name))throw new Error('invalid-list');if(['mi lista','my list','privados','private games','biblioteca','library'].includes(name.toLocaleLowerCase())||existing.some(n=>n.toLocaleLowerCase()===name.toLocaleLowerCase()&&n.toLocaleLowerCase()!==previous?.toLocaleLowerCase()))throw new Error('duplicate-list');if(!previous&&existing.length>=30)throw new Error('too-many-lists');return name;}
export const inList=(g,selection)=>selection==='private'?g.friendsPrivate===true:g.tracked&&g.friendsPrivate!==true&&(selection==='all'||selection.startsWith('custom:')&&g.lists.some(n=>n.toLocaleLowerCase()===selection.slice(7).toLocaleLowerCase()));
export const shouldShare=g=>g.tracked&&g.friendsPrivate!==true;
