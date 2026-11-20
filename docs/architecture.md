# Architecture

```
Phone (Flutter)                       PC (.NET 8)
 touch/gyro -> logical state  --WS-->  ZedPad.Server (transport, pairing, sessions)
                                           |
                                      ZedPad.Core (protocol, state diff, mapping, profiles)
                                           |
                                      IInputSink
                                      /          \
                          ZedPad.Input Keyboard   ZedPad.Input Gamepad (ViGEm, later)
```

## Projects
- `ZedPad.Core`: protocol codec, `ControllerState`, stick-to-key mapping (deadzone + hysteresis), profile model, `IInputSink`. No Windows or WebSocket dependencies; builds on macOS.
- `ZedPad.Input`: Windows input sinks (SendInput with scan codes, ViGEm later). Targets plain `net8.0`; Windows API calls are guarded at runtime so the solution still builds on macOS.
- `ZedPad.Server`: Kestrel WebSocket transport, pairing, sessions, console UI.
- `ZedPad.Tests`: unit + integration tests for Core and Server.

## Principles
- State, not events: the phone sends current state; the server diffs against the previous state.
- Flutter emits logical actions; `profiles/*.json` maps actions to keys. Nothing game-specific in networking.
- Release all inputs on disconnect, watchdog timeout, shutdown.
- Protocol is transport-agnostic; `docs/protocol.md` is the source of truth.

