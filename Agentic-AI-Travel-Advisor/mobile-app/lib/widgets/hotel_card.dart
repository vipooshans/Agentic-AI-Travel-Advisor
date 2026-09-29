import 'package:flutter/material.dart';
import '../models/hotel.dart';
import 'catalog_image.dart';

class HotelCard extends StatelessWidget {
  final Hotel hotel;
  final VoidCallback onTap;

  const HotelCard({super.key, required this.hotel, required this.onTap});

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
            CatalogImage(imageUrl: hotel.imageUrl, height: 160, fallbackIcon: Icons.hotel),
            Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(hotel.name, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                  const SizedBox(height: 4),
                  Text('${hotel.city}, ${hotel.country}', style: TextStyle(color: Colors.grey.shade600)),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Icon(Icons.meeting_room_outlined, size: 16, color: Colors.blue.shade700),
                      const SizedBox(width: 6),
                      Text('${hotel.roomCount} rooms', style: TextStyle(color: Colors.blue.shade700, fontWeight: FontWeight.w600)),
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
