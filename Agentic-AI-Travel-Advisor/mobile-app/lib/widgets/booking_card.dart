import 'package:flutter/material.dart';
import '../models/booking.dart';

class BookingCard extends StatelessWidget {
  final Booking booking;
  final VoidCallback? onCancel;

  const BookingCard({super.key, required this.booking, this.onCancel});

  Color _statusColor() {
    switch (booking.status) {
      case 1:
        return Colors.green.shade700;
      case 2:
        return Colors.red.shade700;
      case 3:
        return Colors.blue.shade700;
      default:
        return Colors.orange.shade800;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Column(
          children: [
            ListTile(
              leading: Icon(booking.travelPackageId != null ? Icons.card_travel : Icons.hotel),
              title: Text(booking.title),
              subtitle: Text('${booking.checkIn.toString().split(' ')[0]} → ${booking.checkOut.toString().split(' ')[0]}'),
              trailing: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text('Rs. ${booking.totalPrice.toStringAsFixed(0)}', style: const TextStyle(fontWeight: FontWeight.bold)),
                  Text(booking.statusLabel, style: TextStyle(fontSize: 12, color: _statusColor())),
                ],
              ),
            ),
            if (onCancel != null)
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: onCancel,
                  child: const Text('Cancel booking'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
