import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../providers/chat_provider.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final user = auth.user;

    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            CircleAvatar(radius: 40, child: Text(user?.firstName[0] ?? '?', style: const TextStyle(fontSize: 32))),
            const SizedBox(height: 16),
            Text('${user?.firstName ?? ''} ${user?.lastName ?? ''}', style: Theme.of(context).textTheme.headlineSmall),
            Text(user?.email ?? '', style: TextStyle(color: Colors.grey.shade600)),
            Text('Role: ${user?.role ?? ''}'),
            const SizedBox(height: 24),
            Card(
              child: ListTile(
                leading: const Icon(Icons.event_note_outlined, color: Colors.blue),
                title: const Text('Saved itineraries'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => context.push('/itineraries'),
              ),
            ),
            const Spacer(),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton(
                onPressed: () async {
                  await auth.logout();
                  if (context.mounted) {
                    context.read<ChatProvider>().reset();
                    context.go('/login');
                  }
                },
                child: const Text('Sign Out'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
