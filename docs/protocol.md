# ZedPad Protocol v1

Single source of truth. Dart (`app/lib/core/protocol`) and C# (`ZedPad.Core/Protocol`) must both match this file and pass the shared test vectors below. All multi-byte integers are **little-endian**.

## Transport
WebSocket (`ws://<pc-ip>:<port>/`). Control messages are **text** frames (JSON). Controller state is a **binary** frame. Max message size: 1024 bytes; larger messages close the connection.

## Control messages (text, JSON)
| Message | Direction | Fields |
|---|---|---|
| `pair` | phone to PC | `token`, `deviceName`, `version` |
| `pairAck` | PC to phone | `sessionId`, `slot`, `version` |
| `pairReject` | PC to phone | `reason` (`badToken`, `expired`, `version`, `busy`) |
| `ping` / `pong` | both | `t` (sender timestamp, ms) |

Tokens are single-use and expire after ~2 minutes. Until `pairAck`, any binary frame is rejected. Version negotiation: the PC replies with the highest version both support, or `pairReject(version)`.

## GamepadState (binary, 16 bytes)
| Offset | Size | Field | Type |
|---|---|---|---|
| 0 | 1 | Message type, always `0x30` | u8 |
| 1 | 1 | Protocol version, `1` | u8 |
| 2 | 2 | Sequence | u16 |
| 4 | 2 | Left X | i16 |
| 6 | 2 | Left Y | i16 |
| 8 | 2 | Right X | i16 |
| 10 | 2 | Right Y | i16 |
| 12 | 2 | Buttons (bitmask) | u16 |
| 14 | 1 | Left trigger | u8 |
| 15 | 1 | Right trigger | u8 |

Axes: -32768..32767, Y positive = up. Receivers clamp; they never trust values.

### Buttons bitmask (logical)
Bit 0 A, 1 B, 2 X, 3 Y, 4 LB, 5 RB, 6 Start, 7 Back, 8 L3, 9 R3, 10 DPadUp, 11 DPadDown, 12 DPadLeft, 13 DPadRight, 14-15 reserved (must be 0). Football actions (pass, shoot, ...) are logical buttons; the game profile decides the PC key.

### Sequence
u16 with wraparound. A frame is **stale** if `(int16)(seq - lastSeq) <= 0`; stale frames are dropped. Sequence resets on (re)connect.

### Validation (receiver)
Drop the frame (do not close) when: length != 16, type != 0x30, version unsupported, stale, reserved bits set. Frames are rate limited to 120/s per connection; sustained excess closes the connection.

## Timing
Phone sends state up to 60 Hz and on change; heartbeat `ping` every ~1 s. The PC watchdog releases all inputs if no **valid controller frame** arrives for 500-1000 ms.

## Test vectors (both implementations must encode/decode these)
| Name | Fields | Hex |
|---|---|---|
| Neutral | seq 0, all zero | `30 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00` |
| Full right, A pressed | seq 1, LX 32767, buttons 0x0001 | `30 01 01 00 FF 7F 00 00 00 00 00 00 01 00 00 00` |
| Mixed | seq 258, LX -32768, LY 100, RX 0, RY -1, buttons 0x2400, LT 255, RT 7 | `30 01 02 01 00 80 64 00 00 00 FF FF 00 24 FF 07` |

The same vectors are in `docs/protocol-vectors.json`; the C# and Dart tests load that file, so edit both together.

