import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../models/booking.dart';
import '../../models/destination.dart';
import '../../models/report_summary.dart';
import '../../models/travel_package.dart';
import '../../providers/auth_provider.dart';
import '../../services/admin_service.dart';
import '../../services/booking_service.dart';
import '../../services/destination_service.dart';
import '../../services/package_service.dart';
import '../../utils/format.dart';
import '../../widgets/empty_state_widget.dart';
import '../../widgets/error_widget.dart' as app_err;
import '../../widgets/loading_widget.dart';

class PackageDashboardScreen extends StatefulWidget {
  const PackageDashboardScreen({super.key});

  @override
  State<PackageDashboardScreen> createState() => _PackageDashboardScreenState();
}

class _PackageDashboardScreenState extends State<PackageDashboardScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  List<TravelPackage> _myPackages = [];
  List<Booking> _bookings = [];
  List<Destination> _destinations = [];
  ReportSummary? _summary;

  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
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
    final packageService = PackageService(api);
    final bookingService = BookingService(api);
    final destService = DestinationService(api);
    final adminService = AdminService(api);

    try {
      final results = await Future.wait([
        packageService.getMine(),
        bookingService.getAll(),
        destService.getAll(),
        adminService.getSummary().catchError((_) => const ReportSummary()),
      ]);

      if (mounted) {
        setState(() {
          _myPackages = results[0] as List<TravelPackage>;
          _bookings = results[1] as List<Booking>;
          _destinations = results[2] as List<Destination>;
          _summary = results[3] as ReportSummary;
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

  Future<void> _showCreatePackageDialog() async {
    final titleController = TextEditingController();
    final descriptionController = TextEditingController();
    final priceController = TextEditingController();
    final durationController = TextEditingController();
    final maxTravelersController = TextEditingController(text: '10');
    final imageController = TextEditingController();
    int? selectedDestId = _destinations.isNotEmpty ? _destinations.first.id : null;
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
                  Text('Create Travel Package', style: Theme.of(context).textTheme.titleLarge),
                  const SizedBox(height: 16),
                  if (_destinations.isNotEmpty)
                    DropdownButtonFormField<int>(
                      initialValue: selectedDestId,
                      decoration: const InputDecoration(labelText: 'Destination', border: OutlineInputBorder()),
                      items: _destinations
                          .map((d) => DropdownMenuItem(value: d.id, child: Text('${d.name}, ${d.country}')))
                          .toList(),
                      onChanged: (v) => setModalState(() => selectedDestId = v),
                      validator: (v) => v == null ? 'Select destination' : null,
                    ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: titleController,
                    decoration: const InputDecoration(labelText: 'Package Title', border: OutlineInputBorder()),
                    validator: (v) => v == null || v.trim().isEmpty ? 'Enter package title' : null,
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: TextFormField(
                          controller: priceController,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          decoration: const InputDecoration(labelText: 'Price (LKR)', border: OutlineInputBorder()),
                          validator: (v) => (double.tryParse(v ?? '') ?? 0) > 0 ? null : 'Valid price',
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: TextFormField(
                          controller: durationController,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(labelText: 'Duration (Days)', border: OutlineInputBorder()),
                          validator: (v) => (int.tryParse(v ?? '') ?? 0) > 0 ? null : 'Valid days',
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: maxTravelersController,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Max Travelers', border: OutlineInputBorder()),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: descriptionController,
                    maxLines: 2,
                    decoration: const InputDecoration(labelText: 'Description', border: OutlineInputBorder()),
                  ),
                  const SizedBox(height: 20),
                  FilledButton(
                    onPressed: () async {
                      if (!formKey.currentState!.validate() || selectedDestId == null) return;
                      Navigator.pop(ctx);
                      final api = this.context.read<AuthProvider>().api;
                      try {
                        await PackageService(api).createPackage(
                          destinationId: selectedDestId!,
                          title: titleController.text,
                          description: descriptionController.text,
                          price: double.parse(priceController.text),
                          durationDays: int.parse(durationController.text),
                          maxTravelers: int.tryParse(maxTravelersController.text) ?? 10,
                          imageUrl: imageController.text,
                        );
                        if (mounted) {
                          ScaffoldMessenger.of(this.context).showSnackBar(
                            const SnackBar(content: Text('Package created successfully')),
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
                    child: const Text('Create Package'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _showAddActivityDialog(TravelPackage package) async {
    final titleController = TextEditingController();
    final descriptionController = TextEditingController();
    final priceController = TextEditingController(text: '0');
    final dayController = TextEditingController(text: '1');
    final formKey = GlobalKey<FormState>();

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => Padding(
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
                Text('Add Activity to ${package.title}', style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 16),
                TextFormField(
                  controller: titleController,
                  decoration: const InputDecoration(labelText: 'Activity Title', border: OutlineInputBorder()),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Enter title' : null,
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: dayController,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'Day Number', border: OutlineInputBorder()),
                        validator: (v) => (int.tryParse(v ?? '') ?? 0) > 0 ? null : 'Valid day',
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextFormField(
                        controller: priceController,
                        keyboardType: const TextInputType.numberWithOptions(decimal: true),
                        decoration: const InputDecoration(labelText: 'Extra Price (LKR)', border: OutlineInputBorder()),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: descriptionController,
                  maxLines: 2,
                  decoration: const InputDecoration(labelText: 'Description', border: OutlineInputBorder()),
                ),
                const SizedBox(height: 20),
                FilledButton(
                  onPressed: () async {
                    if (!formKey.currentState!.validate()) return;
                    Navigator.pop(ctx);
                    final api = this.context.read<AuthProvider>().api;
                    try {
                      await PackageService(api).addActivity(
                        packageId: package.id,
                        title: titleController.text,
                        description: descriptionController.text,
                        dayNumber: int.parse(dayController.text),
                        price: double.tryParse(priceController.text) ?? 0,
                      );
                      if (mounted) {
                        ScaffoldMessenger.of(this.context).showSnackBar(
                          const SnackBar(content: Text('Activity added successfully')),
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
                  child: const Text('Add Activity'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _deletePackage(TravelPackage package) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete package?'),
        content: Text('Delete "${package.title}"? This cannot be undone.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            child: const Text('Delete'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;
    final api = context.read<AuthProvider>().api;
    try {
      await PackageService(api).deletePackage(package.id);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Package deleted')));
        _loadData();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red));
      }
    }
  }

  Future<void> _updateBookingStatus(Booking booking, int status) async {
    final api = context.read<AuthProvider>().api;
    try {
      await BookingService(api).updateStatus(booking.id, status);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking updated')));
        _loadData();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e'), backgroundColor: Colors.red));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Packages Dashboard'),
        actions: [
          if (user?.isAdmin == true)
            IconButton(
              icon: const Icon(Icons.admin_panel_settings),
              tooltip: 'Admin Dashboard',
              onPressed: () => context.push('/admin'),
            ),
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadData),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            Tab(icon: const Icon(Icons.inventory_2_outlined), text: 'My Packages (${_myPackages.length})'),
            Tab(icon: const Icon(Icons.bookmark_outline), text: 'Bookings (${_bookings.length})'),
          ],
        ),
      ),
      floatingActionButton: _tabController.index == 0
          ? FloatingActionButton.extended(
              onPressed: _showCreatePackageDialog,
              icon: const Icon(Icons.add),
              label: const Text('New Package'),
            )
          : null,
      body: _loading
          ? const LoadingWidget(message: 'Loading packages dashboard...')
          : _error != null
              ? app_err.ErrorDisplayWidget(message: _error!, onRetry: _loadData)
              : TabBarView(
                  controller: _tabController,
                  children: [
                    _buildPackagesTab(),
                    _buildBookingsTab(),
                  ],
                ),
    );
  }

  Widget _buildPackagesTab() {
    final s = _summary ?? const ReportSummary();
    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Stat Cards
          Row(
            children: [
              Expanded(
                child: _SmallMetric(
                  title: 'My Packages',
                  value: '${_myPackages.length}',
                  icon: Icons.inventory_2,
                  color: Colors.teal,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _SmallMetric(
                  title: 'Revenue',
                  value: formatMoney(s.revenue),
                  icon: Icons.payments_outlined,
                  color: Colors.green,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: _SmallMetric(
                  title: 'Bookings',
                  value: '${_bookings.length}',
                  icon: Icons.bookmark,
                  color: Colors.blue,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          if (_myPackages.isEmpty)
            const EmptyStateWidget(
              icon: Icons.inventory_2_outlined,
              title: 'No packages created yet',
              message: 'Tap the "+ New Package" button below to create your first travel package.',
            )
          else
            ..._myPackages.map((p) => Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(p.title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                            ),
                            _ApprovalChip(status: p.approvalStatus, label: p.approvalStatusLabel),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Text('${p.destinationName} • ${p.durationDays} days • Max ${p.maxTravelers ?? 10} travelers'),
                        Text('Price: ${formatMoney(p.price)} / person', style: const TextStyle(fontWeight: FontWeight.w600)),
                        const SizedBox(height: 8),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.end,
                          children: [
                            TextButton.icon(
                              onPressed: () => _showAddActivityDialog(p),
                              icon: const Icon(Icons.add_task, size: 16),
                              label: const Text('Add Activity'),
                            ),
                            IconButton(
                              icon: const Icon(Icons.visibility_outlined),
                              tooltip: 'Preview details',
                              onPressed: () => context.push('/packages/${p.id}'),
                            ),
                            IconButton(
                              icon: const Icon(Icons.delete_outline, color: Colors.red),
                              tooltip: 'Delete package',
                              onPressed: () => _deletePackage(p),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                )),
        ],
      ),
    );
  }

  Widget _buildBookingsTab() {
    if (_bookings.isEmpty) {
      return const EmptyStateWidget(
        icon: Icons.bookmark_border,
        title: 'No bookings received',
        message: 'When travelers book your packages, they will appear here.',
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: _bookings.length,
      itemBuilder: (context, i) {
        final b = _bookings[i];
        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(b.title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                    Chip(
                      label: Text(b.statusLabel, style: const TextStyle(fontSize: 10)),
                      visualDensity: VisualDensity.compact,
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text('Check-in: ${formatDate(b.checkIn)} • Guests: ${b.guests}'),
                Text('Total: ${formatMoney(b.totalPrice)}', style: const TextStyle(fontWeight: FontWeight.w600)),
                if (b.notes != null && b.notes!.isNotEmpty) Text('Notes: ${b.notes}'),
                const SizedBox(height: 8),
                if (b.status == Booking.pending)
                  Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      OutlinedButton(
                        onPressed: () => _updateBookingStatus(b, Booking.cancelled),
                        style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
                        child: const Text('Decline'),
                      ),
                      const SizedBox(width: 8),
                      FilledButton(
                        onPressed: () => _updateBookingStatus(b, Booking.confirmed),
                        child: const Text('Confirm'),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _SmallMetric extends StatelessWidget {
  final String title;
  final String value;
  final IconData icon;
  final Color color;

  const _SmallMetric({required this.title, required this.value, required this.icon, required this.color});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            Icon(icon, color: color, size: 20),
            const SizedBox(height: 4),
            Text(value, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
            Text(title, style: TextStyle(fontSize: 10, color: Colors.grey.shade600)),
          ],
        ),
      ),
    );
  }
}

class _ApprovalChip extends StatelessWidget {
  final int status;
  final String label;

  const _ApprovalChip({required this.status, required this.label});

  @override
  Widget build(BuildContext context) {
    Color bg;
    Color fg;
    switch (status) {
      case 1:
        bg = Colors.green.shade50;
        fg = Colors.green.shade800;
      case 2:
        bg = Colors.red.shade50;
        fg = Colors.red.shade800;
      default:
        bg = Colors.orange.shade50;
        fg = Colors.orange.shade800;
    }
    return Chip(
      label: Text(label, style: TextStyle(fontSize: 10, color: fg)),
      backgroundColor: bg,
      visualDensity: VisualDensity.compact,
      padding: EdgeInsets.zero,
    );
  }
}
