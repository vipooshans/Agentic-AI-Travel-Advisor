import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/travel_package.dart';
import '../providers/auth_provider.dart';
import '../services/package_service.dart';
import '../services/review_service.dart';
import '../services/transportation_service.dart';
import '../utils/format.dart';
import '../widgets/catalog_detail_view.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';
import '../widgets/reviews_section.dart';
import '../widgets/transport_section.dart';

class PackageDetailScreen extends StatefulWidget {
  final int id;
  const PackageDetailScreen({super.key, required this.id});

  @override
  State<PackageDetailScreen> createState() => _PackageDetailScreenState();
}

class _PackageDetailScreenState extends State<PackageDetailScreen> {
  late Future<TravelPackage> _future = _load();

  Future<TravelPackage> _load() => PackageService(context.read<AuthProvider>().api).getById(widget.id);

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return FutureBuilder<TravelPackage>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Scaffold(appBar: AppBar(title: const Text('Package Details')), body: const LoadingWidget());
        }
        if (snapshot.hasError) {
          return Scaffold(
            appBar: AppBar(title: const Text('Package Details')),
            body: ErrorDisplayWidget(
              message: snapshot.error.toString(),
              onRetry: () => setState(() {
                _future = _load();
              }),
            ),
          );
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
                child: Text('Book from ${formatMoney(pkg.pricePerPerson)} per person'),
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
                _InfoChip(icon: Icons.payments_outlined, label: '${formatMoney(pkg.pricePerPerson)} pp'),
                if (pkg.activityCount > 0) _InfoChip(icon: Icons.hiking, label: '${pkg.activityCount} activities'),
                if (pkg.maxTravelers != null && pkg.maxTravelers! > 0)
                  _InfoChip(icon: Icons.groups_outlined, label: 'Up to ${pkg.maxTravelers} travelers'),
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
                      trailing: a.price > 0 ? Text('+${formatMoney(a.price)}') : null,
                    ),
                  )),
            ],
            TransportSection(load: () => TransportationService(api).search(travelPackageId: pkg.id)),
            const SizedBox(height: 24),
            ReviewsSection(
              load: () => ReviewService(api).forPackage(pkg.id),
              averageRating: pkg.averageRating,
              reviewCount: pkg.reviewCount,
            ),
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
