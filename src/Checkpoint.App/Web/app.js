import { visibleRange, nextIndex } from './ui-model.mjs';

const root = document.querySelector('#app');
let state, selected, menu, dragId, layout, dialogMode;
let requestId = 0;
const pending = new Map();
const send = value => { const id=++requestId; window.chrome?.webview?.postMessage({...value,requestId:id}); return id; };
const action = (name, values = {}) => send({ action: name, ...values });
const el = (tag, className, text) => { const node = document.createElement(tag); if (className) node.className = className; if (text != null) node.textContent = text; return node; };
function button(text, name, title, values = {}, className = '') {
  const node = el('button', className, text); node.type = 'button'; node.title = title || text;
  node.setAttribute('aria-label', title || text); node.addEventListener('click', () => action(name, values)); return node;
}
function dismissMenu() { const hadMenu=!!menu; menu?.remove(); menu=undefined; if (hadMenu && state?.mini && selected) root.querySelector(`[data-game="${selected}"]`)?.focus({preventScroll:true}); }
function showMenu(game, x, y) {
  dismissMenu(); if (!state || state.kind !== 'main') return;
  menu = el('div', 'menu'); menu.setAttribute('role', 'menu');
  function entry(text, command, values = {}, checked) {
    const item = button((checked == null ? '' : checked ? '✓  ' : '    ') + text, command, text, values);
    item.setAttribute('role', checked == null ? 'menuitem' : 'menuitemradio'); if (checked != null) item.setAttribute('aria-checked', String(checked));
    item.addEventListener('click', dismissMenu); menu.append(item);
  }
  if (game) {
    state.labels.statuses.forEach((label, value) => entry(label, 'state', { id: game.id, value }, game.state === value));
    menu.append(el('hr')); entry(state.labels.edit, 'edit', { id: game.id }); menu.append(el('hr'));
  }
  if (state.mini) entry(state.labels.exitMini, 'exit-mini');
  [state.labels.fullWindow,state.labels.smallWindow,state.labels.miniature].forEach((label,value)=>entry(label,'window-mode',{value},value===(state.mini?2:state.full?0:1)));
  entry(state.labels.settings, 'settings'); entry(state.labels.search, 'search'); entry(state.labels.add, 'add');
  menu.append(el('hr')); entry(state.labels.pin, 'pin', {}, state.pinned); entry(state.labels.locked, 'lock', {}, state.locked);
  document.body.append(menu);
  menu.style.left = `${Math.max(4, Math.min(x, innerWidth - menu.offsetWidth - 4))}px`;
  menu.style.top = `${Math.max(4, Math.min(y, innerHeight - menu.offsetHeight - 4))}px`;
  menu.querySelector('button')?.focus();
}
document.addEventListener('pointerdown', e => { if (menu && !menu.contains(e.target)) dismissMenu(); });
function preserveTree(build) {
  const focused = document.activeElement, id = focused?.dataset.control;
  const selection = focused?.selectionStart, end = focused?.selectionEnd;
  const passwords = new Map([...root.querySelectorAll('input[type=password]')].map(node => [node.dataset.control,node.value]));
  const inputs = new Map([...root.querySelectorAll("input,textarea")].filter(node => (pending.get(node.dataset.control)||0) > (state.ack||0)).map(node => [node.dataset.control,node.value]));
  const scroll = [...root.querySelectorAll('.type-scroll')].map(node => node.scrollTop);
  build();
  [...root.querySelectorAll('.type-scroll')].forEach((node, index) => node.scrollTop = scroll[index] || 0);
  for (const node of root.querySelectorAll('input,textarea')) { if (inputs.has(node.dataset.control)) node.value=inputs.get(node.dataset.control); else if (node.type==='password' && node.dataset.empty !== 'true') node.value=passwords.get(node.dataset.control)||''; }
  if (id) {
    const target = root.querySelector(`[data-control="${id}"]`);
    if (target) { target.focus({ preventScroll: true }); if (selection != null && target.setSelectionRange && ['input','textarea'].includes(target.tagName.toLowerCase()) && target.type !== 'range') try { target.setSelectionRange(selection, end); } catch {} }
  }
}
function schema(node) {
  if (!node) return el('div');
  let element;
  const update = value => pending.set(String(node.id),send({ action: 'value', control: node.id, value }));
  switch (node.type) {
    case 'text': element = el('div', `type-text${node.heading ? ' heading' : ''}${node.muted ? ' muted' : ''}`, node.text); break;
    case 'button': element = el('button', `type-button${node.accent ? ' accent' : ''}`, node.text); element.type = 'button'; element.addEventListener('click', () => send({ action: 'click', control: node.id })); break;
    case 'input': case 'password': case 'textarea':
      element = el(node.type === 'textarea' ? 'textarea' : 'input', `type-${node.type}`);
      if (node.type !== 'textarea') element.type = node.type === 'password' ? 'password' : 'text';
      if (node.max > 0) element.maxLength = node.max;
      element.value = node.value || ''; element.autocomplete = node.type === 'password' ? 'current-password' : 'off';
      if (node.type === 'password') element.dataset.empty = String(node.empty);
      element.addEventListener('input', () => update(element.value)); break;
    case 'check':
      element = el('label', 'type-check'); { const input = el('input'); input.type = 'checkbox'; input.checked = node.checked; input.disabled = !node.enabled; input.dataset.control = node.id; input.setAttribute('aria-label', node.name || node.text); input.addEventListener('change', () => update(input.checked)); element.append(input, el('span', '', node.text)); } break;
    case 'select':
      element = el('select', 'type-select'); node.options.forEach((text, index) => { const option = el('option', '', text); option.value = index; element.append(option); }); element.value = node.value;
      element.addEventListener('change', () => update(Number(element.value))); break;
    case 'slider': element = el('input', 'type-slider'); element.type = 'range'; element.min = node.min; element.max = node.max; element.step = node.step || .05; element.value = node.value; element.addEventListener('input', () => update(Number(element.value))); break;
    case 'image': element = el('img', 'type-image'); if (node.src) element.src = node.src; element.alt = ''; element.loading = 'lazy'; break;
    case 'details': element = el('details', 'type-details'); element.open = node.open; element.append(el('summary', '', node.text)); element.addEventListener('toggle', () => { if (element.open !== node.open) update(element.open); }); break;
    default: element = el('div', `type-${node.type}`); break;
  }
  if (node.type === 'grid') { element.style.gridTemplateColumns = node.columnWidths?.join(' ') || `repeat(${node.columns},minmax(0,1fr))`; if (node.rows?.length) element.style.gridTemplateRows = node.rows.join(' '); }
  element.dataset.control = node.id; if (node.name) element.setAttribute('aria-label', node.name); if (node.tip) element.title = node.tip;
  if ('disabled' in element) element.disabled = !node.enabled;
  if (node.column) element.style.gridColumn = node.column;
  if (node.row) element.style.gridRow = node.row;
  if (node.width) element.style.width = `${node.width}px`;
  for (const child of node.children || []) element.append(schema(child));
  return element;
}
function frame() {
  root.replaceChildren(); const windowNode = el('main', 'window');
  const header = el('header', 'header'), brand = el('div', 'brand'); brand.append(el('div', 'logo', '⚑'));
  const wordmark = el('div'); wordmark.append(el('strong', '', 'checkpoint'), el('div', 'tagline')); brand.append(wordmark);
  brand.addEventListener('pointerdown', e => { if (e.button === 0 && !state.locked) action('drag'); }); header.append(brand);
  for (const [icon, name, label] of [['◇','pin','pin'], ['⚙','settings','settings'], ['−','hide','hide'], ['×','close','close']]) { const node = button(icon, name, icon, {}, 'icon'); node.dataset.label = label; header.append(node); }
  const modes=el('select','window-mode'); modes.setAttribute('aria-label','Checkpoint'); modes.addEventListener('change',()=>action('window-mode',{value:Number(modes.value)})); header.append(modes);
  const intro = el('section', 'intro'); intro.append(el('h1'), el('div', 'summary'));
  const navigation = el('nav', 'navigation');
  for (const name of ['list','library','friends']) { const node = button('', 'tab', '', { value: name }); node.dataset.tab = name; navigation.append(node); }
  const add = button('', 'add', '', {}, 'accent add'); add.dataset.label = 'add'; navigation.append(add);
  const searchbar = el('div', 'searchbar'), search = el('input'); search.id = 'search'; search.type = 'search'; search.maxLength = 140; search.addEventListener('input', () => pending.set('search',action('search-change', { value: search.value })));
  const filter = el('select'); filter.addEventListener('change', () => action('filter', { value: Number(filter.value) })); searchbar.append(search, filter);
  const viewport = el('section', 'viewport'); viewport.setAttribute('aria-label', 'Checkpoint'); viewport.tabIndex = -1; viewport.addEventListener('scroll', renderGames);
  const footer = el('footer', 'footer'), messages = el('div'); messages.append(el('div', 'connection'), el('div', 'notice')); footer.append(messages);
  const tools = el('div', 'tools'); for (const [icon,name] of [['↶','undo'], ['▤','cycle'], ['↻','sync']]) { const node = button(icon, name, icon, {}, 'icon'); node.dataset.label = name === 'cycle' ? 'view' : name; tools.append(node); } footer.append(tools);
  const strip = el('div', 'dragstrip'); strip.hidden = true; strip.addEventListener('pointerdown', e => { if (e.button === 0 && !state.locked) action('drag'); });
  const resize = el('div', 'resize'); resize.setAttribute('aria-hidden', 'true'); let point;
  resize.addEventListener('pointerdown', e => { if (state.locked) return; point = [e.screenX,e.screenY]; resize.setPointerCapture(e.pointerId); e.preventDefault(); });
  resize.addEventListener('pointermove', e => { if (!point) return; const x=e.screenX-point[0], y=e.screenY-point[1]; point=[e.screenX,e.screenY]; if (x || y) action('resize',{x,y}); }); resize.addEventListener('pointerup', () => point = undefined); resize.addEventListener('lostpointercapture', () => point = undefined);
  windowNode.append(header,intro,navigation,searchbar,viewport,footer,strip,resize); root.append(windowNode);
  windowNode.addEventListener('contextmenu', e => { if (state.mini && !e.target.closest('[data-game]')) { e.preventDefault(); showMenu(null,e.clientX,e.clientY); } });
}
function renderMain() {
  if (dialogMode || !root.querySelector('.window')) frame(); dialogMode = false;
  const host = root.querySelector('.window'); host.classList.toggle('mini', state.mini); host.classList.toggle('full',state.full);
  const modes=host.querySelector('.window-mode'); modes.replaceChildren(); [state.labels.fullWindow,state.labels.smallWindow,state.labels.miniature].forEach((text,index)=>{const option=el('option','',text);option.value=index;modes.append(option);}); modes.value=state.mini?2:state.full?0:1; modes.setAttribute('aria-label',state.labels.windowMode);
  host.style.setProperty('--opacity', state.opacity); host.style.setProperty('--mini-size', `${state.textSize}px`);
  host.querySelector('.tagline').textContent = state.labels.tagline;
  host.querySelector('h1').textContent = state.tab === 'friends' ? state.labels.friendsTitle : state.labels.title;
  host.querySelector('.summary').textContent = state.summary;
  host.querySelectorAll('[data-label]').forEach(node => { const label = state.labels[node.dataset.label]; node.title = label; node.setAttribute('aria-label', label); if (node.classList.contains('add')) node.textContent = '+ '+label; });
  host.querySelectorAll('[data-tab]').forEach(node => { node.textContent = state.labels[node.dataset.tab]; node.classList.toggle('active', state.tab === node.dataset.tab); });
  const search = host.querySelector('#search'); if ((pending.get('search')||0) <= (state.ack||0) && search.value !== state.search) search.value = state.search; search.placeholder = state.labels.search; search.setAttribute('aria-label',state.labels.search);
  const filter = host.querySelector('.searchbar select'); filter.replaceChildren(); [state.labels.all,...state.labels.statuses].forEach((text,index) => { const option=el('option','',text); option.value=index; filter.append(option); }); filter.value = state.filter;
  host.querySelector('.connection').textContent = state.connection; host.querySelector('.notice').textContent = state.notice;
  host.querySelector('[data-label="undo"]').hidden = !state.undo; host.querySelector('[data-label="sync"]').disabled = state.busy;
  host.querySelector('.resize').hidden = state.locked || state.full; host.querySelector('.dragstrip').hidden = !state.mini; host.querySelector('[data-label="pin"]').textContent = state.pinned ? '◆' : '◇';
  host.querySelector('[data-label="view"]').textContent = state.mini ? '☷' : state.grid ? '▦' : state.compact ? '≡' : '▤';
  if (selected && !state.games.some(game => game.id === selected)) selected = undefined;
  if (state.tab === 'friends') preserveTree(() => { const viewport=host.querySelector('.viewport'); const content=el('div','friends-content'); content.append(schema(state.friends)); if (state.friendsBusy) content.querySelectorAll('button,input,select').forEach(node=>node.disabled=true); viewport.replaceChildren(content); });
  else renderGames();
}
function renderGames() {
  if (!state || state.kind !== 'main' || state.tab === 'friends') return;
  const viewport = root.querySelector('.viewport'); if (!viewport) return;
  if (!state.games.length && !state.mini) { const empty=el('div','empty'); empty.append(el('h2','',state.emptyTitle),el('p','',state.emptyText),button(state.labels.add,'add'),button(state.labels.steam,'steam')); if (state.examples) empty.append(button(state.labels.examples,'examples')); viewport.replaceChildren(empty); return; }
  const focusWasRow = document.activeElement?.closest('[data-game]');
  const columns = state.grid && !state.mini ? Math.max(1,Math.floor(viewport.clientWidth / 155)) : 1;
  const rowHeight = state.mini ? state.textSize + 31 : state.grid ? 230 : state.compact ? 94 : 144;
  layout = { columns, rowHeight, viewport };
  viewport.scrollTop=Math.min(viewport.scrollTop,Math.max(0,Math.ceil(state.games.length/columns)*rowHeight-viewport.clientHeight));
  const range=visibleRange(state.games.length,viewport.scrollTop,viewport.clientHeight,rowHeight,columns), rows=el('div','rows'); rows.style.height=`${range.height}px`;
  for (let index=range.start; index<Math.min(range.end,state.games.length); index++) {
    const game=state.games[index], row=el('article',state.mini ? 'minirow' : `game${state.grid ? ' grid' : state.compact ? ' compact' : ''}`);
    row.dataset.game=game.id; row.tabIndex=selected===game.id ? 0 : -1; row.classList.toggle('selected',selected===game.id); row.style.top=`${Math.floor(index/columns)*rowHeight}px`;
    if (!state.mini) { row.style.left=`${(index%columns)*100/columns}%`; row.style.width=`calc(${100/columns}% - ${columns>1 ? 8 : 0}px)`; row.style.height=`${rowHeight-10}px`; }
    else row.style.height=`${rowHeight-3}px`;
    row.addEventListener('focus',()=>selected=game.id); row.addEventListener('contextmenu',e=>{ e.preventDefault(); selected=game.id; showMenu(game,e.clientX,e.clientY); });
    if (state.mini) { const name=el('div','game-title ellipsis',game.title); name.title=game.title; row.append(name,el('div',`status state-${game.state}`,game.status)); row.addEventListener('click',()=>{selected=game.id;row.focus();}); }
    else {
      if (!state.lightweight) { const cover=el('img','cover'); cover.src=game.cover; cover.alt=''; cover.loading='lazy'; cover.addEventListener('error',()=>cover.replaceWith(el('div','cover cover-fallback',game.title.slice(0,1)))); cover.addEventListener('click',()=>action('edit',{id:game.id})); row.append(cover); }
      const details=el('div','details'); details.append(el('div','game-title ellipsis',game.title),el('div','platform ellipsis',game.platform),el('div',`status state-${game.state}`,game.status),el('div','next ellipsis',game.next),el('div','progress ellipsis',game.progress+(game.percent==null?'':` · ${game.percent}%`)));
      if (game.percent != null) { const progress=el('progress'); progress.max=100; progress.value=game.percent; progress.setAttribute('aria-label',game.progress); details.append(progress); } row.append(details);
      const tools=el('div','game-tools'); tools.append(button(game.state===3?'✓':'○','finish',state.labels.finish,{id:game.id},'icon'),button('⋯','edit',state.labels.edit,{id:game.id},'icon'));
      const reorder=el('button','icon','⠿'); reorder.title=game.title; reorder.setAttribute('aria-label',game.title); reorder.draggable=true; reorder.addEventListener('keydown',e=>{if(e.altKey && ['ArrowUp','ArrowDown'].includes(e.key)){e.preventDefault();const index=state.games.findIndex(item=>item.id===game.id),target=state.games[index+(e.key==='ArrowUp'?-1:1)];if(target)action('move',{id:game.id,target:target.id,after:e.key==='ArrowDown'});}}); reorder.addEventListener('dragstart',e=>{dragId=game.id;e.dataTransfer.setData('text/plain',game.id);}); tools.append(reorder); row.append(tools);
      row.addEventListener('dragover',e=>e.preventDefault()); row.addEventListener('drop',e=>{e.preventDefault();if(dragId)action('move',{id:dragId,target:game.id,after:e.clientY>row.getBoundingClientRect().top+row.clientHeight/2});dragId=undefined;});
    }
    rows.append(row);
  }
  viewport.replaceChildren(rows);
  if (focusWasRow && selected) rows.querySelector(`[data-game="${selected}"]`)?.focus({preventScroll:true});
}
document.addEventListener('keydown',e=>{
  if (dialogMode) { if (e.key==='Escape') { e.preventDefault(); send({action:'cancel-dialog'}); } return; }
  if (!state) return;
  const input=e.target.closest?.('input,textarea,select');
  if (e.ctrlKey && e.key.toLowerCase()==='n') {e.preventDefault();action('add');return;}
  if (e.ctrlKey && e.key.toLowerCase()==='f') {e.preventDefault();action('search');return;}
  if (e.ctrlKey && e.key.toLowerCase()==='z' && !input) {e.preventDefault();action('undo');return;}
  if (e.key==='F6') {e.preventDefault();action('cycle');return;}
  if (e.key==='Escape') {e.preventDefault();if(menu)dismissMenu();else action('hide');return;}
  if (menu) {
    const items=[...menu.querySelectorAll('button')], index=items.indexOf(document.activeElement);
    if (['ArrowDown','ArrowUp','Home','End'].includes(e.key)) {e.preventDefault();items[e.key==='Home'?0:e.key==='End'?items.length-1:(index+(e.key==='ArrowDown'?1:-1)+items.length)%items.length]?.focus();}
    else if (e.key==='Enter' || e.key===' ') {e.preventDefault();document.activeElement.click();}
    return;
  }
  if (input || !state.mini || !state.games.length) return;
  const index=state.games.findIndex(game=>game.id===selected);
  if (['ArrowUp','ArrowDown','Home','End','PageUp','PageDown'].includes(e.key)) {
    e.preventDefault(); const next=nextIndex(index,e.key,state.games.length,Math.max(1,Math.floor(layout.viewport.clientHeight/layout.rowHeight))); selected=state.games[next].id;
    const top=next*layout.rowHeight;if(top<layout.viewport.scrollTop)layout.viewport.scrollTop=top;else if(top+layout.rowHeight>layout.viewport.scrollTop+layout.viewport.clientHeight)layout.viewport.scrollTop=top+layout.rowHeight-layout.viewport.clientHeight;
    renderGames();root.querySelector(`[data-game="${selected}"]`)?.focus({preventScroll:true});
  } else if (['Enter',' ','F2'].includes(e.key) && index>=0) {e.preventDefault();if(e.key==='F2')action('edit',{id:selected});else{const row=root.querySelector(`[data-game="${selected}"]`),rect=row.getBoundingClientRect();showMenu(state.games[index],rect.left+10,rect.bottom);}}
});
window.addEventListener('resize',renderGames);
window.chrome?.webview?.addEventListener('message',event=>{
  if (event.data.kind==='focus-game') {selected=event.data.id;requestAnimationFrame(()=>root.querySelector(`[data-game="${selected}"]`)?.focus({preventScroll:true}));return;}
  if (event.data.kind==='focus-search') { requestAnimationFrame(()=>{const search=root.querySelector('#search');search?.focus();search?.select();});return; }
  const firstDialog=state?.kind!=='dialog' && event.data.kind==='dialog';
  state=event.data; window.checkpointState=state;
  for (const [key,id] of pending) if (id <= (state.ack||0)) pending.delete(key); document.documentElement.lang=state.language; document.documentElement.dataset.theme=state.light?'light':'dark';
  if (state.kind==='dialog') {dialogMode=true;preserveTree(()=>{const page=el('main','dialog-page');page.append(schema(state.root));root.replaceChildren(page);});if(firstDialog)root.querySelector('input,select,button')?.focus();}
  else renderMain();
});
action('ready');
