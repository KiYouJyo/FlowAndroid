$ErrorActionPreference = 'Stop'
$source = Get-Content 'src/FlowAndroid.App/Localization/AppStrings.cs' -Raw -Encoding UTF8
$matches = [regex]::Matches($source, '\["(zh-CN|ja-JP|en-US)"\] = new\(\)\s*\{(?<body>.*?)\n\s*\}', 'Singleline')
if ($matches.Count -ne 3) { throw "Expected three language dictionaries, got $($matches.Count)" }
$groups = @{}
foreach ($m in $matches) {
  $key = $m.Groups[1].Value
  $values = @([regex]::Matches($m.Groups['body'].Value, '\["([^"]+)"\]\s*=') | ForEach-Object { $_.Groups[1].Value })
  if ($values.Count -ne ($values | Sort-Object -Unique).Count) { throw "Duplicate localization keys in $key" }
  $groups[$key] = $values | Sort-Object
}
foreach ($lang in @('ja-JP','en-US')) {
  if (Compare-Object $groups['zh-CN'] $groups[$lang]) { throw "Localization key mismatch in $lang" }
}
Write-Host "Three-language key parity OK."
