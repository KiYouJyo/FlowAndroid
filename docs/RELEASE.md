# Release procedure

Status: **no production release exists**. This is a diagnostic preview, not a flashing utility yet.

Build on Windows with Visual Studio 2022/2026 desktop development workload and .NET 10 SDK:

```powershell
dotnet restore FlowAndroid.slnx
dotnet test tests/FlowAndroid.Core.Tests/FlowAndroid.Core.Tests.csproj -c Release
dotnet build src/FlowAndroid.App/FlowAndroid.App.csproj -c Release -p:Platform=x64
```

GitHub Actions builds the Windows unpackaged preview and uploads it as a CI artifact. It depends on the Windows App SDK runtime and Google Android SDK Platform-Tools provided by the user; binaries are not vendored.

A **signed MSIX** release pipeline is a separate milestone. For the future release path, provision a repository-specific signing certificate and secret, validate package identity and publisher, and publish signed package + public certificate + SHA256SUMS + three-language release notes. Never copy another app's signing certificate into this repository and never publish a self-signed production package without clear trust instructions.
