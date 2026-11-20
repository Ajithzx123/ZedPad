import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:zedpad/core/protocol/control_messages.dart';

void main() {
  test('parses pairAck', () {
    final message = ControlMessage.parse(
      '{"type":"pairAck","sessionId":"abc","slot":1,"version":1}',
    );

    expect(message, isA<PairAck>());
    expect((message! as PairAck).sessionId, 'abc');
    expect((message as PairAck).slot, 1);
  });

  test('parses pairReject and pong', () {
    final reject = ControlMessage.parse(
      '{"type":"pairReject","reason":"expired"}',
    );
    final pong = ControlMessage.parse('{"type":"pong","t":7}');

    expect((reject! as PairReject).reason, 'expired');
    expect((pong! as Pong).timestamp, 7);
  });

  test('unknown types are reported, non-messages are null', () {
    expect(ControlMessage.parse('{"type":"dance"}'), isA<UnknownMessage>());
    expect(ControlMessage.parse('not json'), isNull);
    expect(ControlMessage.parse('[1]'), isNull);
    expect(ControlMessage.parse('{"noType":true}'), isNull);
  });

  test('builds the documented outgoing messages', () {
    final pair =
        jsonDecode(pairRequestMessage(token: 'ABCD2345', deviceName: 'Pixel'))
            as Map<String, dynamic>;

    expect(pair, {
      'type': 'pair',
      'token': 'ABCD2345',
      'deviceName': 'Pixel',
      'version': 1,
    });
    expect(jsonDecode(pingMessage(42)), {'type': 'ping', 't': 42});
  });
}
