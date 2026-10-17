param(
  [Parameter(Mandatory=$true)][string]$PackageIdentity,
  [Parameter(Mandatory=$true)][string]$PublisherSubject,
  [string]$Manifest = "src/FlowAndroid.App/Package.appxmanifest"
)
$ErrorActionPreference = 'Stop'
if ($PackageIdentity -notmatch '^[A-Za-z][A-Za-z0-9.-]{2,49}$') { throw 'Invalid MSIX package identity' }
if ($PublisherSubject -notmatch '^CN=.+') { throw 'Publisher subject must start with CN=' }
[xml]$xml = Get-Content $Manifest -Raw -Encoding UTF8
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace('p','http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$identity = $xml.SelectSingleNode('/p:Package/p:Identity',$ns)
if (!$identity) { throw 'MSIX Identity missing' }
$identity.SetAttribute('Name',$PackageIdentity)
$identity.SetAttribute('Publisher',$PublisherSubject)
$xml.Save((Join-Path (Get-Location) $Manifest))
Write-Host "Prepared MSIX package identity $PackageIdentity."
