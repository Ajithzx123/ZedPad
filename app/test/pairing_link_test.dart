import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:zedpad/core/protocol/pairing_link.dart';

void main() {
  final vectors =
      (jsonDecode(File('../docs/pairing-link-vectors.json').readAsStringSync())
              as List<dynamic>)
          .cast<Map<String, dynamic>>();

  for (final v in vectors) {
    test('parses shared vector: ${v['name']}', () {
      final link = PairingLink.parse(v['uri'] as String);

      expect(link, isNotNull);
      expect(link!.hosts, (v['hosts'] as List<dynamic>).cast<String>());
      expect(link.port, v['port']);
      expect(link.token, v['token']);
    });
  }

  group('parse rejects', () {
    for (final bad in [
      '',
      'hello',
      'https://pair?h=1.2.3.4&p=8765&t=ABCD2345',
      'zedpad://other?h=1.2.3.4&p=8765&t=ABCD2345',
      'zedpad://pair?p=8765&t=ABCD2345',
      'zedpad://pair?h=1.2.3.4&t=ABCD2345',
      'zedpad://pair?h=1.2.3.4&p=abc&t=ABCD2345',
      'zedpad://pair?h=1.2.3.4&p=70000&t=ABCD2345',
      'zedpad://pair?h=1.2.3.4&p=8765',
      'zedpad://pair?h=&p=8765&t=ABCD2345',
    ]) {
      test('"$bad"', () => expect(PairingLink.parse(bad), isNull));
    }
  });
}
