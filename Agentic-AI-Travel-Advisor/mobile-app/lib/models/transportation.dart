class Transportation {
  static const _modes = ['Bus', 'Train', 'Car', 'Van', 'Tuk-tuk', 'Flight', 'Ferry'];

  final int id;
  final int mode;
  final String fromLocation;
  final String toLocation;
  final String? departureTime;
  final int durationMinutes;
  final double pricePerPerson;
  final int capacity;
  final String? description;
  final int? travelPackageId;
  final int? destinationId;

  const Transportation({
    required this.id,
    required this.mode,
    required this.fromLocation,
    required this.toLocation,
    this.departureTime,
    required this.durationMinutes,
    required this.pricePerPerson,
    required this.capacity,
    this.description,
    this.travelPackageId,
    this.destinationId,
  });

  String get modeLabel => mode >= 0 && mode < _modes.length ? _modes[mode] : 'Transport';

  /// `HH:mm` from the API's `HH:mm:ss` time of day.
  String? get departureLabel {
    final value = departureTime;
    if (value == null || value.isEmpty) return null;
    return value.length >= 5 ? value.substring(0, 5) : value;
  }

  factory Transportation.fromJson(Map<String, dynamic> json) {
    return Transportation(
      id: (json['id'] as num).toInt(),
      mode: (json['mode'] as num?)?.toInt() ?? -1,
      fromLocation: json['fromLocation'] as String? ?? '',
      toLocation: json['toLocation'] as String? ?? '',
      departureTime: json['departureTime'] as String?,
      durationMinutes: (json['durationMinutes'] as num?)?.toInt() ?? 0,
      pricePerPerson: (json['pricePerPerson'] as num?)?.toDouble() ?? 0,
      capacity: (json['capacity'] as num?)?.toInt() ?? 0,
      description: json['description'] as String?,
      travelPackageId: (json['travelPackageId'] as num?)?.toInt(),
      destinationId: (json['destinationId'] as num?)?.toInt(),
    );
  }
}
