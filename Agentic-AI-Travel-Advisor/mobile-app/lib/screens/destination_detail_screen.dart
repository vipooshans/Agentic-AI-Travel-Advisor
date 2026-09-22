import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../services/destination_service.dart';
import '../services/package_service.dart';
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

    return Scaffold(
      appBar: AppBar(title: const Text('Destination')),
      body: FutureBuilder(
        future: Future.wait([destService.getById(id), pkgService.getAll(destinationId: id)]),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString());
          final dest = (snapshot.data as List)[0];
          final packages = (snapshot.data as List)[1] as List;

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (dest.imageUrl != null)
                  ClipRRect(borderRadius: BorderRadius.circular(12), child: Image.network(dest.imageUrl!, height: 200, width: double.infinity, fit: BoxFit.cover)),
                const SizedBox(height: 16),
                Text(dest.name, style: Theme.of(context).textTheme.headlineSmall),
                Text(dest.country, style: TextStyle(color: Colors.grey.shade600, fontSize: 16)),
                const SizedBox(height: 12),
                if (dest.description != null) Text(dest.description!),
                const SizedBox(height: 8),
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
                    padding: const EdgeInsets.only(bottom: 8),
                    child: PackageCard(package: p, onTap: () => context.push('/packages/${p.id}')),
                  )),
              ],
            ),
          );
        },
      ),
    );
  }
}
