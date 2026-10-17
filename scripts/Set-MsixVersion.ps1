param(
  [Parameter(Mandatory=$true)][int]$BuildNumber,
  [string]$Manifest = 'src/FlowAndroid.App/Package.appxmanifest'
)
$ErrorActionPreference = 'Stop'
if ($BuildNumber -lt 1 -or $BuildNumber -gt 65535) {
 throw 'MSIX build revision must be in range 1–65535. Bump the version scheme before this limit.'
}
[xml]$doc = Get-Content $Manifest -Raw -Encoding UTF8
$ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
$ns.AddNamespace('p','http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$ident = $doc.SelectSingleNode('/p:Package/p:Identity',$ns)
if (!$ident) { throw 'MSIX Identity missing' }
$version = $ident.GetAttribute('Version')
if ($version -notmatch '^(\d+)\.(\d+)\.(\d+)\.(\d+)$') { throw 'Invalid 4-part version' }
$ident.SetAttribute('Version', "$($Matches[1]).$($Matches[2]).$($Matches[3]).$BuildNumber")
$doc.Save((Join-Path (Get-Location) $Manifest))
Write-Host "Build revision set to $BuildNumber."
