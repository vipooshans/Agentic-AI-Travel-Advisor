import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/booking.dart';
import '../providers/auth_provider.dart';
import '../services/booking_service.dart';
import '../widgets/booking_card.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class BookingListScreen extends StatefulWidget {
  const BookingListScreen({super.key});

  @override
  State<BookingListScreen> createState() => _BookingListScreenState();
}

class _BookingListScreenState extends State<BookingListScreen> {
  late Future<List<Booking>> _future;
  int _selectedFilterStatus = -1; // -1 = All, 0 = Pending, 1 = Confirmed, 2 = Cancelled, 3 = Completed

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<List<Booking>> _load() => BookingService(context.read<AuthProvider>().api).getAll();

  Future<void> _cancel(Booking booking) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cancel Booking?'),
        content: Text('Are you sure you want to cancel booking #${booking.id}? This cannot be undone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Keep Booking')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Yes, Cancel'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    try {
      await BookingService(context.read<AuthProvider>().api).updateStatus(booking.id, 2);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking cancelled successfully')));
      setState(() => _future = _load());
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(ErrorDisplayWidget.friendly(e))));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Bookings')),
      body: Column(
        children: [
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [
                ChoiceChip(
                  label: const Text('All'),
                  selected: _selectedFilterStatus == -1,
                  onSelected: (_) => setState(() => _selectedFilterStatus = -1),
                ),
                const SizedBox(width: 8),
                ChoiceChip(
                  label: const Text('Pending'),
                  selected: _selectedFilterStatus == 0,
                  onSelected: (_) => setState(() => _selectedFilterStatus = 0),
                ),
                const SizedBox(width: 8),
                ChoiceChip(
                  label: const Text('Confirmed'),
                  selected: _selectedFilterStatus == 1,
                  onSelected: (_) => setState(() => _selectedFilterStatus = 1),
                ),
                const SizedBox(width: 8),
                ChoiceChip(
                  label: const Text('Completed'),
                  selected: _selectedFilterStatus == 3,
                  onSelected: (_) => setState(() => _selectedFilterStatus = 3),
                ),
                const SizedBox(width: 8),
                ChoiceChip(
                  label: const Text('Cancelled'),
                  selected: _selectedFilterStatus == 2,
                  onSelected: (_) => setState(() => _selectedFilterStatus = 2),
                ),
              ],
            ),
          ),
          Expanded(
            child: FutureBuilder<List<Booking>>(
              future: _future,
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
                if (snapshot.hasError) {
                  return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = _load()));
                }
                var bookings = snapshot.data ?? [];
                if (_selectedFilterStatus != -1) {
                  bookings = bookings.where((b) => b.status == _selectedFilterStatus).toList();
                }
                if (bookings.isEmpty) {
                  return EmptyStateWidget(
                    icon: Icons.bookmark_border,
                    title: 'No bookings found',
                    message: _selectedFilterStatus == -1
                        ? 'Explore destinations and book a hotel or tour package.'
                        : 'No bookings match the selected status filter.',
                    actionLabel: 'Explore Destinations',
                    onAction: () => context.go('/destinations'),
                  );
                }
                return RefreshIndicator(
                  onRefresh: () async => setState(() => _future = _load()),
                  child: ListView.builder(
                    padding: const EdgeInsets.all(12),
                    itemCount: bookings.length,
                    itemBuilder: (_, i) => Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: BookingCard(
                        booking: bookings[i],
                        onCancel: bookings[i].status == 0 ? () => _cancel(bookings[i]) : null,
                      ),
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
