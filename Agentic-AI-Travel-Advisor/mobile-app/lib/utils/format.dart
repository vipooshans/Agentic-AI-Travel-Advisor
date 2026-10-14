/// Formats an amount the same way as the web app: `LKR 16,000` for whole
/// amounts and `LKR 10,368.50` otherwise.
String formatMoney(num amount, [String currency = 'LKR']) {
  final negative = amount < 0;
  final cents = (amount.abs() * 100).round();
  final whole = cents ~/ 100;
  final fraction = cents % 100;

  final digits = whole.toString();
  final grouped = StringBuffer();
  for (var i = 0; i < digits.length; i++) {
    if (i > 0 && (digits.length - i) % 3 == 0) grouped.write(',');
    grouped.write(digits[i]);
  }
  final decimals = fraction == 0 ? '' : '.${fraction.toString().padLeft(2, '0')}';
  return '$currency ${negative ? '-' : ''}$grouped$decimals';
}

String _two(int value) => value.toString().padLeft(2, '0');

/// `yyyy-MM-dd`, the date format the API accepts for query parameters.
String formatApiDate(DateTime date) => '${date.year}-${_two(date.month)}-${_two(date.day)}';

const _months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/// `5 Oct 2026`.
String formatDate(DateTime date) => '${date.day} ${_months[date.month - 1]} ${date.year}';

/// Formats an API date string, falling back to the raw value if it does not parse.
String formatDateString(String? value) {
  if (value == null || value.isEmpty) return '';
  final parsed = DateTime.tryParse(value);
  return parsed == null ? value : formatDate(parsed);
}

String formatDuration(int minutes) {
  if (minutes < 60) return '$minutes min';
  final hours = minutes ~/ 60;
  final rest = minutes % 60;
  return rest == 0 ? '$hours h' : '$hours h $rest min';
}

String formatRating(double? rating, int count) {
  if (rating == null || count == 0) return 'No reviews yet';
  return '${rating.toStringAsFixed(1)} / 5 ($count review${count == 1 ? '' : 's'})';
}
