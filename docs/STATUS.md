# ZedPad Status (handoff between laptops)

Read this first on any machine. Update it at the end of every session and commit + push.

## Current position
- **Phase:** 1 - Connectivity spike: **complete and verified on a real phone**.
- **Last updated:** 2026-11-20 on Dell (Windows)
- **Next step:** Phase 2 (pairing and sessions) after user approval.

## Done
- Phase 0 foundation. Repo: https://github.com/Ajithzx123/ZedPad (public; office account `Ajith-suktha` is a collaborator). CI green on both jobs (Flutter pinned to 3.44.1 in `ci.yml`).
- Phase 1: C# and Dart `GamepadState` codecs sharing `docs/protocol-vectors.json`; Kestrel WebSocket server on `0.0.0.0:8765` (ping/pong, state logging, invalid/stale/oversized handling, per-request logging); Flutter connect screen with selectable error/log text.
- Real test passed: Realme RMX2170 (Android 12) on the PC's Windows hotspot connected to `192.168.137.1:8765`; server logged `State from 192.168.137.31: seq=1 L=(32767,0) buttons=0x0001`.
- Gates green: `dotnet format/build/test` (20 tests), `dart format`, `flutter analyze`, `flutter test` (12 tests).

## Known limits
- No pairing/auth yet: the server accepts any LAN client. Only run on trusted networks until Phase 2.
- Request logging is verbose (Kestrel + `Incoming ...` lines); trim in Phase 2.
- Port 8765 is also used by another program (`PhoneBoxAgent`) on `127.0.0.1` on the Dell; consider moving to a less common port.
- Mac build/test not yet verified; `flutter doctor` reports Android licenses not accepted on the Dell.

## Lessons (Windows networking)
- Phone connect timing out while ping works = Windows Firewall dropping TCP. A mobile hotspot is a **Public** network. Dismissing the firewall popup creates **Block** rules for the exe that override allow rules.
- Fix (admin PowerShell): allow rule `New-NetFirewallRule -DisplayName "ZedPad" -Direction Inbound -Protocol TCP -LocalPort 8765 -Action Allow -Profile Any`, and remove stale blocks: `Get-NetFirewallRule -DisplayName "ZedPad.Server" | Where-Object { $_.Direction -eq 'Inbound' -and $_.Action -eq 'Block' } | Remove-NetFirewallRule`.
- Non-admin sessions get "Access is denied" from `Get-NetFirewallRule`; an empty result there does not mean no rule exists.
- adb wireless debugging port changes between sessions; use `flutter devices`.

## How to run the Phase 1 test
1. PC: `cd server`, `dotnet run --project ZedPad.Server`. Note the `Address: ws://...` lines.
2. Phone on the same network (or PC hotspot): run the app from `app/` (VS Code F5 or `flutter run`), enter PC IP and port 8765, Connect, Ping, Send test state.
3. Server console shows `Device connected` and `State from ...`.

## Machine notes
- Dell (Windows): Flutter at `D:\DevTools\Flutter`. .NET SDK at `C:\Program Files\dotnet`. Admin PowerShell needed only for firewall rules.
- Mac: install Flutter 3.44.1, .NET 8 SDK, Android Studio/SDK; clone repo; whole solution builds there.

## Session log
- 2026-11-20 (Dell): planning, Phase 0 scaffold, Phase 1 code, CI fix, real-phone test passed after firewall fix.
