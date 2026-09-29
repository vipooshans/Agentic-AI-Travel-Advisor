import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../services/destination_service.dart';
import '../services/package_service.dart';
import '../widgets/catalog_detail_view.dart';
import '../widgets/error_widget.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/loading_widget.dart';
import '../widgets/package_card.dart';

class DestinationDetailScreen extends StatelessWidget {
  final int id;
  const DestinationDetailScreen({super.key, required this.id});

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;
    final destService = DestinationService(api);
    final pkgService = PackageService(api);

    return FutureBuilder(
      future: Future.wait([destService.getById(id), pkgService.getAll(destinationId: id)]),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Scaffold(appBar: AppBar(title: const Text('Destination')), body: const LoadingWidget());
        }
        if (snapshot.hasError) {
          return Scaffold(appBar: AppBar(title: const Text('Destination')), body: ErrorDisplayWidget(message: snapshot.error.toString()));
        }
        final dest = (snapshot.data as List)[0];
        final packages = (snapshot.data as List)[1] as List;

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
          ],
        );
      },
    );
  }
}
