$ErrorActionPreference = "Stop"
$assets = Join-Path $PSScriptRoot "Assets"
New-Item -ItemType Directory -Force -Path $assets | Out-Null

foreach ($name in @("Square150x150Logo", "Square44x44Logo", "StoreLogo")) {
    $encoded = (Get-Content -Raw (Join-Path $assets "$name.png.base64")).Trim()
    [IO.File]::WriteAllBytes((Join-Path $assets "$name.png"), [Convert]::FromBase64String($encoded))
}

# UWP requires the splash-screen image to be exactly 620x300 pixels.
Add-Type -AssemblyName System.Drawing
$bitmap = [System.Drawing.Bitmap]::new(620, 300)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$foreground = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(230, 246, 238))
$font = [System.Drawing.Font]::new("Segoe UI", 34, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
try {
    $graphics.Clear([System.Drawing.Color]::FromArgb(7, 17, 14))
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $label = "RETRO PLAYER"
    $size = $graphics.MeasureString($label, $font)
    $x = [single](($bitmap.Width - $size.Width) / 2)
    $y = [single](($bitmap.Height - $size.Height) / 2)
    $graphics.DrawString($label, $font, $foreground, $x, $y)
    $bitmap.Save((Join-Path $assets "SplashScreen.png"), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $font.Dispose()
    $foreground.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

Write-Host "Generated Xbox logo assets and 620x300 splash screen."
