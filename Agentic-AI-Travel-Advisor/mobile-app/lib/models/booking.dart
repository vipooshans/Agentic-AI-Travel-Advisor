class Booking {
  final int id;
  final String userId;
  final int? roomId;
  final int? travelPackageId;
  final DateTime checkIn;
  final DateTime checkOut;
  final int status;
  final double totalPrice;
  final DateTime createdAt;
  final String? roomName;
  final String? hotelName;
  final String? packageTitle;

  const Booking({
    required this.id,
    required this.userId,
    this.roomId,
    this.travelPackageId,
    required this.checkIn,
    required this.checkOut,
    required this.status,
    required this.totalPrice,
    required this.createdAt,
    this.roomName,
    this.hotelName,
    this.packageTitle,
  });

  factory Booking.fromJson(Map<String, dynamic> json) {
    return Booking(
      id: json['id'] as int,
      userId: json['userId'] as String,
      roomId: json['roomId'] as int?,
      travelPackageId: json['travelPackageId'] as int?,
      checkIn: DateTime.parse(json['checkIn'] as String),
      checkOut: DateTime.parse(json['checkOut'] as String),
      status: json['status'] as int,
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
