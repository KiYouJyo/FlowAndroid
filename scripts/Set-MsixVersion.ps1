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
$match = [regex]::Match($version, '^(\d+)\.(\d+)\.(\d+)\.(\d+)
$doc.Save((Join-Path (Get-Location) $Manifest))
Write-Host "Build revision set to $BuildNumber."
)
if (!$match.Success) { throw 'Invalid 4-part version' }
$ident.SetAttribute('Version', "$($match.Groups[1].Value).$($match.Groups[2].Value).$($match.Groups[3].Value).$BuildNumber")
$doc.Save((Join-Path (Get-Location) $Manifest))
Write-Host "Build revision set to $BuildNumber."
