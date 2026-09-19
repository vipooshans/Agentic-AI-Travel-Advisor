import 'dart:convert';

import '../models/itinerary.dart';
import '../models/suggested_plan.dart';
import 'api_service.dart';

class ItineraryService {
  final ApiService _api;
  ItineraryService(this._api);

  Future<List<Itinerary>> getAll() async {
    final response = await _api.get('/api/itineraries');
    if (response.statusCode != 200) throw Exception('Failed to load itineraries');
    return (jsonDecode(response.body) as List)
        .map((i) => Itinerary.fromJson(i as Map<String, dynamic>))
        .toList();
  }

  Future<Itinerary> getById(int id) async {
    final response = await _api.get('/api/itineraries/$id');
    if (response.statusCode != 200) throw Exception('Failed to load itinerary');
    return Itinerary.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<Itinerary> createFromPlan(SuggestedPlan plan) async {
    final response = await _api.post('/api/itineraries', plan.toCreateRequest());
    if (response.statusCode != 200 && response.statusCode != 201) {
      throw Exception(_api.parseErrorMessage(response) ?? 'Failed to save itinerary');
    }
    return Itinerary.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }
}
