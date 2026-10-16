import 'dart:convert';

import '../models/package_activity.dart';
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

  Future<List<TravelPackage>> getMine() async {
    final response = await _api.get('/api/packages/mine');
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((p) => TravelPackage.fromJson(p as Map<String, dynamic>))
        .toList();
  }

  Future<TravelPackage> createPackage({
    required int destinationId,
    required String title,
    String? description,
    required double price,
    required int durationDays,
    String? imageUrl,
    int maxTravelers = 10,
  }) async {
    final response = await _api.post('/api/packages', {
      'destinationId': destinationId,
      'title': title.trim(),
      if (description != null && description.isNotEmpty) 'description': description.trim(),
      'price': price,
      'durationDays': durationDays,
      if (imageUrl != null && imageUrl.isNotEmpty) 'imageUrl': imageUrl.trim(),
      'maxTravelers': maxTravelers,
    });
    _api.ensureSuccess(response, ok: const {200, 201});
    return TravelPackage.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<TravelPackage> updatePackage({
    required int id,
    required int destinationId,
    required String title,
    String? description,
    required double price,
    required int durationDays,
    String? imageUrl,
    int maxTravelers = 10,
  }) async {
    final response = await _api.put('/api/packages/$id', {
      'destinationId': destinationId,
      'title': title.trim(),
      if (description != null) 'description': description.trim(),
      'price': price,
      'durationDays': durationDays,
      if (imageUrl != null) 'imageUrl': imageUrl.trim(),
      'maxTravelers': maxTravelers,
    });
    _api.ensureSuccess(response);
    return TravelPackage.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<void> deletePackage(int id) async {
    final response = await _api.delete('/api/packages/$id');
    _api.ensureSuccess(response, ok: const {200, 204});
  }

  Future<PackageActivity> addActivity({
    required int packageId,
    required String title,
    String? description,
    required int dayNumber,
    required double price,
    int sortOrder = 0,
  }) async {
    final response = await _api.post('/api/packages/$packageId/activities', {
      'title': title.trim(),
      if (description != null && description.isNotEmpty) 'description': description.trim(),
      'dayNumber': dayNumber,
      'price': price,
      'sortOrder': sortOrder,
    });
    _api.ensureSuccess(response, ok: const {200, 201});
    return PackageActivity.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<void> deleteActivity(int packageId, int activityId) async {
    final response = await _api.delete('/api/packages/$packageId/activities/$activityId');
    _api.ensureSuccess(response, ok: const {200, 204});
  }
}
