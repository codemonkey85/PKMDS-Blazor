---
name: implement-issue
description: Implement a PKMDS-Blazor GitHub issue end to end. Use when asked to investigate and implement an issue, bug, feature request, or feature-parity item and prepare it for review.
---

1. Read the full issue and its comments. Extract the current requirements, corrections, acceptance criteria, and unresolved questions.
2. Break the work into concrete tasks and inspect the real code and data paths before deciding on an implementation.
3. For PKHeX behavior, inspect the local PKHeX source checkout described in `AGENTS.md` and follow the production API patterns used there.
4. Implement a focused, maintainable change that follows the repository architecture and coding conventions.
5. Add or update appropriate automated coverage, then run only the local validation allowed by `AGENTS.md`. Do not run `dotnet test` locally.
6. Update documentation and the PKHeX feature-parity roadmap when the issue changes documented behavior or parity status.
7. Review the final diff and, when the user asks for the complete issue workflow, commit, push, and open a pull request with a clear summary and validation notes.
