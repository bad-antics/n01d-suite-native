# Getting Started

## Build Requirements
- Android Studio Hedgehog or newer
- JDK 17+
- Android SDK 34+
- Kotlin 1.9+

## Build
```bash
git clone https://github.com/bad-antics/n01d-suite-native
cd n01d-suite-native
./gradlew assembleDebug
```

## Install on Device
```bash
adb install app/build/outputs/apk/debug/app-debug.apk
```

## Permissions
The app requires:
- `ACCESS_FINE_LOCATION` — WiFi scanning
- `NEARBY_WIFI_DEVICES` — WiFi enumeration (Android 13+)
- `BLUETOOTH_SCAN` — Bluetooth discovery
- `INTERNET` — Network operations
- `ACCESS_NETWORK_STATE` — Network info
