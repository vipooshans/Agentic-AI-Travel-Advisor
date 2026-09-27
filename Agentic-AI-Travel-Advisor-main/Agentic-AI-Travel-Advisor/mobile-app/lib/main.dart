import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'providers/auth_provider.dart';
import 'routes/app_router.dart';
import 'services/auth_service.dart';

void main() {
  final authProvider = AuthProvider(AuthService());
  final router = createAppRouter(authProvider);

  runApp(
    ChangeNotifierProvider.value(
      value: authProvider,
      child: TravelAdvisorApp(router: router),
    ),
  );
}
