param([string]$Project = 'src/FlowAndroid.App/FlowAndroid.App.csproj')
$ErrorActionPreference = 'Stop'
$manifest = Join-Path (Split-Path $Project) 'Package.appxmanifest'
[xml]$doc = Get-Content $manifest -Raw -Encoding UTF8
$ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
$ns.AddNamespace('p','http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$ns.AddNamespace('u','http://schemas.microsoft.com/appx/manifest/uap/windows10')
$ident = $doc.SelectSingleNode('/p:Package/p:Identity',$ns)
if (!$ident -or $ident.GetAttribute('Version') -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Invalid MSIX identity' }
if ($ident.GetAttribute('Publisher') -notmatch '^CN=') { throw 'Publisher must be an X.500 subject' }
if (!$doc.SelectSingleNode('//u:VisualElements',$ns)) { throw 'Missing visual elements' }
foreach ($asset in @('Assets\StoreLogo.png','Assets\Square150x150Logo.png','Assets\Square44x44Logo.png')) {
 if (!(Test-Path (Join-Path (Split-Path $Project) $asset))) { throw "Missing logo: $asset" }
}
Write-Host "MSIX manifest and logos validated."
