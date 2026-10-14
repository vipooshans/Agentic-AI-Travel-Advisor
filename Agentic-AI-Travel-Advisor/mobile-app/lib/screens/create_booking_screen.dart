import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/availability_quote.dart';
import '../providers/auth_provider.dart';
import '../services/booking_service.dart';
import '../utils/format.dart';
import '../utils/validators.dart';

class CreateBookingScreen extends StatefulWidget {
  final int? roomId;
  final int? packageId;

  /// Injected in tests; defaults to the service for the signed-in session.
  final BookingService? service;

  const CreateBookingScreen({super.key, this.roomId, this.packageId, this.service});

  @override
  State<CreateBookingScreen> createState() => _CreateBookingScreenState();
}

class _CreateBookingScreenState extends State<CreateBookingScreen> {
  static const maxNotesLength = 1000;

  final _formKey = GlobalKey<FormState>();
  final _guests = TextEditingController(text: '1');
  final _notes = TextEditingController();
  late DateTime _checkIn = _today().add(const Duration(days: 7));
  late DateTime _checkOut = _checkIn.add(const Duration(days: 3));
  bool _checking = false;
  bool _booking = false;
  String? _error;
  AvailabilityQuote? _quote;

  bool get _isRoom => widget.roomId != null;

  static DateTime _today() {
    final now = DateTime.now();
    return DateTime(now.year, now.month, now.day);
  }

  BookingService get _service => widget.service ?? BookingService(context.read<AuthProvider>().api);

  @override
  void dispose() {
    _guests.dispose();
    _notes.dispose();
    super.dispose();
  }

  String? get _dateError {
    if (_checkIn.isBefore(_today())) return 'Check-in cannot be in the past';
    if (_isRoom && !_checkOut.isAfter(_checkIn)) return 'Check-out must be after check-in';
    return null;
  }

  /// Any change to the inputs makes the previous quote stale.
  void _invalidateQuote() {
    if (_quote != null || _error != null) {
      setState(() {
        _quote = null;
        _error = null;
      });
    }
  }

  bool _validate() {
    final formOk = _formKey.currentState!.validate();
    final dateError = _dateError;
    if (dateError != null) setState(() => _error = dateError);
    return formOk && dateError == null;
  }

  Future<void> _checkAvailability() async {
    if (!_validate()) return;
    setState(() {
      _checking = true;
      _error = null;
      _quote = null;
    });
    try {
      final quote = await _service.checkAvailability(
        roomId: widget.roomId,
        travelPackageId: widget.packageId,
        checkIn: _checkIn,
        checkOut: _isRoom ? _checkOut : null,
        guests: int.parse(_guests.text.trim()),
      );
      if (mounted) setState(() => _quote = quote);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _checking = false);
    }
  }

  Future<void> _submit() async {
    if (!_validate() || _quote?.available != true) return;
    setState(() {
      _booking = true;
      _error = null;
    });
    try {
      final booking = await _service.create(
        roomId: widget.roomId,
        travelPackageId: widget.packageId,
        checkIn: _checkIn,
        checkOut: _isRoom ? _checkOut : null,
        guests: int.parse(_guests.text.trim()),
        notes: _notes.text,
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Booking #${booking.id} created. Status: ${booking.statusLabel}.')),
      );
      context.go('/bookings');
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString().replaceFirst('Exception: ', '');
          _quote = null;
        });
      }
    } finally {
      if (mounted) setState(() => _booking = false);
    }
  }

  Future<void> _pickDate(bool isCheckIn) async {
    final first = isCheckIn ? _today() : _checkIn.add(const Duration(days: 1));
    final current = isCheckIn ? _checkIn : _checkOut;
    final picked = await showDatePicker(
      context: context,
      initialDate: current.isBefore(first) ? first : current,
      firstDate: first,
      lastDate: _today().add(const Duration(days: 365)),
    );
    if (picked == null) return;
    setState(() {
      if (isCheckIn) {
        _checkIn = picked;
        if (!_checkOut.isAfter(_checkIn)) _checkOut = _checkIn.add(const Duration(days: 1));
      } else {
        _checkOut = picked;
      }
    });
    _invalidateQuote();
  }

  @override
  Widget build(BuildContext context) {
    final quote = _quote;
    final busy = _checking || _booking;

    return Scaffold(
      appBar: AppBar(title: const Text('Create Booking')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            Text(_isRoom ? 'Room Booking' : 'Package Booking', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 16),
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(_isRoom ? 'Check-in' : 'Start date'),
              subtitle: Text(formatDate(_checkIn)),
              trailing: const Icon(Icons.calendar_today),
              onTap: busy ? null : () => _pickDate(true),
            ),
            if (_isRoom)
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Check-out'),
                subtitle: Text(formatDate(_checkOut)),
                trailing: const Icon(Icons.calendar_today),
                onTap: busy ? null : () => _pickDate(false),
              ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _guests,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Guests', border: OutlineInputBorder()),
              validator: (v) => Validators.guests(v),
              onChanged: (_) => _invalidateQuote(),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _notes,
              maxLines: 3,
              maxLength: maxNotesLength,
              decoration: const InputDecoration(
                labelText: 'Notes for the provider (optional)',
                border: OutlineInputBorder(),
              ),
              validator: (v) => Validators.maxLength(v, maxNotesLength, 'Notes'),
            ),
            const SizedBox(height: 8),
            OutlinedButton.icon(
              onPressed: busy ? null : _checkAvailability,
              icon: _checking
                  ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.event_available),
              label: const Text('Check availability'),
            ),
            if (quote != null) ...[
              const SizedBox(height: 12),
              _QuoteCard(quote: quote, isRoom: _isRoom),
            ],
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: TextStyle(color: Colors.red.shade700)),
            ],
            const SizedBox(height: 24),
            FilledButton(
              onPressed: busy || quote?.available != true ? null : _submit,
              child: _booking
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : Text(quote?.available == true ? 'Request booking for ${formatMoney(quote!.totalPrice)}' : 'Request booking'),
            ),
            const SizedBox(height: 8),
            Text(
              'New bookings stay Pending until the provider confirms them.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.grey.shade600, fontSize: 12),
            ),
          ],
        ),
      ),
    );
  }
}

class _QuoteCard extends StatelessWidget {
  final AvailabilityQuote quote;
  final bool isRoom;
  const _QuoteCard({required this.quote, required this.isRoom});

  @override
  Widget build(BuildContext context) {
    final color = quote.available ? Colors.green : Colors.orange;
    return Card(
      color: color.shade50,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              quote.available ? 'Available' : 'Not available',
              style: TextStyle(fontWeight: FontWeight.bold, color: color.shade800),
            ),
            if (quote.available) ...[
              Text(
                isRoom
                    ? '${quote.nights} night${quote.nights == 1 ? '' : 's'} · ${quote.guests} guest${quote.guests == 1 ? '' : 's'}'
                    : '${quote.guests} traveler${quote.guests == 1 ? '' : 's'}'
                        '${quote.remainingPlaces != null ? ' · ${quote.remainingPlaces} places left' : ''}',
              ),
              Text('Total ${formatMoney(quote.totalPrice)}', style: const TextStyle(fontWeight: FontWeight.w600)),
            ] else
              Text(quote.reason ?? 'Choose different dates or fewer guests.'),
          ],
        ),
      ),
    );
  }
}
