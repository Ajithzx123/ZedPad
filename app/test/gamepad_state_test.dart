import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:zedpad/core/protocol/gamepad_state.dart';

Uint8List _bytes(String hex) => Uint8List.fromList(
  hex.split(' ').map((h) => int.parse(h, radix: 16)).toList(),
);

void main() {
  final vectors =
      (jsonDecode(File('../docs/protocol-vectors.json').readAsStringSync())
              as List<dynamic>)
          .cast<Map<String, dynamic>>();

  for (final v in vectors) {
    final state = GamepadState(
      sequence: v['sequence'] as int,
      leftX: v['leftX'] as int,
      leftY: v['leftY'] as int,
      rightX: v['rightX'] as int,
      rightY: v['rightY'] as int,
      buttons: v['buttons'] as int,
      leftTrigger: v['leftTrigger'] as int,
      rightTrigger: v['rightTrigger'] as int,
    );
    final frame = _bytes(v['hex'] as String);

    test('encodes shared vector: ${v['name']}', () {
      expect(state.encode(), frame);
    });

    test('decodes shared vector: ${v['name']}', () {
      expect(GamepadState.decode(frame), state);
    });
  }

  group('decode rejects', () {
    final good = _bytes(vectors.first['hex'] as String);

    test('wrong length', () {
      expect(GamepadState.decode(Uint8List(15)), isNull);
      expect(GamepadState.decode(Uint8List(17)), isNull);
    });

    test('wrong type, version and reserved bits', () {
      expect(GamepadState.decode(Uint8List.fromList(good)..[0] = 0x31), isNull);
      expect(GamepadState.decode(Uint8List.fromList(good)..[1] = 2), isNull);
      expect(
        GamepadState.decode(Uint8List.fromList(good)..[13] = 0x80),
        isNull,
      );
    });
  });
}
