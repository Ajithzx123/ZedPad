# 0001 - Stack and transport

Date: 2026-11-20. Status: accepted.

- **Receiver in C#/.NET 8**: first-class SendInput and ViGEm client libraries.
- **Flutter + Provider** for the Android app.
- **WebSocket over LAN** with binary state frames: ordered, simple, enough for 60 Hz on local Wi-Fi. UDP deferred unless measured latency requires it.
- **Keyboard first** (SendInput scan codes) for PES 2000; ViGEm virtual pad in Phase 6 with keyboard fallback.
- **Console receiver UI** until Phase 7.
- **Windows-only code isolated** in `ZedPad.Input` so the Mac can build and test the rest.
