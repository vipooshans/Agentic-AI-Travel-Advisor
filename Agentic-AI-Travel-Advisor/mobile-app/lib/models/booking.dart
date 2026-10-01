class Booking {
  final int id;
  final String userId;
  final int? roomId;
  final int? travelPackageId;
  final int? hotelId;
  final DateTime checkIn;
  final DateTime checkOut;
  final int guests;
  final String? notes;
  final int status;
  final double totalPrice;
  final DateTime createdAt;
  final String? roomName;
  final String? hotelName;
  final String? packageTitle;

  static const pending = 0;
  static const confirmed = 1;
  static const cancelled = 2;
  static const completed = 3;

  const Booking({
    required this.id,
    required this.userId,
    this.roomId,
    this.travelPackageId,
    this.hotelId,
    required this.checkIn,
    required this.checkOut,
    this.guests = 1,
    this.notes,
    required this.status,
    required this.totalPrice,
    required this.createdAt,
    this.roomName,
    this.hotelName,
    this.packageTitle,
  });

  factory Booking.fromJson(Map<String, dynamic> json) {
    return Booking(
      id: (json['id'] as num).toInt(),
      userId: json['userId'] as String,
      roomId: (json['roomId'] as num?)?.toInt(),
      travelPackageId: (json['travelPackageId'] as num?)?.toInt(),
      hotelId: (json['hotelId'] as num?)?.toInt(),
      checkIn: DateTime.parse(json['checkIn'] as String),
      checkOut: DateTime.parse(json['checkOut'] as String),
      guests: (json['guests'] as num?)?.toInt() ?? 1,
      notes: json['notes'] as String?,
      status: (json['status'] as num).toInt(),
      totalPrice: (json['totalPrice'] as num).toDouble(),
      createdAt: DateTime.parse(json['createdAt'] as String),
      roomName: json['roomName'] as String?,
      hotelName: json['hotelName'] as String?,
      packageTitle: json['packageTitle'] as String?,
    );
  }

  String get statusLabel {
    switch (status) {
      case 0:
        return 'Pending';
      case 1:
        return 'Confirmed';
      case 2:
        return 'Cancelled';
      case 3:
        return 'Completed';
      default:
        return 'Unknown';
    }
  }

  String get title => packageTitle ?? hotelName ?? 'Booking #$id';
}
