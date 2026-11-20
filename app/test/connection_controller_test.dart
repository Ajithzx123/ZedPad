import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:zedpad/core/networking/connection_controller.dart';
import 'package:zedpad/core/protocol/gamepad_state.dart';

void main() {
  late HttpServer server;
  late List<Uint8List> receivedFrames;

  setUp(() async {
    receivedFrames = [];
    server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    server.listen((request) async {
      final socket = await WebSocketTransformer.upgrade(request);
      socket.listen((message) {
        if (message is String) {
          socket.add(message.replaceFirst('"ping"', '"pong"'));
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

  test(
    'connects, round-trips a ping and sends a decodable state frame',
    () async {
      final controller = ConnectionController();
      addTearDown(controller.dispose);

      await controller.connect('127.0.0.1', server.port);
      expect(controller.status, ConnectionStatus.connected);

      controller.sendPing();
      await waitFor(() => controller.log.any((l) => l.contains('pong')));
      expect(
        controller.log.any((l) => l.startsWith('<-') && l.contains('pong')),
        isTrue,
      );

      controller.sendTestState();
      await waitFor(() => receivedFrames.isNotEmpty);
      final state = GamepadState.decode(receivedFrames.single);
      expect(state, isNotNull);
      expect(state!.sequence, 1);
      expect(state.leftX, 32767);
      expect(state.buttons, 1);

      await controller.disconnect();
      expect(controller.status, ConnectionStatus.disconnected);
    },
  );

  test('reports failure when nothing is listening', () async {
    final port = server.port;
    await server.close(force: true);
    final controller = ConnectionController();
    addTearDown(controller.dispose);

    await controller.connect('127.0.0.1', port);

    expect(controller.status, ConnectionStatus.failed);
    expect(controller.error, isNotNull);
  });

  test('does not send while disconnected', () async {
    final controller = ConnectionController();
    addTearDown(controller.dispose);

    controller.sendPing();
    controller.sendTestState();

    expect(controller.log, isEmpty);
    expect(controller.status, ConnectionStatus.disconnected);
  });
}
