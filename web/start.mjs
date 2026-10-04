// Inicializa el puente web y el renderizador compartido; presenta un aviso si falla el arranque.

const language = location.hash === '#en' ? 'en' : 'es';
document.documentElement.lang = language;
const home = document.querySelector('[data-presentation]');
if (home) {
  home.textContent = language === 'en' ? 'Overview' : 'Presentación';
  home.href = './#' + language;
}
import('./bridge.mjs').catch(() => {
  const root = document.querySelector('#app');
  root.replaceChildren();
  const message = document.createElement('p');
  message.className = 'boot';
  message.textContent =
    language === 'es'
      ? 'No se pudo abrir Checkpoint. Permite el almacenamiento del sitio y recarga.'
      : 'Checkpoint could not start. Allow site storage and reload.';
  root.append(message);
});
