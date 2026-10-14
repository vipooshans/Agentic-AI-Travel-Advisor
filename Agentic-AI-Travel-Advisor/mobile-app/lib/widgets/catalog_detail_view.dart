import 'package:flutter/material.dart';
import 'catalog_image.dart';

class CatalogDetailView extends StatelessWidget {
  final String title;
  final String? imageUrl;
  final IconData fallbackIcon;
  final List<Widget> children;
  final Widget? bottomBar;

  const CatalogDetailView({
    super.key,
    required this.title,
    required this.imageUrl,
    required this.fallbackIcon,
    required this.children,
    this.bottomBar,
  });

  @override
  Widget build(BuildContext context) {
    final primary = Theme.of(context).colorScheme.primary;

    return Scaffold(
      backgroundColor: const Color(0xFFF7F9FB),
      bottomNavigationBar: bottomBar,
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            pinned: true,
            stretch: true,
            expandedHeight: 300,
            foregroundColor: Colors.white,
            backgroundColor: primary,
            flexibleSpace: FlexibleSpaceBar(
              titlePadding: const EdgeInsetsDirectional.only(start: 56, bottom: 16, end: 16),
              title: Text(
                title,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 18),
              ),
              background: Stack(
                fit: StackFit.expand,
                children: [
                  Positioned.fill(
                    child: CatalogImage(
                      imageUrl: imageUrl,
                      fallbackIcon: fallbackIcon,
                      placeholderColor: const Color(0xFF16324F),
                      iconColor: Colors.white70,
                    ),
                  ),
                  const DecoratedBox(
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topCenter,
                        end: Alignment.bottomCenter,
                        colors: [Color(0x66000000), Color(0x00000000), Color(0xCC000000)],
                        stops: [0, 0.45, 1],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: children,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
