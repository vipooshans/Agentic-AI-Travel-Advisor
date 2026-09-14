import 'package:flutter/material.dart';

void main() {
  runApp(const TravelAdvisorApp());
}

class TravelAdvisorApp extends StatelessWidget {
  const TravelAdvisorApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Travel Advisor',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
        useMaterial3: true,
      ),
      home: const Scaffold(
        body: Center(
          child: Text('Agentic AI Travel Advisor'),
        ),
      ),
    );
  }
}
