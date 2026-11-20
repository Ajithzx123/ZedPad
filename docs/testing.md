# Testing

| Layer | What |
|---|---|
| Unit | Protocol codec (shared vectors), stick normalization, deadzone/hysteresis, gesture classification, button mapping, key state transitions, token expiry |
| Integration | WebSocket pairing, disconnect, release-on-timeout, malformed/oversized frames |
| Manual | Notepad key test, PES 2000 playtest, multiple Android devices |
| Performance | Timestamped frames, PC-side latency log, target < 30 ms on 5 GHz Wi-Fi |

Never weaken or delete a test to make a build pass.
