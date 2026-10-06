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
import '../widgets/catalog_search_bar.dart';
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
    final hotels = HotelService(api);
    final packages = PackageService(api);

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
          SearchableCatalogList<Hotel>(
            load: (s) => hotels.getAll(q: s.query, maxPrice: s.maxPrice),
            hint: 'Hotel, city or country',
            priceLabel: 'Max / night',
            emptyIcon: Icons.hotel_outlined,
            emptyTitle: 'No hotels yet',
            emptyMessage: 'Approved hotels will show up here.',
            noMatchTitle: 'No hotels match your search',
            itemBuilder: (context, hotel) => HotelCard(hotel: hotel, onTap: () => context.push('/hotels/${hotel.id}')),
          ),
          SearchableCatalogList<TravelPackage>(
            load: (s) => packages.getAll(q: s.query, maxPrice: s.maxPrice),
            hint: 'Package or destination',
            priceLabel: 'Max price',
            emptyIcon: Icons.card_travel,
            emptyTitle: 'No packages yet',
            emptyMessage: 'Approved travel packages will show up here.',
            noMatchTitle: 'No packages match your search',
            itemBuilder: (context, pkg) => PackageCard(package: pkg, onTap: () => context.push('/packages/${pkg.id}')),
          ),
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

  void _reload() {
    setState(() {
      _future = widget.service.getAll();
    });
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<Destination>>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
        if (snapshot.hasError) {
          return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: _reload);
        }
        final items = snapshot.data ?? [];
        if (items.isEmpty) {
          return EmptyStateWidget(
            icon: Icons.public,
            title: 'No destinations',
            message: 'Travel destinations will appear here when they are added.',
            actionLabel: 'Retry',
            onAction: _reload,
          );
        }
        return RefreshIndicator(
          onRefresh: () async => _reload(),
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

/// A catalog list with keyword and maximum-price search, backed by the API's
/// `q` and `maxPrice` query parameters.
class SearchableCatalogList<T> extends StatefulWidget {
  final Future<List<T>> Function(CatalogSearch search) load;
  final String hint;
  final String priceLabel;
  final IconData emptyIcon;
  final String emptyTitle;
  final String emptyMessage;
  final String noMatchTitle;
  final Widget Function(BuildContext context, T item) itemBuilder;

  const SearchableCatalogList({
    super.key,
    required this.load,
    required this.hint,
    required this.priceLabel,
    required this.emptyIcon,
    required this.emptyTitle,
    required this.emptyMessage,
    required this.noMatchTitle,
    required this.itemBuilder,
  });

  @override
  State<SearchableCatalogList<T>> createState() => _SearchableCatalogListState<T>();
}

class _SearchableCatalogListState<T> extends State<SearchableCatalogList<T>> {
  CatalogSearch _search = const CatalogSearch();
  late Future<List<T>> _future = widget.load(_search);

  void _reload([CatalogSearch? search]) {
    setState(() {
      if (search != null) _search = search;
      _future = widget.load(_search);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        CatalogSearchBar(hint: widget.hint, priceLabel: widget.priceLabel, onSearch: _reload),
        Expanded(
          child: FutureBuilder<List<T>>(
            future: _future,
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
              if (snapshot.hasError) {
                return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: _reload);
              }
              final items = snapshot.data ?? [];
              if (items.isEmpty) {
                final filtered = !_search.isEmpty;
                return EmptyStateWidget(
                  icon: widget.emptyIcon,
                  title: filtered ? widget.noMatchTitle : widget.emptyTitle,
                  message: filtered ? 'Try different keywords or a higher price.' : widget.emptyMessage,
                  actionLabel: 'Retry',
                  onAction: _reload,
                );
              }
              return RefreshIndicator(
                onRefresh: () async => _reload(),
                child: ListView.builder(
                  padding: const EdgeInsets.all(12),
                  itemCount: items.length,
                  itemBuilder: (context, i) => Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: widget.itemBuilder(context, items[i]),
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
