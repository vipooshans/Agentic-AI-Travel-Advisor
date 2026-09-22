import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/destination.dart';
import '../models/hotel.dart';
import '../models/travel_package.dart';
import '../providers/auth_provider.dart';
import '../services/destination_service.dart';
import '../services/hotel_service.dart';
import '../services/package_service.dart';
import '../widgets/destination_card.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/error_widget.dart';
import '../widgets/hotel_card.dart';
import '../widgets/loading_widget.dart';
import '../widgets/package_card.dart';

class DestinationListScreen extends StatefulWidget {
  final String? initialTab;
  const DestinationListScreen({super.key, this.initialTab});

  @override
  State<DestinationListScreen> createState() => _DestinationListScreenState();
}

class _DestinationListScreenState extends State<DestinationListScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;

  static int _indexFor(String? tab) {
    switch (tab) {
      case 'hotels':
        return 1;
      case 'packages':
        return 2;
      default:
        return 0;
    }
  }

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this, initialIndex: _indexFor(widget.initialTab));
  }

  @override
  void didUpdateWidget(covariant DestinationListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.initialTab != widget.initialTab) {
      _tabController.index = _indexFor(widget.initialTab);
    }
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final api = context.read<AuthProvider>().api;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Explore'),
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Destinations'),
            Tab(text: 'Hotels'),
            Tab(text: 'Packages'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _DestinationsTab(service: DestinationService(api)),
          _HotelsTab(service: HotelService(api)),
          _PackagesTab(service: PackageService(api)),
        ],
      ),
    );
  }
}

class _DestinationsTab extends StatefulWidget {
  final DestinationService service;
  const _DestinationsTab({required this.service});

  @override
  State<_DestinationsTab> createState() => _DestinationsTabState();
}

class _DestinationsTabState extends State<_DestinationsTab> {
  late Future<List<Destination>> _future;

  @override
  void initState() {
    super.initState();
    _future = widget.service.getAll();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<Destination>>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
        if (snapshot.hasError) {
          return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = widget.service.getAll()));
        }
        final items = snapshot.data ?? [];
        if (items.isEmpty) {
          return EmptyStateWidget(
            icon: Icons.public,
            title: 'No destinations',
            message: 'Travel destinations will appear here when they are added.',
            actionLabel: 'Retry',
            onAction: () => setState(() => _future = widget.service.getAll()),
          );
        }
        return RefreshIndicator(
          onRefresh: () async => setState(() => _future = widget.service.getAll()),
          child: GridView.builder(
            padding: const EdgeInsets.all(12),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, childAspectRatio: 0.75, crossAxisSpacing: 12, mainAxisSpacing: 12),
            itemCount: items.length,
            itemBuilder: (_, i) => DestinationCard(destination: items[i], onTap: () => context.push('/destinations/${items[i].id}')),
          ),
        );
      },
    );
  }
}

class _HotelsTab extends StatefulWidget {
  final HotelService service;
  const _HotelsTab({required this.service});

  @override
  State<_HotelsTab> createState() => _HotelsTabState();
}

class _HotelsTabState extends State<_HotelsTab> {
  late Future<List<Hotel>> _future;

  @override
  void initState() {
    super.initState();
    _future = widget.service.getAll();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
        if (snapshot.hasError) {
          return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = widget.service.getAll()));
        }
        final items = snapshot.data ?? [];
        if (items.isEmpty) {
          return EmptyStateWidget(
            icon: Icons.hotel_outlined,
            title: 'No hotels yet',
            message: 'Approved hotels will show up here.',
            actionLabel: 'Retry',
            onAction: () => setState(() => _future = widget.service.getAll()),
          );
        }
        return RefreshIndicator(
          onRefresh: () async => setState(() => _future = widget.service.getAll()),
          child: ListView.builder(
            padding: const EdgeInsets.all(12),
            itemCount: items.length,
            itemBuilder: (_, i) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: HotelCard(hotel: items[i], onTap: () => context.push('/hotels/${items[i].id}')),
            ),
          ),
        );
      },
    );
  }
}

class _PackagesTab extends StatefulWidget {
  final PackageService service;
  const _PackagesTab({required this.service});

  @override
  State<_PackagesTab> createState() => _PackagesTabState();
}

class _PackagesTabState extends State<_PackagesTab> {
  late Future<List<TravelPackage>> _future;

  @override
  void initState() {
    super.initState();
    _future = widget.service.getAll();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
        if (snapshot.hasError) {
          return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = widget.service.getAll()));
        }
        final items = snapshot.data ?? [];
        if (items.isEmpty) {
          return EmptyStateWidget(
            icon: Icons.card_travel,
            title: 'No packages yet',
            message: 'Approved travel packages will show up here.',
            actionLabel: 'Retry',
            onAction: () => setState(() => _future = widget.service.getAll()),
          );
        }
        return RefreshIndicator(
          onRefresh: () async => setState(() => _future = widget.service.getAll()),
          child: ListView.builder(
            padding: const EdgeInsets.all(12),
            itemCount: items.length,
            itemBuilder: (_, i) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: PackageCard(package: items[i], onTap: () => context.push('/packages/${items[i].id}')),
            ),
          ),
        );
      },
    );
  }
}
