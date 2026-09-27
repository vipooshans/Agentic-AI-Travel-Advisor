import 'package:flutter/material.dart';

import '../models/suggested_plan.dart';

class SuggestedPlanCard extends StatelessWidget {
  final SuggestedPlan plan;
  final bool saving;
  final VoidCallback? onSave;

  const SuggestedPlanCard({
    super.key,
    required this.plan,
    this.saving = false,
    this.onSave,
  });

  @override
  Widget build(BuildContext context) {
    final overBudget = plan.budget > 0 && plan.estimatedCost > plan.budget;
    return Card(
      margin: const EdgeInsets.only(top: 8, bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(plan.title, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            Text(
              '${plan.destinationName} · ${plan.travelers} traveler(s)',
              style: TextStyle(color: Colors.grey.shade600),
            ),
            const SizedBox(height: 8),
            Text(
              'Estimated Rs. ${plan.estimatedCost.toStringAsFixed(0)} / Budget Rs. ${plan.budget.toStringAsFixed(0)}',
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: overBudget ? Colors.orange.shade800 : Colors.green.shade700,
              ),
            ),
            if (plan.hotel != null) ...[
              const SizedBox(height: 8),
              Text('Hotel: ${plan.hotel!.name}${plan.hotel!.roomName != null ? ' — ${plan.hotel!.roomName}' : ''}'),
            ],
            if (plan.package != null) ...[
              const SizedBox(height: 4),
              Text('Package: ${plan.package!.title} (Rs. ${plan.package!.price.toStringAsFixed(0)})'),
            ],
            const SizedBox(height: 12),
            ...plan.items.map((item) => Padding(
                  padding: const EdgeInsets.only(bottom: 6),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      SizedBox(
                        width: 52,
                        child: Text('Day ${item.dayNumber}', style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 12)),
                      ),
                      Expanded(
                        child: Text(
                          item.timeLabel.isEmpty ? item.title : '${item.timeLabel} ${item.title}',
                        ),
                      ),
                    ],
                  ),
                )),
            if (onSave != null) ...[
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: saving ? null : onSave,
                  style: FilledButton.styleFrom(shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12))),
                  child: Text(saving ? 'Saving...' : 'Save itinerary'),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
