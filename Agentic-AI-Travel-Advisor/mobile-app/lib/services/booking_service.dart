import 'dart:convert';

import '../models/availability_quote.dart';
import '../models/booking.dart';
import '../utils/format.dart';
import 'api_service.dart';

class BookingService {
  final ApiService _api;
  BookingService(this._api);

  Future<List<Booking>> getAll() async {
    final response = await _api.get('/api/bookings');
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((b) => Booking.fromJson(b as Map<String, dynamic>))
        .toList();
  }

  /// Asks the API whether the room or package can be booked and what it costs.
  Future<AvailabilityQuote> checkAvailability({
    int? roomId,
    int? travelPackageId,
    required DateTime checkIn,
    DateTime? checkOut,
    required int guests,
  }) async {
    final response = await _api.get('/api/bookings/availability', query: {
      'roomId': roomId,
      'travelPackageId': travelPackageId,
      'checkIn': formatApiDate(checkIn),
      'checkOut': checkOut == null ? null : formatApiDate(checkOut),
      'guests': guests,
    });
    _api.ensureSuccess(response);
    return AvailabilityQuote.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<Booking> create({
    int? roomId,
    int? travelPackageId,
    required DateTime checkIn,
    DateTime? checkOut,
    int guests = 1,
    String? notes,
  }) async {
    final body = <String, dynamic>{
      'checkIn': formatApiDate(checkIn),
      'guests': guests,
    };
    if (roomId != null) body['roomId'] = roomId;
    if (travelPackageId != null) body['travelPackageId'] = travelPackageId;
    if (checkOut != null) body['checkOut'] = formatApiDate(checkOut);
    if (notes != null && notes.trim().isNotEmpty) body['notes'] = notes.trim();

    final response = await _api.post('/api/bookings', body);
    _api.ensureSuccess(response, ok: const {200, 201});
    return Booking.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<Booking> updateStatus(int id, int status) async {
    final response = await _api.patch('/api/bookings/$id/status', {'status': status});
    _api.ensureSuccess(response);
    return Booking.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }
}
