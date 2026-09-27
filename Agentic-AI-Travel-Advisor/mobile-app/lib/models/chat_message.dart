import 'suggested_plan.dart';

class ChatMessage {
  final String role;
  final String content;
  final SuggestedPlan? suggestedPlan;

  const ChatMessage({
    required this.role,
    required this.content,
    this.suggestedPlan,
  });

  bool get isUser => role == 'user';

  factory ChatMessage.fromJson(Map<String, dynamic> json) {
    return ChatMessage(
      role: json['role'] as String? ?? 'assistant',
      content: json['content'] as String? ?? '',
      suggestedPlan: json['suggestedPlan'] != null
          ? SuggestedPlan.fromJson(json['suggestedPlan'] as Map<String, dynamic>)
          : null,
    );
  }
}
