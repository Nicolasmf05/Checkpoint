export const defaults = Object.freeze({add:'Ctrl+N',search:'Ctrl+F',undo:'Ctrl+Z',view:'F6',hide:'Escape',shortcuts:'F1',edit:'F2',up:'ArrowUp',down:'ArrowDown',first:'Home',last:'End',pageUp:'PageUp',pageDown:'PageDown',gameMenu:'Enter',moveUp:'Alt+ArrowUp',moveDown:'Alt+ArrowDown',global:'Ctrl+Alt+C'});
export const shortcutLabels = Object.freeze({add:'Añadir juego',search:'Buscar juego',undo:'Recuperar último juego eliminado',view:'Cambiar vista',hide:'Ocultar widget',shortcuts:'Atajos de teclado',edit:'Editar juego',up:'Juego anterior',down:'Juego siguiente',first:'Primer juego',last:'Último juego',pageUp:'Página anterior',pageDown:'Página siguiente',gameMenu:'Abrir menú del juego',moveUp:'Mover juego hacia arriba',moveDown:'Mover juego hacia abajo',global:'Mostrar / ocultar desde cualquier aplicación'});
const special=['Escape','ArrowUp','ArrowDown','Home','End','PageUp','PageDown','Enter','Space'];
export function canonical(value) {
 const parts=String(value).trim().split('+').map(x=>x.trim());let key=parts.pop();key=({'↑':'ArrowUp','↓':'ArrowDown',inicio:'Home',fin:'End','repág':'PageUp','avpág':'PageDown',intro:'Enter',esc:'Escape',espacio:'Space'})[key?.toLowerCase()]||key;
 const modifiers=parts.map(x=>({ctrl:'Ctrl',control:'Ctrl',alt:'Alt',shift:'Shift'})[x.toLowerCase()]);
 if(modifiers.some(x=>!x)||new Set(modifiers).size!==modifiers.length)throw new Error('invalid-shortcut');
 let normalized=special.find(x=>x.toLowerCase()===key?.toLowerCase())||(/^f([1-9]|1\d|2[0-4])$/i.test(key)?key.toUpperCase():/^[a-z0-9]$/i.test(key)?key.toUpperCase():null);
 if(!normalized || /^[A-Z0-9]$/.test(normalized)&&!modifiers.some(x=>x==='Ctrl'||x==='Alt'))throw new Error('invalid-shortcut');
 return [...['Ctrl','Alt','Shift'].filter(x=>modifiers.includes(x)),normalized].join('+');
}
export function validateShortcuts(values) {
 const result={},seen=new Set();for(const name of Object.keys(defaults)){const value=canonical(values[name]??defaults[name]);if(seen.has(value)||value==='Space'||value==='Escape'&&name!=='hide')throw new Error('duplicate-shortcut');seen.add(value);result[name]=value;}
 if(!result.global.includes('Ctrl+')&&!result.global.includes('Alt+'))throw new Error('invalid-shortcut');return result;
}
export function effectiveShortcuts(values){try{return validateShortcuts({...defaults,...values});}catch{return {...defaults};}}
export function eventGesture(event){if(event.metaKey||event.isComposing||event.getModifierState?.('AltGraph'))return null;const key=event.key===' '?'Space':event.key.length===1?event.key.toUpperCase():event.key;return [...(event.ctrlKey?['Ctrl']:[]),...(event.altKey?['Alt']:[]),...(event.shiftKey?['Shift']:[]),key].join('+');}
export const matches=(event,value)=>eventGesture(event)===value;

export function displayGesture(value,language='es'){const keys=language==='en'?{ArrowUp:'↑',ArrowDown:'↓'}:{ArrowUp:'↑',ArrowDown:'↓',Home:'Inicio',End:'Fin',PageUp:'RePág',PageDown:'AvPág',Enter:'Intro',Escape:'Esc',Space:'Espacio'};return value.split(/( \/ |\+)/).map(k=>keys[k]||k).join('');}
