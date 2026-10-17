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
- [x] Live Google OTA/Factory catalog integration smoke test in GitHub Actions (official CN mirror).
- [ ] Confirm parser behavior on the user’s Windows machine/network.
- [ ] Test physical Pixel Recovery state changes / signature failures / disconnected USB.
- [x] Post-reboot build fingerprint comparison against the expected OTA build (code/tests).
- [ ] Physical Pixel post-reboot verification with real firmware.
- [ ] Additional variant/carrier policy where the OTA origin requires it.

## Factory v0.3
- [x] Read-only outer ZIP inspection and official SHA-256 comparison.
- [ ] Model/SKU/bootloader anti-rollback and A/B slot compatibility matrix.
- [ ] Reviewed, explicit and recoverable fastboot plan (never blindly execute flash-all scripts).
- [ ] Validated unlock/wipe/relock flow with physical confirmations, if later permitted.
- [ ] Multi-model hardware acceptance and rescue playbooks.

## Distribution & expansions
- [x] Parameterized unsigned MSIX and gated signed candidate workflow (unsigned build green).
- [ ] Configure dedicated product publisher identity and signing certificate in GitHub.
- [x] SignTool/public-cert-export/SHA256SUMS/trilingual-release workflow prepared.
- [ ] Real signed MSIX verification once certificate is configured.
- [ ] Microsoft Store onboarding, identity, policy review and real store submission.
- [ ] Community ROM adapters; require independent device policies and test coverage.
