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
    final initial = (user?.firstName.isNotEmpty == true) ? user!.firstName[0] : '?';

    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          CircleAvatar(radius: 40, child: Text(initial, style: const TextStyle(fontSize: 32))),
          const SizedBox(height: 16),
          Text('${user?.firstName ?? ''} ${user?.lastName ?? ''}', style: Theme.of(context).textTheme.headlineSmall),
          Text(user?.email ?? '', style: TextStyle(color: Colors.grey.shade600)),
          Text('Role: ${user?.role ?? ''}'),
          const SizedBox(height: 24),
          _HubTile(icon: Icons.person_outline, title: 'Edit profile', onTap: () => context.push('/profile/edit')),
          _HubTile(icon: Icons.bookmark_outline, title: 'My bookings', onTap: () => context.go('/bookings')),
          _HubTile(icon: Icons.event_note_outlined, title: 'Saved itineraries', onTap: () => context.push('/itineraries')),
          _HubTile(icon: Icons.tune, title: 'Travel preferences', onTap: () => context.push('/preferences')),
          _HubTile(icon: Icons.history, title: 'AI conversation history', onTap: () => context.push('/conversations')),
          const SizedBox(height: 24),
          OutlinedButton(
            onPressed: () async {
              await auth.logout();
              if (context.mounted) {
                context.read<ChatProvider>().reset();
                context.go('/login');
              }
            },
            child: const Text('Sign Out'),
          ),
        ],
      ),
    );
  }
}

class _HubTile extends StatelessWidget {
  final IconData icon;
  final String title;
  final VoidCallback onTap;

  const _HubTile({required this.icon, required this.title, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: Icon(icon, color: Colors.blue),
        title: Text(title),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}
