import 'dart:convert';

import '../models/chat_message.dart';
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
}
