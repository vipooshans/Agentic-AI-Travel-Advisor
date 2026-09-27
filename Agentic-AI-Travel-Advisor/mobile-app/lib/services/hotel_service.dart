import 'dart:convert';

import '../models/hotel.dart';
import 'api_service.dart';

class HotelService {
  final ApiService _api;
  HotelService(this._api);

  Future<List<Hotel>> getAll({String? city, String? country}) async {
    var path = '/api/hotels';
    final params = <String>[];
    if (city != null) params.add('city=$city');
    if (country != null) params.add('country=$country');
    if (params.isNotEmpty) path += '?${params.join('&')}';

    final response = await _api.get(path);
    if (response.statusCode != 200) throw Exception('Failed to load hotels');
    return (jsonDecode(response.body) as List)
        .map((h) => Hotel.fromJson(h))
        .toList();
  }

  Future<Hotel> getById(int id) async {
    final response = await _api.get('/api/hotels/$id');
    if (response.statusCode != 200) throw Exception('Hotel not found');
    return Hotel.fromJson(jsonDecode(response.body));
  }
}
