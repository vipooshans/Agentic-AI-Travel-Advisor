import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../providers/auth_provider.dart';
import '../screens/booking_list_screen.dart';
import '../screens/create_booking_screen.dart';
import '../screens/destination_detail_screen.dart';
import '../screens/destination_list_screen.dart';
import '../screens/home_screen.dart';
import '../screens/hotel_detail_screen.dart';
import '../screens/login_screen.dart';
import '../screens/package_detail_screen.dart';
import '../screens/profile_screen.dart';
import '../screens/register_screen.dart';
import '../screens/splash_screen.dart';
import '../widgets/main_shell.dart';

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
        if (isSplash || isAuthRoute) return isSplash ? '/login' : null;
        return '/login';
      }

      if (status == AuthStatus.authenticated && (isSplash || isAuthRoute)) {
        return '/home';
      }

      return null;
    },
    routes: [
      GoRoute(path: '/', builder: (_, __) => const SplashScreen()),
      GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, __) => const RegisterScreen()),
      ShellRoute(
        builder: (_, __, child) => MainShell(child: child),
        routes: [
          GoRoute(path: '/home', builder: (_, __) => const HomeScreen()),
          GoRoute(path: '/destinations', builder: (_, __) => const DestinationListScreen()),
          GoRoute(path: '/bookings', builder: (_, __) => const BookingListScreen()),
          GoRoute(path: '/profile', builder: (_, __) => const ProfileScreen()),
        ],
      ),
      GoRoute(
        path: '/destinations/:id',
        builder: (_, state) => DestinationDetailScreen(id: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: '/hotels/:id',
        builder: (_, state) => HotelDetailScreen(id: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: '/packages/:id',
        builder: (_, state) => PackageDetailScreen(id: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: '/bookings/new',
        builder: (_, state) => CreateBookingScreen(
          roomId: state.uri.queryParameters['roomId'] != null ? int.parse(state.uri.queryParameters['roomId']!) : null,
          packageId: state.uri.queryParameters['packageId'] != null ? int.parse(state.uri.queryParameters['packageId']!) : null,
        ),
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
