// Catalog widgets must fit long real-world text on a narrow phone with a
// larger system font, without overflow errors (DEF-022).
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:travel_advisor/models/destination.dart';
import 'package:travel_advisor/models/hotel.dart';
import 'package:travel_advisor/models/travel_package.dart';
import 'package:travel_advisor/widgets/destination_card.dart';
import 'package:travel_advisor/widgets/hotel_card.dart';
import 'package:travel_advisor/widgets/package_card.dart';
import 'package:travel_advisor/widgets/reviews_section.dart';

const narrowPhone = Size(320, 640);

Future<void> pumpNarrow(WidgetTester tester, Widget child) async {
  tester.view.physicalSize = narrowPhone * 2;
  tester.view.devicePixelRatio = 2;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(MaterialApp(
    home: MediaQuery(
      data: const MediaQueryData(size: narrowPhone, textScaler: TextScaler.linear(1.3)),
      child: Scaffold(body: SingleChildScrollView(padding: const EdgeInsets.all(12), child: child)),
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('a destination card in the Home carousel fits a long name and country', (tester) async {
    await pumpNarrow(
      tester,
      SizedBox(
        width: 160,
        height: 200,
        child: DestinationCard(
          destination: const Destination(id: 1, name: 'Nuwara Eliya Highlands', country: 'United Arab Emirates'),
          onTap: () {},
        ),
      ),
    );
    expect(tester.takeException(), isNull);
    expect(find.text('Nuwara Eliya Highlands'), findsOneWidget);
  });

  testWidgets('a hotel card fits many rooms, a rating and a large price', (tester) async {
    await pumpNarrow(
      tester,
      HotelCard(
        hotel: const Hotel(
          id: 1,
          name: 'Heritance Tea Factory',
          address: 'Kandapola',
          city: 'Nuwara Eliya',
          country: 'Sri Lanka',
          roomCount: 124,
          minPricePerNight: 125000,
          averageRating: 4.7,
          reviewCount: 1280,
        ),
        onTap: () {},
      ),
    );
    expect(tester.takeException(), isNull);
    expect(find.text('from LKR 125,000'), findsOneWidget);
  });

  testWidgets('a package card fits a long duration and a rating', (tester) async {
    await pumpNarrow(
      tester,
      PackageCard(
        package: const TravelPackage(
          id: 1,
          title: 'Cultural Triangle and East Coast Explorer',
          price: 250000,
          durationDays: 14,
          destinationId: 1,
          destinationName: 'Trincomalee',
          destinationCountry: 'Sri Lanka',
          activityCount: 9,
          averageRating: 4.8,
          reviewCount: 1024,
        ),
        onTap: () {},
      ),
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('the reviews header fits the rating summary', (tester) async {
    await pumpNarrow(tester, ReviewsSection(load: () async => const [], averageRating: 4.6, reviewCount: 1280));
    expect(tester.takeException(), isNull);
    expect(find.text('4.6 / 5 (1280 reviews)'), findsOneWidget);
  });
}
