import test from 'node:test';
import assert from 'node:assert/strict';
import {reviewItems} from '../review.mjs';
test('review combines providers without exposing removed goals or losing local overrides',()=>{
 const game={achievements:[{id:'same',unlocked:false,hidden:true}],retroAchievements:[{id:'same',unlocked:true}],manualAchievements:[{id:'local',unlocked:false}],removedAchievements:['retro:same'],achievementOverrides:{'steam:same':true}};
 const items=reviewItems(game);
 assert.equal(items.length,2);assert.equal(items[0].unlocked,true);assert.equal(items[0].sourceUnlocked,false);assert.equal(items[0].provider,'steam');assert.equal(items[1].provider,'manual');
 assert.equal(game.achievements[0].unlocked,false);assert.deepEqual(reviewItems({}),[]);
});
