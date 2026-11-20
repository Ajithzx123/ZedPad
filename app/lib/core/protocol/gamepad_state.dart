import 'dart:typed_data';

/// Constants from docs/protocol.md (v1).
abstract final class ProtocolInfo {
  static const int gamepadStateType = 0x30;
  static const int version = 1;
  static const int gamepadStateLength = 16;
}

/// One controller state snapshot (see docs/protocol.md, GamepadState).
class GamepadState {
  const GamepadState({
    required this.sequence,
    this.leftX = 0,
    this.leftY = 0,
    this.rightX = 0,
    this.rightY = 0,
    this.buttons = 0,
    this.leftTrigger = 0,
    this.rightTrigger = 0,
  });

  final int sequence;
  final int leftX;
  final int leftY;
  final int rightX;
  final int rightY;
  final int buttons;
  final int leftTrigger;
  final int rightTrigger;

  Uint8List encode() {
    final data = ByteData(ProtocolInfo.gamepadStateLength)
      ..setUint8(0, ProtocolInfo.gamepadStateType)
      ..setUint8(1, ProtocolInfo.version)
      ..setUint16(2, sequence, Endian.little)
      ..setInt16(4, leftX, Endian.little)
      ..setInt16(6, leftY, Endian.little)
      ..setInt16(8, rightX, Endian.little)
      ..setInt16(10, rightY, Endian.little)
      ..setUint16(12, buttons, Endian.little)
      ..setUint8(14, leftTrigger)
      ..setUint8(15, rightTrigger);
    return data.buffer.asUint8List();
  }

  /// Returns null for any frame the protocol says to drop.
  static GamepadState? decode(Uint8List frame) {
    if (frame.length != ProtocolInfo.gamepadStateLength ||
        frame[0] != ProtocolInfo.gamepadStateType ||
        frame[1] != ProtocolInfo.version) {
      return null;
    }
    final data = ByteData.sublistView(frame);
    final buttons = data.getUint16(12, Endian.little);
    if (buttons & 0xC000 != 0) return null;
    return GamepadState(
      sequence: data.getUint16(2, Endian.little),
      leftX: data.getInt16(4, Endian.little),
      leftY: data.getInt16(6, Endian.little),
      rightX: data.getInt16(8, Endian.little),
      rightY: data.getInt16(10, Endian.little),
      buttons: buttons,
      leftTrigger: frame[14],
      rightTrigger: frame[15],
    );
  }

  @override
  bool operator ==(Object other) =>
      other is GamepadState &&
      other.sequence == sequence &&
      other.leftX == leftX &&
      other.leftY == leftY &&
      other.rightX == rightX &&
      other.rightY == rightY &&
      other.buttons == buttons &&
      other.leftTrigger == leftTrigger &&
      other.rightTrigger == rightTrigger;

  @override
  int get hashCode => Object.hash(
    sequence,
    leftX,
    leftY,
    rightX,
    rightY,
    buttons,
    leftTrigger,
    rightTrigger,
  );

  @override
  String toString() =>
      'GamepadState(seq: $sequence, L: ($leftX, $leftY), '
      'R: ($rightX, $rightY), buttons: 0x${buttons.toRadixString(16)})';
}
