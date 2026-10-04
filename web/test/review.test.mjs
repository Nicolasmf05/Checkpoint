import test from 'node:test';
import assert from 'node:assert/strict';
import {reviewItems,runAchievementReview} from '../review.mjs';
test('review combines providers without exposing removed goals or losing local overrides',()=>{
 const game={achievements:[{id:'same',unlocked:false,hidden:true}],retroAchievements:[{id:'same',unlocked:true}],manualAchievements:[{id:'local',unlocked:false}],removedAchievements:['retro:same'],achievementOverrides:{'steam:same':true}};
 const items=reviewItems(game);
 assert.equal(items.length,2);assert.equal(items[0].unlocked,true);assert.equal(items[0].sourceUnlocked,false);assert.equal(items[0].provider,'steam');assert.equal(items[1].provider,'manual');
 assert.equal(game.achievements[0].unlocked,false);assert.deepEqual(reviewItems({}),[]);
});

test('review queue overlaps three requests and waits for all of them',async()=>{
 const controller=new AbortController();let active=0,peak=0,done=0,release;
 const gate=new Promise(resolve=>release=resolve);
 const running=runAchievementReview(Array.from({length:9},(_,i)=>i),async()=>{active++;peak=Math.max(peak,active);await gate;active--;done++;},controller.signal,{spacing:0});
 await new Promise(resolve=>setTimeout(resolve,15));assert.equal(peak,3);assert.equal(done,0);
 release();await running;assert.equal(done,9);assert.equal(active,0);
});
test('review cancellation drains active requests without starting queued games',async()=>{
 const controller=new AbortController();let started=0,finished=0;
 const running=runAchievementReview(Array.from({length:20},(_,i)=>i),async(_,signal)=>{
  started++;await new Promise((resolve,reject)=>signal.addEventListener('abort',()=>{finished++;reject(signal.reason);},{once:true}));
 },controller.signal,{spacing:0});
 await new Promise(resolve=>setTimeout(resolve,15));controller.abort();await assert.rejects(running);assert.equal(started,3);assert.equal(finished,3);
});
test('review request starts are spaced even when responses are immediate',async()=>{
 const controller=new AbortController(),starts=[];
 await runAchievementReview([1,2,3,4],async()=>starts.push(performance.now()),controller.signal,{spacing:20});
 assert.equal(starts.length,4);assert.ok(starts.at(-1)-starts[0]>=50);
});
