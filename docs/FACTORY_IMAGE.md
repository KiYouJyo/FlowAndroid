# Factory Image: read-only inspection

FlowAndroid can read the outer factory-image ZIP, validate the archive structure, identify the codename, and compare SHA-256 with Google's HTTPS checksum catalog at https://developers.google.com/android/images.

**Fastboot partition writes, bootloader unlock/relock, erase, wipe, and downloaded flash-all script execution are disabled.** Google's factory image warning says factory installs erase user data. Pixel 6/8 (May 2025) and Pixel 10 (May 2026) families have specific anti-rollback/slot caveats. Never apply a generic flashing sequence without a verified per-SKU/firmware rollback matrix.

Activation requires hardware-backed acceptance tests for each Pixel family, USB interruption recovery, Google checksum verification, measured bootloader state and physical confirmation.
