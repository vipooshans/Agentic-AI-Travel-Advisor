import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/destination.dart';
import '../models/travel_package.dart';
import '../providers/auth_provider.dart';
import '../services/destination_service.dart';
import '../services/package_service.dart';
import '../services/transportation_service.dart';
import '../widgets/catalog_detail_view.dart';
import '../widgets/error_widget.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/loading_widget.dart';
import '../widgets/package_card.dart';
import '../widgets/transport_section.dart';

class DestinationDetailScreen extends StatefulWidget {
  final int id;
  const DestinationDetailScreen({super.key, required this.id});

  @override
  State<DestinationDetailScreen> createState() => _DestinationDetailScreenState();
}

class _DestinationDetailScreenState extends State<DestinationDetailScreen> {
  late Future<List<Object>> _future = _load();

  Future<List<Object>> _load() {
    final api = context.read<AuthProvider>().api;
    return Future.wait<Object>([
      DestinationService(api).getById(widget.id),
      PackageService(api).getAll(destinationId: widget.id),
    ]);
  }

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return FutureBuilder(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Scaffold(appBar: AppBar(title: const Text('Destination')), body: const LoadingWidget());
        }
        if (snapshot.hasError) {
          return Scaffold(
            appBar: AppBar(title: const Text('Destination')),
            body: ErrorDisplayWidget(
              message: snapshot.error.toString(),
              onRetry: () => setState(() {
                _future = _load();
              }),
            ),
          );
        }
        final dest = snapshot.data![0] as Destination;
        final packages = snapshot.data![1] as List<TravelPackage>;

        return CatalogDetailView(
          title: dest.name,
          imageUrl: dest.imageUrl,
          fallbackIcon: Icons.landscape,
          children: [
            Text(dest.country, style: TextStyle(color: Colors.grey.shade700, fontSize: 16)),
            const SizedBox(height: 12),
            if (dest.description != null) ...[
              Text(dest.description!, style: const TextStyle(height: 1.4, fontSize: 15)),
              const SizedBox(height: 12),
            ],
            Text('${dest.packageCount ?? packages.length} travel packages available'),
            const SizedBox(height: 24),
            Text('Packages', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            if (packages.isEmpty)
              const EmptyStateWidget(
                icon: Icons.card_travel,
                title: 'No packages',
                message: 'There are no approved packages for this destination yet.',
              )
            else
              ...packages.map((p) => Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: PackageCard(package: p, onTap: () => context.push('/packages/${p.id}')),
                  )),
            TransportSection(load: () => TransportationService(api).search(destinationId: widget.id)),
          ],
        );
      },
    );
  }
}
