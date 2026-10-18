# Pixel safety policy

## Read-only commands
- `adb version`, `adb devices -l`, `adb -s SERIAL shell getprop ro.product.device` (and other fixed system properties, including `ro.build.date.utc`)
- `fastboot --version`, `fastboot devices -l`, `fastboot -s SERIAL getvar product`
- Local OTA/Factory ZIP metadata inspection and SHA-256; official Google HTTPS checksum-list retrieval.

## Single guarded OTA command
- `adb -s SERIAL sideload VERIFIED_OFFICIAL_FULL_OTA.zip` only after checksum matching, Pixel codename and build-timestamp checks, single-device binding, backups, license and manual Recovery confirmation.
- This is **experimental**, not real-device certified. No other write command is available.
- The app does not treat ADB process exit code 0 as proof that Recovery finished installation successfully.

## Explicitly prohibited
- `fastboot flashing unlock`, `fastboot flashing lock`, `fastboot flash`, `fastboot erase`, `fastboot -w`.
- Arbitrary `adb reboot` or arbitrary `adb shell`, downloaded `flash-all.bat` or user-supplied shell commands. The only permitted Recovery transition is **explicitly confirmed** `adb -s SERIAL reboot recovery` after official OTA hash, backup, battery and one-device identity checks.
- Factory image updates, downgrades, unofficial ROM installation, arbitrary image/partition choices or silent reboot.

## Before a production release
- Real Pixel test matrix with physical confirmation, sufficiently charged device, USB disconnection recovery and logs.
- Per-SKU image compatibility, bootloader anti-rollback and dual-slot policy.
- Google SHA-256 provenance parser acceptance against current public pages.
- Signed releases with verifiable publisher identity and diagnostics sanitization.
