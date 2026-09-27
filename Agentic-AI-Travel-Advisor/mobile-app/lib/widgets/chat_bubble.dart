import 'package:flutter/material.dart';

import '../models/chat_message.dart';
import 'suggested_plan_card.dart';

class ChatBubble extends StatelessWidget {
  final ChatMessage message;
  final bool saving;
  final VoidCallback? onSave;

  const ChatBubble({
    super.key,
    required this.message,
    this.saving = false,
    this.onSave,
  });

  @override
  Widget build(BuildContext context) {
    final isUser = message.isUser;
    return Align(
      alignment: isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: MediaQuery.of(context).size.width * 0.85),
        child: Column(
          crossAxisAlignment: isUser ? CrossAxisAlignment.end : CrossAxisAlignment.start,
          children: [
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
            if (message.suggestedPlan != null)
              SuggestedPlanCard(
                plan: message.suggestedPlan!,
                saving: saving,
                onSave: onSave,
              ),
          ],
        ),
      ),
    );
  }
}
