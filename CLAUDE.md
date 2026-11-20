# ZedPad - Engineering Guidelines

## Start of every session
1. Read `docs/STATUS.md` (where we are, next step) and `docs/ROADMAP.md` (plan).
2. Work only on the current phase; never start the next phase without user approval.
3. Before ending: update `docs/STATUS.md` (position, done, next step, session log), tick `docs/ROADMAP.md`, commit.

The work is done from two laptops (Dell/Windows office, Mac home). Everything needed to continue must be in the repo, not in chat.

## Objective
Production-quality app turning an Android phone into a responsive PC game controller. Real product, not a throwaway prototype.

## Standards
- Clean, idiomatic, maintainable Dart and C#. Simple over clever. No over-abstraction, giant files, placeholder code, obvious comments, or swallowed errors. Minimal dependencies.
- Flutter: Provider; no business logic in widgets; immutable models; handle loading/disconnected/reconnecting/error; dispose streams and timers; no `dynamic`, no unjustified `!`.
- C#: .NET 8, nullable enabled, async + CancellationToken, DI, thin `Program.cs`, interfaces only at real boundaries, no global mutable state.
- Windows-specific code lives only in `ZedPad.Input` (all projects target plain net8.0 so the Mac builds everything).

## Architecture
- `ZedPad.Core`: protocol codec, ControllerState, mapping, profiles, `IInputSink`. No Windows/WebSocket deps.
- `ZedPad.Input`: keyboard/gamepad sinks. `ZedPad.Server`: transport, pairing, sessions.
- `docs/protocol.md` is the single source of truth; Dart and C# share hex test vectors.
- Phone sends state, not events. Flutter emits logical actions; profile JSON decides keys.
- Release all inputs on disconnect, watchdog timeout, shutdown.
- Record lasting decisions in `docs/decisions/`.

## Security
Validate all network input, enforce version and max size, CSPRNG single-use expiring pairing tokens, never log secrets, LAN-only by default.

## Skills
Check available skills and read relevant ones before a task (Flutter, architecture, review). Never claim a skill that was not available or read.

## Quality gate (before saying done)
Flutter: `dart format --set-exit-if-changed .`, `flutter analyze`, `flutter test`
.NET: `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test`
Review the full diff. Never claim tests passed unless run. Never weaken tests.

