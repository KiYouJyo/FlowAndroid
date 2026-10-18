# Release / distribution

**No signed, production, or Store release exists.** FlowAndroid has an unsigned Windows x64 CI diagnostic artifact. OTA Sideload code is experimental and must not be treated as physical-device-certified.

## CI builds

Windows + .NET 10 SDK + WinUI/Windows App SDK support:

```powershell
dotnet test tests/FlowAndroid.Core.Tests/FlowAndroid.Core.Tests.csproj -c Release
dotnet build src/FlowAndroid.App/FlowAndroid.App.csproj -c Release -p:Platform=x64
```

The CI job checks localization parity and uploads a Windows binary artifact requiring Windows App SDK Runtime and Google SDK Platform-Tools.

## Explicit unsigned diagnostic prerelease

Run **Actions → Unsigned diagnostic prerelease → Run workflow**. This creates an artifact including:
- `FlowAndroid-unsigned-diagnostic-x64.zip`
- `SHA256SUMS.txt`
- `README.txt`

Optional `publish_prerelease=true` publishes a clearly marked **unsigned, experimental GitHub prerelease**. Never represent this zip as a signed installer.

## Signed MSIX and Microsoft Store blockers

A production MSIX needs app identity and a **repository-specific** trusted signing certificate (plus signing secrets and policies). Microsoft Store publication separately needs a Partner Center product identity and submission/approval. Neither was supplied. Do not copy certificates or store identities from unrelated apps, and do not claim Store release or real Pixel acceptance until verified.

## FlowAndroid-specific MSIX pipeline

The single-project MSIX CI workflow is [msix-package.yml](../.github/workflows/msix-package.yml). A normal push builds an **unsigned test-only MSIX** with SHA256SUMS and three-language notes. For signature setup, read [CERTIFICATE_SETUP.md](CERTIFICATE_SETUP.md); a signed candidate requires the specific PFX secrets and package identity. A signed GitHub candidate cannot be published without manually asserting hardware acceptance. Microsoft Store submission has its own Partner Center identity and review and is not automated without access.

### Versioning

Both GitHub MSIX candidates and Store upload candidates replace the fourth field of the package version with their workflow run number (starting from 0.1.0.0), to avoid same-version update collisions. The version-stamping helper validates the MSIX 16-bit field limit. The GitHub and Store channels must retain intentionally distinct package identities and version sequences when configured.

### Native update distribution

In-app updating requires a genuine **stable** signed GitHub Release (not an Actions artifact or release candidate). The signed MSIX name must be `FlowAndroid-<major>.<minor>.<patch>-x64.msix` and accompanied by `SHA256SUMS.txt`. GitHub publication is gated by manual `publish_stable=true`, `sign_package=true`, `hardware_accepted=true` and a three-part `release_version`; the workflow never silently overwrites a release. A separate compile-flagged Store version uses native `StoreContext` for in-app update. Read [IN_APP_UPDATES.md](IN_APP_UPDATES.md).
