# Local screenshot cover fixtures

These three JPEG files are existing Steam library cover assets previously used in Checkpoint's isolated README captures. They were copied from `.qa/readme-captures/en-list/covers`, without pixel edits. Their origin is also embedded in each file.

| File          | Game          | Artwork owner    | Original delivery URL                                                           |
| ------------- | ------------- | ---------------- | ------------------------------------------------------------------------------- |
| `367520.jpg`  | Hollow Knight | Team Cherry      | `https://cdn.cloudflare.steamstatic.com/steam/apps/367520/library_600x900.jpg`  |
| `1145360.jpg` | Hades         | Supergiant Games | `https://cdn.cloudflare.steamstatic.com/steam/apps/1145360/library_600x900.jpg` |
| `620.jpg`     | Portal 2      | Valve            | `https://cdn.cloudflare.steamstatic.com/steam/apps/620/library_600x900.jpg`     |

The artwork retains its owners' rights. These files are screenshot fixtures, not Creative Commons assets and not images generated with AI. Capture runners use the local bytes so reproduction does not depend on a CDN, external account or live game library.

Windows/WebView2 gallery capture uses the same directory through `CHECKPOINT_GALLERY_COVERS`. Browser gallery capture loads these fixtures through `scripts/capture-gallery.mjs`; user data and account sessions are never read.
