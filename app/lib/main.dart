import 'package:flutter/material.dart';

void main() => runApp(const ZedPadApp());

class ZedPadApp extends StatelessWidget {
  const ZedPadApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'ZedPad',
      theme: ThemeData(colorSchemeSeed: Colors.green, useMaterial3: true),
      home: const Scaffold(body: Center(child: Text('ZedPad'))),
    );
  }
}
