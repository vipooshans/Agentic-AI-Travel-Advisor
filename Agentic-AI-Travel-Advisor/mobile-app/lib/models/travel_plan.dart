int _int(Object? value, [int fallback = 0]) => (value as num?)?.toInt() ?? fallback;
int? _intOrNull(Object? value) => (value as num?)?.toInt();
double _double(Object? value) => (value as num?)?.toDouble() ?? 0;
double? _doubleOrNull(Object? value) => (value as num?)?.toDouble();
bool _bool(Object? value) => value as bool? ?? false;

List<T> _list<T>(Object? value, T Function(Map<String, dynamic>) parse) =>
    (value as List? ?? const []).map((e) => parse(e as Map<String, dynamic>)).toList();

/// The structured, tool-grounded plan returned by `POST /api/ai/chat`.
class TravelPlan {
  final String destination;
  final int? destinationId;
  final String? country;
  final int duration;
  final int nights;
  final String startDate;
  final String endDate;
  final int travelers;
  final double budget;
  final String currency;
  final double estimatedTotal;
  final bool withinBudget;
  final double accommodationCost;
  final double packagesCost;
  final double transportationCost;
  final List<PlanHotel> hotels;
  final List<PlanPackage> travelPackages;
  final List<PlanActivity> activities;
  final List<PlanTransport> transportation;
  final List<PlanDay> itinerary;
  final List<String> assumptions;
  final List<String> warnings;

  const TravelPlan({
    required this.destination,
    this.destinationId,
    this.country,
    required this.duration,
    required this.nights,
    required this.startDate,
    required this.endDate,
    required this.travelers,
    required this.budget,
    this.currency = 'LKR',
    required this.estimatedTotal,
    required this.withinBudget,
    this.accommodationCost = 0,
    this.packagesCost = 0,
    this.transportationCost = 0,
    this.hotels = const [],
    this.travelPackages = const [],
    this.activities = const [],
    this.transportation = const [],
    this.itinerary = const [],
    this.assumptions = const [],
    this.warnings = const [],
  });

  factory TravelPlan.fromJson(Map<String, dynamic> json) {
    final costs = json['costBreakdown'] as Map<String, dynamic>? ?? const {};
    return TravelPlan(
      destination: json['destination'] as String? ?? '',
      destinationId: _intOrNull(json['destinationId']),
      country: json['country'] as String?,
      duration: _int(json['duration']),
      nights: _int(json['nights']),
      startDate: json['startDate'] as String? ?? '',
      endDate: json['endDate'] as String? ?? '',
      travelers: _int(json['travelers'], 1),
      budget: _double(json['budget']),
      currency: json['currency'] as String? ?? 'LKR',
      estimatedTotal: _double(json['estimatedTotal']),
      withinBudget: _bool(json['withinBudget']),
      accommodationCost: _double(costs['accommodation']),
      packagesCost: _double(costs['packages']),
      transportationCost: _double(costs['transportation']),
      hotels: _list(json['hotels'], PlanHotel.fromJson),
      travelPackages: _list(json['travelPackages'], PlanPackage.fromJson),
      activities: _list(json['activities'], PlanActivity.fromJson),
      transportation: _list(json['transportation'], PlanTransport.fromJson),
      itinerary: _list(json['itinerary'], PlanDay.fromJson),
      assumptions: (json['assumptions'] as List? ?? const []).map((e) => e.toString()).toList(),
      warnings: (json['warnings'] as List? ?? const []).map((e) => e.toString()).toList(),
    );
  }

  PlanHotel? get selectedHotel => hotels.where((h) => h.selected).firstOrNull;
  PlanPackage? get selectedPackage => travelPackages.where((p) => p.selected).firstOrNull;
  List<PlanTransport> get selectedTransport => transportation.where((t) => t.selected).toList();

  /// Body for `POST /api/itineraries`, matching the web app's mapping.
  Map<String, dynamic> toItineraryRequest() {
    final parts = [selectedHotel?.name, selectedPackage?.title].whereType<String>().toList();
    final summary = StringBuffer('$travelers traveler${travelers == 1 ? '' : 's'}, $duration days');
    if (parts.isNotEmpty) summary.write(' · ${parts.join(' + ')}');
    final time = RegExp(r'^\d{2}:\d{2}$');

    return {
      'title': '$destination trip',
      'startDate': startDate,
      'endDate': endDate,
      'destinationId': destinationId,
      'estimatedCost': estimatedTotal,
      'summary': summary.toString(),
      'items': [
        for (final day in itinerary)
          for (var i = 0; i < day.items.length; i++)
            {
              'dayNumber': day.day,
              'title': day.items[i].title,
              'description': day.items[i].description,
              'startTime': time.hasMatch(day.items[i].time) ? '${day.items[i].time}:00' : null,
              'sortOrder': i,
            },
      ],
    };
  }
}

class PlanHotel {
  final int hotelId;
  final int roomId;
  final String name;
  final String roomName;
  final String city;
  final double pricePerNight;
  final int nights;
  final int rooms;
  final int capacity;
  final double totalCost;
  final double? averageRating;
  final bool selected;
  final bool availabilityChecked;

  const PlanHotel({
    required this.hotelId,
    required this.roomId,
    required this.name,
    required this.roomName,
    required this.city,
    required this.pricePerNight,
    required this.nights,
    required this.rooms,
    required this.capacity,
    required this.totalCost,
    this.averageRating,
    this.selected = false,
    this.availabilityChecked = false,
  });

  factory PlanHotel.fromJson(Map<String, dynamic> json) => PlanHotel(
        hotelId: _int(json['hotelId']),
        roomId: _int(json['roomId']),
        name: json['name'] as String? ?? '',
        roomName: json['roomName'] as String? ?? '',
        city: json['city'] as String? ?? '',
        pricePerNight: _double(json['pricePerNight']),
        nights: _int(json['nights']),
        rooms: _int(json['rooms'], 1),
        capacity: _int(json['capacity']),
        totalCost: _double(json['totalCost']),
        averageRating: _doubleOrNull(json['averageRating']),
        selected: _bool(json['selected']),
        availabilityChecked: _bool(json['availabilityChecked']),
      );
}

class PlanPackage {
  final int packageId;
  final String title;
  final int durationDays;
  final double pricePerPerson;
  final double totalCost;
  final int? remainingPlaces;
  final double? averageRating;
  final bool selected;
  final bool availabilityChecked;

  const PlanPackage({
    required this.packageId,
    required this.title,
    required this.durationDays,
    required this.pricePerPerson,
    required this.totalCost,
    this.remainingPlaces,
    this.averageRating,
    this.selected = false,
    this.availabilityChecked = false,
  });

  factory PlanPackage.fromJson(Map<String, dynamic> json) => PlanPackage(
        packageId: _int(json['packageId']),
        title: json['title'] as String? ?? '',
        durationDays: _int(json['durationDays']),
        pricePerPerson: _double(json['pricePerPerson']),
        totalCost: _double(json['totalCost']),
        remainingPlaces: _intOrNull(json['remainingPlaces']),
        averageRating: _doubleOrNull(json['averageRating']),
        selected: _bool(json['selected']),
        availabilityChecked: _bool(json['availabilityChecked']),
      );
}

class PlanActivity {
  final String title;
  final String? category;
  final int day;
  final double pricePerPerson;
  final int? packageId;
  final bool includedInCost;

  const PlanActivity({
    required this.title,
    this.category,
    required this.day,
    required this.pricePerPerson,
    this.packageId,
    this.includedInCost = false,
  });

  factory PlanActivity.fromJson(Map<String, dynamic> json) => PlanActivity(
        title: json['title'] as String? ?? '',
        category: json['category'] as String?,
        day: _int(json['day']),
        pricePerPerson: _double(json['pricePerPerson']),
        packageId: _intOrNull(json['packageId']),
        includedInCost: _bool(json['includedInCost']),
      );
}

class PlanTransport {
  final int transportationId;
  final String mode;
  final String from;
  final String to;
  final String? departureTime;
  final int durationMinutes;
  final double pricePerPerson;
  final int trips;
  final double totalCost;
  final bool selected;

  const PlanTransport({
    required this.transportationId,
    required this.mode,
    required this.from,
    required this.to,
    this.departureTime,
    required this.durationMinutes,
    required this.pricePerPerson,
    required this.trips,
    required this.totalCost,
    this.selected = false,
  });

  factory PlanTransport.fromJson(Map<String, dynamic> json) => PlanTransport(
        transportationId: _int(json['transportationId']),
        mode: json['mode']?.toString() ?? '',
        from: json['from'] as String? ?? '',
        to: json['to'] as String? ?? '',
        departureTime: json['departureTime'] as String?,
        durationMinutes: _int(json['durationMinutes']),
        pricePerPerson: _double(json['pricePerPerson']),
        trips: _int(json['trips'], 1),
        totalCost: _double(json['totalCost']),
        selected: _bool(json['selected']),
      );
}

class PlanDay {
  final int day;
  final String date;
  final List<PlanDayItem> items;

  const PlanDay({required this.day, required this.date, this.items = const []});

  factory PlanDay.fromJson(Map<String, dynamic> json) => PlanDay(
        day: _int(json['day']),
        date: json['date'] as String? ?? '',
        items: _list(json['items'], PlanDayItem.fromJson),
      );
}

class PlanDayItem {
  final String time;
  final String type;
  final String title;
  final String? description;

  const PlanDayItem({required this.time, required this.type, required this.title, this.description});

  factory PlanDayItem.fromJson(Map<String, dynamic> json) => PlanDayItem(
        time: json['time'] as String? ?? '',
        type: json['type'] as String? ?? '',
        title: json['title'] as String? ?? '',
        description: json['description'] as String?,
      );
}

/// A booking the assistant has quoted but not created. Nothing is booked
/// until the user confirms it and the backend accepts the booking.
class BookingProposal {
  final String id;
  final String kind;
  final int? hotelId;
  final int? roomId;
  final int? travelPackageId;
  final String title;
  final String checkIn;
  final String? checkOut;
  final int guests;
  final double quotedTotal;
  final String currency;
  final DateTime expiresAt;

  const BookingProposal({
    required this.id,
    required this.kind,
    this.hotelId,
    this.roomId,
    this.travelPackageId,
    required this.title,
    required this.checkIn,
    this.checkOut,
    required this.guests,
    required this.quotedTotal,
    this.currency = 'LKR',
    required this.expiresAt,
  });

  factory BookingProposal.fromJson(Map<String, dynamic> json) => BookingProposal(
        id: json['id'].toString(),
        kind: json['kind'] as String? ?? 'room',
        hotelId: _intOrNull(json['hotelId']),
        roomId: _intOrNull(json['roomId']),
        travelPackageId: _intOrNull(json['travelPackageId']),
        title: json['title'] as String? ?? 'Booking',
        checkIn: json['checkIn'] as String? ?? '',
        checkOut: json['checkOut'] as String?,
        guests: _int(json['guests'], 1),
        quotedTotal: _double(json['quotedTotal']),
        currency: json['currency'] as String? ?? 'LKR',
        expiresAt: DateTime.tryParse(json['expiresAt'] as String? ?? '') ?? DateTime.fromMillisecondsSinceEpoch(0),
      );

  bool isExpired([DateTime? now]) => !(now ?? DateTime.now()).isBefore(expiresAt);
}
