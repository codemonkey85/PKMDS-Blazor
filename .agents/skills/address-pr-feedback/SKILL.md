---
name: address-pr-feedback
description: Address unresolved GitHub pull request review feedback in PKMDS-Blazor. Use when asked to handle PR comments, review threads, requested changes, or reviewer follow-up.
---

1. Read every unresolved review thread and relevant PR comment before changing code. Ignore already-resolved feedback unless it supplies necessary context.
2. Break the feedback into concrete tasks and identify any comments that conflict or need clarification.
3. Reply to each unresolved comment individually, explaining what you will change and why.
4. Implement the smallest coherent changes that address the feedback. Preserve unrelated work and follow `AGENTS.md`.
5. Run the repository-approved formatting and build checks. Do not run `dotnet test` locally; leave tests to GitHub Actions.
6. Review the resulting diff, commit it, and push it when the user has asked for the PR feedback workflow to be completed.
7. Reply with the result where useful and resolve each thread only after its change has landed.
