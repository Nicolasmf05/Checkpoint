import test from 'node:test';
import assert from 'node:assert/strict';
import {defaults,canonical,validateShortcuts,effectiveShortcuts,matches,eventGesture} from '../../src/Checkpoint.App/Web/shortcuts.mjs';
test('gestures use exact modifiers and canonical order',()=>{
 assert.equal(canonical('shift+control+n'),'Ctrl+Shift+N');
 assert.ok(matches({key:'N',ctrlKey:true,shiftKey:true},'Ctrl+Shift+N'));
 assert.ok(!matches({key:'n',ctrlKey:true,shiftKey:true},'Ctrl+N'));
 assert.equal(eventGesture({key:'n',ctrlKey:true,metaKey:true}),null);
 assert.equal(eventGesture({key:'n',ctrlKey:true,isComposing:true}),null);
});
test('validation refuses ambiguity and unsafe typing gestures',()=>{
 assert.deepEqual(validateShortcuts(defaults),defaults);
 for(const value of ['N','Shift+N','Ctrl+Ctrl+N','Meta+N','F25','Ctrl+'])assert.throws(()=>canonical(value));
 assert.throws(()=>validateShortcuts({...defaults,add:defaults.search}));
 assert.throws(()=>validateShortcuts({...defaults,add:'Escape'}));
 assert.throws(()=>validateShortcuts({...defaults,gameMenu:'Space'}));
 assert.throws(()=>validateShortcuts({...defaults,global:'F12'}));
 assert.deepEqual(effectiveShortcuts({add:'broken'}),defaults);
 assert.equal(validateShortcuts({...defaults,add:'ctrl+shift+n'}).add,'Ctrl+Shift+N');
});
