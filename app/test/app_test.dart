import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zedpad/main.dart';

void main() {
  testWidgets('shows the connect screen disconnected', (tester) async {
    await tester.pumpWidget(const ZedPadApp());

    expect(find.text('Disconnected'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Connect'), findsOneWidget);
    expect(
      tester
          .widget<OutlinedButton>(find.widgetWithText(OutlinedButton, 'Ping'))
          .onPressed,
      isNull,
    );
  });
}
