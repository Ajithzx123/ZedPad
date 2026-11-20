/// The URI encoded in the pairing QR code (see docs/protocol.md).
class PairingLink {
  const PairingLink({
    required this.hosts,
    required this.port,
    required this.token,
  });

  final List<String> hosts;
  final int port;
  final String token;

  /// Returns null unless [text] is a complete `zedpad://pair` link.
  static PairingLink? parse(String text) {
    final uri = Uri.tryParse(text.trim());
    if (uri == null || uri.scheme != 'zedpad' || uri.host != 'pair') {
      return null;
    }
    final hosts = uri.queryParametersAll['h'] ?? const <String>[];
    final port = int.tryParse(uri.queryParameters['p'] ?? '');
    final token = uri.queryParameters['t'];
    if (hosts.isEmpty ||
        hosts.any((h) => h.isEmpty) ||
        port == null ||
        port < 1 ||
        port > 65535 ||
        token == null ||
        token.isEmpty) {
      return null;
    }
    return PairingLink(hosts: hosts, port: port, token: token);
  }
}
