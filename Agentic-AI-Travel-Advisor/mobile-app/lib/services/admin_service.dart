import 'dart:convert';

import '../models/hotel.dart';
import '../models/report_summary.dart';
import '../models/staff_user.dart';
import '../models/travel_package.dart';
import 'api_service.dart';

class AdminService {
  final ApiService _api;
  AdminService(this._api);

  Future<ReportSummary> getSummary() async {
    final response = await _api.get('/api/reports/summary');
    _api.ensureSuccess(response);
    return ReportSummary.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<List<StaffUser>> getUsers({String? role}) async {
    final response = await _api.get('/api/users', query: role != null && role.isNotEmpty ? {'role': role} : null);
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((u) => StaffUser.fromJson(u as Map<String, dynamic>))
        .toList();
  }

  Future<StaffUser> createStaffUser({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    required String role,
  }) async {
    final response = await _api.post('/api/users', {
      'email': email.trim(),
      'password': password,
      'firstName': firstName.trim(),
      'lastName': lastName.trim(),
      'role': role,
    });
    _api.ensureSuccess(response, ok: const {200, 201});
    return StaffUser.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<void> setUserActive(String id, bool isActive) async {
    final response = await _api.patch('/api/users/$id/active', {'isActive': isActive});
    _api.ensureSuccess(response);
  }

  Future<List<Hotel>> getPendingHotels() async {
    final response = await _api.get('/api/hotels', query: {'approvalStatus': 'Pending'});
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((h) => Hotel.fromJson(h as Map<String, dynamic>))
        .toList();
  }

  Future<List<TravelPackage>> getPendingPackages() async {
    final response = await _api.get('/api/packages', query: {'approvalStatus': 'Pending'});
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((p) => TravelPackage.fromJson(p as Map<String, dynamic>))
        .toList();
  }

  Future<void> setHotelApproval(int id, int status) async {
    final response = await _api.patch('/api/hotels/$id/approval', {'status': status});
    _api.ensureSuccess(response);
  }

  Future<void> setPackageApproval(int id, int status) async {
    final response = await _api.patch('/api/packages/$id/approval', {'status': status});
    _api.ensureSuccess(response);
  }
}
