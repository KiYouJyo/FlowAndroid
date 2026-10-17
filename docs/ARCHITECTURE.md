# Architecture

```text
WinUI 3 + Mica (zh-CN / ja-JP / en-US, live)
    |
    +-- Pixel device diagnostics --> AndroidDeviceService --> ADB/Fastboot (read-only)
    |
    +-- Official Google metadata --> GoogleFirmwareCatalog (fixed HTTPS hosts)
    |                                  --> GoogleFirmwareVerifier (SHA-256)
    |
    +-- OTA package --> OtaPackageInspector (metadata / post-timestamp)
    |                        --> PixelOtaPreflight
    |                        --> PixelOtaTransfer (single serial-bound ADB sideload)
    |
    +-- Factory image --> FactoryImageInspector (read only; writes disabled)
```

## Trust boundaries

- Invoke executables with `ProcessStartInfo.ArgumentList`; no shell interpolation or arbitrary command textbox.
- The untrusted ZIP is never executed. Factory archive scripts are not run or extracted.
- Published SHA-256 is fetched only from fixed HTTPS Google Developers catalog URLs; ZIP fingerprints alone are not proof of publisher authenticity.
- Device identity is captured during a single authorized ADB session; sideload requires the same serial in Recovery and a single connected device.
- OTA requires explicit acceptance of backup responsibility, Google terms, Recovery mode and a second modal confirmation.
- Compare target `post-timestamp` with current device `ro.build.date.utc`; missing or lower timestamps prohibit OTA.
- No automatic Recovery reboot, factory flashing, bootloader locking, wipe, silent downgrade, or arbitrary fastboot commands.
- Core logic has mock-runner unit tests and is independently testable without hardware.

## Unresolved

Google's live webpage structure may evolve, and parsing fails closed on changes. OTA acceptance tests on physical devices and interrupted-transfer recovery are outstanding. Model/SKU-specific anti-rollback verification beyond build timestamp is required for factory operations.
