import 'package:flutter_test/flutter_test.dart';
import 'package:zedpad/main.dart';

void main() {
  testWidgets('app starts', (tester) async {
    await tester.pumpWidget(const ZedPadApp());
    expect(find.text('ZedPad'), findsOneWidget);
  });
}
