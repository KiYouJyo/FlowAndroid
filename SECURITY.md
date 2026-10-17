# Security policy

FlowAndroid controls potentially destructive device maintenance workflows. Do not submit real device serials, private firmware, credentials or signing keys in public issues.

Report vulnerabilities privately via GitHub's security advisory reporting (if enabled), or contact the repository owner privately.

## Current boundaries

- No raw shell, adb shell, arbitrary fastboot flash, wipe, sideload, unlock, or relock controls.
- Firmware metadata and its locally computed SHA-256 do **not** establish trusted origin or signature authenticity.
- Unrecognized devices, multiple devices, unavailable tools, and mismatched firmware fail closed.
- Never retry a failed firmware operation blindly, unplug during active writes, or relock a device with unofficial images.
