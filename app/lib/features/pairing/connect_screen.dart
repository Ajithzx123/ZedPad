import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/networking/connection_controller.dart';

/// Phase 1 spike: manual IP:port entry. Becomes the pairing screen in Phase 2.
class ConnectScreen extends StatefulWidget {
  const ConnectScreen({super.key});

  @override
  State<ConnectScreen> createState() => _ConnectScreenState();
}

class _ConnectScreenState extends State<ConnectScreen> {
  final _host = TextEditingController();
  final _port = TextEditingController(text: '8765');

  @override
  void dispose() {
    _host.dispose();
    _port.dispose();
    super.dispose();
  }

  void _connect(ConnectionController controller) {
    final port = int.tryParse(_port.text.trim());
    final host = _host.text.trim();
    if (host.isEmpty || port == null) return;
    controller.connect(host, port);
  }

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<ConnectionController>();
    final connected = controller.status == ConnectionStatus.connected;
    final busy = controller.status == ConnectionStatus.connecting;

    return Scaffold(
      appBar: AppBar(title: const Text('ZedPad')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _AddressFields(host: _host, port: _port, enabled: !connected),
            const SizedBox(height: 12),
            _StatusLine(controller: controller),
            const SizedBox(height: 12),
            _Actions(
              connected: connected,
              busy: busy,
              onConnect: () => _connect(controller),
              onDisconnect: controller.disconnect,
              onPing: controller.sendPing,
              onState: controller.sendTestState,
            ),
            const SizedBox(height: 12),
            Expanded(child: _LogView(lines: controller.log)),
          ],
        ),
      ),
    );
  }
}

class _AddressFields extends StatelessWidget {
  const _AddressFields({
    required this.host,
    required this.port,
    required this.enabled,
  });

  final TextEditingController host;
  final TextEditingController port;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          flex: 3,
          child: TextField(
            controller: host,
            enabled: enabled,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: 'PC address',
              hintText: '192.168.1.20',
              border: OutlineInputBorder(),
            ),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: TextField(
            controller: port,
            enabled: enabled,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: 'Port',
              border: OutlineInputBorder(),
            ),
          ),
        ),
      ],
    );
  }
}

class _StatusLine extends StatelessWidget {
  const _StatusLine({required this.controller});

  final ConnectionController controller;

  @override
  Widget build(BuildContext context) {
    final text = switch (controller.status) {
      ConnectionStatus.disconnected => 'Disconnected',
      ConnectionStatus.connecting => 'Connecting...',
      ConnectionStatus.connected => 'Connected',
      ConnectionStatus.failed => controller.error ?? 'Connection failed',
    };
    final isError = controller.status == ConnectionStatus.failed;
    return SelectableText(
      text,
      style: TextStyle(
        color: isError ? Theme.of(context).colorScheme.error : null,
      ),
    );
  }
}

class _Actions extends StatelessWidget {
  const _Actions({
    required this.connected,
    required this.busy,
    required this.onConnect,
    required this.onDisconnect,
    required this.onPing,
    required this.onState,
  });

  final bool connected;
  final bool busy;
  final VoidCallback onConnect;
  final VoidCallback onDisconnect;
  final VoidCallback onPing;
  final VoidCallback onState;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        if (connected)
          OutlinedButton(
            onPressed: onDisconnect,
            child: const Text('Disconnect'),
          )
        else
          FilledButton(
            onPressed: busy ? null : onConnect,
            child: const Text('Connect'),
          ),
        OutlinedButton(
          onPressed: connected ? onPing : null,
          child: const Text('Ping'),
        ),
        OutlinedButton(
          onPressed: connected ? onState : null,
          child: const Text('Send test state'),
        ),
      ],
    );
  }
}

class _LogView extends StatelessWidget {
  const _LogView({required this.lines});

  final List<String> lines;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        border: Border.all(color: Theme.of(context).dividerColor),
        borderRadius: BorderRadius.circular(8),
      ),
      child: ListView.builder(
        reverse: true,
        padding: const EdgeInsets.all(8),
        itemCount: lines.length,
        itemBuilder: (_, i) => SelectableText(
          lines[lines.length - 1 - i],
          style: const TextStyle(fontFamily: 'monospace', fontSize: 12),
        ),
      ),
    );
  }
}
