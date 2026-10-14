import 'package:flutter/material.dart';

import '../models/transportation.dart';
import '../utils/format.dart';

/// Transport options for a destination or package. Renders nothing when
/// there are none, so screens can always include it.
class TransportSection extends StatefulWidget {
  final Future<List<Transportation>> Function() load;
  const TransportSection({super.key, required this.load});

  @override
  State<TransportSection> createState() => _TransportSectionState();
}

class _TransportSectionState extends State<TransportSection> {
  late final Future<List<Transportation>> _future = widget.load();

  static IconData _icon(int mode) => switch (mode) {
        0 => Icons.directions_bus,
        1 => Icons.train,
        5 => Icons.flight,
        6 => Icons.directions_boat,
        _ => Icons.directions_car,
      };

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<Transportation>>(
      future: _future,
      builder: (context, snapshot) {
        final options = snapshot.data ?? const [];
        if (snapshot.connectionState != ConnectionState.done || snapshot.hasError || options.isEmpty) {
          return const SizedBox.shrink();
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 24),
            Text('Getting there', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            for (final option in options)
              Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: ListTile(
                  leading: Icon(_icon(option.mode)),
                  title: Text('${option.fromLocation} → ${option.toLocation}'),
                  subtitle: Text([
                    option.modeLabel,
                    if (option.departureLabel != null) 'departs ${option.departureLabel}',
                    formatDuration(option.durationMinutes),
                  ].join(' · ')),
                  trailing: Text('${formatMoney(option.pricePerPerson)}\nper person', textAlign: TextAlign.right),
                ),
              ),
          ],
        );
      },
    );
  }
}
