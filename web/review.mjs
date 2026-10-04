// The explicit settings review includes imported, untracked and private games.
export function reviewItems(game){
 const removed=new Set(game.removedAchievements||[]),overrides=game.achievementOverrides||{};
 return [['steam',game.achievements],['retro',game.retroAchievements],['manual',game.manualAchievements]].flatMap(([provider,items])=>(items||[]).map(a=>{
  const key=provider+':'+a.id;return {...a,key,provider,sourceUnlocked:a.unlocked===true,unlocked:Object.hasOwn(overrides,key)?overrides[key]:a.unlocked===true};
 })).filter(a=>!removed.has(a.key));
}
