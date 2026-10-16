import 'dart:convert';

import '../models/hotel.dart';
import '../models/room.dart';
import 'api_service.dart';

class HotelService {
  final ApiService _api;
  HotelService(this._api);

  Future<List<Hotel>> getAll({String? q, String? city, String? country, double? maxPrice, int? guests}) async {
    final response = await _api.get('/api/hotels', query: {
      'q': q,
      'city': city,
      'country': country,
      'maxPrice': maxPrice,
      'guests': guests,
    });
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((h) => Hotel.fromJson(h as Map<String, dynamic>))
        .toList();
  }

  Future<Hotel> getById(int id) async {
    final response = await _api.get('/api/hotels/$id');
    _api.ensureSuccess(response);
    return Hotel.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<List<Hotel>> getMine() async {
    final response = await _api.get('/api/hotels/mine');
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((h) => Hotel.fromJson(h as Map<String, dynamic>))
        .toList();
  }

  Future<Hotel> createHotel({
    required String name,
    required String address,
    required String city,
    required String country,
    String? description,
    String? imageUrl,
  }) async {
    final response = await _api.post('/api/hotels', {
      'name': name.trim(),
      'address': address.trim(),
      'city': city.trim(),
      'country': country.trim(),
      if (description != null && description.isNotEmpty) 'description': description.trim(),
      if (imageUrl != null && imageUrl.isNotEmpty) 'imageUrl': imageUrl.trim(),
    });
    _api.ensureSuccess(response, ok: const {200, 201});
    return Hotel.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<Hotel> updateHotel({
    required int id,
    required String name,
    required String address,
    required String city,
    required String country,
    String? description,
    String? imageUrl,
  }) async {
    final response = await _api.put('/api/hotels/$id', {
      'name': name.trim(),
      'address': address.trim(),
      'city': city.trim(),
      'country': country.trim(),
      if (description != null) 'description': description.trim(),
      if (imageUrl != null) 'imageUrl': imageUrl.trim(),
    });
    _api.ensureSuccess(response);
    return Hotel.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<void> deleteHotel(int id) async {
    final response = await _api.delete('/api/hotels/$id');
    _api.ensureSuccess(response, ok: const {200, 204});
  }

  Future<List<Room>> getRooms(int hotelId) async {
    final response = await _api.get('/api/hotels/$hotelId/rooms');
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((r) => Room.fromJson(r as Map<String, dynamic>))
        .toList();
  }

  Future<Room> createRoom({
    required int hotelId,
    required String name,
    required String roomType,
    required double pricePerNight,
    required int capacity,
  }) async {
    final response = await _api.post('/api/hotels/$hotelId/rooms', {
      'name': name.trim(),
      'roomType': roomType.trim(),
      'pricePerNight': pricePerNight,
      'capacity': capacity,
    });
    _api.ensureSuccess(response, ok: const {200, 201});
    return Room.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<void> setRoomAvailability(int hotelId, int roomId, bool isAvailable) async {
    final response = await _api.patch(
      '/api/hotels/$hotelId/rooms/$roomId/availability',
      {'isAvailable': isAvailable},
    );
    _api.ensureSuccess(response);
  }

  Future<void> deleteRoom(int hotelId, int roomId) async {
    final response = await _api.delete('/api/hotels/$hotelId/rooms/$roomId');
    _api.ensureSuccess(response, ok: const {200, 204});
  }
}
