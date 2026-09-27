# Android platform notes

The `mobile-app/android/` folder is checked in with:

- `applicationId` / namespace: `com.traveladvisor.app`
- `minSdk` 21
- Cleartext HTTP enabled (`usesCleartextTraffic` + `network_security_config.xml`) for classroom demos against `http://LAN:5000` or ngrok HTTP

## First-time setup (if regenerating)

```powershell
# from Agentic-AI-Travel-Advisor
powershell -File scripts/setup-android.ps1
```

Or:

```bash
cd mobile-app
flutter create . --platforms=android --org com.traveladvisor --project-name travel_advisor
# then re-apply cleartext flags / copy network_security_config.xml from this overlay
```

## Build the APK

```bash
cd mobile-app
flutter pub get
flutter build apk --release --dart-define=API_BASE_URL=http://YOUR_LAN_IP:5000
```

Emulator default (no dart-define): `http://10.0.2.2:5000`

APK output: `build/app/outputs/flutter-apk/app-release.apk`
