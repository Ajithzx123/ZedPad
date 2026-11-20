import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/networking/connection_controller.dart';
import '../../core/protocol/pairing_link.dart';
import 'scan_screen.dart';

/// Pairs with the PC by QR code, or by typing its address and pairing code.
class ConnectScreen extends StatefulWidget {
  const ConnectScreen({super.key});

  @override
  State<ConnectScreen> createState() => _ConnectScreenState();
}

class _ConnectScreenState extends State<ConnectScreen> {
  final _host = TextEditingController();
  final _port = TextEditingController(text: '8765');
  final _code = TextEditingController();

  @override
  void dispose() {
    _host.dispose();
    _port.dispose();
    _code.dispose();
    super.dispose();
  }

  Future<void> _scan(ConnectionController controller) async {
    final link = await Navigator.of(
      context,
    ).push<PairingLink>(MaterialPageRoute(builder: (_) => const ScanScreen()));
    if (link == null) return;
    await controller.pair(
      hosts: link.hosts,
      port: link.port,
      token: link.token,
    );
  }

  Future<void> _connectManually(ConnectionController controller) async {
    final port = int.tryParse(_port.text.trim());
    final host = _host.text.trim();
    final code = _code.text.trim();
    if (host.isEmpty || port == null || code.isEmpty) return;
    await controller.pair(hosts: [host], port: port, token: code);
  }

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<ConnectionController>();
    final connected = controller.status == ConnectionStatus.connected;
    final busy =
        controller.status == ConnectionStatus.connecting ||
        controller.status == ConnectionStatus.pairing;

    return Scaffold(
      appBar: AppBar(title: const Text('ZedPad')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _StatusLine(controller: controller),
          const SizedBox(height: 12),
          if (!connected) ...[
            FilledButton.icon(
              onPressed: busy ? null : () => _scan(controller),
              icon: const Icon(Icons.qr_code_scanner),
              label: const Text('Scan QR code'),
            ),
            const SizedBox(height: 20),
            const Text('Or enter the details shown on the PC'),
            const SizedBox(height: 8),
            _ManualFields(
              host: _host,
              port: _port,
              code: _code,
              enabled: !busy,
            ),
            const SizedBox(height: 12),
            OutlinedButton(
              onPressed: busy ? null : () => _connectManually(controller),
              child: const Text('Connect'),
            ),
          ] else
            _ConnectedActions(controller: controller),
          const SizedBox(height: 16),
          SizedBox(height: 240, child: _LogView(lines: controller.log)),
        ],
      ),
    );
  }
}

class _ManualFields extends StatelessWidget {
  const _ManualFields({
    required this.host,
    required this.port,
    required this.code,
    required this.enabled,
  });

  final TextEditingController host;
  final TextEditingController port;
  final TextEditingController code;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Row(
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
        ),
        const SizedBox(height: 12),
        TextField(
          controller: code,
          enabled: enabled,
          textCapitalization: TextCapitalization.characters,
          decoration: const InputDecoration(
            labelText: 'Pairing code',
            hintText: 'ABCD-EFGH',
            border: OutlineInputBorder(),
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
      ConnectionStatus.pairing => 'Pairing...',
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

class _ConnectedActions extends StatelessWidget {
  const _ConnectedActions({required this.controller});

  final ConnectionController controller;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        OutlinedButton(
          onPressed: controller.disconnect,
          child: const Text('Disconnect'),
        ),
        OutlinedButton(
          onPressed: controller.sendPing,
          child: const Text('Ping'),
        ),
        OutlinedButton(
          onPressed: controller.sendTestState,
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
