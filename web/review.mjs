// The explicit settings review includes imported, untracked and private games.
export function reviewItems(game){
 const removed=new Set(game.removedAchievements||[]),overrides=game.achievementOverrides||{};
 return [['steam',game.achievements],['retro',game.retroAchievements],['manual',game.manualAchievements]].flatMap(([provider,items])=>(items||[]).map(a=>{
  const key=provider+':'+a.id;return {...a,key,provider,sourceUnlocked:a.unlocked===true,unlocked:Object.hasOwn(overrides,key)?overrides[key]:a.unlocked===true};
 })).filter(a=>!removed.has(a.key));
}

// Three requests at most, and no more than 80 starts/minute for a full review.
export async function runAchievementReview(ids,process,signal,{spacing=750,concurrency=3}={}){
 if(!Number.isInteger(concurrency)||concurrency<1||concurrency>3||spacing<0)throw new Error('invalid-review-options');
 let cursor=0,nextStart=0,gate=Promise.resolve();
 const pause=ms=>new Promise((resolve,reject)=>{
  signal.throwIfAborted();const abort=()=>{clearTimeout(timer);reject(signal.reason);};
  const timer=setTimeout(()=>{signal.removeEventListener('abort',abort);resolve();},ms);
  signal.addEventListener('abort',abort,{once:true});
 });
 async function worker(){
  while(cursor<ids.length){signal.throwIfAborted();const id=ids[cursor++];
   const turn=gate.then(async()=>{signal.throwIfAborted();const wait=Math.max(0,nextStart-performance.now());if(wait)await pause(wait);nextStart=performance.now()+spacing;});
   gate=turn.catch(()=>{});await turn;signal.throwIfAborted();await process(id,signal);
  }
 }
 const results=await Promise.allSettled(Array.from({length:Math.min(concurrency,ids.length)},worker));
 const error=results.find(r=>r.status==='rejected');if(error)throw error.reason;
}
