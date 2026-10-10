import { themes } from './presentation-themes.mjs';

const english = {
  skip: 'Skip to content',
  navigation: 'Main navigation',
  language: 'Language',
  navLibrary: 'Library',
  navExperience: 'Views',
  navProgress: 'Achievements & friends',
  theme: 'Theme',
  darkTheme: 'Dark',
  menuOpen: 'Open menu',
  menuClose: 'Close menu',
  heroTitle: 'Where were\nyou?',
  heroNote: 'A game collection that remembers.',
  intro:
    'That boss you still need to beat. The stubborn achievement.\nThe game you left halfway through. Keep track of where you stopped and come back when you feel like it.',
  open: 'Open Checkpoint',
  download: 'Download for Windows',
  account: 'You don’t need an account to keep track of your games.',
  coverAlt: 'Portal 2 cover art showing Atlas and P-body',
  previewGoal: 'Goal: finish the achievements',
  previewPlayed: 'Time played',
  previewVisible: 'Visible',
  previewVisibility: 'Friends only',
  previewProgress: '9 of 10 achievements in this example',
  previewAchievements: 'Achievements completed',
  previewTasks: 'Tasks',
  tryTask: 'Try checking one off',
  previewTaskDone: 'Finish co-op mode',
  previewTaskPending: 'Find all the secrets',
  previewNotes: 'For the next session',
  previewNote: 'One achievement left. Ask Dani if they’re up for it.',
  sample: 'Example game details · The data is illustrative. Your games are saved in the app.',
  libraryTitle: 'There’s time.\nThere are lots of games.',
  libraryText:
    'You don’t have to finish everything. Keep what you’re playing close by and leave the rest for another day.',
  exampleStates: 'Example game statuses',
  playing: 'Playing',
  pending: 'Pending',
  finished: 'Story finished',
  lists: 'For now. For later.',
  listsText:
    'A list for what you’re playing. Another for recommendations. And the one that gets longer every time there’s a sale.',
  goals: '“What was I doing?”',
  goalsText:
    'Write down that unbeaten boss, the missing collectible or something you wanted to try. Your future self will thank you.',
  views: 'The covers count, too.',
  viewsText:
    'Browse your collection in a grid, skim a list or keep a Miniature window on your Windows desktop.',
  experienceTitle: 'Your collection,\nyour way.',
  experienceText: 'Big cover art or a straightforward list. Try all three views.',
  galleryLabel: 'Checkpoint views',
  viewGrid: 'Grid',
  viewList: 'List',
  viewMini: 'Miniature',
  galleryGridAlt: 'Checkpoint grid view with cover art, game status and personal goals',
  galleryGridTitle: 'The one with that cover.\nThat’s what I want to play.',
  galleryGridText:
    'Sometimes a cover is all it takes to remember a game. Here they all are, with their status and what you still want to do.',
  galleryGridNote: 'Web app screenshot · Sample collection',
  galleryListAlt: 'Checkpoint list view with Hollow Knight, Hades and Portal 2',
  galleryListTitle: 'A quick look\nbefore you play.',
  galleryListText:
    'The name, the status and the next goal. One after another, so you can find what you need without hunting around.',
  galleryListNote: 'Web app screenshot · Sample collection',
  galleryMiniAlt: 'Checkpoint Miniature mode showing three games and their statuses',
  galleryMiniTitle: 'A little space\nbeside your game.',
  galleryMiniText:
    'Keep your list in a small window while you do other things. Your games stay close without taking over the desktop.',
  galleryMiniNote: 'Miniature mode · Available on Windows',
  galleryThemeNote:
    'Real app screenshots in the dark theme. The selector changes this page and the example game details.',
  progressTitle: 'One left.\nOr a few more.',
  steamTitle: 'Don’t add them one by one.',
  steamText:
    'Link Steam to import your games and check achievements. Progress refreshes when Checkpoint opens and periodically.',
  steamNote: 'Your Steam game details need to be public.',
  friendsLabel: 'Friends',
  friends: '“What are you playing?”',
  friendsText:
    'Add friends with their Checkpoint code and share your progress. If you’d rather keep a game to yourself, make it private.',
  friendsNote: 'You need a Checkpoint account to use friends.',
  windowsTitle: 'Between Discord\nand your game.',
  windowsText:
    'Keep Checkpoint on your desktop, switch to Miniature mode and check pending achievements when it detects a game.',
  windowsDownload: 'Download for Windows',
  requirements: 'Requirements and installation notes',
  miniatureAlt: 'Checkpoint Miniature mode showing three games and their statuses',
  miniatureCaption: 'The same collection, in a smaller window.',
  data: 'Your games\nstay with you.',
  dataText:
    'On the web, your collection is saved in this browser. You can start without signing up.',
  backups:
    'Make a JSON backup to keep it safe or move it elsewhere. Your private library does not automatically sync between the web and Windows.',
  guide: 'How the web version works',
  finalTitle: 'Keep track of where you were.\nThen keep playing.',
  finalAction: 'Add your first game',
  finalNote: 'Start with the one you’re playing now.',
  credit: 'Created by',
  footerNavigation: 'Information',
  privacy: 'Privacy',
  license: 'License',
  title: 'Checkpoint — Where were you?',
  description:
    'Your backlog, your achievements and where you left off. Organize your collection with Checkpoint, in your browser or on Windows.',
};

const root = document.documentElement;
const header = document.querySelector('.site-header');
const menu = document.querySelector('.menu-toggle');
const themeSelect = document.querySelector('#presentation-theme');
const galleryTabs = [...document.querySelectorAll('[data-gallery-tab]')];
const nodes = [...document.querySelectorAll('[data-copy], [data-copy-alt], [data-copy-aria]')];
function readCopy(node) {
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

const parameters = new URLSearchParams(location.search);
let language = 'es';
let themeId = 'dark';
try {
  language = sessionStorage.getItem('checkpoint.presentation.language') || language;
} catch {}
try {
  themeId = localStorage.getItem('checkpoint.presentation.theme') || themeId;
} catch {}
if (['#es', '#en'].includes(location.hash)) language = location.hash.slice(1);
if (['es', 'en'].includes(parameters.get('lang'))) language = parameters.get('lang');
if (themes.some((theme) => theme.id === parameters.get('theme'))) themeId = parameters.get('theme');
if (!themes.some((theme) => theme.id === themeId)) themeId = 'dark';
let view =
  galleryTabs.find((tab) => tab.dataset.galleryTab === parameters.get('view'))?.dataset
    .galleryTab || 'grid';

function updateUrl() {
  const url = new URL(location.href);
  url.searchParams.set('lang', language);
  url.searchParams.set('theme', themeId);
  url.searchParams.set('view', view);
  // Los enlaces a secciones conservan su ancla; #es/#en se sustituyen por el idioma actual.
  if (['#es', '#en'].includes(url.hash)) url.hash = language;
  history.replaceState(null, '', url);
}
function setMenu(open, restoreFocus = false) {
  header.classList.toggle('is-menu-open', open);
  menu.setAttribute('aria-expanded', String(open));
  menu.dataset.copyAria = open ? 'menuClose' : 'menuOpen';
  menu.setAttribute('aria-label', (language === 'en' ? english : spanish)[menu.dataset.copyAria]);
  if (restoreFocus) menu.focus();
}
function updateDemo() {
  const checked = [...document.querySelectorAll('.preview-tasks input')].filter(
    (input) => input.checked,
  ).length;
  const number = new Intl.NumberFormat(language);
  document.querySelector('[data-demo-tasks]').textContent =
    language === 'en'
      ? number.format(checked) + ' of 2 tasks completed'
      : number.format(checked) + ' de 2 tareas completadas';
  document.querySelector('[data-demo-hours]').textContent = number.format(11.5) + '\u00a0h';
  document.querySelector('[data-demo-percent]').textContent = new Intl.NumberFormat(language, {
    style: 'percent',
  }).format(0.9);
}
function render(nextLanguage) {
  language = nextLanguage === 'en' ? 'en' : 'es';
  const copy = language === 'en' ? english : spanish;
  root.lang = language;
  document.title = copy.title;
  document.querySelector('meta[name="description"]').content = copy.description;
  for (const node of nodes) {
    const key = node.dataset.copy || node.dataset.copyAlt || node.dataset.copyAria;
    if (node.dataset.copyAlt) node.alt = copy[key];
    else if (node.dataset.copyAria) node.setAttribute('aria-label', copy[key]);
    else {
      const lines = copy[key].split('\n');
      node.replaceChildren();
      lines.forEach((line, index) => {
        if (index) node.append(document.createElement('br'));
        node.append(document.createTextNode(line));
      });
    }
  }
  for (const link of document.querySelectorAll('[data-open-app]'))
    link.href = 'app.html#' + language;
  for (const image of document.querySelectorAll('[data-image-es]')) {
    const source = language === 'en' ? image.dataset.imageEn : image.dataset.imageEs;
    if (image.getAttribute('src') !== source) image.src = source;
  }
  const docs =
    'https://github.com/Nicolasmf05/Checkpoint/blob/main/docs/' + (language === 'en' ? 'en/' : '');
  document.querySelector('[data-guide]').href = docs + 'WEB.md';
  document.querySelector('[data-download-guide]').href = docs + 'DOWNLOAD-NOTES.md';
  document.querySelector('[data-privacy-guide]').href = docs + 'PRIVACY.md';
  for (const button of document.querySelectorAll('[data-language]'))
    button.setAttribute('aria-pressed', String(button.dataset.language === language));
  themeSelect.replaceChildren(
    ...themes.map((theme) => new Option(theme[language], theme.id, false, theme.id === themeId)),
  );
  updateDemo();
  try {
    sessionStorage.setItem('checkpoint.presentation.language', language);
  } catch {}
}
function applyTheme(id) {
  const theme = themes.find((item) => item.id === id) || themes[0];
  themeId = theme.id;
  root.dataset.theme = theme.id;
  root.dataset.scheme = theme.scheme;
  document.querySelector('meta[name="theme-color"]').content = theme.background;
  themeSelect.value = theme.id;
  try {
    localStorage.setItem('checkpoint.presentation.theme', theme.id);
  } catch {}
}
function selectView(tab, focus = false) {
  view = tab.dataset.galleryTab;
  for (const item of galleryTabs) {
    const selected = item === tab;
    item.setAttribute('aria-selected', String(selected));
    item.tabIndex = selected ? 0 : -1;
    document.getElementById(item.getAttribute('aria-controls')).hidden = !selected;
  }
  if (focus) tab.focus();
}

render(language);
applyTheme(themeId);
selectView(galleryTabs.find((tab) => tab.dataset.galleryTab === view));
themeSelect.addEventListener('change', () => {
  applyTheme(themeSelect.value);
  updateUrl();
});
for (const button of document.querySelectorAll('[data-language]'))
  button.addEventListener('click', () => {
    render(button.dataset.language);
    updateUrl();
  });
for (const input of document.querySelectorAll('.preview-tasks input'))
  input.addEventListener('change', updateDemo);
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
for (const tab of galleryTabs) {
  tab.addEventListener('click', () => {
    selectView(tab);
    updateUrl();
  });
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
    updateUrl();
  });
}
if ('serviceWorker' in navigator) navigator.serviceWorker.register('./sw.js').catch(() => {});
