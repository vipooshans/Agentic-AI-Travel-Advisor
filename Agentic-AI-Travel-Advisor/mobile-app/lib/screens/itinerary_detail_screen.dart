import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../models/itinerary.dart';
import '../providers/auth_provider.dart';
import '../services/itinerary_service.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class ItineraryDetailScreen extends StatelessWidget {
  final int id;
  const ItineraryDetailScreen({super.key, required this.id});

  @override
  Widget build(BuildContext context) {
    final service = ItineraryService(context.read<AuthProvider>().api);

    return Scaffold(
      appBar: AppBar(title: const Text('Itinerary')),
      body: FutureBuilder<Itinerary>(
        future: service.getById(id),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString());
          final itinerary = snapshot.data;
          if (itinerary == null) return const ErrorDisplayWidget(message: 'Itinerary not found.');

          final grouped = <int, List<ItineraryItem>>{};
          for (final item in itinerary.items) {
            grouped.putIfAbsent(item.dayNumber, () => []).add(item);
          }

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(itinerary.title, style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: 8),
              Text('${itinerary.destinationName ?? 'Trip'} · ${itinerary.dateRange}'),
              Text('Status: ${itinerary.statusLabel}', style: TextStyle(color: Colors.grey.shade600)),
              if (itinerary.estimatedCost != null)
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Text('Estimated cost: Rs. ${itinerary.estimatedCost!.toStringAsFixed(0)}', style: const TextStyle(fontWeight: FontWeight.bold)),
                ),
              if (itinerary.summary != null && itinerary.summary!.isNotEmpty) ...[
                const SizedBox(height: 12),
                Text(itinerary.summary!, style: TextStyle(color: Colors.grey.shade700)),
              ],
              const SizedBox(height: 16),
              for (final day in grouped.keys.toList()..sort()) ...[
                Text('Day $day', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                ...grouped[day]!.map(
                  (item) => Card(
                    child: ListTile(
                      leading: item.timeLabel.isEmpty ? const Icon(Icons.place_outlined) : Text(item.timeLabel, style: const TextStyle(fontWeight: FontWeight.w600)),
                      title: Text(item.title),
                      subtitle: item.description == null ? null : Text(item.description!),
                    ),
                  ),
                ),
                const SizedBox(height: 12),
              ],
            ],
          );
        },
      ),
    );
  }
}
