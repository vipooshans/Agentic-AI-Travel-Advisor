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
  final int approvalStatus;

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
    this.approvalStatus = 1,
    this.totalPrice,
    this.maxTravelers,
    this.averageRating,
    this.reviewCount = 0,
    this.activities,
  });

  double get pricePerPerson => totalPrice ?? price;

  String get approvalStatusLabel {
    switch (approvalStatus) {
      case 0:
        return 'Pending';
      case 1:
        return 'Approved';
      case 2:
        return 'Rejected';
      default:
        return 'Pending';
    }
  }

  factory TravelPackage.fromJson(Map<String, dynamic> json) {
    int parseStatus(dynamic val) {
      if (val is num) return val.toInt();
      if (val is String) {
        final s = val.toLowerCase();
        if (s == 'approved') return 1;
        if (s == 'rejected') return 2;
        return 0;
      }
      return 1;
    }

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
      approvalStatus: parseStatus(json['approvalStatus']),
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
