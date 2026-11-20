# ZedPad Status (handoff between laptops)

Read this first on any machine. Update it at the end of every session and commit + push.

## Current position
- **Phase:** 1 - Connectivity spike (code done; waiting for the real phone-to-PC test)
- **Last updated:** 2026-11-20 on Dell (Windows)
- **Next step:** run the server, allow it through Windows Firewall, connect from the phone, confirm frames appear. Then close Phase 1 and approve Phase 2.

## Done
- Phase 0 foundation, pushed to https://github.com/Ajithzx123/ZedPad (office account `Ajith-suktha` is a collaborator).
- Phase 1 code (uncommitted until the user asks):
  - `ZedPad.Core/Protocol`: `GamepadState`, `GamepadStateCodec`, `SequenceNumber`, `ProtocolInfo`.
  - `docs/protocol-vectors.json`: shared vectors loaded by C# and Dart tests.
  - `ZedPad.Server`: Kestrel WebSocket on `0.0.0.0:8765`, console banner with LAN addresses, ping/pong, state logging (on input change), drops invalid/stale frames, closes oversized messages.
  - `app/`: `GamepadState` codec, `ConnectionController` (web_socket_channel), `ConnectScreen` (IP + port, Connect, Ping, Send test state, log). INTERNET permission added.
- Gates green on Dell: `dotnet format/build/test` (20 tests), `dart format`, `flutter analyze`, `flutter test` (12 tests).

## Not done / known limits
- Not yet tested with a real phone. No pairing/auth yet: the Phase 1 server accepts any LAN client. Do not leave it running on untrusted networks.
- Windows Firewall will probably block port 8765 on first run (allow private networks).
- `flutter doctor` / Android device not verified.
- CI result for the Phase 0 push not confirmed.

## How to run the Phase 1 test
1. PC: `cd server` then `dotnet run --project ZedPad.Server`. Note an `Address: ws://192.168.x.x:8765` line.
2. Phone on the same Wi-Fi: `cd app` then `flutter run`, enter the PC IP and port 8765, Connect, Ping, Send test state.
3. PC console should show `Device connected`, and a `State from ...` line.

## Open questions
- Android test device (model, Android version).

## Machine notes
- Dell (Windows): Flutter at `D:\DevTools\Flutter`. .NET SDK at `C:\Program Files\dotnet` (new shells have it on PATH).
- Mac: install Flutter, .NET 8 SDK, Android Studio/SDK; clone repo; whole solution builds there.

## Session log
- 2026-11-20 (Dell): planning, Phase 0 scaffold and push, Phase 1 code complete with gates green.
