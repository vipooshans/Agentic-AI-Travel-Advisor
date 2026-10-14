import 'package:flutter/material.dart';

class ErrorDisplayWidget extends StatelessWidget {
  final String message;
  final VoidCallback? onRetry;

  const ErrorDisplayWidget({super.key, required this.message, this.onRetry});

  static String friendly(Object? error) {
    final raw = error?.toString() ?? 'Something went wrong.';
    final cleaned = raw.replaceFirst(RegExp(r'^Exception:\s*'), '');
    final lower = cleaned.toLowerCase();
    if (lower.contains('401') || lower.contains('unauthorized')) {
      return 'Please sign in again.';
    }
    if (lower.contains('404') || lower.contains('not found')) {
      return 'We could not find that item.';
    }
    if (lower.contains('500') || lower.contains('socket') || lower.contains('failed host lookup') || lower.contains('connection')) {
      return 'Unable to reach the server. Check your connection and try again.';
    }
    return cleaned;
  }

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.error_outline, size: 48, color: Colors.red.shade300),
            const SizedBox(height: 16),
            Text(friendly(message), textAlign: TextAlign.center),
            if (onRetry != null) ...[
              const SizedBox(height: 16),
              FilledButton(onPressed: onRetry, child: const Text('Retry')),
            ],
          ],
        ),
      ),
    );
  }
}
