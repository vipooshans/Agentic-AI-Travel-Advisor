import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../providers/chat_provider.dart';
import '../widgets/chat_bubble.dart';

class ChatScreen extends StatefulWidget {
  const ChatScreen({super.key});

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final _controller = TextEditingController();
  final _scroll = ScrollController();

  static const _starter = 'Plan a 3-day trip to Ella under Rs. 50,000.';

  @override
  void dispose() {
    _controller.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _scrollToEnd() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!_scroll.hasClients) return;
      _scroll.animateTo(
        _scroll.position.maxScrollExtent,
        duration: const Duration(milliseconds: 250),
        curve: Curves.easeOut,
      );
    });
  }

  Future<void> _send(String text) async {
    final content = text.trim();
    if (content.isEmpty) return;
    _controller.clear();
    final auth = context.read<AuthProvider>();
    await context.read<ChatProvider>().send(auth.api, content);
    _scrollToEnd();
  }

  Future<void> _save() async {
    final auth = context.read<AuthProvider>();
    final chat = context.read<ChatProvider>();
    final saved = await chat.saveLatestPlan(auth.api);
    if (!mounted) return;
    if (saved == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(chat.error ?? 'Could not save itinerary')),
      );
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: const Text('Itinerary saved'),
        action: SnackBarAction(
          label: 'View',
          onPressed: () => context.push('/itineraries/${saved.id}'),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final chat = context.watch<ChatProvider>();
    if (chat.sending) _scrollToEnd();

    return Scaffold(
      appBar: AppBar(
        title: const Text('AI Travel Assistant'),
        actions: [
          IconButton(
            tooltip: 'Saved itineraries',
            onPressed: () => context.push('/itineraries'),
            icon: const Icon(Icons.event_note_outlined),
          ),
        ],
      ),
      body: Column(
        children: [
          if (chat.error != null)
            Container(
              width: double.infinity,
              color: Colors.red.shade50,
              padding: const EdgeInsets.all(12),
              child: Text(chat.error!, style: TextStyle(color: Colors.red.shade700)),
            ),
          Expanded(
            child: chat.messages.isEmpty
                ? _EmptyState(onStarter: () => _send(_starter))
                : ListView.builder(
                    controller: _scroll,
                    padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
                    itemCount: chat.messages.length + (chat.sending ? 1 : 0),
                    itemBuilder: (context, index) {
                      if (index == chat.messages.length) {
                        return const Padding(
                          padding: EdgeInsets.symmetric(vertical: 8),
                          child: Align(
                            alignment: Alignment.centerLeft,
                            child: CircularProgressIndicator(),
                          ),
                        );
                      }
                      final message = chat.messages[index];
                      final isLatestPlan = message.suggestedPlan != null && identical(message.suggestedPlan, chat.latestPlan);
                      return ChatBubble(
                        message: message,
                        saving: chat.saving,
                        onSave: isLatestPlan ? _save : null,
                      );
                    },
                  ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _controller,
                      minLines: 1,
                      maxLines: 4,
                      textInputAction: TextInputAction.send,
                      onSubmitted: chat.sending ? null : _send,
                      decoration: InputDecoration(
                        hintText: 'Ask to plan a trip...',
                        filled: true,
                        fillColor: Colors.grey.shade100,
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(24), borderSide: BorderSide.none),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  IconButton.filled(
                    onPressed: chat.sending ? null : () => _send(_controller.text),
                    icon: const Icon(Icons.send),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  final VoidCallback onStarter;
  const _EmptyState({required this.onStarter});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.smart_toy_outlined, size: 56, color: Colors.blue),
            const SizedBox(height: 16),
            Text('Plan your next trip', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 8),
            Text(
              'Tell me a destination, dates, and budget. I will recommend hotels, packages, and a day-by-day itinerary.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.grey.shade600),
            ),
            const SizedBox(height: 24),
            ActionChip(
              avatar: const Icon(Icons.auto_awesome, size: 18),
              label: const Text('Plan a 3-day trip to Ella under Rs. 50,000.'),
              onPressed: onStarter,
            ),
          ],
        ),
      ),
    );
  }
}
