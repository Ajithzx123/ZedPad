import 'dart:convert';

import 'gamepad_state.dart';

/// Text (JSON) control messages from docs/protocol.md.
sealed class ControlMessage {
  const ControlMessage();

  /// Returns null if [text] is not a JSON object with a string `type`.
  static ControlMessage? parse(String text) {
    final Object? decoded;
    try {
      decoded = jsonDecode(text);
    } on FormatException {
      return null;
    }
    if (decoded is! Map<String, dynamic>) return null;

    return switch (decoded['type']) {
      'pairAck' => PairAck(
        sessionId: decoded['sessionId'] as String? ?? '',
        slot: decoded['slot'] as int? ?? 0,
      ),
      'pairReject' => PairReject(decoded['reason'] as String? ?? 'unknown'),
      'pong' => Pong(decoded['t'] as int? ?? 0),
      String() => const UnknownMessage(),
      _ => null,
    };
  }
}

class PairAck extends ControlMessage {
  const PairAck({required this.sessionId, required this.slot});

  final String sessionId;
  final int slot;
}

class PairReject extends ControlMessage {
  const PairReject(this.reason);

  final String reason;
}

class Pong extends ControlMessage {
  const Pong(this.timestamp);

  final int timestamp;
}

class UnknownMessage extends ControlMessage {
  const UnknownMessage();
}

String pairRequestMessage({required String token, required String deviceName}) {
  return jsonEncode({
    'type': 'pair',
    'token': token,
    'deviceName': deviceName,
    'version': ProtocolInfo.version,
  });
}

String pingMessage(int timestamp) {
  return jsonEncode({'type': 'ping', 't': timestamp});
}
