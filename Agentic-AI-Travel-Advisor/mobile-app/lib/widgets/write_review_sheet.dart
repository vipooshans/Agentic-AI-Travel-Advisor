import 'package:flutter/material.dart';

import '../services/review_service.dart';
import '../utils/validators.dart';

/// Rating (1-5) and optional comment for a completed booking.
class WriteReviewSheet extends StatefulWidget {
  final String title;
  final Future<void> Function(int rating, String comment) onSubmit;

  const WriteReviewSheet({super.key, required this.title, required this.onSubmit});

  @override
  State<WriteReviewSheet> createState() => _WriteReviewSheetState();
}

class _WriteReviewSheetState extends State<WriteReviewSheet> {
  final _formKey = GlobalKey<FormState>();
  final _comment = TextEditingController();
  int _rating = 0;
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final formOk = _formKey.currentState!.validate();
    if (_rating < 1 || _rating > 5) {
      setState(() => _error = 'Choose a rating from 1 to 5 stars');
      return;
    }
    if (!formOk) return;
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      await widget.onSubmit(_rating, _comment.text.trim());
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(20, 20, 20, 20 + MediaQuery.of(context).viewInsets.bottom),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Review ${widget.title}', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                for (var i = 1; i <= 5; i++)
                  IconButton(
                    tooltip: '$i star${i == 1 ? '' : 's'}',
                    onPressed: _submitting
                        ? null
                        : () => setState(() {
                              _rating = i;
                              _error = null;
                            }),
                    icon: Icon(i <= _rating ? Icons.star : Icons.star_border, color: Colors.amber.shade700, size: 32),
                  ),
              ],
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _comment,
              maxLines: 4,
              maxLength: ReviewService.maxCommentLength,
              decoration: const InputDecoration(labelText: 'Comment (optional)', border: OutlineInputBorder()),
              validator: (v) => Validators.maxLength(v, ReviewService.maxCommentLength, 'Comment'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 8),
              Text(_error!, style: TextStyle(color: Colors.red.shade700)),
            ],
            const SizedBox(height: 12),
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: Text(_submitting ? 'Submitting...' : 'Submit review'),
            ),
          ],
        ),
      ),
    );
  }
}
