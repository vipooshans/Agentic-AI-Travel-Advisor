import 'room.dart';

class Hotel {
  final int id;
  final String name;
  final String address;
  final String city;
  final String country;
  final String? description;
  final int roomCount;
  final List<Room>? rooms;

  const Hotel({
    required this.id,
    required this.name,
    required this.address,
    required this.city,
    required this.country,
    this.description,
    required this.roomCount,
    this.rooms,
  });

  factory Hotel.fromJson(Map<String, dynamic> json) {
    return Hotel(
      id: json['id'] as int,
      name: json['name'] as String,
      address: json['address'] as String,
      city: json['city'] as String,
      country: json['country'] as String,
      description: json['description'] as String?,
      roomCount: json['roomCount'] as int? ?? 0,
      rooms: json['rooms'] != null
          ? (json['rooms'] as List).map((r) => Room.fromJson(r)).toList()
          : null,
    );
  }
}
