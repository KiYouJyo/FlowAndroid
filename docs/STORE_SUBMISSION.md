# Microsoft Store (separate from signed GitHub release)

A Store package is staged by `.github/workflows/store-package.yml` and **does not require the sideload PFX certificate**, because the Microsoft Store re-signs MSIX during certification. See Microsoft documentation: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/package-version-numbering.

Manual prerequisites:

1. Open Partner Center, reserve `FlowAndroid`, obtain the exact MSIX package identity and publisher string.
2. Configure GitHub **repository variables** `FLOWANDROID_STORE_PACKAGE_ID` and `FLOWANDROID_STORE_PUBLISHER` using exact Partner Center values (including `CN=`).
3. Manually run **Actions → Microsoft Store submission package → Run workflow**. Download its `.msixupload` and SHA256SUMS artifact.
4. First complete the Windows/Pixel acceptance checklist in `docs/MANUAL_ACCEPTANCE.md`.
5. Manually upload the package to Partner Center and complete store listing, privacy disclosure, screenshots, categories, age ratings, compliance information and submission review.

The workflow does not authenticate to Partner Center or claim an approved public Store listing. Store and GitHub MSIX identities, certificates and update channels may differ. Single-project MSIX generates one MSIX at a time; the workflow wraps the single file in an upload archive if it does not automatically produce an `.msixupload`.
