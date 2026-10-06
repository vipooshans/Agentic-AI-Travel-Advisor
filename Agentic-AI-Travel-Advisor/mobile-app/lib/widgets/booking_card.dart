import 'package:flutter/material.dart';
import '../models/booking.dart';
import '../utils/format.dart';

class BookingCard extends StatelessWidget {
  final Booking booking;
  final VoidCallback? onCancel;
  final VoidCallback? onReview;
  final bool reviewed;

  const BookingCard({super.key, required this.booking, this.onCancel, this.onReview, this.reviewed = false});

  Color _statusColor() {
    switch (booking.status) {
      case Booking.confirmed:
        return Colors.green.shade700;
      case Booking.cancelled:
        return Colors.red.shade700;
      case Booking.completed:
        return Colors.blue.shade700;
      default:
        return Colors.orange.shade800;
    }
  }

  @override
  Widget build(BuildContext context) {
    final dates = booking.travelPackageId != null
        ? 'Starts ${formatDate(booking.checkIn)}'
        : '${formatDate(booking.checkIn)} → ${formatDate(booking.checkOut)}';
    return Card(
      child: Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Column(
          children: [
            ListTile(
              leading: Icon(booking.travelPackageId != null ? Icons.card_travel : Icons.hotel),
              title: Text(booking.title),
              subtitle: Text('$dates\n${booking.guests} guest${booking.guests == 1 ? '' : 's'}'),
              isThreeLine: true,
              trailing: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(formatMoney(booking.totalPrice), style: const TextStyle(fontWeight: FontWeight.bold)),
                  Text(booking.statusLabel, style: TextStyle(fontSize: 12, color: _statusColor())),
                ],
              ),
            ),
            if (onCancel != null || onReview != null || reviewed)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    if (reviewed)
                      Padding(
                        padding: const EdgeInsets.only(right: 12),
                        child: Text('Reviewed', style: TextStyle(color: Colors.grey.shade600)),
                      ),
                    if (onReview != null) TextButton(onPressed: onReview, child: const Text('Write a review')),
                    if (onCancel != null) TextButton(onPressed: onCancel, child: const Text('Cancel booking')),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}
