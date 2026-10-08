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

  static const _quickPrompts = [
    'Plan a 3-day trip to Ella under Rs. 50,000.',
    'Beach vacation in Mirissa for 2 adults.',
    'Cultural tour to Sigiriya & Kandy in October.',
    'Wildlife safari in Yala National Park.',
  ];

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
                ? _EmptyState(
                    prompts: _quickPrompts,
                    onSelectPrompt: (p) => _send(p),
                  )
                : ListView.builder(
                    controller: _scroll,
                    padding: const EdgeInsets.all(16),
                    itemCount: chat.messages.length,
                    itemBuilder: (_, i) => ChatBubble(
                      message: chat.messages[i],
                      onSavePlan: chat.suggestedPlan != null && i == chat.messages.length - 1
                          ? _save
                          : null,
                    ),
                  ),
          ),
          if (chat.sending)
            const Padding(
              padding: EdgeInsets.all(8),
              child: LinearProgressIndicator(),
            ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(8),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _controller,
                      decoration: InputDecoration(
                        hintText: 'Ask AI: destination, budget, days...',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(24)),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      ),
                      onSubmitted: _send,
                    ),
                  ),
                  const SizedBox(width: 8),
                  IconButton(
                    onPressed: chat.sending ? null : () => _send(_controller.text),
                    icon: const Icon(Icons.send),
                    color: Theme.of(context).colorScheme.primary,
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
  final List<String> prompts;
  final ValueChanged<String> onSelectPrompt;

  const _EmptyState({
    required this.prompts,
    required this.onSelectPrompt,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.smart_toy_outlined, size: 64, color: Color(0xFF1E56A0)),
            const SizedBox(height: 16),
            const Text(
              'Agentic AI Advisor',
              style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Tell me where you want to travel, your budget, and preferences. I will tailor recommendations and schedule your day-by-day plan.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.grey),
            ),
            const SizedBox(height: 24),
            const Align(
              alignment: Alignment.centerLeft,
              child: Text(
                'Try asking:',
                style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13, color: Colors.grey),
              ),
            ),
            const SizedBox(height: 8),
            ...prompts.map(
              (p) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: SizedBox(
                  width: double.infinity,
                  child: OutlinedButton(
                    style: OutlinedButton.styleFrom(
                      alignment: Alignment.centerLeft,
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                    onPressed: () => onSelectPrompt(p),
                    child: Text(p, style: const TextStyle(fontSize: 13)),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
