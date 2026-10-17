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
