import 'dart:convert';

import '../models/itinerary.dart';
import '../models/suggested_plan.dart';
import '../models/travel_plan.dart';
import 'api_service.dart';

class ItineraryService {
  final ApiService _api;
  ItineraryService(this._api);

  Future<List<Itinerary>> getAll() async {
    final response = await _api.get('/api/itineraries');
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((i) => Itinerary.fromJson(i as Map<String, dynamic>))
        .toList();
  }

  Future<Itinerary> getById(int id) async {
    final response = await _api.get('/api/itineraries/$id');
    _api.ensureSuccess(response);
    return Itinerary.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<Itinerary> createFromPlan(SuggestedPlan plan) => _create(plan.toCreateRequest());

  Future<Itinerary> createFromTravelPlan(TravelPlan plan) => _create(plan.toItineraryRequest());

  Future<Itinerary> _create(Map<String, dynamic> body) async {
    final response = await _api.post('/api/itineraries', body);
    _api.ensureSuccess(response, ok: const {200, 201});
    return Itinerary.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }
}
