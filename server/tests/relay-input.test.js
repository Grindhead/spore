'use strict';
const {test,after}=require('node:test'),assert=require('node:assert/strict'),{once}=require('node:events'),{WebSocket}=require('ws');
const relay=require('../relay-server');
const aliceToken='a'.repeat(40),bobToken='b'.repeat(40);
relay.CFG.PROGRESS_PLAYER_TOKENS=JSON.stringify({alice:aliceToken,bob:bobToken});
const request=async (path,token=aliceToken,options={})=>fetch(`http://127.0.0.1:${relay.server.address().port}${path}`,{...options,headers:{authorization:'Bearer '+token,...options.headers}});
test('invalid JSON values and percent encoding cannot crash the real relay',async()=>{relay.server.listen(0,'127.0.0.1');await once(relay.server,'listening');const socket=new WebSocket(`ws://127.0.0.1:${relay.server.address().port}`);await once(socket,'open');for(const message of ['null','[]','true','"string"','{'])socket.send(message);socket.send(JSON.stringify({type:'list_public_lobbies'}));const [response]=await once(socket,'message');assert.equal(JSON.parse(response).type,'public_lobbies');const malformed=await request('/api/progress/autosave/%E0%A4%A');assert.equal(malformed.status,400);socket.close();await once(socket,'close')});
test('two same-IP players each retain their own gameplay message budget',async()=>{const sockets=[new WebSocket(`ws://127.0.0.1:${relay.server.address().port}`),new WebSocket(`ws://127.0.0.1:${relay.server.address().port}`)];await Promise.all(sockets.map(s=>once(s,'open')));for(let i=0;i<70;i++)for(const socket of sockets)socket.send(JSON.stringify({type:'pong'}));for(const socket of sockets){socket.send(JSON.stringify({type:'list_public_lobbies'}));const [reply]=await once(socket,'message');assert.equal(JSON.parse(reply).type,'public_lobbies');assert.equal(socket.readyState,WebSocket.OPEN)}const close=once(sockets[0],'close');for(let i=0;i<70;i++)sockets[0].send(JSON.stringify({type:'pong'}));const [code]=await close;assert.equal(code,1008);assert.equal(sockets[1].readyState,WebSocket.OPEN);sockets[1].close();await once(sockets[1],'close')});
test('authenticated server identity isolates slots, rejects shared/spoofed credentials and deletes one slot',async()=>{
 const {mkdtempSync,rmSync}=require('node:fs');const {tmpdir}=require('node:os');const {join}=require('node:path');
 const folder=mkdtempSync(join(tmpdir(),'spore-progress-'));relay.CFG.AUTOSAVE_DIR=folder;relay.CFG.GAME_TOKEN='shared-game-token';
 try {
  assert.equal((await request('/api/progress/autosave',relay.CFG.GAME_TOKEN,{method:'POST',body:JSON.stringify({slot:0})})).status,401);
  for(const slot of [0,1])assert.equal((await request('/api/progress/autosave/slots/'+slot,aliceToken,{method:'POST',headers:{'x-player-id':'bob'},body:JSON.stringify({slot,value:'alice-'+slot})})).status,200);
  assert.equal((await request('/api/progress/autosave/alice',bobToken)).status,403);
  assert.equal((await request('/api/progress/autosave/slots/0',bobToken)).status,404);
  assert.deepEqual((await (await request('/api/progress/autosave/slots')).json()).slots,[0,1]);
  assert.equal((await (await request('/api/progress/autosave/slots/1')).json()).data.value,'alice-1');
  assert.equal((await request('/api/progress/autosave/slots/0',aliceToken,{method:'DELETE'})).status,200);
  assert.equal((await request('/api/progress/autosave/slots/0')).status,404);
  assert.equal((await request('/api/progress/autosave/slots/1')).status,200);
  assert.equal((await request('/api/progress/autosave/slots/10')).status,400);
  assert.equal((await request('/api/progress/autosave/slots/1',aliceToken,{method:'POST',body:JSON.stringify({slot:0})})).status,400);
 } finally {rmSync(folder,{recursive:true,force:true});}
});
after(async()=>{for(const socket of relay.wss.clients)socket.terminate();await new Promise(resolve=>relay.wss.close(()=>relay.server.close(resolve)))});
