import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../providers/auth_provider.dart';
import '../screens/home_screen.dart';
import '../screens/login_screen.dart';
import '../screens/register_screen.dart';
import '../screens/splash_screen.dart';

GoRouter createAppRouter(AuthProvider authProvider) {
  return GoRouter(
    initialLocation: '/',
    refreshListenable: authProvider,
    redirect: (context, state) {
      final status = authProvider.status;
      final location = state.matchedLocation;

      if (status == AuthStatus.unknown) {
        return location == '/' ? null : '/';
      }

      final isAuthRoute = location == '/login' || location == '/register';
      final isSplash = location == '/';

      if (status == AuthStatus.unauthenticated) {
        if (isSplash || isAuthRoute) {
          return isSplash ? '/login' : null;
        }
        return '/login';
      }

      if (status == AuthStatus.authenticated) {
        if (isSplash || isAuthRoute) {
          return '/home';
        }
      }

      return null;
    },
    routes: [
      GoRoute(
        path: '/',
        builder: (context, state) => const SplashScreen(),
      ),
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: '/home',
        builder: (context, state) => const HomeScreen(),
      ),
    ],
  );
}

class TravelAdvisorApp extends StatelessWidget {
  final GoRouter router;

  const TravelAdvisorApp({super.key, required this.router});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Travel Advisor',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
        useMaterial3: true,
      ),
      routerConfig: router,
    );
  }
}
