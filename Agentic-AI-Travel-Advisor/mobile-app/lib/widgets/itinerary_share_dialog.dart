import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../models/itinerary.dart';
import '../utils/formatters.dart';

/// Modal dialog providing a shareable formatted markdown summary of an itinerary.
class ItineraryShareDialog extends StatelessWidget {
  final Itinerary itinerary;

  const ItineraryShareDialog({super.key, required this.itinerary});

  String _generateShareText() {
    final buffer = StringBuffer();
    buffer.writeln('🏝️ Travel Plan: ${itinerary.title}');
    if (itinerary.destinationName != null) {
      buffer.writeln('📍 Destination: ${itinerary.destinationName}');
    }
    buffer.writeln('🗓️ Dates: ${itinerary.dateRange}');
    if (itinerary.estimatedCost != null) {
      buffer.writeln('💰 Estimated Budget: ${TravelFormatters.formatCurrency(itinerary.estimatedCost!)}');
    }
    buffer.writeln('\n--- Daily Schedule ---');
    final grouped = <int, List<ItineraryItem>>{};
    for (final item in itinerary.items) {
      grouped.putIfAbsent(item.dayNumber, () => []).add(item);
    }
    for (final day in grouped.keys.toList()..sort()) {
      buffer.writeln('Day $day:');
      for (final item in grouped[day]!) {
        final time = item.timeLabel.isNotEmpty ? '${item.timeLabel} - ' : '';
        buffer.writeln('  • $time${item.title}');
        if (item.description != null && item.description!.isNotEmpty) {
          buffer.writeln('    ${item.description}');
        }
      }
    }
    buffer.writeln('\nGenerated with Agentic AI Travel Advisor');
    return buffer.toString();
  }

  @override
  Widget build(BuildContext context) {
    final shareText = _generateShareText();
    return AlertDialog(
      title: const Text('Share Itinerary'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Copy your itinerary summary to share with your fellow travelers:',
              style: TextStyle(fontSize: 14),
            ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.grey.shade100,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.grey.shade300),
              ),
              child: Text(
                shareText,
                style: const TextStyle(fontFamily: 'monospace', fontSize: 12),
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Close'),
        ),
        ElevatedButton.icon(
          icon: const Icon(Icons.copy, size: 18),
          label: const Text('Copy to Clipboard'),
          onPressed: () async {
            await Clipboard.setData(ClipboardData(text: shareText));
            if (context.mounted) {
              Navigator.of(context).pop();
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Itinerary copied to clipboard!')),
              );
            }
          },
        ),
      ],
    );
  }
}
