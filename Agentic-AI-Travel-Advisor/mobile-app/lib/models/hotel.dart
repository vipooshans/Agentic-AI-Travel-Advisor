import 'room.dart';

class Hotel {
  final int id;
  final String name;
  final String address;
  final String city;
  final String country;
  final String? description;
  final String? imageUrl;
  final int roomCount;
  final double? minPricePerNight;
  final double? averageRating;
  final int reviewCount;
  final List<Room>? rooms;

  const Hotel({
    required this.id,
    required this.name,
    required this.address,
    required this.city,
    required this.country,
    this.description,
    this.imageUrl,
    required this.roomCount,
    this.minPricePerNight,
    this.averageRating,
    this.reviewCount = 0,
    this.rooms,
  });

  factory Hotel.fromJson(Map<String, dynamic> json) {
    return Hotel(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String,
      address: json['address'] as String? ?? '',
      city: json['city'] as String? ?? '',
      country: json['country'] as String? ?? '',
      description: json['description'] as String?,
      imageUrl: json['imageUrl'] as String?,
      roomCount: (json['roomCount'] as num?)?.toInt() ?? 0,
      minPricePerNight: (json['minPricePerNight'] as num?)?.toDouble(),
      averageRating: (json['averageRating'] as num?)?.toDouble(),
      reviewCount: (json['reviewCount'] as num?)?.toInt() ?? 0,
      rooms: json['rooms'] != null
          ? (json['rooms'] as List).map((r) => Room.fromJson(r)).toList()
          : null,
    );
  }
}
