import 'package_activity.dart';

class TravelPackage {
  final int id;
  final String title;
  final String? description;
  final double price;
  final int durationDays;
  final int destinationId;
  final String destinationName;
  final String destinationCountry;
  final String? imageUrl;
  final int activityCount;

  /// Package price plus included activity prices, per person.
  final double? totalPrice;
  final int? maxTravelers;
  final double? averageRating;
  final int reviewCount;
  final List<PackageActivity>? activities;

  const TravelPackage({
    required this.id,
    required this.title,
    this.description,
    required this.price,
    required this.durationDays,
    required this.destinationId,
    required this.destinationName,
    required this.destinationCountry,
    this.imageUrl,
    required this.activityCount,
    this.totalPrice,
    this.maxTravelers,
    this.averageRating,
    this.reviewCount = 0,
    this.activities,
  });

  double get pricePerPerson => totalPrice ?? price;

  factory TravelPackage.fromJson(Map<String, dynamic> json) {
    return TravelPackage(
      id: (json['id'] as num).toInt(),
      title: json['title'] as String,
      description: json['description'] as String?,
      price: (json['price'] as num).toDouble(),
      durationDays: (json['durationDays'] as num).toInt(),
      destinationId: (json['destinationId'] as num).toInt(),
      destinationName: json['destinationName'] as String? ?? '',
      destinationCountry: json['destinationCountry'] as String? ?? '',
      imageUrl: json['imageUrl'] as String?,
      activityCount: (json['activityCount'] as num?)?.toInt() ?? 0,
      totalPrice: (json['totalPrice'] as num?)?.toDouble(),
      maxTravelers: (json['maxTravelers'] as num?)?.toInt(),
      averageRating: (json['averageRating'] as num?)?.toDouble(),
      reviewCount: (json['reviewCount'] as num?)?.toInt() ?? 0,
      activities: json['activities'] != null
          ? (json['activities'] as List)
              .map((a) => PackageActivity.fromJson(a))
              .toList()
          : null,
    );
  }
}
