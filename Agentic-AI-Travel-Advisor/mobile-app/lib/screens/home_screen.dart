import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/destination.dart';
import '../providers/auth_provider.dart';
import '../services/destination_service.dart';
import '../widgets/destination_card.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final service = DestinationService(auth.api);

    return Scaffold(
      appBar: AppBar(title: const Text('Travel Advisor')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Hello, ${auth.user?.firstName ?? 'Traveler'}!', style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 8),
            Text('Where would you like to go?', style: TextStyle(color: Colors.grey.shade600)),
            const SizedBox(height: 16),
            Card(
              child: ListTile(
                leading: const Icon(Icons.smart_toy, color: Colors.blue),
                title: const Text('AI Travel Assistant'),
                subtitle: const Text('Plan a trip in chat and save an itinerary'),
                onTap: () => context.go('/chat'),
              ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(child: _QuickLink(icon: Icons.explore, label: 'Destinations', onTap: () => context.go('/destinations'))),
                const SizedBox(width: 12),
                Expanded(child: _QuickLink(icon: Icons.hotel, label: 'Hotels', onTap: () => context.go('/destinations?tab=hotels'))),
                const SizedBox(width: 12),
                Expanded(child: _QuickLink(icon: Icons.card_travel, label: 'Packages', onTap: () => context.go('/destinations?tab=packages'))),
              ],
            ),
            const SizedBox(height: 24),
            Text('Featured Destinations', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            SizedBox(
              height: 200,
              child: FutureBuilder<List<Destination>>(
                future: service.getAll(),
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
                  if (snapshot.hasError) {
                    return ErrorDisplayWidget(message: snapshot.error.toString());
                  }
                  final items = snapshot.data ?? [];
                  if (items.isEmpty) {
                    return const EmptyStateWidget(
                      icon: Icons.public,
                      title: 'No destinations yet',
                      message: 'Check back soon for featured places to visit.',
                    );
                  }
                  return ListView.builder(
                    scrollDirection: Axis.horizontal,
                    itemCount: items.length,
                    itemBuilder: (_, i) => SizedBox(
                      width: 160,
                      child: Padding(
                        padding: const EdgeInsets.only(right: 12),
                        child: DestinationCard(
                          destination: items[i],
                          onTap: () => context.push('/destinations/${items[i].id}'),
                        ),
                      ),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _QuickLink extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _QuickLink({required this.icon, required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 16),
          child: Column(
            children: [
              Icon(icon, color: Colors.blue),
              const SizedBox(height: 8),
              Text(label, style: const TextStyle(fontSize: 12)),
            ],
          ),
        ),
      ),
    );
  }
}
