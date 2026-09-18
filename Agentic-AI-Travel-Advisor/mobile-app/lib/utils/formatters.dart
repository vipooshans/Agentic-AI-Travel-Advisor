/// String and numerical formatting utilities for travel data representation.
class TravelFormatters {
  /// Formats currency values in Sri Lankan Rupee (LKR) notation with comma thousands grouping.
  static String formatCurrency(num amount) {
    final int rounded = amount.round();
    final String str = rounded.toString();
    final RegExp reg = RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))');
    final formatted = str.replaceAllMapped(reg, (Match m) => '${m[1]},');
    return 'Rs. $formatted';
  }

  /// Formats duration string (e.g. "3 Days / 2 Nights").
  static String formatDuration(int days, int nights) {
    final dayStr = days == 1 ? '1 Day' : '$days Days';
    final nightStr = nights == 1 ? '1 Night' : '$nights Nights';
    return '$dayStr / $nightStr';
  }

  /// Formats nights with appropriate pluralization.
  static String formatNights(int nights) {
    return nights == 1 ? '1 night' : '$nights nights';
  }

  /// Formats date ranges cleanly with human-readable night duration suffix.
  static String formatDateRange(DateTime start, DateTime end) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    final startMonth = months[start.month - 1];
    final endMonth = months[end.month - 1];
    final nights = end.difference(start).inDays;
    final nightsLabel = formatNights(nights > 0 ? nights : 1);

    if (start.month == end.month && start.year == end.year) {
      return '$startMonth ${start.day} - ${end.day}, ${start.year} ($nightsLabel)';
    }
    return '$startMonth ${start.day} - $endMonth ${end.day}, ${end.year} ($nightsLabel)';
  }
}
