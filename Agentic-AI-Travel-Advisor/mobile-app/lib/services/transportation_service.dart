import 'dart:convert';

import '../models/transportation.dart';
import 'api_service.dart';

class TransportationService {
  final ApiService _api;
  TransportationService(this._api);

  Future<List<Transportation>> search({int? destinationId, int? travelPackageId, String? from, String? to}) async {
    final response = await _api.get('/api/transportation', query: {
      'destinationId': destinationId,
      'travelPackageId': travelPackageId,
      'from': from,
      'to': to,
    });
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((t) => Transportation.fromJson(t as Map<String, dynamic>))
        .toList();
  }
}
