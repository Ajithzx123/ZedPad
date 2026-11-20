import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zedpad/main.dart';

void main() {
  testWidgets('shows the pairing options while disconnected', (tester) async {
    await tester.pumpWidget(const ZedPadApp());

    expect(find.text('Disconnected'), findsOneWidget);
    expect(find.text('Scan QR code'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Connect'), findsOneWidget);
    expect(find.text('Ping'), findsNothing);
  });
}
