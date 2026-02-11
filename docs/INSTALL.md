# n01d Suite Native Installation

## From Source
```bash
git clone https://github.com/bad-antics/n01d-suite-native
cd n01d-suite-native
./gradlew assembleRelease
adb install app/build/outputs/apk/release/app-release.apk
```

## Requirements
- Android 10+ (API 29+)
- ARM64 or x86_64
- ~50MB storage
