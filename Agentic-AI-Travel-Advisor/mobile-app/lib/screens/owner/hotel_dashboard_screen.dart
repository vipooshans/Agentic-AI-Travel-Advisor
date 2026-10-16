import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../models/booking.dart';
import '../../models/hotel.dart';
import '../../models/report_summary.dart';
import '../../models/room.dart';
import '../../providers/auth_provider.dart';
import '../../services/admin_service.dart';
import '../../services/booking_service.dart';
import '../../services/hotel_service.dart';
import '../../utils/format.dart';
import '../../widgets/empty_state_widget.dart';
import '../../widgets/error_widget.dart' as app_err;
import '../../widgets/loading_widget.dart';

class HotelDashboardScreen extends StatefulWidget {
  const HotelDashboardScreen({super.key});

  @override
  State<HotelDashboardScreen> createState() => _HotelDashboardScreenState();
}

class _HotelDashboardScreenState extends State<HotelDashboardScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  List<Hotel> _myHotels = [];
  List<Booking> _bookings = [];
  ReportSummary? _summary;

  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _loadData();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    final api = context.read<AuthProvider>().api;
    final hotelService = HotelService(api);
    final bookingService = BookingService(api);
    final adminService = AdminService(api);

    try {
      final results = await Future.wait([
        hotelService.getMine(),
        bookingService.getAll(),
        adminService.getSummary().catchError((_) => const ReportSummary()),
      ]);

      if (mounted) {
        setState(() {
          _myHotels = results[0] as List<Hotel>;
          _bookings = results[1] as List<Booking>;
          _summary = results[2] as ReportSummary;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString().replaceFirst('Exception: ', '');
          _loading = false;
        });
      }
    }
  }

  Future<void> _showCreateHotelDialog() async {
    final nameController = TextEditingController();
    final addressController = TextEditingController();
    final cityController = TextEditingController();
    final countryController = TextEditingController(text: 'Sri Lanka');
    final descriptionController = TextEditingController();
    final imageController = TextEditingController();
    final formKey = GlobalKey<FormState>();

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => Padding(
        padding: EdgeInsets.only(
          left: 20,
          right: 20,
          top: 20,
          bottom: MediaQuery.of(context).viewInsets.bottom + 20,
        ),
        child: Form(
          key: formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Add Hotel Property', style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 16),
                TextFormField(
                  controller: nameController,
                  decoration: const InputDecoration(labelText: 'Hotel Name', border: OutlineInputBorder()),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Enter hotel name' : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: addressController,
                  decoration: const InputDecoration(labelText: 'Address', border: OutlineInputBorder()),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Enter address' : null,
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: cityController,
                        decoration: const InputDecoration(labelText: 'City', border: OutlineInputBorder()),
                        validator: (v) => v == null || v.trim().isEmpty ? 'Enter city' : null,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextFormField(
                        controller: countryController,
                        decoration: const InputDecoration(labelText: 'Country', border: OutlineInputBorder()),
                        validator: (v) => v == null || v.trim().isEmpty ? 'Enter country' : null,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: descriptionController,
                  maxLines: 2,
                  decoration: const InputDecoration(labelText: 'Description', border: OutlineInputBorder()),
                ),
                const SizedBox(height: 20),
                FilledButton(
                  onPressed: () async {
                    if (!formKey.currentState!.validate()) return;
                    Navigator.pop(ctx);
                    final api = this.context.read<AuthProvider>().api;
                    try {
                      await HotelService(api).createHotel(
                        name: nameController.text,
                        address: addressController.text,
                        city: cityController.text,
                        country: countryController.text,
                        description: descriptionController.text,
                        imageUrl: imageController.text,
                      );
                      if (mounted) {
                        ScaffoldMessenger.of(this.context).showSnackBar(
                          const SnackBar(content: Text('Hotel added successfully')),
                        );
                        _loadData();
                      }
                    } catch (e) {
                      if (mounted) {
                        ScaffoldMessenger.of(this.context).showSnackBar(
                          SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red),
                        );
                      }
                    }
                  },
                  child: const Text('Add Hotel'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _manageRooms(Hotel hotel) async {
    final api = context.read<AuthProvider>().api;
    final hotelService = HotelService(api);

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => StatefulBuilder(
        builder: (context, setModalState) {
          return FutureBuilder<List<Room>>(
            future: hotelService.getRooms(hotel.id),
            builder: (context, snapshot) {
              final rooms = snapshot.data ?? [];
              final loadingRooms = snapshot.connectionState == ConnectionState.waiting;

              return Container(
                height: MediaQuery.of(context).size.height * 0.75,
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Text(
                            'Rooms - ${hotel.name}',
                            style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                          ),
                        ),
                        IconButton(
                          icon: const Icon(Icons.add_circle, color: Colors.blue),
                          tooltip: 'Add Room',
                          onPressed: () async {
                            await _showAddRoomDialog(hotel);
                            setModalState(() {});
                          },
                        ),
                      ],
                    ),
                    const Divider(),
                    if (loadingRooms)
                      const Expanded(child: LoadingWidget(message: 'Loading rooms...'))
                    else if (rooms.isEmpty)
                      const Expanded(
                        child: EmptyStateWidget(
                          icon: Icons.hotel_outlined,
                          title: 'No rooms configured',
                          message: 'Tap the "+" icon above to add rooms to this hotel.',
                        ),
                      )
                    else
                      Expanded(
                        child: ListView.builder(
                          itemCount: rooms.length,
                          itemBuilder: (context, i) {
                            final r = rooms[i];
                            return Card(
                              margin: const EdgeInsets.only(bottom: 8),
                              child: ListTile(
                                leading: const Icon(Icons.bed, color: Colors.amber),
                                title: Text(r.name),
                                subtitle: Text('${r.roomType} • Max ${r.capacity} guests • ${formatMoney(r.pricePerNight)}/night'),
                                trailing: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Switch(
                                      value: r.isAvailable,
                                      onChanged: (val) async {
                                        await hotelService.setRoomAvailability(hotel.id, r.id, val);
                                        setModalState(() {});
                                      },
                                    ),
                                    IconButton(
                                      icon: const Icon(Icons.delete_outline, size: 20, color: Colors.red),
                                      onPressed: () async {
                                        await hotelService.deleteRoom(hotel.id, r.id);
                                        setModalState(() {});
                                      },
                                    ),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
                      ),
                  ],
                ),
              );
            },
          );
        },
      ),
    );

    _loadData();
  }

  Future<void> _showAddRoomDialog(Hotel hotel) async {
    final nameController = TextEditingController();
    final typeController = TextEditingController(text: 'Deluxe');
    final priceController = TextEditingController();
    final capacityController = TextEditingController(text: '2');
    final formKey = GlobalKey<FormState>();

    await showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Add Room to ${hotel.name}'),
        content: Form(
          key: formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: nameController,
                  decoration: const InputDecoration(labelText: 'Room Name', border: OutlineInputBorder()),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Enter room name' : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: typeController,
                  decoration: const InputDecoration(labelText: 'Room Type (e.g. Deluxe, Suite)', border: OutlineInputBorder()),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Enter room type' : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: priceController,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(labelText: 'Price per night (LKR)', border: OutlineInputBorder()),
                  validator: (v) => (double.tryParse(v ?? '') ?? 0) > 0 ? null : 'Valid price',
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: capacityController,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Capacity (Guests)', border: OutlineInputBorder()),
                  validator: (v) => (int.tryParse(v ?? '') ?? 0) > 0 ? null : 'Valid capacity',
                ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
          FilledButton(
            onPressed: () async {
              if (!formKey.currentState!.validate()) return;
              Navigator.pop(ctx);
              final api = this.context.read<AuthProvider>().api;
              try {
                await HotelService(api).createRoom(
                  hotelId: hotel.id,
                  name: nameController.text,
                  roomType: typeController.text,
                  pricePerNight: double.parse(priceController.text),
                  capacity: int.parse(capacityController.text),
                );
                if (mounted) {
                  ScaffoldMessenger.of(this.context).showSnackBar(
                    const SnackBar(content: Text('Room added successfully')),
                  );
                }
              } catch (e) {
                if (mounted) {
                  ScaffoldMessenger.of(this.context).showSnackBar(
                    SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red),
                  );
                }
              }
            },
            child: const Text('Add Room'),
          ),
        ],
      ),
    );
  }

  Future<void> _deleteHotel(Hotel hotel) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete hotel?'),
        content: Text('Delete "${hotel.name}"? This cannot be undone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;
    final api = context.read<AuthProvider>().api;
    try {
      await HotelService(api).deleteHotel(hotel.id);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Hotel deleted')));
        _loadData();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red));
      }
    }
  }

  Future<void> _updateBookingStatus(Booking booking, int status) async {
    final api = context.read<AuthProvider>().api;
    try {
      await BookingService(api).updateStatus(booking.id, status);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking updated')));
        _loadData();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final totalRooms = _myHotels.fold<int>(0, (sum, h) => sum + h.roomCount);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Hotels Dashboard'),
        actions: [
          if (user?.isAdmin == true)
            IconButton(
              icon: const Icon(Icons.admin_panel_settings),
              tooltip: 'Admin Dashboard',
              onPressed: () => context.push('/admin'),
            ),
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadData),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            Tab(icon: const Icon(Icons.apartment_outlined), text: 'My Hotels (${_myHotels.length})'),
            Tab(icon: const Icon(Icons.bookmark_outline), text: 'Bookings (${_bookings.length})'),
          ],
        ),
      ),
      floatingActionButton: _tabController.index == 0
          ? FloatingActionButton.extended(
              onPressed: _showCreateHotelDialog,
              icon: const Icon(Icons.add_business),
              label: const Text('Add Hotel'),
            )
          : null,
      body: _loading
          ? const LoadingWidget(message: 'Loading hotels dashboard...')
          : _error != null
              ? app_err.ErrorDisplayWidget(message: _error!, onRetry: _loadData)
              : TabBarView(
                  controller: _tabController,
                  children: [
                    _buildHotelsTab(totalRooms),
                    _buildBookingsTab(),
                  ],
                ),
    );
  }

  Widget _buildHotelsTab(int totalRooms) {
    final s = _summary ?? const ReportSummary();
    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Stat Cards
          Row(
            children: [
              Expanded(
                child: _SmallMetric(
                  title: 'My Hotels',
                  value: '${_myHotels.length}',
                  icon: Icons.apartment,
                  color: Colors.amber,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _SmallMetric(
                  title: 'Total Rooms',
                  value: '$totalRooms',
                  icon: Icons.bed,
                  color: Colors.blue,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _SmallMetric(
                  title: 'Revenue',
                  value: formatMoney(s.revenue),
                  icon: Icons.payments_outlined,
                  color: Colors.green,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          if (_myHotels.isEmpty)
            const EmptyStateWidget(
              icon: Icons.apartment_outlined,
              title: 'No hotels listed yet',
              message: 'Tap "+ Add Hotel" below to register your hotel property.',
            )
          else
            ..._myHotels.map((h) => Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(h.name, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                            ),
                            _ApprovalChip(status: h.approvalStatus, label: h.approvalStatusLabel),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Text('${h.address}, ${h.city}, ${h.country}'),
                        Text('${h.roomCount} rooms configured • Rating: ${h.averageRating != null ? h.averageRating!.toStringAsFixed(1) : "N/A"}'),
                        const SizedBox(height: 8),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.end,
                          children: [
                            TextButton.icon(
                              onPressed: () => _manageRooms(h),
                              icon: const Icon(Icons.meeting_room_outlined, size: 16),
                              label: const Text('Manage Rooms'),
                            ),
                            IconButton(
                              icon: const Icon(Icons.visibility_outlined),
                              tooltip: 'Preview details',
                              onPressed: () => context.push('/hotels/${h.id}'),
                            ),
                            IconButton(
                              icon: const Icon(Icons.delete_outline, color: Colors.red),
                              tooltip: 'Delete hotel',
                              onPressed: () => _deleteHotel(h),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                )),
        ],
      ),
    );
  }

  Widget _buildBookingsTab() {
    if (_bookings.isEmpty) {
      return const EmptyStateWidget(
        icon: Icons.bookmark_border,
        title: 'No bookings received',
        message: 'When guests reserve rooms at your properties, they will appear here.',
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: _bookings.length,
      itemBuilder: (context, i) {
        final b = _bookings[i];
        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(b.title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                    Chip(
                      label: Text(b.statusLabel, style: const TextStyle(fontSize: 10)),
                      visualDensity: VisualDensity.compact,
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text('Check-in: ${formatDate(b.checkIn)} • Check-out: ${formatDate(b.checkOut)}'),
                Text('Guests: ${b.guests} • Total: ${formatMoney(b.totalPrice)}', style: const TextStyle(fontWeight: FontWeight.w600)),
                if (b.notes != null && b.notes!.isNotEmpty) Text('Notes: ${b.notes}'),
                const SizedBox(height: 8),
                if (b.status == Booking.pending)
                  Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      OutlinedButton(
                        onPressed: () => _updateBookingStatus(b, Booking.cancelled),
                        style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
                        child: const Text('Decline'),
                      ),
                      const SizedBox(width: 8),
                      FilledButton(
                        onPressed: () => _updateBookingStatus(b, Booking.confirmed),
                        child: const Text('Confirm'),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _SmallMetric extends StatelessWidget {
  final String title;
  final String value;
  final IconData icon;
  final Color color;

  const _SmallMetric({required this.title, required this.value, required this.icon, required this.color});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            Icon(icon, color: color, size: 20),
            const SizedBox(height: 4),
            Text(value, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
            Text(title, style: TextStyle(fontSize: 10, color: Colors.grey.shade600)),
          ],
        ),
      ),
    );
  }
}

class _ApprovalChip extends StatelessWidget {
  final int status;
  final String label;

  const _ApprovalChip({required this.status, required this.label});

  @override
  Widget build(BuildContext context) {
    Color bg;
    Color fg;
    switch (status) {
      case 1:
        bg = Colors.green.shade50;
        fg = Colors.green.shade800;
      case 2:
        bg = Colors.red.shade50;
        fg = Colors.red.shade800;
      default:
        bg = Colors.orange.shade50;
        fg = Colors.orange.shade800;
    }
    return Chip(
      label: Text(label, style: TextStyle(fontSize: 10, color: fg)),
      backgroundColor: bg,
      visualDensity: VisualDensity.compact,
      padding: EdgeInsets.zero,
    );
  }
}
