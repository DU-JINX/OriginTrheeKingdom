# Unity 4 Android Port Migration Log

## Current Direction

- Source zip is preserved read-only.
- Port project root: `/Users/xix/OriginalUnity4AndroidPort`.
- Target Unity version: `6000.5.1f1`.
- Target Android artifact: debug installable APK.

## Completed

- Extracted old Unity 4 project while stripping the invalid mojibake zip prefix.
- Added Unity 6000 `Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt`.
- Moved old Editor extensions out of `Assets` into `MigrationDisabled/LegacyEditorAssets`.
- Moved NGUI sample project out of `Assets` into `MigrationDisabled/NGUI/Examples`.
- Migrated core Unity 4 shortcut APIs used by the runtime and NGUI.
- Patched `Assets/ex2D/Core/ex2D.Runtime.dll` to replace removed Unity 4 component shortcut calls.
- Converted `Assets/Scripts/Android/Resolution.js` to `Resolution.cs` while preserving the original `.meta` GUID.
- Added repeatable Android build entrypoint: `OriginalPortAndroidBuild.BuildAndroidDebugApk`.
- Added repeatable scene smoke test entrypoint: `OriginalPortSceneSmokeTest.RunSceneOpenSmokeTest`.
- Rebuilt Android APK with IL2CPP and both `arm64-v8a` and `armeabi-v7a` native libraries.
- Removed the Android development/debuggable build flag from the installable debug-signed APK.
- Unity scene smoke test passed for all main scenes with `missingScripts=0`:
  - `StartScene`
  - `ContinueGame`
  - `HowToPlay`
  - `SelectKing`
  - `InternalAffairs`
  - `Strategy`
  - `SelectGeneralToWar`
  - `WarScene`
  - `GameVictory`
  - `GameOver`
- Built debug APK successfully:
  - `/Users/xix/OriginalUnity4AndroidPort/Builds/Android/ThreeKingdomsOriginalPort-debug.apk`
  - Package: `com.xix.threekingdoms.originalport`
  - Label: `三国群英传原版迁移`
  - ABI: `arm64-v8a`, `armeabi-v7a`
  - SDK: min `26`, target `36`
  - Signature: Android debug v2 signature verified.

## Known Downgrades

- Old ex2D/NGUI Editor tools are disabled.
- NGUI example scenes/scripts are disabled.
- The first Android target is a debug APK, not a release-signed AAB.
- On-device install and interactive play flow are not verified yet because no Android device is connected.
- Playability still needs manual smoke testing from `StartScene` into `Strategy` on a real device or emulator.
