import {account,friendCode} from './model.mjs';
export class RemoteError extends Error{constructor(code){super(code);this.code=code;}}
export class BrowserApi {
  constructor(config,fetchImpl=(...args)=>fetch(...args)){this.config=config;this.fetch=fetchImpl;this.social=this.restore('checkpoint-social');this.steam=this.restore('checkpoint-steam');}
  restore(key){try{return JSON.parse(sessionStorage.getItem(key));}catch{return null;}}
  save(key,value){try{if(value)sessionStorage.setItem(key,JSON.stringify(value));else sessionStorage.removeItem(key);}catch{}}
  async request(path,body,{steam=false,auth=false,method=body===undefined?'GET':'POST'}={}) {
    if(auth&&!steam)await this.refresh();
    const headers={'content-type':'application/json'};if(!steam)headers.apikey=this.config.publishableKey;
    if(auth)headers.authorization='Bearer '+(steam?this.steam?.token:this.social?.access_token);
    const response=await this.fetch(this.config.url+(steam?'/functions/v1/checkpoint-steam/':'/')+path,{method,headers,body:body===undefined?undefined:JSON.stringify(body),credentials:'omit',referrerPolicy:'no-referrer',signal:AbortSignal.timeout(35000)});
    const text=await response.text();let data;try{data=text?JSON.parse(text):null;}catch{throw new RemoteError('remote');}
    if(!response.ok)throw new RemoteError(data?.code||data?.error_code||(response.status===401?'unauthorized':response.status===403?'forbidden':response.status===429?'rate':'remote'));
    return data;
  }
  accept(data){if(!data?.access_token||!data?.refresh_token||!data?.user?.id||!Number.isFinite(data.expires_in)||data.expires_in<=0)throw new RemoteError('auth');this.social={...data,expires:Date.now()+data.expires_in*1000};this.save('checkpoint-social',this.social);}
  async refresh(){if(!this.social)throw new RemoteError('unauthorized');if(this.social.expires>Date.now()+60000)return;if(!this.refreshing)this.refreshing=this.request('auth/v1/token?grant_type=refresh_token',{refresh_token:this.social.refresh_token}).then(data=>{if(data?.user?.id!==this.social?.user?.id)throw new RemoteError('unauthorized');this.accept(data);}).finally(()=>this.refreshing=null);await this.refreshing;}
  async login(username,password,name){const email=account(username);if(name!==undefined){if(password.length<8||!name.trim()||name.length>50)throw new RemoteError('weak_password');this.accept(await this.request('auth/v1/signup',{email,password,data:{display_name:name.trim()}}));}else this.accept(await this.request('auth/v1/token?grant_type=password',{email,password}));}
  async logout(){try{if(this.social)await this.request('auth/v1/logout?scope=local',{}, {auth:true});}finally{this.social=null;this.save('checkpoint-social',null);}}
  rpc(name,body){return this.request('rest/v1/rpc/'+name,body,{auth:true});}
  profiles(){return this.request('rest/v1/cp_profiles?select=user_id,display_name,friend_code&limit=500',undefined,{auth:true});}
  friendships(){return this.request('rest/v1/cp_friendships?select=user_low,user_high&limit=500',undefined,{auth:true});}
  requests(){return this.request('rest/v1/cp_friend_requests?select=id,sender_id,recipient_id,status&status=eq.pending&limit=100',undefined,{auth:true});}
  publications(id){return this.request('rest/v1/cp_game_publications?owner_id=eq.'+encodeURIComponent(id)+'&select=owner_id,game_id,revision,operation_id,is_shared,operation_payload,updated_at&limit=10000',undefined,{auth:true});}
  async invite(code){const found=await this.rpc('cp_find_friend',{p_code:friendCode(code,true)});if(!found.length)throw new RemoteError('not-found');return this.request('rest/v1/cp_friend_requests',{sender_id:this.social.user.id,recipient_id:found[0].user_id},{auth:true});}
  answer(id,accept){return this.request('rest/v1/cp_friend_requests?id=eq.'+id,{status:accept?'accepted':'rejected'},{auth:true,method:'PATCH'});}
  cancel(id){return this.request('rest/v1/cp_friend_requests?id=eq.'+id,undefined,{auth:true,method:'DELETE'});}
  unfriend(id){const [low,high]=[this.social.user.id,id].sort();return this.request('rest/v1/cp_friendships?user_low=eq.'+low+'&user_high=eq.'+high,undefined,{auth:true,method:'DELETE'});}
  block(id){return this.request('rest/v1/cp_blocks',{blocker_id:this.social.user.id,blocked_id:id},{auth:true});}
  publish(id,operation){return this.rpc('cp_publish_game',{p_game_id:id,p_expected_revision:operation.revision,p_operation_id:operation.id,p_game:operation.payload});}
  coverSearch(title,excluded,refresh=false){return this.request('functions/v1/checkpoint-covers/v1/search',{title,excluded,refresh});}
  async coverImage(id){
    if(!/^[A-Za-z0-9_-]{1,80}$/.test(id))throw new RemoteError('igdb-unavailable');
    const response=await this.fetch(this.config.url+'/functions/v1/checkpoint-covers/v1/image/'+id,{headers:{apikey:this.config.publishableKey},credentials:'omit',referrerPolicy:'no-referrer',signal:AbortSignal.timeout(35000)});
    if(!response.ok||!response.headers.get('content-type')?.startsWith('image/'))throw new RemoteError('igdb-unavailable');
    const reader=response.body.getReader(),parts=[];let length=0;try{for(;;){const {done,value}=await reader.read();if(done)break;length+=value.length;if(length>2000000)throw new RemoteError('igdb-unavailable');parts.push(value);}}finally{await reader.cancel();}
    return new Blob(parts,{type:response.headers.get('content-type')});
  }
  steamRequest(path,body,auth=true){return this.request(path,body,{steam:true,auth});}
  async unlink(){try{if(this.steam)await this.steamRequest('v1/auth/logout',{});}finally{this.steam=null;this.save('checkpoint-steam',null);}}
}

