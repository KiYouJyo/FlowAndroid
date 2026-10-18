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

## Beginner experience (October 2026)
- [x] Guided UI steps: diagnose USB authorization and Pixel identity; detect insufficient charge; show beginner-friendly solutions.
- [x] Official OTA production candidate selection with strict codename matching and ambiguous regional variants blocked.
- [x] Google checksum-verified, cancellable download to an app-local user cache and local ZIP alternative.
- [x] One explicitly confirmed, serial-bound ADB Recovery reboot with additional preflight gates.
- [x] Seven-screen offline interactive tutorial without enabling any device writes.
- [x] Supervised Recovery Sideload, safe progress indicator, exact post-reboot build fingerprint verification.
- [x] Collapse risky factory-image read-only inspection into advanced options.
- [x] Unit-tested no-device, multi-device, mismatch, tampered download, ambiguous firmware and Recovery interception scenarios.
- [ ] Manual UI walkthrough on a clean Windows installation.
- [ ] Real Pixel hardware USB/Recovery/Sideload tests; carrier/SKU-specific compatibility validation.

## App self-update
- [x] About page native WinUI 3 GitHub/Store update controls and trilingual status/release notes.
- [x] GitHub latest stable release check, version-aware signed MSIX asset selection and SHA256SUMS verification.
- [x] Windows WinVerifyTrust signed-package validation, same MSIX name/publisher and strictly newer package-version checks.
- [x] Restart Manager + ForceApplicationShutdown package update handoff; block app replacement during Pixel OTA transfer.
- [x] Compile-time dedicated Microsoft Store channel using StoreContext download/install APIs.
- [x] Manual, physical-acceptance-gated stable publishing path with semantic versioned MSIX assets.
- [x] Offline core unit tests for malformed/old/prerelease/malicious release and bad checksums.
- [ ] Test in-place update from one signed MSIX to another on a real Windows desktop.
- [ ] Submit/approve the Microsoft Store build in Partner Center and exercise live Store updates.
