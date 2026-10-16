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
  final int approvalStatus;
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
    this.approvalStatus = 1,
    this.minPricePerNight,
    this.averageRating,
    this.reviewCount = 0,
    this.rooms,
  });

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

  factory Hotel.fromJson(Map<String, dynamic> json) {
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

    return Hotel(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String,
      address: json['address'] as String? ?? '',
      city: json['city'] as String? ?? '',
      country: json['country'] as String? ?? '',
      description: json['description'] as String?,
      imageUrl: json['imageUrl'] as String?,
      roomCount: (json['roomCount'] as num?)?.toInt() ?? 0,
      approvalStatus: parseStatus(json['approvalStatus']),
      minPricePerNight: (json['minPricePerNight'] as num?)?.toDouble(),
      averageRating: (json['averageRating'] as num?)?.toDouble(),
      reviewCount: (json['reviewCount'] as num?)?.toInt() ?? 0,
      rooms: json['rooms'] != null
          ? (json['rooms'] as List).map((r) => Room.fromJson(r)).toList()
          : null,
    );
  }
}
