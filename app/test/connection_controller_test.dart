import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:zedpad/core/networking/connection_controller.dart';
import 'package:zedpad/core/protocol/gamepad_state.dart';

const _validToken = 'ABCD2345';

void main() {
  late HttpServer server;
  late List<Uint8List> receivedFrames;
  late List<Map<String, dynamic>> receivedTexts;

  setUp(() async {
    receivedFrames = [];
    receivedTexts = [];
    server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    server.listen((request) async {
      final socket = await WebSocketTransformer.upgrade(request);
      socket.listen((message) {
        if (message is String) {
          final json = jsonDecode(message) as Map<String, dynamic>;
          receivedTexts.add(json);
          switch (json['type']) {
            case 'pair':
              if (json['token'] == _validToken) {
                socket.add(
                  jsonEncode({
                    'type': 'pairAck',
                    'sessionId': 'abc',
                    'slot': 1,
                    'version': 1,
                  }),
                );
              } else {
                socket.add(
                  jsonEncode({'type': 'pairReject', 'reason': 'badToken'}),
                );
                socket.close();
              }
            case 'ping':
              socket.add(jsonEncode({'type': 'pong', 't': json['t']}));
          }
        } else if (message is List<int>) {
          receivedFrames.add(Uint8List.fromList(message));
        }
      });
    });
  });

  tearDown(() => server.close(force: true));

  Future<void> waitFor(bool Function() condition) async {
    for (var i = 0; i < 100 && !condition(); i++) {
      await Future<void>.delayed(const Duration(milliseconds: 20));
    }
  }

  test('pairs, round-trips a ping and sends a decodable state frame', () async {
    final controller = ConnectionController();
    addTearDown(controller.dispose);

    await controller.pair(
      hosts: ['127.0.0.1'],
      port: server.port,
      token: _validToken,
    );
    expect(controller.status, ConnectionStatus.connected);
    expect(receivedTexts.first['type'], 'pair');
    expect(receivedTexts.first['token'], _validToken);
    expect(receivedTexts.first['version'], 1);

    controller.sendPing();
    await waitFor(() => controller.log.any((l) => l.contains('pong')));
    expect(controller.log.any((l) => l.startsWith('<- pong')), isTrue);

    controller.sendTestState();
    await waitFor(() => receivedFrames.isNotEmpty);
    final state = GamepadState.decode(receivedFrames.single);
    expect(state, isNotNull);
    expect(state!.sequence, 1);
    expect(state.leftX, 32767);
    expect(state.buttons, 1);

    await controller.disconnect();
    expect(controller.status, ConnectionStatus.disconnected);
  });

  test('a wrong pairing code fails with a clear message', () async {
    final controller = ConnectionController();
    addTearDown(controller.dispose);

    await controller.pair(
      hosts: ['127.0.0.1'],
      port: server.port,
      token: 'WRONGCODE',
    );

    expect(controller.status, ConnectionStatus.failed);
    expect(controller.error, contains('Wrong pairing code'));
  });

  test('cannot send before pairing completes', () async {
    final controller = ConnectionController();
    addTearDown(controller.dispose);

    controller.sendPing();
    controller.sendTestState();

    expect(controller.log, isEmpty);
    expect(controller.status, ConnectionStatus.disconnected);
  });

  test('reports failure when nothing is listening', () async {
    final port = server.port;
    await server.close(force: true);
    final controller = ConnectionController();
    addTearDown(controller.dispose);

    await controller.pair(hosts: ['127.0.0.1'], port: port, token: _validToken);

    expect(controller.status, ConnectionStatus.failed);
    expect(controller.error, contains('Could not reach the PC'));
  });
}
