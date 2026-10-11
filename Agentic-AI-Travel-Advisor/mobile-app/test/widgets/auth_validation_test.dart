import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:travel_advisor/providers/auth_provider.dart';
import 'package:travel_advisor/screens/login_screen.dart';
import 'package:travel_advisor/services/auth_service.dart';

void main() {
  testWidgets('LoginScreen shows validation errors for invalid inputs', (tester) async {
    final auth = AuthProvider(AuthService());
    await tester.pumpWidget(
      ChangeNotifierProvider.value(
        value: auth,
        child: const MaterialApp(home: LoginScreen()),
      ),
    );

    // Enter invalid email
    final emailField = find.widgetWithText(TextFormField, 'Email');
    await tester.enterText(emailField, 'invalid-email-format');

    // Tap sign in
    await tester.tap(find.text('Sign In'));
    await tester.pump();

    // Verify validation error
    expect(find.text('Enter a valid email address'), findsOneWidget);
    expect(find.text('Password is required'), findsOneWidget);
  });
}
