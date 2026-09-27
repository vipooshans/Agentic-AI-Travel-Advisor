import 'dart:convert';

import '../models/booking.dart';
import 'api_service.dart';

class BookingService {
  final ApiService _api;
  BookingService(this._api);

  Future<List<Booking>> getAll() async {
    final response = await _api.get('/api/bookings');
    if (response.statusCode != 200) throw Exception('Failed to load bookings');
    return (jsonDecode(response.body) as List)
        .map((b) => Booking.fromJson(b))
        .toList();
  }

  Future<Booking> create({
    int? roomId,
    int? travelPackageId,
    required DateTime checkIn,
    DateTime? checkOut,
  }) async {
    final body = <String, dynamic>{
      'checkIn': checkIn.toIso8601String(),
    };
    if (roomId != null) body['roomId'] = roomId;
    if (travelPackageId != null) body['travelPackageId'] = travelPackageId;
    if (checkOut != null) body['checkOut'] = checkOut.toIso8601String();

    final response = await _api.post('/api/bookings', body);
    if (response.statusCode != 200 && response.statusCode != 201) {
      throw Exception(_api.parseErrorMessage(response) ?? 'Booking failed');
    }
    return Booking.fromJson(jsonDecode(response.body));
  }

  Future<Booking> updateStatus(int id, int status) async {
    final response = await _api.patch('/api/bookings/$id/status', {'status': status});
    if (response.statusCode != 200) {
      throw Exception(_api.parseErrorMessage(response) ?? 'Could not update booking');
    }
    return Booking.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }
}
