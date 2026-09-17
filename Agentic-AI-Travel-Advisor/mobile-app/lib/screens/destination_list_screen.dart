import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../models/destination.dart';
import '../providers/auth_provider.dart';
import '../services/destination_service.dart';
import '../services/hotel_service.dart';
import '../services/package_service.dart';
import '../widgets/destination_card.dart';
import '../widgets/error_widget.dart';
import '../widgets/hotel_card.dart';
import '../widgets/loading_widget.dart';
import '../widgets/package_card.dart';

class DestinationListScreen extends StatefulWidget {
  const DestinationListScreen({super.key});

  @override
  State<DestinationListScreen> createState() => _DestinationListScreenState();
}

class _DestinationListScreenState extends State<DestinationListScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
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

class _DestinationsTab extends StatelessWidget {
  final DestinationService service;
  const _DestinationsTab({required this.service});

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<Destination>>(
      future: service.getAll(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
        if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => (context as Element).markNeedsBuild());
        final items = snapshot.data ?? [];
        return GridView.builder(
          padding: const EdgeInsets.all(12),
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, childAspectRatio: 0.75, crossAxisSpacing: 12, mainAxisSpacing: 12),
          itemCount: items.length,
          itemBuilder: (_, i) => DestinationCard(destination: items[i], onTap: () => context.push('/destinations/${items[i].id}')),
        );
      },
    );
  }
}

class _HotelsTab extends StatelessWidget {
  final HotelService service;
  const _HotelsTab({required this.service});

  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: service.getAll(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
        if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString());
        final items = snapshot.data ?? [];
        return ListView.builder(
          padding: const EdgeInsets.all(12),
          itemCount: items.length,
          itemBuilder: (_, i) => Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: HotelCard(hotel: items[i], onTap: () => context.push('/hotels/${items[i].id}')),
          ),
        );
      },
    );
  }
}

class _PackagesTab extends StatelessWidget {
  final PackageService service;
  const _PackagesTab({required this.service});

  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: service.getAll(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
        if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString());
        final items = snapshot.data ?? [];
        return ListView.builder(
          padding: const EdgeInsets.all(12),
          itemCount: items.length,
          itemBuilder: (_, i) => Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: PackageCard(package: items[i], onTap: () => context.push('/packages/${items[i].id}')),
          ),
        );
      },
    );
  }
}
