import 'package:flutter/material.dart';
import '../models/hotel.dart';
import '../utils/format.dart';
import 'catalog_image.dart';
import 'icon_label.dart';

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
                  Wrap(
                    alignment: WrapAlignment.spaceBetween,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    spacing: 16,
                    runSpacing: 6,
                    children: [
                      Text.rich(TextSpan(children: [
                        iconLabel(
                          Icons.meeting_room_outlined,
                          '${hotel.roomCount} rooms',
                          iconColor: Colors.blue.shade700,
                          style: TextStyle(color: Colors.blue.shade700, fontWeight: FontWeight.w600),
                        ),
                        if (hotel.reviewCount > 0 && hotel.averageRating != null) ...[
                          iconLabelGap,
                          iconLabel(Icons.star, '${hotel.averageRating!.toStringAsFixed(1)} (${hotel.reviewCount})', iconColor: Colors.amber.shade700),
                        ],
                      ])),
                      if (hotel.minPricePerNight != null)
                        Text('from ${formatMoney(hotel.minPricePerNight!)}', style: const TextStyle(fontWeight: FontWeight.w600)),
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
