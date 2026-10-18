# Supervised Pixel OTA workflow

Implementation exists but has **not** passed physical-device acceptance.

1. Back up data, use one authorized Google Pixel and official Android SDK Platform-Tools.
2. Download the correct full OTA ZIP from https://developers.google.com/android/ota after reading Google's license terms.
3. Select it in FlowAndroid, compare the local SHA-256 with the published Google SHA-256 through the **Verify Google SHA-256 online** control.
4. Confirm no pending OTA, stable cable and compatible release. The app requires a readable battery level of at least 50%, and forbids downgrades.
5. In the app, select **Reboot into Recovery** (explicit confirmation, only after device/firmware checks), or enter Recovery manually. If the red exclamation mark appears, hold Power and tap Volume Up once to open the menu. Choose **Apply update from ADB**.
6. Confirm backup, license terms and Recovery mode in the app; review a second modal confirmation.
7. The app blocks unknown/older OTA build timestamps, re-hashes the ZIP, confirms one originally-bound serial in `sideload`, then sends `adb -s SERIAL sideload ZIP`. It never executes arbitrary downloaded scripts.
8. Check the phone's Recovery screen for installation success. Select **Reboot system now** on the phone manually, then verify Android version.

**Limitations:** Google webpage layout changes fail closed; a matching hash is not a blanket guarantee of device rollback safety. Some devices may change serial in Recovery and are denied until binding is supported. The UI cannot guarantee a backup exists. It does not auto-downgrade or relock bootloaders. Transfer completion is not installation completion.

Google’s official Chinese mirror at https://developers.google.cn/android/ota lists Pixel download links using googledownloads.cn, so the app accepts exactly those two fixed download hosts (dl.google.com and googledownloads.cn) after parsing the fixed Google Developers HTTPS catalog pages. Never trust user-supplied domains.
