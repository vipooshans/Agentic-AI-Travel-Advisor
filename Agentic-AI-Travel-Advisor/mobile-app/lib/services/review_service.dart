import 'dart:convert';

import '../models/review.dart';
import 'api_service.dart';

class ReviewService {
  static const maxCommentLength = 2000;

  final ApiService _api;
  ReviewService(this._api);

  Future<List<Review>> forHotel(int hotelId) => _list('/api/hotels/$hotelId/reviews');

  Future<List<Review>> forPackage(int packageId) => _list('/api/packages/$packageId/reviews');

  /// For a traveler the API returns only their own reviews.
  Future<List<Review>> mine() => _list('/api/reviews');

  Future<Review> create({required int bookingId, required int rating, String? comment}) async {
    final response = await _api.post('/api/reviews', {
      'bookingId': bookingId,
      'rating': rating,
      if (comment != null && comment.trim().isNotEmpty) 'comment': comment.trim(),
    });
    _api.ensureSuccess(response, ok: const {200, 201});
    return Review.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<List<Review>> _list(String path) async {
    final response = await _api.get(path);
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((r) => Review.fromJson(r as Map<String, dynamic>))
        .toList();
  }
}
