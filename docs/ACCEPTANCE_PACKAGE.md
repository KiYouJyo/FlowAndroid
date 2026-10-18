# FlowAndroid — signed hardware acceptance package

The GitHub Actions [MSIX build and signing](../.github/workflows/msix-package.yml) workflow produces a downloadable artifact named **FlowAndroid-MSIX-signed-acceptance** after running with the repository signing certificate. No GitHub Release or Store publication occurs automatically.

## Contents

- `FlowAndroid-0.1.0-x64.msix` — signed Windows x64 preview build
- `FlowAndroid-public.cer` — publisher certificate (public certificate only, no private key)
- `SHA256SUMS.txt` — MSIX SHA-256 for corruption checking
- `SIGNATURE-STATUS.txt` — signer fingerprint and trust status on GitHub's runner
- `Install-Acceptance.ps1` — **verify-only by default**, with explicit installation and certificate trust opt-in
- `MANUAL_ACCEPTANCE.md` and `ACCEPTANCE_RECORD_TEMPLATE.md`
- zh-CN, ja-JP and en-US release notes

## Windows verification

Extract the GitHub Actions artifact ZIP, open PowerShell in the folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Acceptance.ps1
```

The above command **does not install anything**. It checks SHA-256 and signer thumbprint, and shows the certificate trust status.

For an already trusted certificate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Acceptance.ps1 -Install
```

Only on a disposable test PC, after comparing the fingerprint and intentionally accepting a non-public test publisher:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Acceptance.ps1 -Install -TrustPublisher
```

The last option explicitly prompts for `TRUST` before adding the certificate to CurrentUser\TrustedPeople. It never imports to TrustedRoot. Windows still validates the MSIX when installed. If a public certificate chain is absent, this build remains **test-only**. Install Microsoft's official Windows App SDK Runtime separately if Windows reports a missing framework dependency.

**This is not a production-certified Pixel flasher.** Actual factory partition writes are disabled, and any OTA testing must use a backed-up expendable Pixel. Record findings in the included acceptance template.
