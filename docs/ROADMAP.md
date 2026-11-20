# ZedPad Roadmap

Turn an Android phone into a game controller for a Windows PC (first target: PES 2000).
Phone (Flutter) streams controller **state** as binary frames over LAN WebSocket; the Windows receiver (.NET 8) injects keyboard input (later a ViGEm virtual pad).

Live progress lives in [STATUS.md](STATUS.md). This file is the stable plan; tick boxes here when a phase item is done.

## Machines and what each can do

| Machine | Can do | Cannot do |
|---|---|---|
| Dell (office, Windows) | Everything: server, SendInput, ViGEm, PES 2000 test, Android build | - |
| Mac (home) | Flutter app, Android build/run, `ZedPad.Core` + tests (.NET is cross-platform), docs | Run `ZedPad.Input` keyboard sink, test in-game input |

Rule: Windows-only code stays in `ZedPad.Input` so the Mac can build and test everything else.
Mac note: all projects target plain `net8.0` (no `net8.0-windows`), so the full solution builds and tests on macOS; Windows API calls are guarded at runtime.

## Phase 0 - Foundation (STOP after this, get approval)
- [x] Git repo + remote (sync between laptops)
- [x] `CLAUDE.md`, `AGENTS.md`, `README.md`
- [x] `docs/`: architecture, protocol (with hex test vectors), development, testing, decisions/
- [x] .NET solution: Core, Input, Server, Tests (nullable on, analyzers)
- [x] Flutter skeleton in `app/` (Provider, folder layout, analysis options)
- [x] CI (GitHub Actions): format, analyze, build, test for both sides
- [ ] Clean build + tests green on Mac (Dell done)

## Phase 1 - Connectivity spike
- [x] Console receiver: Kestrel WebSocket, prints IP/port/status/logs
- [x] Flutter client connects, sends Ping and binary `GamepadState`
- [x] Protocol codec in Core (C#) and `core/protocol` (Dart) sharing hex vectors
- Done when: real phone frames appear in the PC console. **Verified 2026-11-20** (Realme RMX2170, Android 12, via PC hotspot).

## Phase 2 - Pairing and sessions
- [x] CSPRNG one-time token, ~2 min expiry, single use, never logged
- [x] QR in console (IP:port+token); phone scans or enters manually
- [x] Session creation, `PairAck`/`PairReject`, reject unauthorized
- [x] Phone connection state UI (Disconnected, Connecting, Pairing, Connected, Failed; Reconnecting comes in Phase 5)

## Phase 3 - Input engine (critical)
- [ ] `IInputSink` in Core, `KeyboardSink` (SendInput, scan codes) in Input
- [ ] State diffing (previous vs incoming), release-all
- [ ] Stick to keys: normalize, deadzone, threshold + hysteresis
- [ ] Profile loader + `profiles/pes2000.json` (logical action to key)
- Done when: Notepad shows correct keys, then in-game keys work.

## Phase 4 - Football controller
- [ ] Floating joystick, gesture pad (tap, 4 swipes, hold, two-finger tap)
- [ ] Flutter emits logical actions only; profile maps to keys
- [ ] Sensitivity/deadzone settings
- Done when: PES 2000 is playable.

## Phase 5 - Reliability
- [ ] Heartbeat ~1 s, watchdog 500-1000 ms on last valid controller frame
- [ ] Reconnect with backoff, duplicate-session handling
- [ ] Wi-Fi drop, app background/screen lock, PC sleep/wake
- [ ] Latency logging (target < 30 ms on 5 GHz)

## Phase 6 - Racing
- [ ] Gyro steering, calibrate, deadzone, EMA smoothing, curves
- [ ] ViGEm Xbox 360 sink; keyboard fallback if driver missing

## Phase 7 - Productization
- [ ] Settings, profile editor, classic gamepad mode
- [ ] Multi-phone (one sink/slot per connection)
- [ ] Tray/desktop UI, installer, driver management, crash handling
- [ ] Release builds (APK, Windows)

## Working agreement
- One phase item at a time; each ends with format + analyze + tests + diff review.
- Do not start the next phase without approval.
- Every session: read `docs/STATUS.md` first, update it before ending.






