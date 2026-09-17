import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../services/package_service.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class PackageDetailScreen extends StatelessWidget {
  final int id;
  const PackageDetailScreen({super.key, required this.id});

  @override
  Widget build(BuildContext context) {
    final service = PackageService(context.read<AuthProvider>().api);

    return Scaffold(
      appBar: AppBar(title: const Text('Package Details')),
      body: FutureBuilder(
        future: service.getById(id),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString());
          final pkg = snapshot.data!;

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(pkg.title, style: Theme.of(context).textTheme.headlineSmall),
                Text('${pkg.destinationName}, ${pkg.destinationCountry}', style: TextStyle(color: Colors.grey.shade600)),
                const SizedBox(height: 8),
                Text('${pkg.durationDays} days · \$${pkg.price.toStringAsFixed(0)}', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18, color: Colors.blue)),
                const SizedBox(height: 12),
                if (pkg.description != null) Text(pkg.description!),
                if (pkg.activities != null && pkg.activities!.isNotEmpty) ...[
                  const SizedBox(height: 24),
                  Text('Activities', style: Theme.of(context).textTheme.titleMedium),
                  ...pkg.activities!.map((a) => ListTile(
                    leading: CircleAvatar(child: Text('${a.dayNumber}')),
                    title: Text(a.title),
                    subtitle: Text(a.description ?? ''),
                    trailing: a.price > 0 ? Text('+\$${a.price.toStringAsFixed(0)}') : null,
                  )),
                ],
                const SizedBox(height: 24),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton(
                    onPressed: () => context.push('/bookings/new?packageId=${pkg.id}'),
                    child: const Text('Book This Package'),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
