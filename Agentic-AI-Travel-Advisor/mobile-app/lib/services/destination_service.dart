import 'dart:convert';

import '../models/destination.dart';
import 'api_service.dart';

class DestinationService {
  final ApiService _api;
  DestinationService(this._api);

  Future<List<Destination>> getAll() async {
    final response = await _api.get('/api/destinations');
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((d) => Destination.fromJson(d))
        .toList();
  }

  Future<Destination> getById(int id) async {
    final response = await _api.get('/api/destinations/$id');
    _api.ensureSuccess(response);
    return Destination.fromJson(jsonDecode(response.body));
  }
}
