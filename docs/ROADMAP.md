# Roadmap

## v0.1.0 — safe foundation (in progress)
- [x] Eikura-like repository structure and three-language documentation.
- [x] WinUI 3 native shell / Mica / NavigationView.
- [x] Live zh-CN / ja-JP / en-US language switching.
- [x] Read-only ADB / Fastboot discovery and device inspection.
- [x] Local OTA zip metadata check + SHA-256.
- [x] Fail-closed Pixel compatibility preflight.
- [x] Pure core tests and Windows CI pipeline.
- [ ] Real-device UX smoke test; release an explicitly labeled diagnostic preview.

## v0.2.0 — guided official OTA
- [ ] Verify official payload authenticity against trusted provenance / published digests.
- [ ] Dedicated one-device sideload state machine, cancellation rules, disconnect handling and deterministic logs.
- [ ] Explicit data backup / safety gate and supervised real-device acceptance tests.
- [ ] Recovery navigation with human confirmation and post-install validation.

## v0.3.0 — official factory images
- [ ] Machine-readable signed device catalog and rollback protection.
- [ ] Model-specific flashing plan and multiple independent confirmation gates.
- [ ] Recovery playbook, A/B slot coverage, anti-rollback matrix.

## Later
- [ ] ROM adapters with independently maintained safety rules.
- [ ] GitHub release signed MSIX, certificate, SHA256SUMS and multilingual release notes.
- [ ] Microsoft Store package channel (separate signing/identity).
