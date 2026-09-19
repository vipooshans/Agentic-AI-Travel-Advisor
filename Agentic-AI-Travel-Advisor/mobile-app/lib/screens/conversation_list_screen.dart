import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../providers/chat_provider.dart';
import '../services/chat_service.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class ConversationListScreen extends StatelessWidget {
  const ConversationListScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;
    final service = ChatService(api);

    return Scaffold(
      appBar: AppBar(title: const Text('AI conversations')),
      body: FutureBuilder(
        future: service.listConversations(),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString());
          final items = snapshot.data ?? [];
          if (items.isEmpty) {
            return const Center(child: Text('No conversations yet. Chat with the AI assistant.'));
          }
          return ListView.builder(
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
                    final loaded = await service.getConversation(c.id);
                    if (!context.mounted) return;
                    context.read<ChatProvider>().loadConversation(loaded.$1, loaded.$2);
                    context.go('/chat');
                  },
                ),
              );
            },
          );
        },
      ),
    );
  }
}
