import 'package:flutter/material.dart';

import '../models/chat_message.dart';
import 'booking_proposal_card.dart';
import 'suggested_plan_card.dart';
import 'travel_plan_card.dart';

class ChatBubble extends StatelessWidget {
  static const _statusLabels = {
    'refused': 'Request declined',
    'booking_failed': 'Booking not created',
    'no_match': 'No match found',
    'over_budget': 'Over budget',
    'clarification': 'Needs more details',
  };

  final ChatMessage message;
  final bool saving;
  final VoidCallback? onSave;
  final bool canConfirm;
  final VoidCallback? onConfirm;
  final VoidCallback? onViewBookings;

  const ChatBubble({
    super.key,
    required this.message,
    this.saving = false,
    this.onSave,
    this.canConfirm = false,
    this.onConfirm,
    this.onViewBookings,
  });

  @override
  Widget build(BuildContext context) {
    final isUser = message.isUser;
    final statusLabel = _statusLabels[message.status];
    return Align(
      alignment: isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: MediaQuery.of(context).size.width * 0.9),
        child: Column(
          crossAxisAlignment: isUser ? CrossAxisAlignment.end : CrossAxisAlignment.start,
          children: [
            if (!isUser && statusLabel != null)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(statusLabel, style: TextStyle(fontSize: 12, color: Colors.grey.shade700)),
              ),
            Container(
              margin: const EdgeInsets.symmetric(vertical: 4),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                color: isUser ? Colors.blue : Colors.grey.shade200,
                borderRadius: BorderRadius.circular(16).copyWith(
                  bottomRight: isUser ? const Radius.circular(4) : null,
                  bottomLeft: isUser ? null : const Radius.circular(4),
                ),
              ),
              child: Text(
                message.content,
                style: TextStyle(color: isUser ? Colors.white : Colors.black87, height: 1.35),
              ),
            ),
            if (message.plan != null)
              TravelPlanCard(plan: message.plan!, saving: saving, onSave: onSave)
            else if (message.suggestedPlan != null)
              SuggestedPlanCard(plan: message.suggestedPlan!, saving: saving, onSave: onSave),
            if (message.pendingBooking != null)
              BookingProposalCard(
                proposal: message.pendingBooking!,
                canConfirm: canConfirm,
                onConfirm: onConfirm,
              ),
            if (message.status == 'booking_created' && message.bookingId != null)
              BookingResultCard(bookingId: message.bookingId!, booking: message.booking, onView: onViewBookings),
          ],
        ),
      ),
    );
  }
}
