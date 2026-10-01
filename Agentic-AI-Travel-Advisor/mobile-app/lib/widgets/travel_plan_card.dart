import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../models/travel_plan.dart';
import '../utils/format.dart';

class TravelPlanCard extends StatelessWidget {
  final TravelPlan plan;
  final bool saving;
  final VoidCallback? onSave;

  const TravelPlanCard({super.key, required this.plan, this.saving = false, this.onSave});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final hotel = plan.selectedHotel;
    final package = plan.selectedPackage;
    final transport = plan.selectedTransport;
    String money(num v) => formatMoney(v, plan.currency);
    final budgetColor = plan.withinBudget ? Colors.green.shade700 : Colors.orange.shade800;

    return Semantics(
      container: true,
      label: 'Trip plan for ${plan.destination}',
      child: Card(
        margin: const EdgeInsets.symmetric(vertical: 8),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('${plan.destination} trip', style: theme.textTheme.titleMedium),
              const SizedBox(height: 4),
              Text(
                [
                  '${plan.travelers} traveler${plan.travelers == 1 ? '' : 's'}',
                  '${plan.duration} day${plan.duration == 1 ? '' : 's'}',
                  if (plan.startDate.isNotEmpty) '${formatDateString(plan.startDate)} – ${formatDateString(plan.endDate)}',
                ].join(' · '),
                style: TextStyle(color: Colors.grey.shade600),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      'Estimated ${money(plan.estimatedTotal)}',
                      style: TextStyle(fontWeight: FontWeight.bold, color: budgetColor),
                    ),
                  ),
                  Chip(
                    label: Text(plan.withinBudget ? 'Within budget' : 'Over budget'),
                    labelStyle: TextStyle(color: budgetColor, fontSize: 12),
                    visualDensity: VisualDensity.compact,
                  ),
                ],
              ),
              if (plan.budget > 0) Text('Budget ${money(plan.budget)}', style: TextStyle(color: Colors.grey.shade700)),
              if (plan.accommodationCost > 0) _CostLine('Accommodation', money(plan.accommodationCost)),
              if (plan.packagesCost > 0) _CostLine('Packages', money(plan.packagesCost)),
              if (plan.transportationCost > 0) _CostLine('Transport', money(plan.transportationCost)),
              if (hotel != null) ...[
                const _SectionTitle('Stay'),
                _LinkTile(
                  title: hotel.name,
                  subtitle:
                      '${hotel.roomName} · ${money(hotel.pricePerNight)} × ${hotel.nights} night${hotel.nights == 1 ? '' : 's'}'
                      '${hotel.rooms > 1 ? ' × ${hotel.rooms} rooms' : ''} = ${money(hotel.totalCost)}',
                  onTap: () => context.push('/hotels/${hotel.hotelId}'),
                ),
              ],
              if (package != null) ...[
                const _SectionTitle('Package'),
                _LinkTile(
                  title: package.title,
                  subtitle: '${package.durationDays} days · ${money(package.pricePerPerson)} per person = ${money(package.totalCost)}',
                  onTap: () => context.push('/packages/${package.packageId}'),
                ),
              ],
              if (plan.activities.isNotEmpty) ...[
                const _SectionTitle('Activities'),
                for (final activity in plan.activities)
                  _Bullet(
                    'Day ${activity.day}: ${activity.title}'
                    '${activity.includedInCost ? ' (included)' : activity.pricePerPerson > 0 ? ' · ${money(activity.pricePerPerson)} pp' : ''}',
                  ),
              ],
              if (transport.isNotEmpty) ...[
                const _SectionTitle('Transport'),
                for (final leg in transport)
                  _Bullet(
                    '${leg.mode}: ${leg.from} → ${leg.to}'
                    '${leg.departureTime != null && leg.departureTime!.isNotEmpty ? ' at ${leg.departureTime}' : ''}'
                    ' · ${formatDuration(leg.durationMinutes)} · ${money(leg.totalCost)}',
                  ),
              ],
              if (plan.itinerary.isNotEmpty) ...[
                const _SectionTitle('Day by day'),
                for (final day in plan.itinerary) ...[
                  Padding(
                    padding: const EdgeInsets.only(top: 6, bottom: 2),
                    child: Text(
                      'Day ${day.day}${day.date.isNotEmpty ? ' · ${formatDateString(day.date)}' : ''}',
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                  ),
                  for (final item in day.items) _Bullet(item.time.isEmpty ? item.title : '${item.time}  ${item.title}'),
                ],
              ],
              if (plan.warnings.isNotEmpty) ...[
                const SizedBox(height: 12),
                for (final warning in plan.warnings)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 4),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(Icons.warning_amber_rounded, size: 18, color: Colors.orange.shade800),
                        const SizedBox(width: 6),
                        Expanded(child: Text(warning, style: TextStyle(color: Colors.orange.shade900))),
                      ],
                    ),
                  ),
              ],
              if (plan.assumptions.isNotEmpty) ...[
                const SizedBox(height: 8),
                for (final assumption in plan.assumptions)
                  Text(assumption, style: TextStyle(color: Colors.grey.shade600, fontSize: 12)),
              ],
              if (onSave != null) ...[
                const SizedBox(height: 12),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton(
                    onPressed: saving ? null : onSave,
                    child: Text(saving ? 'Saving...' : 'Save itinerary'),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  final String text;
  const _SectionTitle(this.text);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 14, bottom: 4),
      child: Text(text, style: Theme.of(context).textTheme.labelLarge?.copyWith(color: Colors.blue.shade800)),
    );
  }
}

class _CostLine extends StatelessWidget {
  final String label;
  final String value;
  const _CostLine(this.label, this.value);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 2),
      child: Row(
        children: [
          Expanded(child: Text(label, style: TextStyle(color: Colors.grey.shade700))),
          Text(value),
        ],
      ),
    );
  }
}

class _Bullet extends StatelessWidget {
  final String text;
  const _Bullet(this.text);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('•  '),
          Expanded(child: Text(text)),
        ],
      ),
    );
  }
}

class _LinkTile extends StatelessWidget {
  final String title;
  final String subtitle;
  final VoidCallback onTap;
  const _LinkTile({required this.title, required this.subtitle, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
                  Text(subtitle, style: TextStyle(color: Colors.grey.shade700, fontSize: 13)),
                ],
              ),
            ),
            const Icon(Icons.chevron_right),
          ],
        ),
      ),
    );
  }
}
