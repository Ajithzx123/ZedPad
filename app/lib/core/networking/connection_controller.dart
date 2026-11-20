import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

import '../protocol/gamepad_state.dart';

enum ConnectionStatus { disconnected, connecting, connected, failed }

/// Owns the WebSocket to the PC receiver and exposes its state to the UI.
class ConnectionController extends ChangeNotifier {
  static const int _maxLogLines = 50;
  static const int _buttonA = 0x0001;

  ConnectionStatus _status = ConnectionStatus.disconnected;
  String? _error;
  final List<String> _log = [];
  WebSocketChannel? _channel;
  StreamSubscription<dynamic>? _subscription;
  int _sequence = 0;

  ConnectionStatus get status => _status;
  String? get error => _error;
  List<String> get log => List.unmodifiable(_log);

  Future<void> connect(String host, int port) async {
    if (_status == ConnectionStatus.connecting ||
        _status == ConnectionStatus.connected) {
      return;
    }
    _setStatus(ConnectionStatus.connecting);
    _error = null;
    _sequence = 0;

    final channel = WebSocketChannel.connect(
      Uri(scheme: 'ws', host: host, port: port),
    );
    try {
      await channel.ready;
    } catch (e) {
      _error = 'Could not connect to $host:$port ($e)';
      _setStatus(ConnectionStatus.failed);
      return;
    }

    _channel = channel;
    _subscription = channel.stream.listen(
      _onMessage,
      onError: (Object e) => _closed('Connection error: $e'),
      onDone: () => _closed(null),
    );
    _addLog('Connected to $host:$port');
    _setStatus(ConnectionStatus.connected);
  }

  Future<void> disconnect() async {
    final channel = _channel;
    _channel = null;
    await _subscription?.cancel();
    _subscription = null;
    await channel?.sink.close();
    if (_status != ConnectionStatus.disconnected) {
      _addLog('Disconnected');
      _setStatus(ConnectionStatus.disconnected);
    }
  }

  void sendPing() {
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    if (_send(jsonEncode({'type': 'ping', 't': timestamp}))) {
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
    if (message is String) _addLog('<- $message');
  }

  void _closed(String? error) {
    _channel = null;
    _subscription = null;
    _error = error;
    _addLog(error ?? 'Server closed the connection');
    _setStatus(
      error == null ? ConnectionStatus.disconnected : ConnectionStatus.failed,
    );
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
