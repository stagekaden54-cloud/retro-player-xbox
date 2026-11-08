$ErrorActionPreference = "Stop"
$assets = Join-Path $PSScriptRoot "Assets"
New-Item -ItemType Directory -Force -Path $assets | Out-Null
foreach ($name in @("Square150x150Logo", "Square44x44Logo", "StoreLogo")) {
    $encoded = (Get-Content -Raw (Join-Path $assets "$name.png.base64")).Trim()
    [IO.File]::WriteAllBytes((Join-Path $assets "$name.png"), [Convert]::FromBase64String($encoded))
}
Write-Host "Generated Xbox package logo assets."
