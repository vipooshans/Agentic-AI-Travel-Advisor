import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/itinerary.dart';
import '../providers/auth_provider.dart';
import '../services/itinerary_service.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class ItineraryListScreen extends StatefulWidget {
  const ItineraryListScreen({super.key});

  @override
  State<ItineraryListScreen> createState() => _ItineraryListScreenState();
}

class _ItineraryListScreenState extends State<ItineraryListScreen> {
  late Future<List<Itinerary>> _future;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<List<Itinerary>> _load() => ItineraryService(context.read<AuthProvider>().api).getAll();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Saved itineraries')),
      body: FutureBuilder<List<Itinerary>>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) {
            return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = _load()));
          }
          final items = snapshot.data ?? [];
          if (items.isEmpty) {
            return EmptyStateWidget(
              icon: Icons.map_outlined,
              title: 'No saved itineraries',
              message: 'Chat with the AI assistant to plan a trip, then save it.',
              actionLabel: 'Open AI chat',
              onAction: () => context.go('/chat'),
            );
          }
          return RefreshIndicator(
            onRefresh: () async => setState(() => _future = _load()),
            child: ListView.builder(
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
            ),
          );
        },
      ),
    );
  }
}
