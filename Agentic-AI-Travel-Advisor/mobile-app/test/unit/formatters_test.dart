import 'package:flutter_test/flutter_test.dart';
import 'package:travel_advisor/utils/formatters.dart';

void main() {
  group('TravelFormatters', () {
    test('formats currency properly into LKR', () {
      expect(TravelFormatters.formatCurrency(0), 'Rs. 0');
      expect(TravelFormatters.formatCurrency(500), 'Rs. 500');
      expect(TravelFormatters.formatCurrency(25000), 'Rs. 25,000');
      expect(TravelFormatters.formatCurrency(1500000), 'Rs. 1,500,000');
    });

    test('formats duration into days and nights', () {
      expect(TravelFormatters.formatDuration(1, 1), '1 Day / 1 Night');
      expect(TravelFormatters.formatDuration(3, 2), '3 Days / 2 Nights');
      expect(TravelFormatters.formatDuration(5, 4), '5 Days / 4 Nights');
    });

    test('formats night counts with pluralization', () {
      expect(TravelFormatters.formatNights(1), '1 night');
      expect(TravelFormatters.formatNights(3), '3 nights');
    });

    test('formats date range within same month', () {
      final start = DateTime(2026, 9, 20);
      final end = DateTime(2026, 9, 25);
      final res = TravelFormatters.formatDateRange(start, end);
      expect(res, 'Sep 20 - 25, 2026 (5 nights)');
    });

    test('formats date range across different months', () {
      final start = DateTime(2026, 9, 28);
      final end = DateTime(2026, 10, 2);
      final res = TravelFormatters.formatDateRange(start, end);
      expect(res, 'Sep 28 - Oct 2, 2026 (4 nights)');
    });
  });
}
