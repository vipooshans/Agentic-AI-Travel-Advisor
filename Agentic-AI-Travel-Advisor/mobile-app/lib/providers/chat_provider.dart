import 'package:flutter/foundation.dart';

import '../models/chat_message.dart';
import '../models/itinerary.dart';
import '../models/suggested_plan.dart';
import '../services/api_service.dart';
import '../services/chat_service.dart';
import '../services/itinerary_service.dart';

class ChatProvider extends ChangeNotifier {
  int? _conversationId;
  final List<ChatMessage> _messages = [];
  bool _sending = false;
  bool _saving = false;
  String? _error;
  int? _savedItineraryId;

  int? get conversationId => _conversationId;
  List<ChatMessage> get messages => List.unmodifiable(_messages);
  bool get sending => _sending;
  bool get saving => _saving;
  String? get error => _error;
  int? get savedItineraryId => _savedItineraryId;

  SuggestedPlan? get latestPlan {
    for (final message in _messages.reversed) {
      if (message.suggestedPlan != null) return message.suggestedPlan;
    }
    return null;
  }

  Future<void> send(ApiService api, String text) async {
    final content = text.trim();
    if (content.isEmpty || _sending) return;

    _error = null;
    _savedItineraryId = null;
    _messages.add(ChatMessage(role: 'user', content: content));
    _sending = true;
    notifyListeners();

    try {
      final result = await ChatService(api).send(
        conversationId: _conversationId,
        message: content,
      );
      _conversationId = result.conversationId;
      _messages.add(result.message);
    } catch (e) {
      _error = e.toString().replaceFirst('Exception: ', '');
      _messages.add(const ChatMessage(
        role: 'assistant',
        content: 'Sorry, I could not complete that request. Please try again.',
      ));
    } finally {
      _sending = false;
      notifyListeners();
    }
  }

  Future<Itinerary?> saveLatestPlan(ApiService api) async {
    final plan = latestPlan;
    if (plan == null || _saving) return null;

    _saving = true;
    _error = null;
    notifyListeners();
    try {
      final saved = await ItineraryService(api).createFromPlan(plan);
      _savedItineraryId = saved.id;
      return saved;
    } catch (e) {
      _error = e.toString().replaceFirst('Exception: ', '');
      return null;
    } finally {
      _saving = false;
      notifyListeners();
    }
  }

  void reset() {
    _conversationId = null;
    _messages.clear();
    _sending = false;
    _saving = false;
    _error = null;
    _savedItineraryId = null;
    notifyListeners();
  }
}
