const english={
 intro:'A game list with cover art, goals and progress. Available in your browser and as a Windows app.',
 open:'Open Checkpoint',download:'Download for Windows',
 account:'Your library works without an account. Sign in inside the app to connect with your Checkpoint friends.',
 example:'Example list',game:'Game',state:'Status',achievements:'Achievements',playing:'Playing',pending:'Pending',finished:'Story finished',
 sample:'Example data. Enter Checkpoint to open your own library.',features:'What you can do',
 lists:'Lists and progress',listsText:'Organize multiple lists, change game status and save notes, tasks and personal goals.',
 steamText:'Connect your account to import games and view achievements. Sync runs when the app opens and periodically.',
 friends:'Checkpoint friends',friendsText:'Share progress with friends using your Checkpoint code. You can keep games private.',
 windows:'Windows app',windowsText:'Use a translucent window, Miniature mode and game detection to view pending achievements.',
 data:'Your browser data',dataText:'Your library is saved in this browser. Import and export JSON backups. Your private library does not automatically sync between the web and Windows.',
 guide:'Read the web guide',credit:'Created by',license:'License'
};
const nodes=[...document.querySelectorAll('[data-copy]')];
const spanish=Object.fromEntries(nodes.map(node=>[node.dataset.copy,node.textContent]));
function render(language){
 const en=language==='en';document.documentElement.lang=language;
 for(const node of nodes)node.textContent=(en?english:spanish)[node.dataset.copy];
 document.querySelector('[data-open-app]').href='app.html#'+language;
 document.querySelector('[data-guide]').href='https://github.com/Nicolasmf05/Checkpoint/blob/main/docs/'+(en?'en/':'')+'WEB.md';
 document.querySelectorAll('[data-language]').forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.language===language)));
 try{sessionStorage.setItem('checkpoint.presentation.language',language);}catch{}
}
let language='es';try{language=sessionStorage.getItem('checkpoint.presentation.language')||language;}catch{}
if(['#es','#en'].includes(location.hash))language=location.hash.slice(1);
render(language==='en'?'en':'es');
for(const button of document.querySelectorAll('[data-language]'))button.addEventListener('click',()=>render(button.dataset.language));
if('serviceWorker'in navigator)navigator.serviceWorker.register('./sw.js').catch(()=>{});
