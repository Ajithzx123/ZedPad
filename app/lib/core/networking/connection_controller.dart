import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

import '../protocol/control_messages.dart';
import '../protocol/gamepad_state.dart';

enum ConnectionStatus { disconnected, connecting, pairing, connected, failed }

/// Owns the WebSocket to the PC receiver and exposes its state to the UI.
class ConnectionController extends ChangeNotifier {
  static const Duration _connectTimeout = Duration(seconds: 3);
  static const Duration _pairTimeout = Duration(seconds: 5);
  static const int _maxLogLines = 50;
  static const int _buttonA = 0x0001;
  static const String _deviceName = 'ZedPad Phone';

  ConnectionStatus _status = ConnectionStatus.disconnected;
  String? _error;
  final List<String> _log = [];
  WebSocketChannel? _channel;
  StreamSubscription<dynamic>? _subscription;
  Completer<ControlMessage>? _pairReply;
  int _sequence = 0;

  ConnectionStatus get status => _status;
  String? get error => _error;
  List<String> get log => List.unmodifiable(_log);

  /// Connects to the first reachable host and pairs with [token].
  Future<void> pair({
    required List<String> hosts,
    required int port,
    required String token,
  }) async {
    if (_status == ConnectionStatus.connecting ||
        _status == ConnectionStatus.pairing ||
        _status == ConnectionStatus.connected) {
      return;
    }
    _error = null;
    _sequence = 0;
    _setStatus(ConnectionStatus.connecting);

    for (final host in hosts) {
      _addLog('Trying $host:$port');
      final channel = await _open(host, port);
      if (channel != null) {
        await _pairOver(channel, token);
        return;
      }
    }
    _fail(
      'Could not reach the PC at ${hosts.join(', ')} (port $port). '
      'Check that both are on the same network and the firewall allows it.',
    );
  }

  Future<void> disconnect() async {
    await _detach();
    if (_status != ConnectionStatus.disconnected) {
      _addLog('Disconnected');
      _setStatus(ConnectionStatus.disconnected);
    }
  }

  void sendPing() {
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    if (_send(pingMessage(timestamp))) {
      _addLog('-> ping');
    }
  }

  /// Sends one state frame with the A button held, to prove the binary path.
  void sendTestState() {
    final state = GamepadState(
      sequence: _nextSequence(),
      leftX: 32767,
      buttons: _buttonA,
    );
    if (_send(state.encode())) {
      _addLog('-> $state');
    }
  }

  Future<WebSocketChannel?> _open(String host, int port) async {
    final channel = WebSocketChannel.connect(
      Uri(scheme: 'ws', host: host, port: port),
    );
    try {
      await channel.ready.timeout(_connectTimeout);
      return channel;
    } catch (e) {
      _addLog('$host unreachable: $e');
      unawaited(channel.sink.close().catchError((Object _) {}));
      return null;
    }
  }

  Future<void> _pairOver(WebSocketChannel channel, String token) async {
    final reply = Completer<ControlMessage>();
    _pairReply = reply;
    _channel = channel;
    _setStatus(ConnectionStatus.pairing);

    _subscription = channel.stream.listen(
      _onMessage,
      onError: (Object e) {
        if (!reply.isCompleted) reply.completeError(e);
        _closed('Connection error: $e');
      },
      onDone: () {
        if (!reply.isCompleted) reply.completeError('connection closed');
        _closed(null);
      },
    );
    channel.sink.add(pairRequestMessage(token: token, deviceName: _deviceName));

    try {
      final message = await reply.future.timeout(_pairTimeout);
      _pairReply = null;
      switch (message) {
        case PairAck():
          _addLog('Paired (slot ${message.slot})');
          _setStatus(ConnectionStatus.connected);
        case PairReject():
          await _detach();
          _fail(_describeRejection(message.reason));
        default:
          await _detach();
          _fail('Unexpected reply from the PC.');
      }
    } catch (e) {
      _pairReply = null;
      await _detach();
      _fail('Pairing failed: $e');
    }
  }

  String _describeRejection(String reason) {
    return switch (reason) {
      'badToken' => 'Wrong pairing code. Press R on the PC for a new one.',
      'expired' => 'The pairing code expired. Press R on the PC for a new one.',
      'version' => 'Version mismatch. Update the app or the PC server.',
      'busy' => 'The PC is busy with another device.',
      _ => 'Pairing was rejected ($reason).',
    };
  }

  int _nextSequence() {
    _sequence = (_sequence + 1) & 0xFFFF;
    return _sequence;
  }

  bool _send(Object data) {
    final channel = _channel;
    if (_status != ConnectionStatus.connected || channel == null) return false;
    channel.sink.add(data);
    return true;
  }

  void _onMessage(dynamic message) {
    if (message is! String) return;
    final parsed = ControlMessage.parse(message);
    final pending = _pairReply;
    if (_status == ConnectionStatus.pairing &&
        pending != null &&
        !pending.isCompleted &&
        (parsed is PairAck || parsed is PairReject)) {
      pending.complete(parsed!);
      return;
    }
    if (parsed is Pong) {
      _addLog('<- pong (${parsed.timestamp})');
    } else {
      _addLog('<- $message');
    }
  }

  /// The server ended an established connection.
  void _closed(String? error) {
    if (_status != ConnectionStatus.connected) return;
    _channel = null;
    _subscription = null;
    if (error == null) {
      _addLog('Server closed the connection');
      _setStatus(ConnectionStatus.disconnected);
    } else {
      _fail(error);
    }
  }

  Future<void> _detach() async {
    final channel = _channel;
    final subscription = _subscription;
    _channel = null;
    _subscription = null;
    await subscription?.cancel();
    await channel?.sink.close();
  }

  void _fail(String message) {
    _error = message;
    _addLog('ERROR: $message');
    _setStatus(ConnectionStatus.failed);
  }

  void _setStatus(ConnectionStatus status) {
    _status = status;
    notifyListeners();
  }

  void _addLog(String line) {
    _log.add(line);
    if (_log.length > _maxLogLines) _log.removeAt(0);
    notifyListeners();
  }

  @override
  void dispose() {
    unawaited(_subscription?.cancel());
    unawaited(_channel?.sink.close());
    super.dispose();
  }
}
