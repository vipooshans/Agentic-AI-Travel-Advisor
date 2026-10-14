import 'package:flutter/material.dart';

import '../models/booking.dart';
import '../models/travel_plan.dart';
import '../utils/format.dart';

/// A quote from the assistant. It never says the booking exists; only the
/// backend's reply to a confirmation can do that.
class BookingProposalCard extends StatelessWidget {
  final BookingProposal proposal;
  final bool canConfirm;
  final VoidCallback? onConfirm;
  final DateTime? now;

  const BookingProposalCard({
    super.key,
    required this.proposal,
    this.canConfirm = false,
    this.onConfirm,
    this.now,
  });

  @override
  Widget build(BuildContext context) {
    final expired = proposal.isExpired(now);
    final dates = proposal.checkOut == null || proposal.checkOut!.isEmpty
        ? formatDateString(proposal.checkIn)
        : '${formatDateString(proposal.checkIn)} – ${formatDateString(proposal.checkOut)}';

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 8),
      color: Colors.blue.shade50,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Booking proposal', style: Theme.of(context).textTheme.labelLarge),
            const SizedBox(height: 4),
            Text(proposal.title, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Text('$dates · ${proposal.guests} guest${proposal.guests == 1 ? '' : 's'}'),
            Text(
              'Quoted total ${formatMoney(proposal.quotedTotal, proposal.currency)}',
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              'Nothing is booked until you confirm.',
              style: TextStyle(color: Colors.grey.shade700, fontSize: 12),
            ),
            const SizedBox(height: 8),
            if (expired)
              const Chip(label: Text('Expired'))
            else if (canConfirm)
              SizedBox(
                width: double.infinity,
                child: FilledButton(onPressed: onConfirm, child: const Text('Confirm booking')),
              )
            else
              const Chip(label: Text('Superseded')),
          ],
        ),
      ),
    );
  }
}

/// Shows the booking exactly as the backend returned it.
class BookingResultCard extends StatelessWidget {
  final int bookingId;
  final Booking? booking;
  final VoidCallback? onView;

  const BookingResultCard({super.key, required this.bookingId, this.booking, this.onView});

  @override
  Widget build(BuildContext context) {
    final b = booking;
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text('Booking #$bookingId', style: Theme.of(context).textTheme.titleMedium)),
                if (b != null) Chip(label: Text(b.statusLabel), visualDensity: VisualDensity.compact),
              ],
            ),
            if (b != null) ...[
              Text(b.title),
              Text('${formatDate(b.checkIn)} – ${formatDate(b.checkOut)} · ${b.guests} guest${b.guests == 1 ? '' : 's'}'),
              Text('Total ${formatMoney(b.totalPrice)}', style: const TextStyle(fontWeight: FontWeight.bold)),
              if (b.status == Booking.pending)
                Padding(
                  padding: const EdgeInsets.only(top: 6),
                  child: Text(
                    'The provider still has to confirm this booking.',
                    style: TextStyle(color: Colors.grey.shade700, fontSize: 12),
                  ),
                ),
            ],
            if (onView != null)
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(onPressed: onView, child: const Text('View my bookings')),
              ),
          ],
        ),
      ),
    );
  }
}
