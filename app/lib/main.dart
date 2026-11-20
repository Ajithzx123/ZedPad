import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/networking/connection_controller.dart';
import 'features/pairing/connect_screen.dart';

void main() => runApp(const ZedPadApp());

class ZedPadApp extends StatelessWidget {
  const ZedPadApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (_) => ConnectionController(),
      child: MaterialApp(
        title: 'ZedPad',
        theme: ThemeData(colorSchemeSeed: Colors.green, useMaterial3: true),
        home: const ConnectScreen(),
      ),
    );
  }
}
