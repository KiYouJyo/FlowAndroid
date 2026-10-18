param(
  [Parameter(Mandatory=$true)][int]$BuildNumber,
  [string]$ProductVersion = '0.1.0',
  [string]$Manifest = 'src/FlowAndroid.App/Package.appxmanifest'
)
$ErrorActionPreference = 'Stop'
if ($BuildNumber -lt 1 -or $BuildNumber -gt 65535) {
    throw 'MSIX build revision must be within 1..65535.'
}
[xml]$doc = Get-Content $Manifest -Raw -Encoding UTF8
$ns = [System.Xml.XmlNamespaceManager]::new($doc.NameTable)
$ns.AddNamespace('p','http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$identity = $doc.SelectSingleNode('/p:Package/p:Identity',$ns)
if ($null -eq $identity) { throw 'Missing package identity.' }
$version = $identity.GetAttribute('Version')
$parts = $version.Split('.')
if ($parts.Count -ne 4) { throw 'MSIX version must have four numeric fields.' }
foreach ($part in $parts) {
    $value = 0
    if (-not [int]::TryParse($part,[ref]$value) -or $value -lt 0 -or $value -gt 65535) {
        throw "Invalid MSIX version component."
    }
}
if ($ProductVersion -notmatch '^\d+\.\d+\.\d+
$identity.SetAttribute('Version',$newVersion)
$doc.Save((Join-Path (Get-Location) $Manifest))
Write-Host "MSIX version stamped: $newVersion"
) { throw 'ProductVersion must be major.minor.patch.' }
$components = $ProductVersion.Split('.')
foreach ($component in $components) {
    $parsed = 0
    if (-not [int]::TryParse($component, [ref]$parsed) -or $parsed -lt 0 -or $parsed -gt 65535) {
        throw 'Invalid product-version component.'
    }
}
$newVersion = '{0}.{1}.{2}.{3}' -f $components[0],$components[1],$components[2],$BuildNumber
$identity.SetAttribute('Version',$newVersion)
$doc.Save((Join-Path (Get-Location) $Manifest))
Write-Host "MSIX version stamped: $newVersion"
