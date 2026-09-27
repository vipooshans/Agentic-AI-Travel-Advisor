import 'dart:convert';

import '../models/chat_message.dart';
import '../models/conversation_summary.dart';
import '../models/suggested_plan.dart';
import 'api_service.dart';

class ChatResult {
  final int conversationId;
  final ChatMessage message;

  const ChatResult({required this.conversationId, required this.message});
}

class ChatService {
  final ApiService _api;
  ChatService(this._api);

  Future<ChatResult> send({int? conversationId, required String message}) async {
    final body = <String, dynamic>{'message': message};
    if (conversationId != null) body['conversationId'] = conversationId;

    final response = await _api.post(
      '/api/ai/chat',
      body,
      timeout: const Duration(seconds: 90),
    );
    if (response.statusCode != 200) {
      throw Exception(_api.parseErrorMessage(response) ?? 'AI request failed');
    }

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return ChatResult(
      conversationId: (data['conversationId'] as num).toInt(),
      message: ChatMessage(
        role: 'assistant',
        content: data['message'] as String? ?? '',
        suggestedPlan: data['suggestedPlan'] != null
            ? SuggestedPlan.fromJson(data['suggestedPlan'] as Map<String, dynamic>)
            : null,
      ),
    );
  }

  Future<List<ConversationSummary>> listConversations() async {
    final response = await _api.get('/api/ai/conversations');
    if (response.statusCode != 200) throw Exception('Failed to load conversations');
    return (jsonDecode(response.body) as List)
        .map((c) => ConversationSummary.fromJson(c as Map<String, dynamic>))
        .toList();
  }

  Future<(int id, List<ChatMessage> messages)> getConversation(int id) async {
    final response = await _api.get('/api/ai/conversations/$id');
    if (response.statusCode != 200) throw Exception('Failed to load conversation');
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final messages = (data['messages'] as List? ?? [])
        .map((m) => ChatMessage.fromJson(m as Map<String, dynamic>))
        .toList();
    return ((data['id'] as num).toInt(), messages);
  }
}
