class SuggestedPlan {
  final String title;
  final int? destinationId;
  final String destinationName;
  final DateTime startDate;
  final DateTime endDate;
  final int travelers;
  final double budget;
  final double estimatedCost;
  final String? summary;
  final SuggestedHotel? hotel;
  final SuggestedPackage? package;
  final List<SuggestedPlanItem> items;

  const SuggestedPlan({
    required this.title,
    this.destinationId,
    required this.destinationName,
    required this.startDate,
    required this.endDate,
    required this.travelers,
    required this.budget,
    required this.estimatedCost,
    this.summary,
    this.hotel,
    this.package,
    this.items = const [],
  });

  factory SuggestedPlan.fromJson(Map<String, dynamic> json) {
    return SuggestedPlan(
      title: json['title'] as String? ?? 'Travel plan',
      destinationId: json['destinationId'] as int?,
      destinationName: json['destinationName'] as String? ?? '',
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: DateTime.parse(json['endDate'] as String),
      travelers: (json['travelers'] as num?)?.toInt() ?? 1,
      budget: (json['budget'] as num?)?.toDouble() ?? 0,
      estimatedCost: (json['estimatedCost'] as num?)?.toDouble() ?? 0,
      summary: json['summary'] as String?,
      hotel: json['hotel'] != null ? SuggestedHotel.fromJson(json['hotel'] as Map<String, dynamic>) : null,
      package: json['package'] != null ? SuggestedPackage.fromJson(json['package'] as Map<String, dynamic>) : null,
      items: json['items'] != null
          ? (json['items'] as List).map((i) => SuggestedPlanItem.fromJson(i as Map<String, dynamic>)).toList()
          : const [],
    );
  }

  Map<String, dynamic> toCreateRequest() {
    return {
      'title': title,
      'startDate': startDate.toUtc().toIso8601String(),
      'endDate': endDate.toUtc().toIso8601String(),
      if (destinationId != null) 'destinationId': destinationId,
      'estimatedCost': estimatedCost,
      if (summary != null) 'summary': summary,
      'items': items
          .map((i) => {
                'dayNumber': i.dayNumber,
                'title': i.title,
                if (i.description != null) 'description': i.description,
                if (i.startTime != null) 'startTime': i.startTime,
                'sortOrder': i.sortOrder,
              })
          .toList(),
    };
  }
}

class SuggestedHotel {
  final int id;
  final String name;
  final String city;
  final int? roomId;
  final String? roomName;
  final double pricePerNight;

  const SuggestedHotel({
    required this.id,
    required this.name,
    required this.city,
    this.roomId,
    this.roomName,
    required this.pricePerNight,
  });

  factory SuggestedHotel.fromJson(Map<String, dynamic> json) {
    return SuggestedHotel(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String,
      city: json['city'] as String? ?? '',
      roomId: json['roomId'] as int?,
      roomName: json['roomName'] as String?,
      pricePerNight: (json['pricePerNight'] as num?)?.toDouble() ?? 0,
    );
  }
}

class SuggestedPackage {
  final int id;
  final String title;
  final double price;
  final int durationDays;

  const SuggestedPackage({
    required this.id,
    required this.title,
    required this.price,
    required this.durationDays,
  });

  factory SuggestedPackage.fromJson(Map<String, dynamic> json) {
    return SuggestedPackage(
      id: (json['id'] as num).toInt(),
      title: json['title'] as String,
      price: (json['price'] as num).toDouble(),
      durationDays: (json['durationDays'] as num?)?.toInt() ?? 0,
    );
  }
}

class SuggestedPlanItem {
  final int dayNumber;
  final String title;
  final String? description;
  final String? startTime;
  final int sortOrder;

  const SuggestedPlanItem({
    required this.dayNumber,
    required this.title,
    this.description,
    this.startTime,
    required this.sortOrder,
  });

  factory SuggestedPlanItem.fromJson(Map<String, dynamic> json) {
    return SuggestedPlanItem(
      dayNumber: (json['dayNumber'] as num).toInt(),
      title: json['title'] as String,
      description: json['description'] as String?,
      startTime: json['startTime']?.toString(),
      sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
    );
  }

  String get timeLabel {
    if (startTime == null || startTime!.isEmpty) return '';
    final value = startTime!;
    if (value.length >= 5) return value.substring(0, 5);
    return value;
  }
}
