import 'package:flutter/material.dart';

class CatalogImage extends StatelessWidget {
  final String? imageUrl;
  final double? height;
  final IconData fallbackIcon;
  final BoxFit fit;
  final Color placeholderColor;
  final Color iconColor;

  const CatalogImage({
    super.key,
    required this.imageUrl,
    required this.fallbackIcon,
    this.height,
    this.fit = BoxFit.cover,
    this.placeholderColor = const Color(0xFFD6EAF8),
    this.iconColor = const Color(0xFF1A5276),
  });

  @override
  Widget build(BuildContext context) {
    final framed = imageUrl == null || imageUrl!.isEmpty
        ? _placeholder()
        : Image.network(
            imageUrl!,
            fit: fit,
            width: double.infinity,
            height: double.infinity,
            errorBuilder: (_, __, ___) => _placeholder(),
            loadingBuilder: (context, child, progress) {
              if (progress == null) return child;
              return _placeholder(child: const CircularProgressIndicator(strokeWidth: 2));
            },
          );

    return SizedBox(
      width: double.infinity,
      height: height ?? double.infinity,
      child: framed,
    );
  }

  Widget _placeholder({Widget? child}) {
    return ColoredBox(
      color: placeholderColor,
      child: Center(
        child: child ?? Icon(fallbackIcon, size: height == null ? 72 : 48, color: iconColor),
      ),
    );
  }
}
