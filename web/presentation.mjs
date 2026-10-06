// El idioma solo cambia la presentación y los enlaces. Nunca inicia la app ni una sesión.
const english = {
  skip: 'Skip to content',
  navigation: 'Main navigation',
  language: 'Language',
  navLibrary: 'Library',
  navExperience: 'Experience',
  navProgress: 'Progress',
  start: 'Get started',
  menuOpen: 'Open menu',
  menuClose: 'Close menu',
  heroEyebrow: 'ONE GAME AT A TIME',
  heroTitle: 'Your next',
  heroAccent: 'adventure.',
  intro:
    'All your games. Your goals. Your next adventure.\nOne place to keep playing at your own pace.',
  open: 'Open Checkpoint',
  download: 'Download for Windows',
  account: 'Your library, without creating an account.',
  explore: 'Explore Checkpoint',
  libraryAlt:
    'Checkpoint for Windows with a sample collection of Hollow Knight, Hades and Portal 2',
  sample: 'Windows app · Sample collection',
  libraryEyebrow: 'A PLACE FOR EVERY GAME',
  libraryTitle: 'Every game.',
  libraryAccent: 'In its place.',
  libraryText:
    'From the game you have been meaning to start to that one last achievement. Organize your collection and decide what comes next.',
  lists: 'A list for every moment.',
  listsText:
    'Create different lists. Keep track of what you are playing, what you have finished and what can wait.',
  exampleStates: 'Example game statuses',
  playing: 'Playing',
  pending: 'Pending',
  finished: 'Story finished',
  goals: 'Your way forward.',
  goalsText:
    'Keep goals, tasks and notes in each game’s details. Return to your next session knowing exactly where you left off.',
  goalsDetail: 'Story · Achievements · Personal goals',
  views: 'Your collection, your way.',
  viewsText:
    'Cover art, a list or a grid. Choose how to browse your games and change your perspective whenever you like.',
  viewsDetail: 'List · Compact · Grid',
  experienceEyebrow: 'FIND YOUR PERSPECTIVE',
  experienceTitle: 'One space.',
  experienceAccent: 'Three ways to see it.',
  experienceText:
    'Your library can take a different shape. Choose a view and see how it fits your routine.',
  galleryLabel: 'Checkpoint views',
  viewGrid: 'Grid',
  viewList: 'List',
  viewMini: 'Miniature',
  galleryGridAlt: 'Checkpoint grid view with cover art, game status and personal goals',
  galleryGridTitle: 'A collection worth exploring.',
  galleryGridText:
    'Every cover is the beginning of a story. Browse your games at a glance and choose your next session.',
  galleryGridNote: 'Windows view · Sample collection',
  galleryListAlt: 'Checkpoint list view in Spanish with Hollow Knight, Hades and Portal 2',
  galleryListTitle: 'All in order. Nothing in the way.',
  galleryListText:
    'The game, its status and your next goal. A straightforward view that keeps your attention on what matters.',
  galleryListNote: 'Windows view · Spanish example',
  galleryMiniAlt: 'Checkpoint Miniature mode showing three games and their statuses',
  galleryMiniTitle: 'A little space for your big plans.',
  galleryMiniText:
    'Your games and their status, close at hand. Windows Miniature mode leaves room on your desktop for everything else.',
  galleryMiniNote: 'Miniature mode · Available on Windows',
  progressEyebrow: 'EVERY SESSION COUNTS',
  progressTitle: 'Small goals.',
  progressAccent: 'Great stories.',
  steamTitle: 'Your games already have a starting point.',
  steamText:
    'Link Steam to import your library and check your achievements. Checkpoint refreshes your progress when the app opens and periodically.',
  steamNote: 'Your Steam game details must be public.',
  friendsLabel: 'FRIENDS',
  friends: 'Good games bring people together.',
  friendsText:
    'Connect with friends using your Checkpoint code and share your progress. You choose which games to keep private.',
  friendsNote: 'A Checkpoint account is optional for your library and required for friends.',
  windowsEyebrow: 'CHECKPOINT FOR WINDOWS',
  windowsTitle: 'On your desktop.',
  windowsAccent: 'Part of your routine.',
  windowsText:
    'A translucent window. A Miniature mode. Pending achievements when a game is detected. A companion that fits alongside your sessions.',
  windowsDownload: 'Get Checkpoint for Windows',
  requirements: 'Requirements and installation notes',
  miniatureAlt: 'Checkpoint Miniature mode showing three games and their statuses',
  miniatureCaption: 'Miniature mode · The essentials, close at hand.',
  dataEyebrow: 'YOUR LIBRARY, YOUR PACE',
  data: 'Your games.\nYour space.',
  dataText:
    'Your library is saved in this browser. You do not need an account to organize your games.',
  backups:
    'Export JSON backups to keep your data or move it elsewhere. Your private library does not automatically sync between the web and Windows.',
  guide: 'Read the web guide',
  finalEyebrow: 'YOUR NEXT CHECKPOINT AWAITS',
  finalTitle: 'Enjoy your\nnext session.',
  finalAction: 'Start my collection',
  finalNote: 'In your browser or on Windows. Your choice.',
  credit: 'Created by',
  footerNavigation: 'Information',
  privacy: 'Privacy',
  license: 'License',
  title: 'Checkpoint — Your next session starts here',
  description:
    'Organize your games, track your achievements and enjoy your next session. Checkpoint, in your browser and on Windows.',
};
const nodes = [...document.querySelectorAll('[data-copy], [data-copy-alt], [data-copy-aria]')];
function readCopy(node) {
  // Conserva los saltos editoriales aunque un panel todavía esté oculto.
  const copy = node.cloneNode(true);
  copy.querySelectorAll('br').forEach((br) => br.replaceWith('\u0000'));
  return copy.textContent
    .split('\u0000')
    .map((line) => line.replace(/\s+/g, ' ').trim())
    .join('\n');
}
const spanish = Object.fromEntries(
  nodes.map((node) => [
    node.dataset.copy || node.dataset.copyAlt || node.dataset.copyAria,
    node.dataset.copyAlt
      ? node.alt
      : node.dataset.copyAria
        ? node.getAttribute('aria-label')
        : readCopy(node),
  ]),
);
spanish.menuClose = 'Cerrar menú';
spanish.title = document.title;
spanish.description = document.querySelector('meta[name="description"]').content;
const header = document.querySelector('.site-header');
const menu = document.querySelector('.menu-toggle');
let language = 'es';

function setMenu(open, restoreFocus = false) {
  header.classList.toggle('is-menu-open', open);
  menu.setAttribute('aria-expanded', String(open));
  menu.dataset.copyAria = open ? 'menuClose' : 'menuOpen';
  menu.setAttribute('aria-label', (language === 'en' ? english : spanish)[menu.dataset.copyAria]);
  if (restoreFocus) menu.focus();
}
function render(nextLanguage) {
  language = nextLanguage === 'en' ? 'en' : 'es';
  const copy = language === 'en' ? english : spanish;
  document.documentElement.lang = language;
  document.title = copy.title;
  document.querySelector('meta[name="description"]').content = copy.description;
  for (const node of nodes) {
    const key = node.dataset.copy || node.dataset.copyAlt || node.dataset.copyAria;
    if (node.dataset.copyAlt) node.alt = copy[key];
    else if (node.dataset.copyAria) node.setAttribute('aria-label', copy[key]);
    else {
      // Conserva los saltos editoriales sin introducir HTML en las traducciones.
      const lines = copy[key].split('\n');
      node.replaceChildren();
      lines.forEach((line, index) => {
        if (index) node.append(document.createElement('br'));
        node.append(document.createTextNode(line));
      });
    }
  }
  document.querySelectorAll('[data-open-app]').forEach((link) => {
    link.href = 'app.html#' + language;
  });
  for (const image of document.querySelectorAll('[data-image-es]')) {
    const source = language === 'en' ? image.dataset.imageEn : image.dataset.imageEs;
    if (image.getAttribute('src') !== source) image.src = source;
  }
  const docs =
    'https://github.com/Nicolasmf05/Checkpoint/blob/main/docs/' + (language === 'en' ? 'en/' : '');
  document.querySelector('[data-guide]').href = docs + 'WEB.md';
  document.querySelector('[data-download-guide]').href = docs + 'DOWNLOAD-NOTES.md';
  document.querySelector('[data-privacy-guide]').href = docs + 'PRIVACY.md';
  document
    .querySelectorAll('[data-language]')
    .forEach((button) =>
      button.setAttribute('aria-pressed', String(button.dataset.language === language)),
    );
  try {
    sessionStorage.setItem('checkpoint.presentation.language', language);
  } catch {}
}
try {
  language = sessionStorage.getItem('checkpoint.presentation.language') || language;
} catch {}
if (['#es', '#en'].includes(location.hash)) language = location.hash.slice(1);
render(language);
for (const button of document.querySelectorAll('[data-language]'))
  button.addEventListener('click', () => render(button.dataset.language));
menu.addEventListener('click', () => setMenu(menu.getAttribute('aria-expanded') !== 'true'));
document.querySelector('.site-navigation').addEventListener('click', (event) => {
  if (event.target.closest('a')) setMenu(false);
});
document.addEventListener('keydown', (event) => {
  if (event.key === 'Escape' && menu.getAttribute('aria-expanded') === 'true') setMenu(false, true);
});
document.addEventListener('click', (event) => {
  if (!header.contains(event.target)) setMenu(false);
});
matchMedia('(min-width: 901px)').addEventListener('change', () => setMenu(false));
const galleryTabs = [...document.querySelectorAll('[data-gallery-tab]')];
function selectView(tab, focus = false) {
  for (const item of galleryTabs) {
    const selected = item === tab;
    item.setAttribute('aria-selected', String(selected));
    item.tabIndex = selected ? 0 : -1;
    document.getElementById(item.getAttribute('aria-controls')).hidden = !selected;
  }
  if (focus) tab.focus();
}
for (const tab of galleryTabs) {
  tab.addEventListener('click', () => selectView(tab));
  tab.addEventListener('keydown', (event) => {
    const index = galleryTabs.indexOf(tab);
    let next;
    if (event.key === 'ArrowRight') next = (index + 1) % galleryTabs.length;
    else if (event.key === 'ArrowLeft')
      next = (index + galleryTabs.length - 1) % galleryTabs.length;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = galleryTabs.length - 1;
    else return;
    event.preventDefault();
    selectView(galleryTabs[next], true);
  });
}
if ('IntersectionObserver' in window && !matchMedia('(prefers-reduced-motion: reduce)').matches) {
  const observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries)
        if (entry.isIntersecting) {
          entry.target.classList.add('is-visible');
          observer.unobserve(entry.target);
        }
    },
    { threshold: 0.08 },
  );
  for (const node of document.querySelectorAll('.reveal')) observer.observe(node);
  document.documentElement.classList.add('motion-ready');
}
if ('serviceWorker' in navigator) navigator.serviceWorker.register('./sw.js').catch(() => {});
