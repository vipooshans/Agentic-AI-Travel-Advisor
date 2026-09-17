import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../services/booking_service.dart';

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
  bool _loading = false;
  String? _error;

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
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking created!')));
        context.go('/bookings');
      }
    } catch (e) {
      setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _pickDate(bool isCheckIn) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: isCheckIn ? _checkIn : _checkOut,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      setState(() {
        if (isCheckIn) {
          _checkIn = picked;
          if (_checkOut.isBefore(_checkIn)) _checkOut = _checkIn.add(const Duration(days: 1));
        } else {
          _checkOut = picked;
        }
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Create Booking')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(widget.roomId != null ? 'Room Booking' : 'Package Booking', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 24),
            ListTile(
              title: const Text('Check-in'),
              subtitle: Text(_checkIn.toString().split(' ')[0]),
              trailing: const Icon(Icons.calendar_today),
              onTap: () => _pickDate(true),
            ),
            if (widget.roomId != null)
              ListTile(
                title: const Text('Check-out'),
                subtitle: Text(_checkOut.toString().split(' ')[0]),
                trailing: const Icon(Icons.calendar_today),
                onTap: () => _pickDate(false),
              ),
            if (_error != null) ...[
              const SizedBox(height: 16),
              Text(_error!, style: TextStyle(color: Colors.red.shade700)),
            ],
            const Spacer(),
            FilledButton(
              onPressed: _loading ? null : _submit,
              child: _loading ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Confirm Booking'),
            ),
          ],
        ),
      ),
    );
  }
}
