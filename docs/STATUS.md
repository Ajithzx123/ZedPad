# ZedPad Status (handoff between laptops)

Read this first on any machine. Update it at the end of every session and commit + push.

## Current position
- **Phase:** 2 - Pairing and sessions: code complete and committed, automated tests green, **not yet tested on a real phone**.
- **Last updated:** 2026-11-21 on Dell (Windows)
- **Next step:** real-phone pairing test (QR scan and manual code), then approve Phase 3.

## Done
- Phase 0 foundation and Phase 1 connectivity spike (verified on a Realme RMX2170 over the PC hotspot). Repo: https://github.com/Ajithzx123/ZedPad (public; office account `Ajith-suktha` is a collaborator). CI green at c111085.
- Phase 2 (committed in 1855cee; CI fixes in 6aef39f and d2717c9: guarded debug request logging, .NET SDK pinned with `global.json`):
  - Core: `PairingTokenService` (8 chars, CSPRNG, 2 min, single use, constant-time compare, injectable clock), `PairingLink`, `ControlMessages` (parse + outgoing JSON).
  - Server: connection must pair within 10 s; unpaired data closes the connection; wrong/expired token gets `pairReject` after 500 ms then closes; version mismatch does not burn the token. Console prints a QR code plus a typed code; press R for a new code. Virtual adapters (vEthernet, Hyper-V, WSL...) are left out of the advertised addresses. Request logging moved to Debug level; Microsoft logs filtered to Warning.
  - App: `ConnectionController.pair(hosts, port, token)` tries each host (3 s timeout) then pairs (5 s); statuses Disconnected, Connecting, Pairing, Connected, Failed; QR scan screen (`mobile_scanner`, CAMERA permission) and manual entry (address, port, code); clear messages for wrong/expired code.
  - Docs: `protocol.md` pairing rules and link format; shared vectors in `docs/pairing-link-vectors.json`.
- Gates green on Dell: .NET 46 tests, Flutter 29 tests, format + analyze clean.

## Known limits
- Console QR assumes a dark terminal background (light modules drawn as filled blocks). A light-theme console shows an inverted code; use the typed code instead.
- The server issues one token at a time and a single slot (multi-phone comes in Phase 7). No reconnect yet (Phase 5).
- Port 8765 is also used by another program (`PhoneBoxAgent`) on `127.0.0.1` on the Dell; consider a less common port.
- Mac build/test not yet verified; `flutter doctor` reports Android licenses not accepted on the Dell.

## Lessons (Windows networking)
- Phone connect timing out while ping works = Windows Firewall dropping TCP. A mobile hotspot is a **Public** network. Dismissing the firewall popup creates **Block** rules for the exe that override allow rules.
- Fix (admin PowerShell): allow rule `New-NetFirewallRule -DisplayName "ZedPad" -Direction Inbound -Protocol TCP -LocalPort 8765 -Action Allow -Profile Any`, and remove stale blocks: `Get-NetFirewallRule -DisplayName "ZedPad.Server" | Where-Object { $_.Direction -eq 'Inbound' -and $_.Action -eq 'Block' } | Remove-NetFirewallRule`.
- Non-admin sessions get "Access is denied" from `Get-NetFirewallRule`; an empty result there does not mean no rule exists.
- adb wireless debugging port changes between sessions; use `flutter devices`.

## How to test pairing
1. PC: in a real terminal (so R works) `cd server`, `dotnet run --project ZedPad.Server`. A QR and a code like `ABCD-EFGH` appear (valid 2 minutes; press R for a new one).
2. Phone on the same network or PC hotspot: run the app from `app/`, tap Scan QR code, scan the console. Or enter address, port 8765 and the code manually.
3. Phone shows Connected; server logs `Device paired`. Ping and Send test state still work.
4. Try a wrong code (expect "Wrong pairing code") and reuse of the same code (expect rejection).

## Machine notes
- Dell (Windows): Flutter at `D:\DevTools\Flutter`. .NET SDK at `C:\Program Files\dotnet`. Admin PowerShell needed only for firewall rules.
- Mac: install Flutter 3.44.1, .NET 8 SDK, Android Studio/SDK; clone repo; whole solution builds there.

## Session log
- 2026-11-20 (Dell): planning, Phase 0 scaffold, Phase 1 code and real-phone test, CI fix, Phase 2 code complete with gates green.
- 2026-11-21 (Dell): committed Phase 2, fixed CI build (SDK pin); corrected stale STATUS wording.
