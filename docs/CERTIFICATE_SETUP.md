# FlowAndroid MSIX signing configuration

Configure only **FlowAndroid's own** certificate and package identity. Do not reuse unrelated product signing identities without intentionally choosing to share the Windows package publisher identity.

1. Ensure an appropriate code-signing certificate with a private key, valid period and trust chain is available. The certificate's subject is the package Publisher. For public distribution, the publisher must be trusted by Windows; self-signed test certificates are **not** equivalent to public trust.
2. Export the certificate securely as a password-protected PFX locally. **Never commit the PFX, password or encoded value**.
3. In GitHub repository **Settings → Secrets and variables → Actions**, configure repository secrets:
   - `FLOWANDROID_PFX_BASE64`: Base64 of the password-protected PFX.
   - `FLOWANDROID_PFX_PASSWORD`: PFX export password.
4. Configure repository variable `FLOWANDROID_PACKAGE_ID` with a stable MSIX identity, e.g. `KiYouJyo.FlowAndroid`. Later, Partner Center may assign a specific identity; use the Partner Center identity for Store packaging and do not assume it matches a custom sideload publisher.
5. Generate Base64 locally in PowerShell, **do not paste it into a chat**:

```powershell
$pfx = [System.IO.File]::ReadAllBytes("C:\secure\FlowAndroid.pfx")
[Convert]::ToBase64String($pfx) | Set-Clipboard
```

6. Open **Actions → MSIX build and signing → Run workflow**. Initially set `sign_package=true`, `publish_candidate=false`, `hardware_accepted=false`. Inspect CI artifacts and signing verification.
7. Signed GitHub release candidates require a passing real-device checklist, `sign_package=true`, `publish_candidate=true`, and `hardware_accepted=true`. No automated release occurs on normal pushes.

## Microsoft Store

Microsoft Store signs submitted MSIX after certification, so a separate PFX is not required for Store distribution. See [STORE_SUBMISSION](STORE_SUBMISSION.md) for the unsigned Store upload workflow. A Microsoft Store submission requires Partner Center onboarding, app reservation, exact Store Identity and publisher, independent policy review and approval. The signed GitHub channel and Store channel cannot be assumed interchangeable. After Partner Center information is available, configure a separate Store upload workflow using the assigned identity. Single-project MSIX produces a single .msix, **not** an MSIXBundle. See https://learn.microsoft.com/windows/apps/package-and-deploy/ci-for-winui3 and https://learn.microsoft.com/windows/apps/windows-app-sdk/single-project-msix.
