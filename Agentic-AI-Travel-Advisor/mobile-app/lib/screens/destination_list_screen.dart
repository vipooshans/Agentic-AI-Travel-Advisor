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
import '../widgets/category_filter_chips.dart';
import '../widgets/destination_card.dart';
import '../widgets/empty_state_widget.dart';
import '../widgets/error_widget.dart';
import '../widgets/hotel_card.dart';
import '../widgets/package_card.dart';
import '../widgets/skeleton_loader.dart';

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
        title: const Text('Explore Sri Lanka'),
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
  String _searchQuery = '';
  String _selectedCategory = 'All';

  static const _categories = ['All', 'Cultural', 'Nature', 'Beaches', 'Hill Country'];

  @override
  void initState() {
    super.initState();
    _future = widget.service.getAll();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
          child: TextField(
            decoration: InputDecoration(
              hintText: 'Search destinations (e.g. Ella, Kandy, Galle)...',
              prefixIcon: const Icon(Icons.search),
              contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
            ),
            onChanged: (q) => setState(() => _searchQuery = q.trim().toLowerCase()),
          ),
        ),
        CategoryFilterChips(
          categories: _categories,
          selectedCategory: _selectedCategory,
          onSelected: (cat) => setState(() => _selectedCategory = cat),
        ),
        const SizedBox(height: 8),
        Expanded(
          child: FutureBuilder<List<Destination>>(
            future: _future,
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return const SkeletonCardList(itemCount: 4);
              }
              if (snapshot.hasError) {
                return ErrorDisplayWidget(
                  message: snapshot.error.toString(),
                  onRetry: () => setState(() => _future = widget.service.getAll()),
                );
              }
              var items = snapshot.data ?? [];
              if (_searchQuery.isNotEmpty) {
                items = items.where((d) => d.name.toLowerCase().contains(_searchQuery) || (d.description?.toLowerCase().contains(_searchQuery) ?? false)).toList();
              }
              if (items.isEmpty) {
                return EmptyStateWidget(
                  icon: Icons.public,
                  title: 'No destinations found',
                  message: 'Try adjusting your search query or selected filter category.',
                  actionLabel: 'Reset Filters',
                  onAction: () => setState(() {
                    _searchQuery = '';
                    _selectedCategory = 'All';
                  }),
                );
              }
              return RefreshIndicator(
                onRefresh: () async => setState(() => _future = widget.service.getAll()),
                child: GridView.builder(
                  padding: const EdgeInsets.all(12),
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 2,
                    childAspectRatio: 0.75,
                    crossAxisSpacing: 12,
                    mainAxisSpacing: 12,
                  ),
                  itemCount: items.length,
                  itemBuilder: (_, i) => DestinationCard(
                    destination: items[i],
                    onTap: () => context.push('/destinations/${items[i].id}'),
                  ),
                ),
              );
            },
          ),
        ),
      ],
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
    return FutureBuilder<List<Hotel>>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const SkeletonCardList(itemCount: 4);
        if (snapshot.hasError) {
          return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = widget.service.getAll()));
        }
        final items = snapshot.data ?? [];
        if (items.isEmpty) {
          return EmptyStateWidget(
            icon: Icons.hotel_outlined,
            title: 'No hotels found',
            message: 'Browse back soon for certified accommodations.',
            actionLabel: 'Refresh',
            onAction: () => setState(() => _future = widget.service.getAll()),
          );
        }
        return RefreshIndicator(
          onRefresh: () async => setState(() => _future = widget.service.getAll()),
          child: ListView.separated(
            padding: const EdgeInsets.all(12),
            itemCount: items.length,
            separatorBuilder: (_, __) => const SizedBox(height: 12),
            itemBuilder: (_, i) => HotelCard(hotel: items[i], onTap: () => context.push('/hotels/${items[i].id}')),
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
    return FutureBuilder<List<TravelPackage>>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const SkeletonCardList(itemCount: 4);
        if (snapshot.hasError) {
          return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _future = widget.service.getAll()));
        }
        final items = snapshot.data ?? [];
        if (items.isEmpty) {
          return EmptyStateWidget(
            icon: Icons.card_travel_outlined,
            title: 'No travel packages',
            message: 'Curated travel packages will appear once released.',
            actionLabel: 'Refresh',
            onAction: () => setState(() => _future = widget.service.getAll()),
          );
        }
        return RefreshIndicator(
          onRefresh: () async => setState(() => _future = widget.service.getAll()),
          child: ListView.separated(
            padding: const EdgeInsets.all(12),
            itemCount: items.length,
            separatorBuilder: (_, __) => const SizedBox(height: 12),
            itemBuilder: (_, i) => PackageCard(package: items[i], onTap: () => context.push('/packages/${items[i].id}')),
          ),
        );
      },
    );
  }
}
