# Architecture

```text
src/FlowAndroid.App   WinUI 3 shell, pages, live UI localization
         |
src/FlowAndroid.Core  Process boundary, parsing, firmware inspection, safety checks
         |
       Official Android SDK Platform-Tools (adb / fastboot)
         |
       USB-connected Pixel (read-only diagnostics)
```

## Design constraints

1. Process execution uses `ProcessStartInfo.ArgumentList` and never invokes a system shell or interpolated command string.
2. Read-only tool operations are explicitly allowlisted. A generic execute-command textbox is intentionally absent.
3. The UI never interprets a compatible OTA filename as evidence of trust. It reads Android OTA metadata and computes SHA-256; signature trust remains a future milestone.
4. Device serial and hardware codename are separate identifiers. Unknown or ambiguous targets must be rejected, not guessed.
5. Multiple connected devices fail closed until a serial-binding UX is implemented.
6. Package parsing is bounded and does not extract archive entries to disk.
7. GUI supports light/dark/high-contrast through WinUI resources; strings change without application restart.
8. `src/FlowAndroid.Core` is testable independently of Windows and USB hardware.

## Why no write operations in the first build?

Pixel variants have anti-rollback and partition-layout constraints. Treating a SHA-256 calculated from an arbitrary downloaded ZIP as a manufacturer's signature would be a security error. The initial preview intentionally stops at a validated plan, before executing sideload or fastboot flash.
