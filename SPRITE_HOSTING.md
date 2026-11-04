# Sprite hosting UAT (#1359)

This is a partial rollout for UAT. The app uses Cloudflare R2 through
`https://sprites.pkmds.app/pokemon/`, with the existing bundled sprites as fallbacks.
**Do not merge this rollout to production until the full collection is published and verified.**

## Current sample collection

Bucket: `pkmds-sprites-test`. There are 17 canonical objects, totaling 6,270,107 bytes
(5.98 MiB), copied without transformation from `codemonkey85/sprites` commit
`a7a3d62bb80a654ab5e01b488659929af0077c31`.
[The sample manifest](tools/data/r2-uat-sprites.json) records every object key, byte count,
and SHA-256. Object keys omit the upstream `sprites/` directory; the public URL is
the host plus the key. Legacy trial URLs under `/sprites/pokemon/` are not used by the app.

Available examples:

- HOME: Pikachu (male, female, shiny male), Iron Moth, Unown B, and placeholder 0.
- Game sprites: male non-shiny Pikachu in Red/Blue, Black/White, Ultra Sun/Ultra Moon,
  Let's Go, Sword/Shield, BDSP, Legends: Arceus, Scarlet/Violet, and Legends: Z-A.
- Shiny male Pikachu in Scarlet/Violet; shiny Celebi from Gold (the app retains its
  existing bundled padded Celebi override).

Everything else deliberately exercises the bundled fallback. For example, Bulbasaur,
shiny female Pikachu, and most form picker previews have no hosted sample.
No full export/upload pipeline or new sprite conversion is included in this phase.

## Transport and caching

`SpriteSource.PokemonBaseUrl` composes the HOME/game URLs. Keep the published service
worker's `spritePathPrefix` in sync; a test enforces this. Form and game mappings remain
in `PokeApiSpriteUrls`.

Remote images use anonymous CORS. The public bucket allows GET/HEAD from any origin
using [this Wrangler CORS configuration](tools/data/r2-uat-cors.json), including changing
Azure PR preview origins. These are public images; no cookies or credentials are sent.
The samples currently use their original PNG/GIF/WebP content types and
`Cache-Control: public, max-age=3600`.

The published service worker caches sprites on demand, coalesces concurrent requests
for the same URL, and retains at most 512 hosted images in `pkmds-sprites-v2` across
app updates. It caches successful image responses only. Missing/offline images and
Cache Storage failures retain the bundled fallback. Bundled sprites are cached on
demand in the app version cache. Embedded hosts continue to suppress external sprites.

The hosted sprite cache is cache-first and does not revalidate existing entries.
Changing hosted bytes at an existing URL therefore needs an explicit cache migration
or versioned URL. Final versioning, storage limits and invalidation are part of the
full rollout; the one-hour CDN TTL alone does not expire service worker entries.
The settings action "Clear Sprite Cache" clears only the in-memory loaded state,
not browser HTTP or service worker caches.

## UAT checklist

The existing PR workflow deploys an Azure Static Web Apps preview for PRs targeting
`main`; use the URL posted on the PR. Test in a separate browser profile or private
window, with copies of save files.

1. Open a save with male non-shiny Pikachu. In Settings, test HOME and game sprite
   styles for the available games above. Confirm PNG, GIF and WebP images render;
   confirm GIF/WebP animation where present.
2. Test HOME female Pikachu, shiny male Pikachu, Iron Moth and Unown B. Confirm the
   correct paths and images; check other species and missing variants retain bundled art.
3. Switch boxes, sprite styles, saves and the two Trade panes. A loaded sprite in one
   game must not hide another game's fallback. Form picker failures must also fall back.
4. Fill a box with identical Pikachu. With the published worker controlling the page,
   the first concurrent miss should produce one network fetch per distinct URL;
   gender/shiny/game variants are distinct. Revisit to confirm browser/worker cache reuse.
5. After the worker is active, view a sample, go offline and revisit it. Viewed hosted
   images should load from cache; unseen hosted images use bundled art **if that bundled
   fallback has already been cached**. First-time images cannot be guaranteed offline.
6. Test a deliberately missing URL (404), a blocked sprite request, and an unavailable
   Cache Storage environment. A network image should still render when caching fails.
7. Check an embedded host (`?host=test`): no external sprite requests should occur.

For cache diagnosis use a fresh profile/private window or unregister only the service
worker. Never clear site data: that destroys save backups and the Pokemon Bank in IndexedDB.

## Remaining work before production

- Build the full reproducible export/upload process with source revision and manifest,
  incremental upload/resume, content types, checksums and completeness checks.
- Choose production URL versioning/cache invalidation and final object cache headers.
- Publish and verify all required species, forms, genders, shiny variants and game sets.
- Complete UAT, including Safari/iOS offline behavior and realistic storage pressure.
- Confirm DNSSEC activation independently; DNSSEC signing does not block sprite UAT.

Keep #1359 open for this work. DNS hosting and registrar transfer are already on Cloudflare;
the full sprite rollout is separate from that completed migration.
