#Requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$Install,
    [switch]$TrustPublisher
)
$ErrorActionPreference = 'Stop'
$folder = $PSScriptRoot
$packages = @(Get-ChildItem -Path $folder -File -Filter 'FlowAndroid-*-x64.msix')
if ($packages.Count -ne 1) { throw 'Expected exactly one FlowAndroid versioned MSIX.' }
$msix = $packages[0].FullName
$msixName = $packages[0].Name
$publicCert = Join-Path $folder 'FlowAndroid-public.cer'
$manifest = Join-Path $folder 'SHA256SUMS.txt'
if (!(Test-Path $msix) -or !(Test-Path $manifest)) {
    throw 'The acceptance package is incomplete (MSIX or SHA256SUMS missing).'
}
$line = @(Get-Content $manifest | Where-Object { $_.EndsWith("  $msixName") })
$pattern = '^([a-fA-F0-9]{64})\s+' + [regex]::Escape($msixName) + '
    throw 'Invalid or ambiguous checksum manifest.'
}
$expected = $Matches[1]
$actual = (Get-FileHash $msix -Algorithm SHA256).Hash
if ($actual -ne $expected) { throw 'Package checksum mismatch. Do not install.' }
Write-Host "SHA-256 matched: $actual"
if (!(Test-Path $publicCert)) {
    throw 'Unsigned candidate or missing publisher certificate. Installation is disabled.'
}
$cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($publicCert)
$signature = Get-AuthenticodeSignature -LiteralPath $msix
if (!$signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $cert.Thumbprint) {
    throw 'The MSIX signer does not match the included public certificate.'
}
if ($signature.Status -in @('HashMismatch','NotSigned','NotSupportedFileFormat')) {
    throw "Invalid package signature status: $($signature.Status)"
}
Write-Host "Publisher: $($cert.Subject)"
Write-Host "Publisher thumbprint: $($cert.Thumbprint)"
Write-Host "Local signer trust: $($signature.Status)"
if (!$Install) {
    Write-Host "Verification complete. No software or certificate has been installed."
    Write-Host 'Review the signer thumbprint, then rerun with -Install to install.'
    exit 0
}
if ($signature.Status -ne 'Valid' -and !$TrustPublisher) {
    throw 'The publisher is not trusted on this PC. Review the certificate and rerun with -Install -TrustPublisher on a dedicated test PC if appropriate.'
}
if ($TrustPublisher) {
    Write-Warning "You are about to trust $($cert.Subject) for the CURRENT WINDOWS USER."
    Write-Warning "Certificate thumbprint: $($cert.Thumbprint)"
    $answer = Read-Host 'Type TRUST to add the certificate to CurrentUser\TrustedPeople'
    if ($answer -cne 'TRUST') { throw 'Certificate trust was not approved.' }
    $store = 'Cert:\CurrentUser\TrustedPeople'
    if (!(Get-ChildItem $store | Where-Object { $_.Thumbprint -eq $cert.Thumbprint })) {
        Import-Certificate -FilePath $publicCert -CertStoreLocation $store | Out-Null
    }
}
Write-Host 'Installing FlowAndroid MSIX...'
Add-AppxPackage -Path $msix -ErrorAction Stop
Write-Host 'MSIX installed. Continue the manual acceptance checklist; do NOT run OTA on a primary Pixel.'

if ($line.Count -ne 1 -or $line[0] -notmatch $pattern) {
    throw 'Invalid or ambiguous checksum manifest.'
}
$expected = $Matches[1]
$actual = (Get-FileHash $msix -Algorithm SHA256).Hash
if ($actual -ne $expected) { throw 'Package checksum mismatch. Do not install.' }
Write-Host "SHA-256 matched: $actual"
if (!(Test-Path $publicCert)) {
    throw 'Unsigned candidate or missing publisher certificate. Installation is disabled.'
}
$cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($publicCert)
$signature = Get-AuthenticodeSignature -LiteralPath $msix
if (!$signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $cert.Thumbprint) {
    throw 'The MSIX signer does not match the included public certificate.'
}
if ($signature.Status -in @('HashMismatch','NotSigned','NotSupportedFileFormat')) {
    throw "Invalid package signature status: $($signature.Status)"
}
Write-Host "Publisher: $($cert.Subject)"
Write-Host "Publisher thumbprint: $($cert.Thumbprint)"
Write-Host "Local signer trust: $($signature.Status)"
if (!$Install) {
    Write-Host "Verification complete. No software or certificate has been installed."
    Write-Host 'Review the signer thumbprint, then rerun with -Install to install.'
    exit 0
}
if ($signature.Status -ne 'Valid' -and !$TrustPublisher) {
    throw 'The publisher is not trusted on this PC. Review the certificate and rerun with -Install -TrustPublisher on a dedicated test PC if appropriate.'
}
if ($TrustPublisher) {
    Write-Warning "You are about to trust $($cert.Subject) for the CURRENT WINDOWS USER."
    Write-Warning "Certificate thumbprint: $($cert.Thumbprint)"
    $answer = Read-Host 'Type TRUST to add the certificate to CurrentUser\TrustedPeople'
    if ($answer -cne 'TRUST') { throw 'Certificate trust was not approved.' }
    $store = 'Cert:\CurrentUser\TrustedPeople'
    if (!(Get-ChildItem $store | Where-Object { $_.Thumbprint -eq $cert.Thumbprint })) {
        Import-Certificate -FilePath $publicCert -CertStoreLocation $store | Out-Null
    }
}
Write-Host 'Installing FlowAndroid MSIX...'
Add-AppxPackage -Path $msix -ErrorAction Stop
Write-Host 'MSIX installed. Continue the manual acceptance checklist; do NOT run OTA on a primary Pixel.'
