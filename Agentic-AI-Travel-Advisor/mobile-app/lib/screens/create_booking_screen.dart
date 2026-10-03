import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../services/booking_service.dart';
import '../utils/formatters.dart';

class CreateBookingScreen extends StatefulWidget {
  final int? roomId;
  final int? packageId;

  const CreateBookingScreen({super.key, this.roomId, this.packageId});

  @override
  State<CreateBookingScreen> createState() => _CreateBookingScreenState();
}

class _CreateBookingScreenState extends State<CreateBookingScreen> {
  DateTime _checkIn = DateTime.now().add(const Duration(days: 7));
  DateTime _checkOut = DateTime.now().add(const Duration(days: 10));
  int _guestCount = 2;
  bool _loading = false;
  String? _error;

  int get _nights {
    final diff = _checkOut.difference(_checkIn).inDays;
    return diff > 0 ? diff : 1;
  }

  Future<void> _submit() async {
    setState(() { _loading = true; _error = null; });
    try {
      final service = BookingService(context.read<AuthProvider>().api);
      await service.create(
        roomId: widget.roomId,
        travelPackageId: widget.packageId,
        checkIn: _checkIn,
        checkOut: widget.roomId != null ? _checkOut : null,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking created successfully!')));
        context.go('/bookings');
      }
    } catch (e) {
      setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _pickDateRange() async {
    final picked = await showDateRangePicker(
      context: context,
      initialDateRange: DateTimeRange(start: _checkIn, end: _checkOut),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      setState(() {
        _checkIn = picked.start;
        _checkOut = picked.end;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final isRoom = widget.roomId != null;

    return Scaffold(
      appBar: AppBar(title: Text(isRoom ? 'Book Hotel Room' : 'Book Travel Package')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(isRoom ? 'Hotel Stay Booking' : 'Curated Tour Package', style: Theme.of(context).textTheme.titleMedium),
                    const SizedBox(height: 8),
                    Text(
                      TravelFormatters.formatDateRange(_checkIn, _checkOut),
                      style: const TextStyle(fontWeight: FontWeight.w600, color: Color(0xFF1E56A0)),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            ListTile(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12), side: BorderSide(color: Colors.grey.shade300)),
              title: const Text('Travel Dates'),
              subtitle: Text('${_checkIn.toString().split(' ')[0]} to ${_checkOut.toString().split(' ')[0]} (${TravelFormatters.formatNights(_nights)})'),
              trailing: const Icon(Icons.date_range, color: Color(0xFF1E56A0)),
              onTap: _pickDateRange,
            ),
            const SizedBox(height: 16),
            ListTile(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12), side: BorderSide(color: Colors.grey.shade300)),
              title: const Text('Travelers / Guests'),
              subtitle: Text('$_guestCount Guests'),
              trailing: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  IconButton(
                    icon: const Icon(Icons.remove_circle_outline),
                    onPressed: _guestCount > 1 ? () => setState(() => _guestCount--) : null,
                  ),
                  Text('$_guestCount', style: const TextStyle(fontWeight: FontWeight.bold)),
                  IconButton(
                    icon: const Icon(Icons.add_circle_outline),
                    onPressed: _guestCount < 8 ? () => setState(() => _guestCount++) : null,
                  ),
                ],
              ),
            ),
            if (_error != null) ...[
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(color: Colors.red.shade50, borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: TextStyle(color: Colors.red.shade700)),
              ),
            ],
            const SizedBox(height: 32),
            FilledButton(
              onPressed: _loading ? null : _submit,
              style: FilledButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
              child: _loading
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Confirm Booking'),
            ),
          ],
        ),
      ),
    );
  }
}
