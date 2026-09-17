import 'package:flutter/material.dart';
import '../models/travel_package.dart';

class PackageCard extends StatelessWidget {
  final TravelPackage package;
  final VoidCallback onTap;

  const PackageCard({super.key, required this.package, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: const CircleAvatar(child: Icon(Icons.card_travel)),
        title: Text(package.title),
        subtitle: Text('${package.destinationName} · ${package.durationDays} days'),
        trailing: Text('\$${package.price.toStringAsFixed(0)}', style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.blue)),
        onTap: onTap,
      ),
    );
  }
}
