import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/itinerary.dart';
import '../providers/auth_provider.dart';
import '../services/itinerary_service.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class ItineraryListScreen extends StatelessWidget {
  const ItineraryListScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final service = ItineraryService(context.read<AuthProvider>().api);

    return Scaffold(
      appBar: AppBar(title: const Text('Saved itineraries')),
      body: FutureBuilder<List<Itinerary>>(
        future: service.getAll(),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString());
          final items = snapshot.data ?? [];
          if (items.isEmpty) {
            return const Center(child: Text('No saved itineraries yet. Chat with the AI to create one.'));
          }
          return ListView.builder(
            padding: const EdgeInsets.all(12),
            itemCount: items.length,
            itemBuilder: (_, i) {
              final itinerary = items[i];
              return Card(
                child: ListTile(
                  leading: const Icon(Icons.map_outlined, color: Colors.blue),
                  title: Text(itinerary.title),
                  subtitle: Text('${itinerary.destinationName ?? 'Trip'} · ${itinerary.dateRange}'),
                  trailing: itinerary.estimatedCost != null
                      ? Text('Rs. ${itinerary.estimatedCost!.toStringAsFixed(0)}', style: const TextStyle(fontWeight: FontWeight.bold))
                      : Text(itinerary.statusLabel, style: TextStyle(color: Colors.grey.shade600, fontSize: 12)),
                  onTap: () => context.push('/itineraries/${itinerary.id}'),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
