import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'providers/auth_provider.dart';
import 'providers/chat_provider.dart';
import 'routes/app_router.dart';
import 'services/auth_service.dart';

/// Application bootstrap entry point.
/// Initializes dependency injection for authentication and conversational AI providers.
void main() {
  WidgetsFlutterBinding.ensureInitialized();
  final authProvider = AuthProvider(AuthService());
  final chatProvider = ChatProvider();
  final router = createAppRouter(authProvider);

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: authProvider),
        ChangeNotifierProvider.value(value: chatProvider),
      ],
      child: TravelAdvisorApp(router: router),
    ),
  );
}
