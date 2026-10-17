# Hardware acceptance checklist / 实机验收 / 実機受け入れ

The first public signed release must **not** be published until all items pass on real hardware. Use a spare Pixel, never a primary phone. Record test date, Pixel marketing model, codename, SKU/carrier, Android build, battery, Windows version, Google Platform-Tools version and package SHA-256. Redact personal serials from published reports.

## Windows / UI (must pass on clean Windows 10 and Windows 11 x64)
- [ ] Run unsigned diagnostic zip only as an internal test. Validate Windows App SDK Runtime dependency behavior.
- [ ] Install a trusted, correctly signed MSIX with matching package identity; verify publisher details and uninstaller.
- [ ] Test normal startup, reopen, light/dark/high contrast, 100%/125%/150%/200% DPI.
- [ ] Switch Chinese/Japanese/English without restart, revisit all tabs, inspect overflows.
- [ ] Test PATH-provided adb/fastboot; and official SDK Platform-Tools placed beside app.
- [ ] Verify logs are not uploaded and are redacted before sharing.

## USB/device state matrix
- [ ] Unplugged -> no device; disabled USB debugging -> no authorized device.
- [ ] One authorized Pixel -> exact model, codename, Android version and battery shown.
- [ ] Unauthorized/offline -> no write path; two devices -> no write path.
- [ ] One Pixel in Fastboot -> read-only, model label only; not mistaken for ADB.
- [ ] Unplug USB during scan and during checksum verification -> graceful failure.
- [ ] Switch handset after catalog verification -> transfer must be blocked.

## Full OTA / Recovery, use a spare Pixel only
- [ ] Obtain correct recent Google full OTA from the official page, accept terms, verify SHA-256.
- [ ] Reject mismatched codename, corrupted ZIP, missing build metadata, lower post-timestamp.
- [ ] Reject battery < 50%, unknown battery, absent backup/terms/Recovery acknowledgements.
- [ ] Enter Recovery manually; verify serial is unchanged, exactly one device is in sideload mode.
- [ ] Confirm transfer and observe on handset; ADB exit status must not be misreported as installation success.
- [ ] Test signature failure and Recovery screen error; ensure no automatic reboot/retry.
- [ ] Simulate/observe USB disconnect on a **noncritical** test device and document recovery.
- [ ] After manual reboot, rescan and verify installed build/system status.
- [ ] For affected Pixel 6/8/10 models, verify active/inactive A/B slot and Google's official rollback warnings before enabling model-specific support.

## Factory Image (read-only acceptance)
- [ ] Verify official factory ZIP parses, checksum matches official Google page.
- [ ] Reject malicious archive paths and unofficial hashes.
- [ ] Confirm no Fastboot write/erase/unlock/relock command is invoked or exposed.
- [ ] Do not classify factory flashing as accepted; separate per-model high-risk validation is required if enabled later.

## Release acceptance record

Fill in `docs/ACCEPTANCE_RECORD_TEMPLATE.md` and attach logs/screenshots as **private** artifacts without serials. Sign off the report manually. Only then set `hardware_accepted=true` in the signed candidate release workflow.
