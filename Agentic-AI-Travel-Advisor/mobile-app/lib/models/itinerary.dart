class Itinerary {
  final int id;
  final String title;
  final DateTime startDate;
  final DateTime endDate;
  final int status;
  final double? estimatedCost;
  final int? destinationId;
  final String? destinationName;
  final String? summary;
  final int itemCount;
  final List<ItineraryItem> items;

  const Itinerary({
    required this.id,
    required this.title,
    required this.startDate,
    required this.endDate,
    required this.status,
    this.estimatedCost,
    this.destinationId,
    this.destinationName,
    this.summary,
    this.itemCount = 0,
    this.items = const [],
  });

  factory Itinerary.fromJson(Map<String, dynamic> json) {
    return Itinerary(
      id: (json['id'] as num).toInt(),
      title: json['title'] as String,
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: DateTime.parse(json['endDate'] as String),
      status: (json['status'] as num?)?.toInt() ?? 0,
      estimatedCost: (json['estimatedCost'] as num?)?.toDouble(),
      destinationId: json['destinationId'] as int?,
      destinationName: json['destinationName'] as String?,
      summary: json['summary'] as String?,
      itemCount: (json['itemCount'] as num?)?.toInt() ?? 0,
      items: json['items'] != null
          ? (json['items'] as List).map((i) => ItineraryItem.fromJson(i as Map<String, dynamic>)).toList()
          : const [],
    );
  }

  String get statusLabel {
    switch (status) {
      case 1:
        return 'Active';
      case 2:
        return 'Completed';
      case 3:
        return 'Archived';
      default:
        return 'Draft';
    }
  }

  String get dateRange {
    String fmt(DateTime d) => d.toLocal().toString().split(' ').first;
    return '${fmt(startDate)} → ${fmt(endDate)}';
  }
}

class ItineraryItem {
  final int id;
  final int dayNumber;
  final String title;
  final String? description;
  final String? startTime;
  final int sortOrder;

  const ItineraryItem({
    required this.id,
    required this.dayNumber,
    required this.title,
    this.description,
    this.startTime,
    required this.sortOrder,
  });

  factory ItineraryItem.fromJson(Map<String, dynamic> json) {
    return ItineraryItem(
      id: (json['id'] as num).toInt(),
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
