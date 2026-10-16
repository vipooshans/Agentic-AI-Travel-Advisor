import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:travel_advisor/providers/auth_provider.dart';
import 'package:travel_advisor/screens/admin/admin_dashboard_screen.dart';
import 'package:travel_advisor/screens/agent/package_dashboard_screen.dart';
import 'package:travel_advisor/screens/owner/hotel_dashboard_screen.dart';
import 'package:travel_advisor/services/auth_service.dart';

import 'helpers.dart';

Map<String, dynamic> reportSummaryJson() => {
      'userCount': 15,
      'hotelOwnerCount': 4,
      'travelAgentCount': 3,
      'hotelCount': 8,
      'packageCount': 6,
      'pendingHotelApprovals': 1,
      'pendingPackageApprovals': 1,
      'pendingBookings': 2,
      'confirmedBookings': 5,
      'cancelledBookings': 1,
      'completedBookings': 10,
      'revenue': 250000.0,
    };

Map<String, dynamic> staffUserJson({String role = 'HOTEL_OWNER', String name = 'Sunil'}) => {
      'id': 'user-123',
      'email': 'sunil@hotel.com',
      'firstName': name,
      'lastName': 'Silva',
      'role': role,
      'isActive': true,
      'createdAt': '2026-09-01T00:00:00Z',
    };

Map<String, dynamic> hotelOwnerHotelJson() => {
      'id': 1,
      'name': 'Grand Cinnamon Resort',
      'address': 'Beach Road',
      'city': 'Bentota',
      'country': 'Sri Lanka',
      'roomCount': 12,
      'approvalStatus': 1,
      'minPricePerNight': 15000.0,
      'averageRating': 4.5,
      'reviewCount': 8,
    };

Map<String, dynamic> agentPackageJson() => {
      'id': 1,
      'title': 'Hill Country Tea Trail',
      'destinationId': 1,
      'destinationName': 'Nuwara Eliya',
      'destinationCountry': 'Sri Lanka',
      'price': 45000.0,
      'durationDays': 4,
      'activityCount': 3,
      'approvalStatus': 1,
      'maxTravelers': 12,
      'averageRating': 4.8,
      'reviewCount': 10,
    };

void main() {
  group('Admin Dashboard Screen', () {
    testWidgets('renders overview metrics and tabs', (tester) async {
      final api = fakeApi((request) async {
        if (request.url.path == '/api/reports/summary') return jsonResponse(reportSummaryJson());
        if (request.url.path == '/api/users') return jsonResponse([staffUserJson()]);
        if (request.url.path == '/api/hotels') return jsonResponse([hotelOwnerHotelJson()]);
        if (request.url.path == '/api/packages') return jsonResponse([agentPackageJson()]);
        return jsonResponse([]);
      });

      final auth = AuthProvider(AuthService(api: api))..initialize();

      await tester.pumpWidget(
        ChangeNotifierProvider<AuthProvider>.value(
          value: auth,
          child: const MaterialApp(home: AdminDashboardScreen()),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Admin Dashboard'), findsOneWidget);
      expect(find.text('Total Revenue'), findsOneWidget);
      expect(find.text('Total Bookings'), findsOneWidget);
      expect(find.text('Registered Users'), findsOneWidget);
      expect(find.text('Pending Approvals'), findsOneWidget);
      expect(find.text('Packages (Agent)'), findsOneWidget);
      expect(find.text('Hotels (Owner)'), findsOneWidget);
    });
  });

  group('Package Dashboard Screen', () {
    testWidgets('renders package agent metrics and packages list', (tester) async {
      final api = fakeApi((request) async {
        if (request.url.path == '/api/packages/mine') return jsonResponse([agentPackageJson()]);
        if (request.url.path == '/api/bookings') return jsonResponse([]);
        if (request.url.path == '/api/destinations') return jsonResponse([]);
        if (request.url.path == '/api/reports/summary') return jsonResponse(reportSummaryJson());
        return jsonResponse([]);
      });

      final auth = AuthProvider(AuthService(api: api))..initialize();

      await tester.pumpWidget(
        ChangeNotifierProvider<AuthProvider>.value(
          value: auth,
          child: const MaterialApp(home: PackageDashboardScreen()),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Packages Dashboard'), findsOneWidget);
      expect(find.text('Hill Country Tea Trail'), findsOneWidget);
      expect(find.text('New Package'), findsOneWidget);
    });
  });

  group('Hotel Dashboard Screen', () {
    testWidgets('renders hotel owner properties and management', (tester) async {
      final api = fakeApi((request) async {
        if (request.url.path == '/api/hotels/mine') return jsonResponse([hotelOwnerHotelJson()]);
        if (request.url.path == '/api/bookings') return jsonResponse([]);
        if (request.url.path == '/api/reports/summary') return jsonResponse(reportSummaryJson());
        return jsonResponse([]);
      });

      final auth = AuthProvider(AuthService(api: api))..initialize();

      await tester.pumpWidget(
        ChangeNotifierProvider<AuthProvider>.value(
          value: auth,
          child: const MaterialApp(home: HotelDashboardScreen()),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Hotels Dashboard'), findsOneWidget);
      expect(find.text('Grand Cinnamon Resort'), findsOneWidget);
      expect(find.text('Manage Rooms'), findsOneWidget);
      expect(find.text('Add Hotel'), findsOneWidget);
    });
  });
}
