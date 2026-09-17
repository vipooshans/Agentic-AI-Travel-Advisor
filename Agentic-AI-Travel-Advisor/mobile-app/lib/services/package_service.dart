import 'dart:convert';

import '../models/travel_package.dart';
import 'api_service.dart';

class PackageService {
  final ApiService _api;
  PackageService(this._api);

  Future<List<TravelPackage>> getAll({int? destinationId}) async {
    var path = '/api/packages';
    if (destinationId != null) path += '?destinationId=$destinationId';

    final response = await _api.get(path);
    if (response.statusCode != 200) throw Exception('Failed to load packages');
    return (jsonDecode(response.body) as List)
        .map((p) => TravelPackage.fromJson(p))
        .toList();
  }

  Future<TravelPackage> getById(int id) async {
    final response = await _api.get('/api/packages/$id');
    if (response.statusCode != 200) throw Exception('Package not found');
    return TravelPackage.fromJson(jsonDecode(response.body));
  }
}
