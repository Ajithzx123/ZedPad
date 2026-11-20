# ZedPad Status (handoff between laptops)

Read this first on any machine. Update it at the end of every session and commit + push.

## Current position
- **Phase:** 0 - Foundation (code complete, waiting for commit/remote and approval to start Phase 1)
- **Last updated:** 2026-11-20 on Dell (Windows)
- **Next step:** commit, create private GitHub repo and add remote, confirm CI is green, then approve Phase 1.

## Done
- Plan approved (see ROADMAP.md). Decisions in `docs/decisions/0001-stack-and-transport.md`.
- Docs: README, ROADMAP, STATUS, architecture, protocol (hex vectors), development, testing. CLAUDE.md / AGENTS.md.
- Flutter skeleton in `app/` (Provider, folder layout, smoke test). `dart format`, `flutter analyze`, `flutter test` pass (Flutter 3.44.1, Dart 3.12.1).
- .NET 8 SDK (8.0.428) installed on Dell. `server/ZedPad.sln` with Core, Input, Server, Tests; shared `Directory.Build.props` (nullable, warnings as errors), `.editorconfig`. `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test` pass (1 smoke test).
- CI workflow `.github/workflows/ci.yml` (written, not yet run).

## Not done
- Nothing committed; no git remote yet.
- `flutter doctor` / Android device not verified.
- CI never run; `ZedPad.Input` is an empty project (Phase 3).

## Open questions
- Git host: private GitHub repo? (user creates, gives URL)
- Android test device (model, Android version).

## Machine notes
- Dell (Windows): Flutter at `D:\DevTools\Flutter`. .NET SDK at `C:\Program Files\dotnet` (open a new shell so PATH picks it up). Run `dotnet` from `server/`, `flutter` from `app/`.
- Mac: install Flutter, .NET 8 SDK, Android Studio/SDK; clone repo; whole solution builds there.

## Session log
- 2026-11-20 (Dell): planning, roadmap, handoff docs; Phase 0 scaffolding complete and gates green.
