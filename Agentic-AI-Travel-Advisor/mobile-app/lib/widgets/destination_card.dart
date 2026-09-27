import 'package:flutter/material.dart';
import '../models/destination.dart';

class DestinationCard extends StatelessWidget {
  final Destination destination;
  final VoidCallback onTap;

  const DestinationCard({super.key, required this.destination, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (destination.imageUrl != null)
              Image.network(destination.imageUrl!, height: 120, width: double.infinity, fit: BoxFit.cover,
                  errorBuilder: (_, __, ___) => Container(height: 120, color: Colors.blue.shade100, child: const Icon(Icons.landscape, size: 48)))
            else
              Container(height: 120, color: Colors.blue.shade100, child: const Icon(Icons.landscape, size: 48)),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(destination.name, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                  Text(destination.country, style: TextStyle(color: Colors.grey.shade600)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
