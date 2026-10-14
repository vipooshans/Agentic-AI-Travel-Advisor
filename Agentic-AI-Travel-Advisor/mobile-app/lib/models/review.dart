class Review {
  final int id;
  final int bookingId;
  final int? hotelId;
  final String? hotelName;
  final int? travelPackageId;
  final String? packageTitle;
  final int rating;
  final String? comment;
  final String authorName;
  final DateTime createdAt;

  const Review({
    required this.id,
    required this.bookingId,
    this.hotelId,
    this.hotelName,
    this.travelPackageId,
    this.packageTitle,
    required this.rating,
    this.comment,
    required this.authorName,
    required this.createdAt,
  });

  factory Review.fromJson(Map<String, dynamic> json) {
    return Review(
      id: (json['id'] as num).toInt(),
      bookingId: (json['bookingId'] as num).toInt(),
      hotelId: (json['hotelId'] as num?)?.toInt(),
      hotelName: json['hotelName'] as String?,
      travelPackageId: (json['travelPackageId'] as num?)?.toInt(),
      packageTitle: json['packageTitle'] as String?,
      rating: (json['rating'] as num).toInt(),
      comment: json['comment'] as String?,
      authorName: json['authorName'] as String? ?? 'Traveler',
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0),
    );
  }
}
