import 'dart:convert';

import '../models/chat_message.dart';
import '../models/conversation_summary.dart';
import 'api_service.dart';

class ChatResult {
  final int conversationId;
  final ChatMessage message;

  const ChatResult({required this.conversationId, required this.message});
}

class ChatService {
  static const chatTimeout = Duration(seconds: 90);

  final ApiService _api;
  ChatService(this._api);

  Future<ChatResult> send({int? conversationId, required String message, String? confirmBookingId}) async {
    final body = <String, dynamic>{'message': message};
    if (conversationId != null) body['conversationId'] = conversationId;
    if (confirmBookingId != null) body['confirmBookingId'] = confirmBookingId;

    final response = await _api.post('/api/ai/chat', body, timeout: chatTimeout);
    _api.ensureSuccess(response);

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    return ChatResult(
      conversationId: (data['conversationId'] as num).toInt(),
      message: ChatMessage.fromJson({...data, 'role': 'assistant'}),
    );
  }

  Future<List<ConversationSummary>> listConversations() async {
    final response = await _api.get('/api/ai/conversations');
    _api.ensureSuccess(response);
    return (jsonDecode(response.body) as List)
        .map((c) => ConversationSummary.fromJson(c as Map<String, dynamic>))
        .toList();
  }

  Future<(int id, List<ChatMessage> messages)> getConversation(int id) async {
    final response = await _api.get('/api/ai/conversations/$id');
    _api.ensureSuccess(response);
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    final messages = (data['messages'] as List? ?? [])
        .map((m) => ChatMessage.fromJson(m as Map<String, dynamic>))
        .toList();
    return ((data['id'] as num).toInt(), messages);
  }
}
