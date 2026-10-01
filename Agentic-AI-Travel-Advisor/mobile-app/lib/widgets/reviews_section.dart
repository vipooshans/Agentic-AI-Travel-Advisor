import 'package:flutter/material.dart';

import '../models/review.dart';
import '../utils/format.dart';

/// Visible reviews for a hotel or package, loaded once.
class ReviewsSection extends StatefulWidget {
  final Future<List<Review>> Function() load;
  final double? averageRating;
  final int reviewCount;

  const ReviewsSection({super.key, required this.load, this.averageRating, this.reviewCount = 0});

  @override
  State<ReviewsSection> createState() => _ReviewsSectionState();
}

class _ReviewsSectionState extends State<ReviewsSection> {
  late Future<List<Review>> _future = widget.load();

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(child: Text('Reviews', style: Theme.of(context).textTheme.titleMedium)),
            StarRating(rating: widget.averageRating ?? 0, size: 18),
            const SizedBox(width: 6),
            Text(formatRating(widget.averageRating, widget.reviewCount), style: TextStyle(color: Colors.grey.shade700)),
          ],
        ),
        const SizedBox(height: 8),
        FutureBuilder<List<Review>>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Padding(
                padding: EdgeInsets.all(12),
                child: Center(child: CircularProgressIndicator()),
              );
            }
            if (snapshot.hasError) {
              return Row(
                children: [
                  Expanded(child: Text('Could not load reviews: ${snapshot.error}', style: TextStyle(color: Colors.red.shade700))),
                  TextButton(
                    onPressed: () => setState(() {
                      _future = widget.load();
                    }),
                    child: const Text('Retry'),
                  ),
                ],
              );
            }
            final reviews = snapshot.data ?? const [];
            if (reviews.isEmpty) {
              return Text('No reviews yet.', style: TextStyle(color: Colors.grey.shade600));
            }
            return Column(children: [for (final review in reviews) _ReviewTile(review: review)]);
          },
        ),
      ],
    );
  }
}

class _ReviewTile extends StatelessWidget {
  final Review review;
  const _ReviewTile({required this.review});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                StarRating(rating: review.rating.toDouble(), size: 16),
                const SizedBox(width: 8),
                Expanded(child: Text(review.authorName, style: const TextStyle(fontWeight: FontWeight.w600))),
                Text(formatDate(review.createdAt.toLocal()), style: TextStyle(color: Colors.grey.shade600, fontSize: 12)),
              ],
            ),
            if (review.comment != null && review.comment!.isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(review.comment!),
            ],
          ],
        ),
      ),
    );
  }
}

class StarRating extends StatelessWidget {
  final double rating;
  final double size;
  const StarRating({super.key, required this.rating, this.size = 16});

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: '${rating.toStringAsFixed(1)} out of 5 stars',
      excludeSemantics: true,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          for (var i = 1; i <= 5; i++)
            Icon(
              rating >= i ? Icons.star : (rating >= i - 0.5 ? Icons.star_half : Icons.star_border),
              size: size,
              color: Colors.amber.shade700,
            ),
        ],
      ),
    );
  }
}
