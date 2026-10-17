# Pixel safety policy

## Approved actions in v0.1
- `adb version`, `adb devices -l`, `adb -s <serial> shell getprop ro.product.device`, `adb -s <serial> shell getprop ro.build.version.release`
- `fastboot --version`, `fastboot devices -l`, `fastboot -s <serial> getvar product`
- Local ZIP metadata inspection and SHA-256 calculation.
All user-supplied serial values are isolated in `ArgumentList` (no shell interpretation).

## Prohibited until dedicated review
- `fastboot flashing unlock`, `fastboot flashing lock`, `fastboot flash`, `fastboot erase`, `fastboot -w`
- `adb sideload`, `adb reboot recovery`, `adb shell` with arbitrary commands.
- Arbitrary image / partition choice, bootloader downgrade, unverifiable ROM packages.

## Preconditions for later write-enabled release
1. Device identity recorded and revalidated before write; no ambiguous multiple-device selection.
2. Explicit validated firmware compatibility including codename, variant and anti-rollback policy.
3. Verified source authenticity, not merely self-computed checksum.
4. Backup warnings, explicit user affirmation and sufficient battery/USB reliability checks.
5. Real Pixel hardware test plan including interruption recovery; simulate failure paths before production.
6. No silent lock/unlock operations; physical confirmation stays on device.
