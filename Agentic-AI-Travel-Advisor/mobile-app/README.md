# Travel Advisor — Flutter Mobile Application

The **Agentic AI Travel Advisor** mobile app delivers an intuitive, high-performance travel discovery and booking experience backed by ASP.NET Core Web API and PostgreSQL.

## Architecture & Design Patterns

- **Architecture**: MVVM with reactive Provider state management.
- **Routing**: Declarative deep-linking navigation powered by `go_router`.
- **UI Framework**: Material 3 theming (`AppTheme`) with glassmorphism card elevation, animated skeleton loaders, and responsive layouts.
- **Secure Storage**: Sensitive auth JWT tokens and cached user preferences persisted via `flutter_secure_storage`.
- **Formatting Utilities**: Centralized currency, date duration, and localized display via `TravelFormatters`.

## Key Mobile Features

1. **Catalog Exploration**:
   - Real-time search query filtering and category chips (Cultural, Nature, Beaches, Hill Country).
   - Shimmer skeleton placeholders during network fetch.
   - Comprehensive hotel, room, and travel package detail views.
2. **Agentic AI Chat Assistant**:
   - Multi-agent conversational planning extracting travel requirements.
   - One-click quick prompt suggestions.
   - Catalog-backed day-by-day suggested itineraries with estimated cost breakdown.
   - Save directly to backend itineraries.
3. **Booking Management**:
   - Interactive date range selection and live cost estimator.
   - Status filtering tabs: All, Pending, Confirmed, Completed, Cancelled.
   - Cancellation confirmation dialog with immediate status reflection.
4. **Itinerary Sharing & Export**:
   - Preview formatted travel schedule with Markdown clipboard export.

## Running Tests

Execute the automated widget and unit test suite:

```bash
cd mobile-app
flutter test
```

## Running the App

```bash
cd mobile-app
flutter pub get
flutter run
```
