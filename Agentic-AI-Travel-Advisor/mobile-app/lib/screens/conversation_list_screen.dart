import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../providers/chat_provider.dart';
import '../services/chat_service.dart';
import '../models/conversation_summary.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class ConversationListScreen extends StatefulWidget {
  const ConversationListScreen({super.key});

  @override
  State<ConversationListScreen> createState() => _ConversationListScreenState();
}

class _ConversationListScreenState extends State<ConversationListScreen> {
  late Future<List<ConversationSummary>> _future;
  bool _opening = false;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<List<ConversationSummary>> _load() => ChatService(context.read<AuthProvider>().api).listConversations();

  @override
  Widget build(BuildContext context) {
    final service = ChatService(context.read<AuthProvider>().api);

    return Scaffold(
      appBar: AppBar(title: const Text('AI conversations')),
      body: Stack(
        children: [
          FutureBuilder(
            future: _future,
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
              if (snapshot.hasError) {
                return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = _load()));
              }
              final items = snapshot.data ?? [];
              if (items.isEmpty) {
                return EmptyStateWidget(
                  icon: Icons.chat_bubble_outline,
                  title: 'No conversations yet',
                  message: 'Start a chat with the AI assistant to plan your trip.',
                  actionLabel: 'Open AI chat',
                  onAction: () => context.go('/chat'),
                );
              }
              return RefreshIndicator(
                onRefresh: () async => setState(() => _future = _load()),
                child: ListView.builder(
                  padding: const EdgeInsets.all(12),
                  itemCount: items.length,
                  itemBuilder: (_, i) {
                    final c = items[i];
                    return Card(
                      child: ListTile(
                        leading: const Icon(Icons.chat_bubble_outline, color: Colors.blue),
                        title: Text(c.title),
                        subtitle: Text(c.updatedAt.toLocal().toString().split('.').first),
                        onTap: () async {
                          setState(() => _opening = true);
                          try {
                            final loaded = await service.getConversation(c.id);
                            if (!context.mounted) return;
                            context.read<ChatProvider>().loadConversation(loaded.$1, loaded.$2);
                            context.go('/chat');
                          } finally {
                            if (mounted) setState(() => _opening = false);
                          }
                        },
                      ),
                    );
                  },
                ),
              );
            },
          ),
          if (_opening)
            const ColoredBox(
              color: Color(0x33000000),
              child: LoadingWidget(message: 'Opening conversation...'),
            ),
        ],
      ),
    );
  }
}
