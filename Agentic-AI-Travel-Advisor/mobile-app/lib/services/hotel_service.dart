import 'dart:convert';

import '../models/hotel.dart';
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
}
