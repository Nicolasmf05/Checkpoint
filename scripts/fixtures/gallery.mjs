// Isolated screenshot examples. These are illustrative records, never user data.
export const galleryGames = [
  { id: '10000000-0000-4000-8000-000000367520', title: 'Hollow Knight', appId: 367520 },
  { id: '10000000-0000-4000-8000-000001145360', title: 'Hades', appId: 1145360 },
  { id: '10000000-0000-4000-8000-000000000620', title: 'Portal 2', appId: 620 },
];

export function gallerySeed(language, covers) {
  const english = language === 'en';
  const pending = english ? 'Up next' : 'Pendientes';
  const favorites = english ? 'Favorites' : 'Favoritos';
  const text = (es, en) => (english ? en : es);
  const task = (number, es, en, done = false) => ({
    id: `20000000-0000-4000-8000-${String(number).padStart(12, '0')}`,
    title: text(es, en),
    done,
  });
  const base = (index) => ({
    id: galleryGames[index].id,
    title: galleryGames[index].title,
    steamAppId: galleryGames[index].appId,
    platform: 'Steam',
    customCover: covers[galleryGames[index].appId],
    tracked: true,
    friendsPrivate: false,
    sortOrder: index,
    addedAt: '2026-10-01T10:00:00Z',
  });
  return {
    revision: 0,
    deleted: [],
    settings: {
      language,
      theme: 'dark',
      layout: 0,
      opacity: 1,
      lightweight: false,
      syncMinutes: 30,
      gameLists: [favorites, pending],
      activeList: 'all',
    },
    games: [
      {
        ...base(0),
        favorite: true,
        status: 'Playing',
        goal: 'Story',
        storyPercent: 45,
        playtimeMinutes: 1860,
        lists: [favorites],
        tasks: [
          task(1, 'Explorar una nueva zona', 'Explore a new area'),
          task(2, 'Volver a la estación', 'Return to the station', true),
        ],
        notes: text(
          'Me quedé junto al banco. La siguiente zona está a la derecha.',
          'I stopped near the bench. The next area is to the right.',
        ),
      },
      {
        ...base(1),
        status: 'Pending',
        goal: 'Story',
        lists: [pending],
        tasks: [task(3, 'Empezar una nueva partida', 'Start a new run')],
        notes: text('Para la próxima tarde libre.', 'For the next free afternoon.'),
      },
      {
        ...base(2),
        status: 'Paused',
        goal: 'Achievements',
        playtimeMinutes: 690,
        lists: [favorites],
        syncedAt: '2026-10-01T11:00:00Z',
        achievements: Array.from({ length: 10 }, (_, index) => ({
          id: 'example-' + index,
          name: text('Logro de ejemplo ', 'Example achievement ') + (index + 1),
          description: text('Progreso ilustrativo de la demo.', 'Illustrative demo progress.'),
          hidden: false,
          unlocked: index < 9,
        })),
        tasks: [
          task(4, 'Terminar el modo cooperativo', 'Finish co-op mode', true),
          task(5, 'Encontrar todos los secretos', 'Find every secret'),
        ],
        notes: text(
          'Volver al modo cooperativo. Nos quedamos antes del último puzle.',
          'Return to co-op. We stopped before the final puzzle.',
        ),
      },
    ],
  };
}
