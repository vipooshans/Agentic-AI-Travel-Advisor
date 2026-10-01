import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/hotel.dart';
import '../providers/auth_provider.dart';
import '../services/hotel_service.dart';
import '../services/review_service.dart';
import '../utils/format.dart';
import '../widgets/catalog_detail_view.dart';
import '../widgets/error_widget.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/loading_widget.dart';
import '../widgets/reviews_section.dart';

class HotelDetailScreen extends StatefulWidget {
  final int id;
  const HotelDetailScreen({super.key, required this.id});

  @override
  State<HotelDetailScreen> createState() => _HotelDetailScreenState();
}

class _HotelDetailScreenState extends State<HotelDetailScreen> {
  late Future<Hotel> _future = _load();

  Future<Hotel> _load() => HotelService(context.read<AuthProvider>().api).getById(widget.id);

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return FutureBuilder<Hotel>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Scaffold(appBar: AppBar(title: const Text('Hotel Details')), body: const LoadingWidget());
        }
        if (snapshot.hasError) {
          return Scaffold(
            appBar: AppBar(title: const Text('Hotel Details')),
            body: ErrorDisplayWidget(
              message: snapshot.error.toString(),
              onRetry: () => setState(() {
                _future = _load();
              }),
            ),
          );
        }
        final hotel = snapshot.data!;

        return CatalogDetailView(
          title: hotel.name,
          imageUrl: hotel.imageUrl,
          fallbackIcon: Icons.hotel,
          children: [
            Row(
              children: [
                Icon(Icons.place_outlined, size: 18, color: Colors.blue.shade700),
                const SizedBox(width: 6),
                Expanded(child: Text('${hotel.city}, ${hotel.country}', style: TextStyle(color: Colors.grey.shade700, fontSize: 16))),
              ],
            ),
            const SizedBox(height: 4),
            Text(hotel.address, style: TextStyle(color: Colors.grey.shade600)),
            const SizedBox(height: 12),
            if (hotel.description != null) Text(hotel.description!, style: const TextStyle(height: 1.4, fontSize: 15)),
            const SizedBox(height: 24),
            Text('Available Rooms', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            if ((hotel.rooms ?? []).isEmpty)
              const EmptyStateWidget(
                icon: Icons.meeting_room_outlined,
                title: 'No rooms listed',
                message: 'This hotel has no rooms available to book yet.',
              )
            else
              ...(hotel.rooms ?? []).map((room) => Card(
                    margin: const EdgeInsets.only(bottom: 10),
                    child: ListTile(
                      title: Text(room.name),
                      subtitle: Text('${room.roomType} · ${room.capacity} guests · ${formatMoney(room.pricePerNight)}/night'),
                      trailing: room.isAvailable
                          ? FilledButton(
                              child: const Text('Book'),
                              onPressed: () => context.push('/bookings/new?roomId=${room.id}'),
                            )
                          : const Text('Unavailable', style: TextStyle(color: Colors.red)),
                    ),
                  )),
            const SizedBox(height: 24),
            ReviewsSection(
              load: () => ReviewService(api).forHotel(hotel.id),
              averageRating: hotel.averageRating,
              reviewCount: hotel.reviewCount,
            ),
          ],
        );
      },
    );
  }
}
