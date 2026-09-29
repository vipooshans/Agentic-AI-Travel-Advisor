import 'package:flutter/material.dart';
import '../models/travel_package.dart';
import 'catalog_image.dart';

class PackageCard extends StatelessWidget {
  final TravelPackage package;
  final VoidCallback onTap;

  const PackageCard({super.key, required this.package, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: Colors.grey.shade200),
      ),
      child: InkWell(
        onTap: onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Stack(
              children: [
                CatalogImage(imageUrl: package.imageUrl, height: 160, fallbackIcon: Icons.card_travel),
                Positioned(
                  right: 12,
                  bottom: 12,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      '\$${package.price.toStringAsFixed(0)}',
                      style: TextStyle(fontWeight: FontWeight.w800, color: Colors.blue.shade800),
                    ),
                  ),
                ),
              ],
            ),
            Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(package.title, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                  const SizedBox(height: 4),
                  Text('${package.destinationName}, ${package.destinationCountry}', style: TextStyle(color: Colors.grey.shade600)),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Icon(Icons.schedule, size: 16, color: Colors.blue.shade700),
                      const SizedBox(width: 6),
                      Text('${package.durationDays} days', style: TextStyle(color: Colors.blue.shade700, fontWeight: FontWeight.w600)),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
