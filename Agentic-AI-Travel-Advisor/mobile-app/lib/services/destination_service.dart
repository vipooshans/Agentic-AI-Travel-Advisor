import 'dart:convert';

import '../models/destination.dart';
import 'api_service.dart';

class DestinationService {
  final ApiService _api;
  DestinationService(this._api);

  Future<List<Destination>> getAll() async {
    final response = await _api.get('/api/destinations');
    if (response.statusCode != 200) throw Exception('Failed to load destinations');
    return (jsonDecode(response.body) as List)
        .map((d) => Destination.fromJson(d))
        .toList();
  }

  Future<Destination> getById(int id) async {
    final response = await _api.get('/api/destinations/$id');
    if (response.statusCode != 200) throw Exception('Destination not found');
    return Destination.fromJson(jsonDecode(response.body));
  }
}
