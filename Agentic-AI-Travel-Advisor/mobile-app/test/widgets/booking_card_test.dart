import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:travel_advisor/models/booking.dart';
import 'package:travel_advisor/widgets/booking_card.dart';

void main() {
  testWidgets('BookingCard renders booking details and status label', (tester) async {
    final booking = Booking(
      id: 101,
      userId: 'user-001',
      hotelName: 'Cinnamon Grand Colombo',
      roomName: 'Deluxe Ocean View',
      checkIn: DateTime(2026, 10, 15),
      checkOut: DateTime(2026, 10, 18),
      status: 0,
      totalPrice: 45000,
      createdAt: DateTime(2026, 10, 10),
    );

    var cancelTapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: BookingCard(
            booking: booking,
            onCancel: () => cancelTapped = true,
          ),
        ),
      ),
    );

    expect(find.text('Cinnamon Grand Colombo'), findsOneWidget);
    expect(find.text('Pending'), findsOneWidget);
    expect(find.text('Rs. 45000'), findsOneWidget);

    await tester.tap(find.text('Cancel booking'));
    expect(cancelTapped, isTrue);
  });
}
