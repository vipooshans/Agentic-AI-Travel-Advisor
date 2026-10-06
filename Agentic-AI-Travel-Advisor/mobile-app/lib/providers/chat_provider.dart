import 'package:flutter/foundation.dart';

import '../models/chat_message.dart';
import '../models/itinerary.dart';
import '../models/suggested_plan.dart';
import '../models/travel_plan.dart';
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
  bool get hasState => _conversationId != null || _messages.isNotEmpty || _error != null;

  SuggestedPlan? get latestPlan {
    for (final message in _messages.reversed) {
      if (message.suggestedPlan != null) return message.suggestedPlan;
    }
    return null;
  }

  /// The most recent assistant message carrying a plan, structured or legacy.
  ChatMessage? get latestPlanMessage {
    for (final message in _messages.reversed) {
      if (!message.isUser && (message.plan != null || message.suggestedPlan != null)) return message;
    }
    return null;
  }

  ChatMessage? get latestAssistantMessage {
    for (final message in _messages.reversed) {
      if (!message.isUser) return message;
    }
    return null;
  }

  /// Only the newest unexpired proposal may be confirmed; older ones have
  /// been superseded by later replies.
  bool canConfirm(ChatMessage message, [DateTime? now]) {
    final proposal = message.pendingBooking;
    return !_sending &&
        proposal != null &&
        identical(message, latestAssistantMessage) &&
        !proposal.isExpired(now);
  }

  Future<void> send(ApiService api, String text) => _send(api, text.trim());

  /// Asks the backend to create the quoted booking. The booking only exists
  /// if the reply has status `booking_created`.
  Future<void> confirm(ApiService api, BookingProposal proposal) {
    return _send(api, 'Confirm booking: ${proposal.title}', confirmBookingId: proposal.id);
  }

  Future<void> _send(ApiService api, String content, {String? confirmBookingId}) async {
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
        confirmBookingId: confirmBookingId,
      );
      _conversationId = result.conversationId;
      _messages.add(result.message);
    } catch (e) {
      _error = e.toString().replaceFirst('Exception: ', '');
      _messages.add(const ChatMessage(
        role: 'assistant',
        content: 'Sorry, I could not complete that request. Please try again.',
        status: 'error',
      ));
    } finally {
      _sending = false;
      notifyListeners();
    }
  }

  Future<Itinerary?> saveLatestPlan(ApiService api) async {
    final message = latestPlanMessage;
    if (message == null || _saving) return null;

    _saving = true;
    _error = null;
    notifyListeners();
    try {
      final service = ItineraryService(api);
      final saved = message.plan != null
          ? await service.createFromTravelPlan(message.plan!, conversationId: _conversationId)
          : await service.createFromPlan(message.suggestedPlan!);
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

  void loadConversation(int conversationId, List<ChatMessage> messages) {
    _conversationId = conversationId;
    _messages
      ..clear()
      ..addAll(messages);
    _error = null;
    _savedItineraryId = null;
    notifyListeners();
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
