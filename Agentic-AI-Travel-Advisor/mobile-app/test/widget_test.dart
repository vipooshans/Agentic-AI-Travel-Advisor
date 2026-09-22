import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:travel_advisor/providers/auth_provider.dart';
import 'package:travel_advisor/screens/login_screen.dart';
import 'package:travel_advisor/services/auth_service.dart';
import 'package:travel_advisor/widgets/empty_state_widget.dart';

void main() {
  testWidgets('empty state shows title and action', (tester) async {
    var tapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: EmptyStateWidget(
          icon: Icons.bookmark_border,
          title: 'No bookings yet',
          message: 'Explore destinations and book a trip.',
          actionLabel: 'Explore',
          onAction: () => tapped = true,
        ),
      ),
    );

    expect(find.text('No bookings yet'), findsOneWidget);
    await tester.tap(find.text('Explore'));
    expect(tapped, isTrue);
  });

  testWidgets('login requires email and password', (tester) async {
    final auth = AuthProvider(AuthService());
    await tester.pumpWidget(
      ChangeNotifierProvider.value(
        value: auth,
        child: const MaterialApp(home: LoginScreen()),
      ),
    );

    await tester.tap(find.text('Sign In'));
    await tester.pump();

    expect(find.text('Email is required'), findsOneWidget);
    expect(find.text('Password is required'), findsOneWidget);
  });
}
