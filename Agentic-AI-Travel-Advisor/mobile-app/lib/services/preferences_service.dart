import 'dart:convert';

import '../models/travel_preferences.dart';
import 'api_service.dart';

class PreferencesService {
  final ApiService _api;
  PreferencesService(this._api);

  Future<TravelPreferences> get() async {
    final response = await _api.get('/api/users/me/preferences');
    _api.ensureSuccess(response);
    return TravelPreferences.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<TravelPreferences> save({
    double? budgetMin,
    double? budgetMax,
    String? preferredClimate,
    String? interests,
  }) async {
    final response = await _api.put('/api/users/me/preferences', {
      'budgetMin': budgetMin,
      'budgetMax': budgetMax,
      'preferredClimate': preferredClimate,
      'interests': interests,
    });
    _api.ensureSuccess(response);
    return TravelPreferences.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }
}
