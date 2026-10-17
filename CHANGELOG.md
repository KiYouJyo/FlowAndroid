# Changelog

## [Unreleased] — v0.1.0 development preview

### Added
- WinUI 3 native Mica shell; live simplified Chinese, Japanese and English.
- Read-only ADB/Fastboot discovery and Google Pixel device identification.
- OTA ZIP metadata inspection, SHA-256 and build timestamp extraction.
- Official Google HTTPS catalog reader and checksum verification, with fail-closed Google-host fallback.
- Guarded **ADB OTA sideload prototype** with physical Recovery step, backup/licensing confirmations, revalidated serial, firmware hash, timestamp anti-downgrade and post-transfer instructions.
- Read-only Factory Image ZIP inspection; archive entries and official Google SHA-256.
- Core unit tests, Windows CI, three-language contract validation.
- Opt-in **unsigned diagnostic** prerelease workflow with zip + SHA256SUMS.

### Safety
- OTA is a device write and has **not been tested on physical Pixels**; no production readiness claim.
- Never execute factory flash-all scripts, unlock/relock, erase, wipe, or partition flash commands.
- Treat network/catalog failures, ambiguous targets, unknown build timestamps and wrong serials as hard blocks.
