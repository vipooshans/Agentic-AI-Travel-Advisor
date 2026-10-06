class Destination {
  final int id;
  final String name;
  final String country;
  final String? description;
  final String? imageUrl;
  final int? packageCount;

  const Destination({
    required this.id,
    required this.name,
    required this.country,
    this.description,
    this.imageUrl,
    this.packageCount,
  });

  factory Destination.fromJson(Map<String, dynamic> json) {
    return Destination(
      id: json['id'] as int,
      name: json['name'] as String,
      country: json['country'] as String,
      description: json['description'] as String?,
      imageUrl: json['imageUrl'] as String?,
      packageCount: json['packageCount'] as int?,
    );
  }
}
