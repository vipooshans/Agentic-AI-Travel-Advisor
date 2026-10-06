import 'booking.dart';
import 'suggested_plan.dart';
import 'travel_plan.dart';

class ChatMessage {
  final String role;
  final String content;

  /// Outcome reported by the backend, e.g. `plan`, `booking_proposal`,
  /// `booking_created`, `booking_failed`, `refused`.
  final String? status;
  final TravelPlan? plan;
  final BookingProposal? pendingBooking;
  final int? bookingId;
  final Booking? booking;

  /// Legacy plan shape kept for conversations saved before structured plans.
  final SuggestedPlan? suggestedPlan;

  const ChatMessage({
    required this.role,
    required this.content,
    this.status,
    this.plan,
    this.pendingBooking,
    this.bookingId,
    this.booking,
    this.suggestedPlan,
  });

  bool get isUser => role == 'user';

  factory ChatMessage.fromJson(Map<String, dynamic> json) {
    final booking = json['booking'] is Map<String, dynamic> ? Booking.fromJson(json['booking'] as Map<String, dynamic>) : null;
    return ChatMessage(
      role: json['role'] as String? ?? 'assistant',
      content: (json['content'] ?? json['message']) as String? ?? '',
      status: json['status'] as String?,
      plan: json['plan'] is Map<String, dynamic> ? TravelPlan.fromJson(json['plan'] as Map<String, dynamic>) : null,
      pendingBooking: json['pendingBooking'] is Map<String, dynamic>
          ? BookingProposal.fromJson(json['pendingBooking'] as Map<String, dynamic>)
          : null,
      bookingId: (json['bookingId'] as num?)?.toInt() ?? booking?.id,
      booking: booking,
      suggestedPlan: json['suggestedPlan'] is Map<String, dynamic>
          ? SuggestedPlan.fromJson(json['suggestedPlan'] as Map<String, dynamic>)
          : null,
    );
  }
}
