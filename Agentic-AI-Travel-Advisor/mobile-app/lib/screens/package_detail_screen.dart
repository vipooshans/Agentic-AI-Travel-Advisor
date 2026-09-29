import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../services/package_service.dart';
import '../widgets/catalog_detail_view.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class PackageDetailScreen extends StatelessWidget {
  final int id;
  const PackageDetailScreen({super.key, required this.id});

  @override
  Widget build(BuildContext context) {
    final service = PackageService(context.read<AuthProvider>().api);

    return FutureBuilder(
      future: service.getById(id),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Scaffold(appBar: AppBar(title: const Text('Package Details')), body: const LoadingWidget());
        }
        if (snapshot.hasError) {
          return Scaffold(appBar: AppBar(title: const Text('Package Details')), body: ErrorDisplayWidget(message: snapshot.error.toString()));
        }
        final pkg = snapshot.data!;

        return CatalogDetailView(
          title: pkg.title,
          imageUrl: pkg.imageUrl,
          fallbackIcon: Icons.card_travel,
          bottomBar: SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
              child: FilledButton(
                onPressed: () => context.push('/bookings/new?packageId=${pkg.id}'),
                child: Text('Book for \$${pkg.price.toStringAsFixed(0)}'),
              ),
            ),
          ),
          children: [
            Text('${pkg.destinationName}, ${pkg.destinationCountry}', style: TextStyle(color: Colors.grey.shade700, fontSize: 16)),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _InfoChip(icon: Icons.schedule, label: '${pkg.durationDays} days'),
                _InfoChip(icon: Icons.payments_outlined, label: '\$${pkg.price.toStringAsFixed(0)}'),
                if (pkg.activityCount > 0) _InfoChip(icon: Icons.hiking, label: '${pkg.activityCount} activities'),
              ],
            ),
            const SizedBox(height: 16),
            if (pkg.description != null) Text(pkg.description!, style: const TextStyle(height: 1.4, fontSize: 15)),
            if (pkg.activities != null && pkg.activities!.isNotEmpty) ...[
              const SizedBox(height: 24),
              Text('Activities', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              ...pkg.activities!.map((a) => Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      leading: CircleAvatar(child: Text('${a.dayNumber}')),
                      title: Text(a.title),
                      subtitle: Text(a.description ?? ''),
                      trailing: a.price > 0 ? Text('+\$${a.price.toStringAsFixed(0)}') : null,
                    ),
                  )),
            ],
          ],
        );
      },
    );
  }
}

class _InfoChip extends StatelessWidget {
  final IconData icon;
  final String label;

  const _InfoChip({required this.icon, required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.blue.shade50,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: Colors.blue.shade800),
          const SizedBox(width: 6),
          Text(label, style: TextStyle(color: Colors.blue.shade800, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}
