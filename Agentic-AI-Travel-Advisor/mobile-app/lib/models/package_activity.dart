class PackageActivity {
  final int id;
  final int travelPackageId;
  final String title;
  final String? description;
  final int dayNumber;
  final double price;
  final int sortOrder;

  const PackageActivity({
    required this.id,
    required this.travelPackageId,
    required this.title,
    this.description,
    required this.dayNumber,
    required this.price,
    required this.sortOrder,
  });

  factory PackageActivity.fromJson(Map<String, dynamic> json) {
    return PackageActivity(
      id: json['id'] as int,
      travelPackageId: json['travelPackageId'] as int,
      title: json['title'] as String,
      description: json['description'] as String?,
      dayNumber: json['dayNumber'] as int,
      price: (json['price'] as num).toDouble(),
      sortOrder: json['sortOrder'] as int,
    );
  }
}
