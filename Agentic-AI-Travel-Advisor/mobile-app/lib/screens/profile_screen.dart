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
          if (user?.isAdmin == true) ...[
            Text('Management Portals', style: Theme.of(context).textTheme.titleSmall?.copyWith(color: Colors.indigo, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            _HubTile(icon: Icons.admin_panel_settings, title: 'Admin Dashboard', onTap: () => context.push('/admin')),
            _HubTile(icon: Icons.inventory_2_outlined, title: 'Packages Dashboard (Agent)', onTap: () => context.push('/packages-dashboard')),
            _HubTile(icon: Icons.apartment_outlined, title: 'Hotels Dashboard (Owner)', onTap: () => context.push('/hotels-dashboard')),
            const SizedBox(height: 16),
          ] else if (user?.isTravelAgent == true) ...[
            Text('Staff Portal', style: Theme.of(context).textTheme.titleSmall?.copyWith(color: Colors.teal, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            _HubTile(icon: Icons.inventory_2_outlined, title: 'Packages Dashboard', onTap: () => context.push('/packages-dashboard')),
            const SizedBox(height: 16),
          ] else if (user?.isHotelOwner == true) ...[
            Text('Staff Portal', style: Theme.of(context).textTheme.titleSmall?.copyWith(color: Colors.amber.shade900, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            _HubTile(icon: Icons.apartment_outlined, title: 'Hotels Dashboard', onTap: () => context.push('/hotels-dashboard')),
            const SizedBox(height: 16),
          ],
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
