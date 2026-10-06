import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/booking.dart';
import '../providers/auth_provider.dart';
import '../services/booking_service.dart';
import '../services/review_service.dart';
import '../widgets/booking_card.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';
import '../widgets/write_review_sheet.dart';

class _BookingsData {
  final List<Booking> bookings;

  /// Null when the user's reviews could not be loaded; reviewing is then hidden.
  final Set<int>? reviewedBookingIds;

  const _BookingsData(this.bookings, this.reviewedBookingIds);
}

class BookingListScreen extends StatefulWidget {
  const BookingListScreen({super.key});

  @override
  State<BookingListScreen> createState() => _BookingListScreenState();
}

class _BookingListScreenState extends State<BookingListScreen> {
  late Future<_BookingsData> _future;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  void _reload() {
    setState(() {
      _future = _load();
    });
  }

  Future<_BookingsData> _load() async {
    final api = context.read<AuthProvider>().api;
    final reviewed = ReviewService(api)
        .mine()
        .then<Set<int>?>((reviews) => reviews.map((r) => r.bookingId).toSet(), onError: (Object _) => null);
    final results = await Future.wait<Object?>([BookingService(api).getAll(), reviewed]);
    return _BookingsData(results[0] as List<Booking>, results[1] as Set<int>?);
  }

  Future<void> _cancel(Booking booking) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancel booking?'),
        content: Text('Cancel ${booking.title}? This cannot be undone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Keep booking')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Cancel booking')),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      await BookingService(context.read<AuthProvider>().api).updateStatus(booking.id, Booking.cancelled);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking cancelled')));
      _reload();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(ErrorDisplayWidget.friendly(e))));
    }
  }

  Future<void> _review(Booking booking) async {
    final service = ReviewService(context.read<AuthProvider>().api);
    final submitted = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      builder: (_) => WriteReviewSheet(
        title: booking.title,
        onSubmit: (rating, comment) => service.create(bookingId: booking.id, rating: rating, comment: comment),
      ),
    );
    if (submitted != true || !mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Thanks for your review')));
    _reload();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Bookings')),
      body: FutureBuilder<_BookingsData>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) {
            return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: _reload);
          }
          final bookings = snapshot.data?.bookings ?? [];
          final reviewed = snapshot.data?.reviewedBookingIds;
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
            onRefresh: () async => _reload(),
            child: ListView.builder(
              padding: const EdgeInsets.all(12),
              itemCount: bookings.length,
              itemBuilder: (_, i) {
                final booking = bookings[i];
                final cancellable = booking.status == Booking.pending || booking.status == Booking.confirmed;
                final isCompleted = booking.status == Booking.completed;
                final alreadyReviewed = reviewed?.contains(booking.id) ?? false;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: BookingCard(
                    booking: booking,
                    onCancel: cancellable ? () => _cancel(booking) : null,
                    onReview: isCompleted && reviewed != null && !alreadyReviewed ? () => _review(booking) : null,
                    reviewed: isCompleted && alreadyReviewed,
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}
