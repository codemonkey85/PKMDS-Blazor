---
name: sync-repos
description: Synchronize external repositories used by PKMDS-Blazor. Use when asked to sync, get latest, refresh related repos, inspect upstream changes, or prepare work that depends on current PKHeX, PokeAPI, Pokemon Showdown, sprites, or plugin sources.
---

1. Read `.sync-repos` from the repository root. Ignore blank lines and comments.
2. Resolve each entry without guessing destructively:
   - Resolve relative paths from the PKMDS-Blazor repository root.
   - Use absolute paths as written after expanding the user's home directory.
   - For bare names or `owner/repo`, check the current repository's parent, `$CODE_ROOT` when set, `~/Code/codemonkey85`, `~/Code`, and `C:\Code`; use the first existing Git repository.
3. Inspect each repository's status, branch, upstream, and remotes before changing it. Never discard, stash, or overwrite local work.
4. Fetch repositories in parallel when practical. Fast-forward the checked-out branch with `git pull --ff-only` only when its worktree state makes that safe; otherwise fetch and report why it was not updated.
5. Obtain any required approval for network access or writes outside the current workspace.
6. Report the resolved path, branch, previous and current commit, update result, and any repositories that were missing, dirty, divergent, or lacked an upstream.
