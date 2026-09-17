class Room {
  final int id;
  final int hotelId;
  final String name;
  final String roomType;
  final double pricePerNight;
  final int capacity;
  final bool isAvailable;

  const Room({
    required this.id,
    required this.hotelId,
    required this.name,
    required this.roomType,
    required this.pricePerNight,
    required this.capacity,
    required this.isAvailable,
  });

  factory Room.fromJson(Map<String, dynamic> json) {
    return Room(
      id: json['id'] as int,
      hotelId: json['hotelId'] as int,
      name: json['name'] as String,
      roomType: json['roomType'] as String,
      pricePerNight: (json['pricePerNight'] as num).toDouble(),
      capacity: json['capacity'] as int,
      isAvailable: json['isAvailable'] as bool,
    );
  }
}
