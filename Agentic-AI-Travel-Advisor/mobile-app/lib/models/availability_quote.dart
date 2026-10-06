class AvailabilityQuote {
  final bool available;
  final String? reason;
  final int nights;
  final int guests;
  final double totalPrice;
  final int? remainingPlaces;

  const AvailabilityQuote({
    required this.available,
    this.reason,
    required this.nights,
    required this.guests,
    required this.totalPrice,
    this.remainingPlaces,
  });

  factory AvailabilityQuote.fromJson(Map<String, dynamic> json) {
    return AvailabilityQuote(
      available: json['available'] as bool? ?? false,
      reason: json['reason'] as String?,
      nights: (json['nights'] as num?)?.toInt() ?? 0,
      guests: (json['guests'] as num?)?.toInt() ?? 1,
      totalPrice: (json['totalPrice'] as num?)?.toDouble() ?? 0,
      remainingPlaces: (json['remainingPlaces'] as num?)?.toInt(),
    );
  }
}
