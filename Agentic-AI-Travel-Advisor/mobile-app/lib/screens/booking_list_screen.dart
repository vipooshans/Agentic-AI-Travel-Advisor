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

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<List<Booking>> _load() => BookingService(context.read<AuthProvider>().api).getAll();

  Future<void> _cancel(Booking booking) async {
    try {
      await BookingService(context.read<AuthProvider>().api).updateStatus(booking.id, 2);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking cancelled')));
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
      body: FutureBuilder(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) {
            return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = _load()));
          }
          final bookings = snapshot.data ?? [];
          if (bookings.isEmpty) {
            return EmptyStateWidget(
              icon: Icons.bookmark_border,
              title: 'No bookings yet',
              message: 'Explore destinations and book a hotel or package.',
              actionLabel: 'Explore',
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
    );
  }
}
