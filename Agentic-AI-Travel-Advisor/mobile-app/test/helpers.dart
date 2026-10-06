import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:travel_advisor/services/api_service.dart';

const testBaseUrl = 'http://api.test';

/// An [ApiService] whose requests are answered by [handler] instead of the network.
ApiService fakeApi(Future<http.Response> Function(http.Request request) handler, {Duration? timeout}) {
  return ApiService(
    client: MockClient(handler),
    baseUrl: testBaseUrl,
    timeout: timeout ?? ApiService.defaultTimeout,
  );
}

http.Response jsonResponse(Object? body, [int status = 200]) {
  return http.Response(
    jsonEncode(body),
    status,
    headers: {'content-type': 'application/json; charset=utf-8'},
  );
}

Map<String, dynamic> userJson({String role = 'USER', String email = 'nimal@example.com'}) => {
      'id': 'user-1',
      'email': email,
      'firstName': 'Nimal',
      'lastName': 'Perera',
      'role': role,
    };

Map<String, dynamic> authJson({String role = 'USER', String token = 'token-abc'}) => {
      'token': token,
      'expiresAt': '2026-10-02T12:00:00Z',
      'user': userJson(role: role),
    };

Map<String, dynamic> planJson() => {
      'destination': 'Ella',
      'destinationId': 4,
      'country': 'Sri Lanka',
      'duration': 3,
      'nights': 2,
      'startDate': '2026-10-10',
      'endDate': '2026-10-12',
      'travelers': 2,
      'budget': 80000,
      'currency': 'LKR',
      'estimatedTotal': 76000,
      'withinBudget': true,
      'costBreakdown': {'accommodation': 32000, 'packages': 36000, 'transportation': 8000},
      'hotels': [
        {
          'hotelId': 3,
          'roomId': 7,
          'name': 'Ella Gap View Inn',
          'roomName': 'Deluxe Double',
          'city': 'Ella',
          'pricePerNight': 16000,
          'nights': 2,
          'rooms': 1,
          'capacity': 2,
          'totalCost': 32000,
          'averageRating': 4.5,
          'selected': true,
          'availabilityChecked': true,
        },
      ],
      'travelPackages': [
        {
          'packageId': 9,
          'title': 'Ella Hill Country Escape',
          'durationDays': 3,
          'pricePerPerson': 18000,
          'totalCost': 36000,
          'remainingPlaces': 6,
          'averageRating': null,
          'selected': true,
          'availabilityChecked': true,
        },
      ],
      'activities': [
        {'title': 'Little Adam’s Peak hike', 'category': 'Hiking', 'day': 2, 'pricePerPerson': 0, 'packageId': 9, 'includedInCost': true},
      ],
      'transportation': [
        {
          'transportationId': 5,
          'mode': 'Train',
          'from': 'Kandy',
          'to': 'Ella',
          'departureTime': '08:47',
          'durationMinutes': 400,
          'pricePerPerson': 2000,
          'trips': 2,
          'totalCost': 8000,
          'selected': true,
        },
      ],
      'itinerary': [
        {
          'day': 1,
          'date': '2026-10-10',
          'items': [
            {'time': '08:47', 'type': 'transport', 'title': 'Train Kandy to Ella', 'description': 'Scenic route'},
            {'time': 'Evening', 'type': 'free', 'title': 'Explore Ella town', 'description': null},
          ],
        },
        {
          'day': 2,
          'date': '2026-10-11',
          'items': [
            {'time': '06:30', 'type': 'activity', 'title': 'Little Adam’s Peak hike', 'description': null},
          ],
        },
      ],
      'assumptions': ['Prices are per the current listings.'],
      'warnings': ['Trains fill up quickly on weekends.'],
    };

Map<String, dynamic> proposalJson({String id = 'prop-123', DateTime? expiresAt}) => {
      'id': id,
      'kind': 'room',
      'hotelId': 3,
      'roomId': 7,
      'travelPackageId': null,
      'title': 'Ella Gap View Inn – Deluxe Double',
      'checkIn': '2026-10-10',
      'checkOut': '2026-10-12',
      'guests': 2,
      'quotedTotal': 32000,
      'currency': 'LKR',
      'expiresAt': (expiresAt ?? DateTime.now().toUtc().add(const Duration(minutes: 30))).toIso8601String(),
    };

Map<String, dynamic> bookingJson({int id = 42, int status = 0, int? travelPackageId}) => {
      'id': id,
      'userId': 'user-1',
      'roomId': travelPackageId == null ? 7 : null,
      'travelPackageId': travelPackageId,
      'hotelId': travelPackageId == null ? 3 : null,
      'checkIn': '2026-10-10T00:00:00Z',
      'checkOut': '2026-10-12T00:00:00Z',
      'guests': 2,
      'notes': null,
      'status': status,
      'totalPrice': 32000,
      'createdAt': '2026-10-01T10:00:00Z',
      'roomName': 'Deluxe Double',
      'hotelName': travelPackageId == null ? 'Ella Gap View Inn' : null,
      'packageTitle': travelPackageId == null ? null : 'Ella Hill Country Escape',
    };

Map<String, dynamic> chatResponseJson({
  int conversationId = 7,
  required String status,
  required String message,
  Map<String, dynamic>? plan,
  Map<String, dynamic>? pendingBooking,
  Map<String, dynamic>? booking,
}) =>
    {
      'conversationId': conversationId,
      'message': message,
      'status': status,
      'plan': plan,
      'pendingBooking': pendingBooking,
      'booking': booking,
      'mode': 'deterministic',
      'agents': ['planner'],
      'toolCalls': [],
    };
