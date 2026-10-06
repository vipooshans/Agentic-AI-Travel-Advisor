import 'dart:convert';

import '../models/travel_package.dart';
import 'api_service.dart';

class PackageService {
  final ApiService _api;
  PackageService(this._api);

  Future<List<TravelPackage>> getAll({String? q, int? destinationId, double? maxPrice, int? maxDurationDays}) async {
    final response = await _api.get('/api/packages', query: {
      'q': q,
      'destinationId': destinationId,
      'maxPrice': maxPrice,
      'maxDurationDays': maxDurationDays,
    });
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((p) => TravelPackage.fromJson(p as Map<String, dynamic>))
        .toList();
  }

  Future<TravelPackage> getById(int id) async {
    final response = await _api.get('/api/packages/$id');
    _api.ensureSuccess(response);
    return TravelPackage.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }
}
