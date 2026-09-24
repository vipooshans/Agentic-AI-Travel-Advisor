import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:travel_advisor/widgets/category_filter_chips.dart';

void main() {
  testWidgets('CategoryFilterChips renders categories and fires onSelected callback', (tester) async {
    String selected = 'All';
    final categories = ['All', 'Beach', 'Cultural', 'Hill Country'];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: StatefulBuilder(
            builder: (context, setState) {
              return CategoryFilterChips(
                categories: categories,
                selectedCategory: selected,
                onSelected: (cat) {
                  setState(() => selected = cat);
                },
              );
            },
          ),
        ),
      ),
    );

    expect(find.text('All'), findsOneWidget);
    expect(find.text('Beach'), findsOneWidget);
    expect(find.text('Cultural'), findsOneWidget);

    await tester.tap(find.text('Beach'));
    await tester.pumpAndSettle();

    expect(selected, 'Beach');
  });
}
