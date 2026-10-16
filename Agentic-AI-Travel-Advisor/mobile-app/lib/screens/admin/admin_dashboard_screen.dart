import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../models/hotel.dart';
import '../../models/report_summary.dart';
import '../../models/staff_user.dart';
import '../../models/travel_package.dart';
import '../../providers/auth_provider.dart';
import '../../services/admin_service.dart';
import '../../utils/format.dart';
import '../../widgets/empty_state_widget.dart';
import '../../widgets/error_widget.dart' as app_err;
import '../../widgets/loading_widget.dart';

class AdminDashboardScreen extends StatefulWidget {
  const AdminDashboardScreen({super.key});

  @override
  State<AdminDashboardScreen> createState() => _AdminDashboardScreenState();
}

class _AdminDashboardScreenState extends State<AdminDashboardScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  ReportSummary? _summary;
  List<StaffUser> _users = [];
  List<Hotel> _pendingHotels = [];
  List<TravelPackage> _pendingPackages = [];

  bool _loading = true;
  String? _error;
  String? _roleFilter;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _loadData();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    final api = context.read<AuthProvider>().api;
    final admin = AdminService(api);

    try {
      final results = await Future.wait([
        admin.getSummary(),
        admin.getUsers(role: _roleFilter),
        admin.getPendingHotels(),
        admin.getPendingPackages(),
      ]);

      if (mounted) {
        setState(() {
          _summary = results[0] as ReportSummary;
          _users = results[1] as List<StaffUser>;
          _pendingHotels = results[2] as List<Hotel>;
          _pendingPackages = results[3] as List<TravelPackage>;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString().replaceFirst('Exception: ', '');
          _loading = false;
        });
      }
    }
  }

  Future<void> _filterUsers(String? role) async {
    setState(() => _roleFilter = role);
    final api = context.read<AuthProvider>().api;
    final users = await AdminService(api).getUsers(role: role);
    if (mounted) {
      setState(() => _users = users);
    }
  }

  Future<void> _toggleUserActive(StaffUser user) async {
    final api = context.read<AuthProvider>().api;
    final newStatus = !user.isActive;
    try {
      await AdminService(api).setUserActive(user.id, newStatus);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${user.fullName} is now ${newStatus ? 'active' : 'suspended'}')),
        );
        _loadData();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to update status: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }

  Future<void> _showCreateStaffDialog() async {
    final emailController = TextEditingController();
    final passwordController = TextEditingController();
    final firstNameController = TextEditingController();
    final lastNameController = TextEditingController();
    String selectedRole = 'HOTEL_OWNER';
    final formKey = GlobalKey<FormState>();

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => StatefulBuilder(
        builder: (context, setModalState) => Padding(
          padding: EdgeInsets.only(
            left: 20,
            right: 20,
            top: 20,
            bottom: MediaQuery.of(context).viewInsets.bottom + 20,
          ),
          child: Form(
            key: formKey,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Create Staff Account', style: Theme.of(context).textTheme.titleLarge),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: firstNameController,
                    decoration: const InputDecoration(labelText: 'First Name', border: OutlineInputBorder()),
                    validator: (v) => v == null || v.trim().isEmpty ? 'Enter first name' : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: lastNameController,
                    decoration: const InputDecoration(labelText: 'Last Name', border: OutlineInputBorder()),
                    validator: (v) => v == null || v.trim().isEmpty ? 'Enter last name' : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: emailController,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(labelText: 'Email Address', border: OutlineInputBorder()),
                    validator: (v) => v != null && v.contains('@') ? null : 'Enter a valid email',
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: passwordController,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: 'Password', border: OutlineInputBorder()),
                    validator: (v) => v != null && v.length >= 6 ? null : 'At least 6 characters',
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<String>(
                    initialValue: selectedRole,
                    decoration: const InputDecoration(labelText: 'Role', border: OutlineInputBorder()),
                    items: const [
                      DropdownMenuItem(value: 'HOTEL_OWNER', child: Text('Hotel Owner')),
                      DropdownMenuItem(value: 'TRAVEL_AGENT', child: Text('Travel Agent')),
                      DropdownMenuItem(value: 'ADMIN', child: Text('Administrator')),
                    ],
                    onChanged: (v) {
                      if (v != null) setModalState(() => selectedRole = v);
                    },
                  ),
                  const SizedBox(height: 20),
                  FilledButton(
                    onPressed: () async {
                      if (!formKey.currentState!.validate()) return;
                      Navigator.pop(ctx);
                      final api = this.context.read<AuthProvider>().api;
                      try {
                        await AdminService(api).createStaffUser(
                          email: emailController.text,
                          password: passwordController.text,
                          firstName: firstNameController.text,
                          lastName: lastNameController.text,
                          role: selectedRole,
                        );
                        if (mounted) {
                          ScaffoldMessenger.of(this.context).showSnackBar(
                            const SnackBar(content: Text('Staff account created successfully')),
                          );
                          _loadData();
                        }
                      } catch (e) {
                        if (mounted) {
                          ScaffoldMessenger.of(this.context).showSnackBar(
                            SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red),
                          );
                        }
                      }
                    },
                    child: const Text('Create User'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _approveHotel(int id, int status) async {
    final api = context.read<AuthProvider>().api;
    try {
      await AdminService(api).setHotelApproval(id, status);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(status == 1 ? 'Hotel approved' : 'Hotel rejected')),
        );
        _loadData();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }

  Future<void> _approvePackage(int id, int status) async {
    final api = context.read<AuthProvider>().api;
    try {
      await AdminService(api).setPackageApproval(id, status);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(status == 1 ? 'Package approved' : 'Package rejected')),
        );
        _loadData();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Admin Dashboard'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadData),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            const Tab(icon: Icon(Icons.analytics_outlined), text: 'Overview'),
            const Tab(icon: Icon(Icons.people_outline), text: 'Users'),
            Tab(
              icon: Badge(
                isLabelVisible: (_pendingHotels.length + _pendingPackages.length) > 0,
                label: Text('${_pendingHotels.length + _pendingPackages.length}'),
                child: const Icon(Icons.approval_outlined),
              ),
              text: 'Approvals',
            ),
          ],
        ),
      ),
      floatingActionButton: _tabController.index == 1
          ? FloatingActionButton.extended(
              onPressed: _showCreateStaffDialog,
              icon: const Icon(Icons.person_add),
              label: const Text('Add Staff'),
            )
          : null,
      body: _loading
          ? const LoadingWidget(message: 'Loading admin dashboard...')
          : _error != null
              ? app_err.ErrorDisplayWidget(message: _error!, onRetry: _loadData)
              : TabBarView(
                  controller: _tabController,
                  children: [
                    _buildOverviewTab(),
                    _buildUsersTab(),
                    _buildApprovalsTab(),
                  ],
                ),
    );
  }

  Widget _buildOverviewTab() {
    final s = _summary ?? const ReportSummary();
    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Banner
          Card(
            color: Colors.indigo.shade50,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 26,
                    backgroundColor: Colors.indigo.shade100,
                    child: const Icon(Icons.admin_panel_settings, color: Colors.indigo, size: 30),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('System Overview', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                        const Text('Platform-wide revenue, catalog and user analytics.'),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Portal Jump Cards
          Text('Staff Portals', style: Theme.of(context).textTheme.titleSmall?.copyWith(color: Colors.grey.shade700)),
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () => context.push('/packages-dashboard'),
                  icon: const Icon(Icons.inventory_2_outlined, color: Colors.teal),
                  label: const Text('Packages (Agent)'),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () => context.push('/hotels-dashboard'),
                  icon: const Icon(Icons.apartment_outlined, color: Colors.amber),
                  label: const Text('Hotels (Owner)'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // KPIs Grid
          Row(
            children: [
              Expanded(
                child: _MetricCard(
                  title: 'Total Revenue',
                  value: formatMoney(s.revenue),
                  icon: Icons.payments_outlined,
                  color: Colors.green,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _MetricCard(
                  title: 'Total Bookings',
                  value: '${s.totalBookings}',
                  subtitle: '${s.pendingBookings} pending',
                  icon: Icons.bookmark_added_outlined,
                  color: Colors.blue,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: _MetricCard(
                  title: 'Pending Approvals',
                  value: '${s.totalPendingApprovals}',
                  subtitle: '${s.pendingHotelApprovals} hotels, ${s.pendingPackageApprovals} packages',
                  icon: Icons.pending_actions_outlined,
                  color: Colors.orange,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _MetricCard(
                  title: 'Registered Users',
                  value: '${s.userCount + s.hotelOwnerCount + s.travelAgentCount}',
                  subtitle: '${s.hotelOwnerCount} owners, ${s.travelAgentCount} agents',
                  icon: Icons.people_outline,
                  color: Colors.purple,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Bookings Breakdown Card
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Bookings Breakdown', style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 12),
                  _BreakdownRow(label: 'Pending Confirmation', count: s.pendingBookings, color: Colors.amber),
                  _BreakdownRow(label: 'Confirmed Bookings', count: s.confirmedBookings, color: Colors.blue),
                  _BreakdownRow(label: 'Completed Trips', count: s.completedBookings, color: Colors.green),
                  _BreakdownRow(label: 'Cancelled Bookings', count: s.cancelledBookings, color: Colors.red),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildUsersTab() {
    return Column(
      children: [
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Row(
            children: [
              ChoiceChip(
                label: const Text('All'),
                selected: _roleFilter == null,
                onSelected: (_) => _filterUsers(null),
              ),
              const SizedBox(width: 8),
              ChoiceChip(
                label: const Text('Travelers'),
                selected: _roleFilter == 'USER',
                onSelected: (_) => _filterUsers('USER'),
              ),
              const SizedBox(width: 8),
              ChoiceChip(
                label: const Text('Hotel Owners'),
                selected: _roleFilter == 'HOTEL_OWNER',
                onSelected: (_) => _filterUsers('HOTEL_OWNER'),
              ),
              const SizedBox(width: 8),
              ChoiceChip(
                label: const Text('Travel Agents'),
                selected: _roleFilter == 'TRAVEL_AGENT',
                onSelected: (_) => _filterUsers('TRAVEL_AGENT'),
              ),
              const SizedBox(width: 8),
              ChoiceChip(
                label: const Text('Admins'),
                selected: _roleFilter == 'ADMIN',
                onSelected: (_) => _filterUsers('ADMIN'),
              ),
            ],
          ),
        ),
        Expanded(
          child: _users.isEmpty
              ? const EmptyStateWidget(icon: Icons.person_off_outlined, title: 'No users found', message: 'No users match this role filter.')
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _users.length,
                  itemBuilder: (context, i) {
                    final u = _users[i];
                    return Card(
                      margin: const EdgeInsets.only(bottom: 12),
                      child: ListTile(
                        leading: CircleAvatar(
                          child: Text(u.firstName.isNotEmpty ? u.firstName[0].toUpperCase() : '?'),
                        ),
                        title: Text(u.fullName.isNotEmpty ? u.fullName : u.email),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(u.email),
                            const SizedBox(height: 4),
                            Wrap(
                              spacing: 6,
                              children: [
                                Chip(
                                  label: Text(u.role, style: const TextStyle(fontSize: 10)),
                                  padding: EdgeInsets.zero,
                                  visualDensity: VisualDensity.compact,
                                ),
                                Chip(
                                  label: Text(
                                    u.isActive ? 'Active' : 'Suspended',
                                    style: TextStyle(
                                      fontSize: 10,
                                      color: u.isActive ? Colors.green.shade800 : Colors.red.shade800,
                                    ),
                                  ),
                                  backgroundColor: u.isActive ? Colors.green.shade50 : Colors.red.shade50,
                                  padding: EdgeInsets.zero,
                                  visualDensity: VisualDensity.compact,
                                ),
                              ],
                            ),
                          ],
                        ),
                        trailing: IconButton(
                          icon: Icon(
                            u.isActive ? Icons.block : Icons.check_circle_outline,
                            color: u.isActive ? Colors.red : Colors.green,
                          ),
                          tooltip: u.isActive ? 'Suspend User' : 'Activate User',
                          onPressed: () => _toggleUserActive(u),
                        ),
                      ),
                    );
                  },
                ),
        ),
      ],
    );
  }

  Widget _buildApprovalsTab() {
    if (_pendingHotels.isEmpty && _pendingPackages.isEmpty) {
      return const EmptyStateWidget(
        icon: Icons.check_circle_outline,
        title: 'All caught up!',
        message: 'No hotels or packages are currently pending approval.',
      );
    }

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (_pendingHotels.isNotEmpty) ...[
          Text('Pending Hotels (${_pendingHotels.length})', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          ..._pendingHotels.map((h) => Card(
                margin: const EdgeInsets.only(bottom: 12),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.apartment, color: Colors.amber),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(h.name, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                          ),
                          const Chip(
                            label: Text('Pending', style: TextStyle(fontSize: 10, color: Colors.orange)),
                            backgroundColor: Color(0xFFFFF3E0),
                            visualDensity: VisualDensity.compact,
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text('${h.city}, ${h.country}', style: TextStyle(color: Colors.grey.shade700)),
                      Text('${h.roomCount} rooms configured'),
                      const SizedBox(height: 12),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          OutlinedButton(
                            onPressed: () => _approveHotel(h.id, 2), // Rejected
                            style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
                            child: const Text('Reject'),
                          ),
                          const SizedBox(width: 8),
                          FilledButton(
                            onPressed: () => _approveHotel(h.id, 1), // Approved
                            style: FilledButton.styleFrom(backgroundColor: Colors.green),
                            child: const Text('Approve'),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              )),
          const SizedBox(height: 16),
        ],
        if (_pendingPackages.isNotEmpty) ...[
          Text('Pending Packages (${_pendingPackages.length})', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          ..._pendingPackages.map((p) => Card(
                margin: const EdgeInsets.only(bottom: 12),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.inventory_2_outlined, color: Colors.teal),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(p.title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                          ),
                          const Chip(
                            label: Text('Pending', style: TextStyle(fontSize: 10, color: Colors.orange)),
                            backgroundColor: Color(0xFFFFF3E0),
                            visualDensity: VisualDensity.compact,
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text('Destination: ${p.destinationName} • ${p.durationDays} days'),
                      Text('Price: ${formatMoney(p.price)} / person'),
                      const SizedBox(height: 12),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [
                          OutlinedButton(
                            onPressed: () => _approvePackage(p.id, 2), // Rejected
                            style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
                            child: const Text('Reject'),
                          ),
                          const SizedBox(width: 8),
                          FilledButton(
                            onPressed: () => _approvePackage(p.id, 1), // Approved
                            style: FilledButton.styleFrom(backgroundColor: Colors.green),
                            child: const Text('Approve'),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              )),
        ],
      ],
    );
  }
}

class _MetricCard extends StatelessWidget {
  final String title;
  final String value;
  final String? subtitle;
  final IconData icon;
  final Color color;

  const _MetricCard({
    required this.title,
    required this.value,
    this.subtitle,
    required this.icon,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 1,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(title, style: TextStyle(color: Colors.grey.shade600, fontSize: 12)),
                Icon(icon, color: color, size: 20),
              ],
            ),
            const SizedBox(height: 8),
            Text(value, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
            if (subtitle != null) ...[
              const SizedBox(height: 4),
              Text(subtitle!, style: TextStyle(fontSize: 11, color: Colors.grey.shade600)),
            ],
          ],
        ),
      ),
    );
  }
}

class _BreakdownRow extends StatelessWidget {
  final String label;
  final int count;
  final Color color;

  const _BreakdownRow({required this.label, required this.count, required this.color});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Container(width: 10, height: 10, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
          const SizedBox(width: 8),
          Expanded(child: Text(label)),
          Text('$count', style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}
