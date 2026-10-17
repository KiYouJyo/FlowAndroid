# Security policy

FlowAndroid can modify an Android device. Do not upload real serial numbers, credentials, private firmware or signing material to public GitHub issues. Report vulnerabilities privately through repository security advisories (if enabled).

## Operational boundaries

- The supervised OTA prototype runs only a single serial-bound `adb sideload` after Google checksum matching, device identity checks and multiple on-screen confirmations.
- OTA transfer changes device software; **no physical Pixel acceptance has been performed**. Do not use this development preview on important devices.
- Google checksum validation is independent of, and does not bypass, package signature checks performed by Android Recovery.
- Unknown hardware/build timestamps, multiple devices, untrusted firmware sources, checksum failures, unsupported transports and downgrade attempts fail closed.
- No generic command shell, unsigned ROM flashing, factory script execution, fastboot partition writes, wiping, bootloader locking/unlocking or silent reboot is exposed.
- ADB transfer completion does not establish installation success. Check Recovery on the phone; do not retry blindly.
- Local log information may contain device characteristics. Review and redact before sharing.
