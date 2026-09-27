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
  final int activityCount;
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
    required this.activityCount,
    this.activities,
  });

  factory TravelPackage.fromJson(Map<String, dynamic> json) {
    return TravelPackage(
      id: json['id'] as int,
      title: json['title'] as String,
      description: json['description'] as String?,
      price: (json['price'] as num).toDouble(),
      durationDays: json['durationDays'] as int,
      destinationId: json['destinationId'] as int,
      destinationName: json['destinationName'] as String,
      destinationCountry: json['destinationCountry'] as String,
      activityCount: json['activityCount'] as int? ?? 0,
      activities: json['activities'] != null
          ? (json['activities'] as List)
              .map((a) => PackageActivity.fromJson(a))
              .toList()
          : null,
    );
  }
}
