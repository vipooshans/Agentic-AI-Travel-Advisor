class TravelPreferences {
  final double? budgetMin;
  final double? budgetMax;
  final String? preferredClimate;
  final String? interests;

  const TravelPreferences({
    this.budgetMin,
    this.budgetMax,
    this.preferredClimate,
    this.interests,
  });

  factory TravelPreferences.fromJson(Map<String, dynamic> json) {
    return TravelPreferences(
      budgetMin: (json['budgetMin'] as num?)?.toDouble(),
      budgetMax: (json['budgetMax'] as num?)?.toDouble(),
      preferredClimate: json['preferredClimate'] as String?,
      interests: json['interests'] as String?,
    );
  }
}
