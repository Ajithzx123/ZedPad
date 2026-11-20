# Development

## Setup
- Flutter stable (3.44+), Android SDK / device with USB debugging.
- .NET 8 SDK (`dotnet --list-sdks` must list 8.x).
- Dell (Windows): everything. Mac: Flutter app, build and tests of the .NET solution (but no in-game input testing).

## Commands
Flutter (in `app/`):
```
dart format --set-exit-if-changed .
flutter analyze
flutter test
```
.NET (in `server/`):
```
dotnet format --verify-no-changes
dotnet build
dotnet test
```
Everything targets `net8.0`, so the whole solution builds and tests on macOS too; only the Windows sinks in `ZedPad.Input` cannot run there (P/Invoke is guarded at runtime).

## Session workflow
Read `docs/STATUS.md`, work the current phase only, update `STATUS.md` and `ROADMAP.md`, commit and push.

