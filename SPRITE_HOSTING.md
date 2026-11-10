# Sprite hosting (#1359)

HOME artwork and game sprites are served from Cloudflare R2 through
`https://sprites.pkmds.app/`, with bundled sprites as fallbacks. Embedded hosts
continue to suppress external sprite requests.

## Collection and provenance

The [collection manifest](tools/data/sprite-collection.json) pins
`codemonkey85/sprites` revision `a7a3d62bb80a654ab5e01b488659929af0077c31`.
It records 30,277 images, 12,602,378,781 bytes (11.74 GiB), with each object key,
content type, byte count, SHA-256 and MD5 checksum. It includes HOME and all 21
mapped game directory/format pairs and their available front sprite variants:
female, shiny, shiny/female and transparent. Files are copied without conversion.
Back sprites and unused formats are excluded. The image copyright remains with
The Pokémon Company; see the source repository's `LICENCE.txt` and contributors.

Bucket: `pkmds-sprites-test`. Collection root:

```text
https://sprites.pkmds.app/collections/a7a3d62bb80a654ab5e01b488659929af0077c31/pokemon/
```

Upstream `sprites/pokemon/` maps to `collections/<revision>/pokemon/`.
Publication status and verification evidence are recorded in PR #1361. A generated
manifest alone does not prove publication: the publisher uploads the public
`collections/<revision>/manifest.json` only after all images pass verification.

The collection was published on November 10, 2026. All 30,277 image MD5s and byte
counts matched, and 80 representative public downloads passed SHA-256, MIME, CORS
and immutable-header checks from the UAT origin. The [public manifest](https://sprites.pkmds.app/collections/a7a3d62bb80a654ab5e01b488659929af0077c31/manifest.json)
matches the committed file (SHA-256
`7de228b3c63c005f3a8f52ceae8b728d01dd9a6c33a6030b1dbd32bd31d535dc`).

## Reproduce, upload and verify

Use Node 22+ and [rclone 1.59+](https://developers.cloudflare.com/r2/examples/rclone/).
Cloudflare recommends rclone or another S3-compatible tool for bulk uploads.
Super Slurper migrates existing cloud buckets; our source files are local.

1. Sync external repositories with the repository's `sync-repos` skill. Check out
   the pinned revision in a clean sprite checkout. Upload rejects a different
   HEAD or local sprite changes.
2. Generate the deterministic manifest. It derives the game directory/format
   pairs from `PokeApiSpriteUrls.cs` and selects tracked front sprite variants:

   ```powershell
   node tools/sprites/publish.mjs manifest --source C:/Code/sprites
   ```

3. In R2's **Manage API Tokens**, create S3 credentials with **Object Read & Write**,
   scoped to `pkmds-sprites-test`. Prefer a short expiration for a one-time upload.
   Configure an rclone remote named `pkmdssprites`: provider `Cloudflare`, region
   `auto`, `no_check_bucket = true`, endpoint
   `https://5a09f006ed56baba01325764d8215ca7.r2.cloudflarestorage.com`.
   Never commit credentials or put them in PR comments.

   Windows users can instead run `./tools/sprites/configure-r2.ps1`. It prompts
   locally and encrypts the secret with Windows DPAPI for the current Windows
   user/computer in gitignored `artifacts/sprites/r2-credentials.xml`.

4. Upload. The publisher checks local bytes against the manifest, then runs
   `rclone copy --checksum --immutable`. Re-run the same command to resume:

   ```powershell
   node tools/sprites/publish.mjs upload --source C:/Code/sprites --concurrency 12

   # Windows DPAPI option; supply the installed rclone executable:
   ./tools/sprites/with-r2.ps1 -RclonePath C:/Tools/rclone.exe -PublisherArguments @('upload', '--source', 'C:/Code/sprites', '--concurrency', '12')
   ```

   Use `--rclone PATH` for a custom executable or `--remote NAME:BUCKET` for another
   configured destination. Copy never deletes remote files. `--immutable` fails
   if an existing object differs. The 256 MiB multipart threshold exceeds the
   size of every selected image, so their S3 ETags are MD5 hashes.

5. Independently recheck with `node tools/sprites/publish.mjs verify`. Verification
   compares every remote MD5 and byte count against the manifest, and requires
   the exact image count. Reports/checksum results go in `artifacts/sprites/`.
   Check representative public HTTPS responses for SHA-256, MIME, CORS and cache
   headers, including Iron Moth HOME (994) and modern animated game sprites.
6. After publication/validation, change `SpriteSource.PokemonBaseUrl`, the
   worker's `spritePathPrefix` and explicit URL expectations together; CI checks
   app/worker agreement. Commit the manifest with that change.

Publishing uses S3 and needs no Worker deployment or `workers.dev` subdomain. The
account preview subdomain enabled during investigation is independent of delivery.

## Ongoing source updates

The **Check sprite repository changes** GitHub workflow runs daily at 09:23 UTC
after it reaches the default branch, and supports manual dispatch. It compares
the published revision with the latest `codemonkey85/sprites` default branch using
Git trees, without downloading the image collection. It filters changes to assets
consumed by this app, records added/changed/removed paths in an artifact and job
summary, and fails when an update needs review. It does not publish assets or use
R2 credentials. GitHub's normal workflow notification preferences control alerts.

For a local check after syncing the sprite checkout:

```powershell
node tools/sprites/check-upstream.mjs --source C:/Code/sprites
```

For an approved update, preserve the prior manifest, regenerate the new manifest,
then pass `--previous-manifest PATH` to `upload`. The publisher writes a collection
delta report and uses rclone `--copy-dest` to copy matching files inside R2. Only
new or changed files need a local upload. Removed files are absent from the new
collection because only the new manifest's paths are copied. The old collection
is retained for rollback; pruning retained collections is a separate deliberate
operation. Each retained complete collection uses additional storage.

The sprite fork's own upstream sync must keep running: this monitor follows the
fork that supplies the app, rather than publishing directly from PokeAPI upstream.

## Transport, cache limits and rollback

Remote images use anonymous CORS. Public GET/HEAD access allows all origins using
[this configuration](tools/data/r2-uat-cors.json), including Azure PR preview
origins. No credentials are sent for public image reads. Uploads preserve the
PNG/GIF/WebP MIME types and set `Cache-Control: public, max-age=31536000, immutable`.
Set the `crossorigin` attribute before `src` when creating image elements; WebKit
can otherwise start/cache a no-CORS request before the attribute is applied
([WebKit bug 291793](https://bugs.webkit.org/show_bug.cgi?id=291793)).

Never replace bytes at a published revision URL. Publish and verify a new revision
before changing the app root. Existing clients keep using the previous revision.
Roll back by restoring the previous app/worker URL constants; retain that collection.
The 17 unversioned trial objects remain for earlier UAT builds and posted links.

The published worker caches sprites on demand in `pkmds-sprites-v2`, shares
concurrent fetches per URL, and bounds the cache to 512 entries **and 128 MiB**.
Writes/eviction are serialized. Revisioned URLs isolate updated bytes; old entries
are evicted within the same budget without deleting a cache an older tab may use.
Images larger than the budget render from the network but are immediately evicted.
Cache Storage failures still return downloaded images. Errors, opaque responses,
and HTML pages are never cached as sprites.
Hosted cache misses revalidate the browser HTTP cache so an old, cached 404 cannot
hide a newly published image. Always finish publication before exposing a new
collection URL in the app or preview. An incomplete-upload 404 may otherwise have
been cached by the browser independently of the service worker.

Bundled sprites are cached lazily in the app version cache. Viewed hosted sprites
work offline; unseen images can only use bundled art that was already cached.
Installation does not download 11.74 GiB. “Clear Sprite Cache” currently clears
only in-memory loaded state, not HTTP or service worker caches.

## UAT and rollout

Use the Azure preview URL posted on PR #1361 and copies of saves in an isolated
profile/private window. Verify HOME base/female/shiny/shiny-female images, named
and numeric forms, Iron Moth, Alcremie permutations, early transparent PNG, Gen 7
GIF and modern animated WebP. Exercise box/party, form pickers, both Trade panes,
and game/style/save changes. Missing/blocked images must restore bundled art,
including an error after a previous success. Check identical concurrent sprites,
offline revisits, byte-budget eviction, Cache Storage failure, update/rollback,
and embedded mode (`?host=test`). Include Safari/iOS validation where available.
Confirm CI build/tests, CodeQL and preview deployment before merging.

For cache diagnosis use a hard refresh, a private window, or unregister only the
service worker. Never clear site data: it destroys IndexedDB backups and the Bank.
