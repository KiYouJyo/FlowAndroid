<p align="center"><img src="docs/assets/logo.svg" width="112" height="112" alt="FlowAndroid icon"></p>
<h1 align="center">FlowAndroid</h1>
<p align="center">A native Windows Android device manager, starting with Google Pixel.</p>
<p align="center">
  <a href="https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml"><img src="https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI"></a>
  <img src="https://img.shields.io/badge/Windows-WinUI%203-0078D4" alt="WinUI 3">
  <img src="https://img.shields.io/badge/Version-0.1.0_preview-64748b" alt="Preview">
</p>
<p align="center"><a href="README.md">简体中文</a> | <a href="README.ja-JP.md">日本語</a> | English</p>

## Beginner-friendly Pixel update wizard

The app now offers a guided path for one authorized Pixel: USB diagnostics, official production full OTA suggestions, checksum-verified download, explicit safe Recovery reboot, supervised Sideload and post-reboot build verification. Multiple regional builds are never guessed. A seven-step device-free tutorial allows previewing the process without creating a fake device or enabling writes. Factory Image is read-only.

See [Beginner guide](docs/BEGINNER_GUIDE.md) and [simulation acceptance matrix](docs/WIZARD_ACCEPTANCE.md). Physical Pixel tests are still required.

## Status

**v0.1.0 is an unreleased developer preview.** Read-only diagnostics, Google checksum verification and a supervised OTA sideload prototype are implemented. OTA transfer DOES change system software; this path has NOT passed real Pixel acceptance and must not be treated as production-ready. Factory partition flashing, lock/unlock and wiping remain disabled.

- Native WinUI 3 / Mica Windows UI with live Chinese / Japanese / English switching.
- ADB / Fastboot detection using official Android SDK Platform-Tools; Pixel model, version, and codename inspection.
- Local full OTA ZIP metadata inspection and SHA-256 fingerprinting.
- Fail-closed handling of multiple / unauthorized / unsupported devices and incompatible OTA codenames.
- Official Google Developers HTTPS checksum matching for local OTA and factory ZIP files.
- Supervised, explicit-consent OTA transfer with revalidated fingerprint, build timestamp and original device serial; Recovery-side installation success must be checked manually.
- Factory images are inspected without executing scripts or writing partitions.
- Session-local diagnostic logs. No telemetry or device-data uploads.

A hash computed locally is **not** a publisher signature. The OTA prototype compares it to an official published Google digest, and device Recovery still validates package signatures. See [OTA instructions](docs/OTA_WORKFLOW.md).

## Development

Windows 10 2004+ or Windows 11, .NET 10 SDK, Visual Studio WinUI / Windows App SDK tools. Install [Android SDK Platform-Tools](https://developer.android.com/tools/releases/platform-tools) into PATH or an app-local `platform-tools` directory.

```powershell
dotnet restore FlowAndroid.slnx
dotnet test tests/FlowAndroid.Core.Tests/FlowAndroid.Core.Tests.csproj -c Release
dotnet build src/FlowAndroid.App/FlowAndroid.App.csproj -c Release -p:Platform=x64
```

Run the generated executable with the matching Windows App SDK Runtime installed. No signed installer, Microsoft Store listing or production Release is published yet. CI build artifacts, once CI succeeds, are for development only: [Actions](https://github.com/KiYouJyo/FlowAndroid/actions/workflows/ci.yml).

## Structure and roadmap

`src/FlowAndroid.App` provides UI, `src/FlowAndroid.Core` provides detection / inspection / safety logic, and `tests` provides pure unit tests. See [architecture](docs/ARCHITECTURE.md), [roadmap](docs/ROADMAP.md), [safety policy](docs/FLASH_SAFETY.md), and [release notes](docs/RELEASE.md).

The v0.2 checksum-verified OTA workflow is implemented but needs real-device acceptance and failure-mode testing. Factory ZIP inspection exists, while destructive fastboot operations remain blocked. An opt-in unsigned GitHub diagnostic prerelease workflow is available; signed MSIX and Store shipping require certificate and identity provisioning.

Local-first privacy. Redact serials and paths before sharing logs. Read [PRIVACY](PRIVACY.md), [SECURITY](SECURITY.md), [CONTRIBUTING](CONTRIBUTING.md). [MIT](LICENSE). © 2026 KiYouJyo.
