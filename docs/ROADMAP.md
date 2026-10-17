# Roadmap — implementation vs. acceptance

## Foundation v0.1
- [x] Native WinUI 3/Mica UI with live zh-CN/ja-JP/en-US.
- [x] Device detection through official ADB/Fastboot.
- [x] OTA metadata, SHA-256 and fail-closed codename checks.
- [x] Core tests, Windows CI and localization-parity checking.
- [x] Unsigned diagnostic prerelease workflow and SHA256SUMS.
- [ ] Physical Windows launch and Pixel device acceptance.
- [ ] Public unsigned preview release activated (requires explicit manual workflow dispatch).

## Guided OTA v0.2
- [x] Google-published official SHA-256 catalog matching.
- [x] Serial-bound Recovery sideload engine, explicit user consent, timeout and failure reporting.
- [x] OTA build-timestamp downgrade prevention.
- [x] Operator instructions and transfer-versus-install-result distinction.
- [ ] Verify current Google HTML catalog parsing against actual live response in Windows.
- [ ] Test physical Pixel Recovery state changes / signature failures / disconnected USB.
- [ ] Post-reboot automated Android version verification and user feedback.
- [ ] Additional variant/carrier policy where the OTA origin requires it.

## Factory v0.3
- [x] Read-only outer ZIP inspection and official SHA-256 comparison.
- [ ] Model/SKU/bootloader anti-rollback and A/B slot compatibility matrix.
- [ ] Reviewed, explicit and recoverable fastboot plan (never blindly execute flash-all scripts).
- [ ] Validated unlock/wipe/relock flow with physical confirmations, if later permitted.
- [ ] Multi-model hardware acceptance and rescue playbooks.

## Distribution & expansions
- [ ] Dedicated product publisher identity and repository signing certificate.
- [ ] Signed MSIX and public cert with SHA256SUMS and trilingual notes.
- [ ] Microsoft Store onboarding, identity, policy review and real store submission.
- [ ] Community ROM adapters; require independent device policies and test coverage.
