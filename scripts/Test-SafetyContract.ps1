$ErrorActionPreference = 'Stop'
$core = Get-Content 'src/FlowAndroid.Core/Devices/PixelOtaTransfer.cs' -Raw
$runner = Get-Content 'src/FlowAndroid.Core/Process/SystemCommandRunner.cs' -Raw
$release = Get-Content '.github/workflows/msix-package.yml' -Raw
if ($runner -notmatch 'UseShellExecute\s*=\s*false' -or $runner -notmatch 'ArgumentList\.Add') {
  throw 'The process runner must use ArgumentList without a shell.'
}
foreach ($unsafe in @('fastboot flash','fastboot erase','flashing unlock','flashing lock','flash-all.bat')) {
 if ($core.Contains($unsafe)) { throw "Unsafe operation in OTA engine: $unsafe" }
}
if ($core -notmatch 'GoogleFirmwareVerifier\.VerifyAsync' -or $core -notmatch 'BatteryPercent\s*<\s*50' -or $core -notmatch 'PostTimestamp') {
 throw 'OTA integrity, battery or anti-downgrade gate missing.'
}
if ($core -notmatch 'devices\.Count != 1' -or $core -notmatch '\["-s", serial, "sideload", ota\.Path\]') {
 throw 'Single-device serial-bound sideload rule missing.'
}
if ($release -notmatch 'inputs\.hardware_accepted == true' -or $release -notmatch 'inputs\.sign_package == true &&') {
 throw 'Release approval condition missing.'
}
Write-Host 'Write safety checks passed.'
