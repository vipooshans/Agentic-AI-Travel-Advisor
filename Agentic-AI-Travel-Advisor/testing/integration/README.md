# End-to-end tests against the real system

All suites here run against the real ASP.NET Core API and its PostgreSQL database. Nothing is mocked. Results are kept in `testing/execution-results/phase8-tests/`.

## Prerequisites

1. PostgreSQL with the `travel_advisor` database, and the API's connection string in user secrets or `ConnectionStrings__DefaultConnection` (see the root README).
2. Start the API in Development, so the demo accounts are seeded. Raise the auth rate limit, because the suites sign in many times:

   ```powershell
   $env:RateLimiting__AuthPermitsPerMinute = '300'
   dotnet run --project web-api --urls http://localhost:5080 --environment Development
   ```

The demo accounts exist only in Development. To test another deployment, override them with `E2E_<ROLE>_EMAIL` / `E2E_<ROLE>_PASSWORD` (Playwright) or `IT_OWNER_*` / `IT_ADMIN_*` dart-defines (Flutter).

## Full journey with database verification (Playwright)

`web-react/e2e/ai-journey.spec.ts` covers the whole journey: UI → API → AI → PostgreSQL → recommendations → itinerary → booking proposal → booking → provider confirmation. After each step it reads the database directly, over a read-only session (`web-react/e2e/db.ts`). The connection string is taken from the environment and never written to disk:

```powershell
# from web-react/, reusing the API's user secret for this shell only
$line = dotnet user-secrets list --project ..\web-api\TravelAdvisor.Api.csproj | Where-Object { $_ -like 'ConnectionStrings:DefaultConnection*' }
$env:E2E_DB_CONNECTION = ($line -split ' = ', 2)[1]
npm run e2e                       # all 17 browser tests
npm run e2e -- e2e/ai-journey.spec.ts
```

If `E2E_DB_CONNECTION` (or `ConnectionStrings__DefaultConnection`) is not set, the journey spec is reported as skipped with that reason. The other specs still run.

## Mobile app journey (Flutter integration_test)

`mobile-app/integration_test/traveler_journey_test.dart` runs the real app in Chrome. A traveler registers, books a room, and plans, saves and confirms a trip with the assistant; the owner's confirmation then appears in the app. Each step is checked through the API.

```powershell
chromedriver --port=4444 --verbose --log-path=$env:TEMP\chromedriver.log   # matching Chrome version
cd mobile-app
flutter drive --driver=test_driver/integration_test.dart --target=integration_test/traveler_journey_test.dart `
  -d web-server --browser-name=chrome --profile --web-port 4173 --dart-define=API_BASE_URL=http://localhost:5080
```

Port 4173 is one of the API's allowed CORS origins. Profile builds strip failure details from the driver output; they appear in the browser console section of the verbose chromedriver log.
