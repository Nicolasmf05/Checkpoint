# Genera los recursos gráficos usados por el manifiesto de Microsoft Store.

# Derive Store icons from the existing Checkpoint branding; no external assets.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $checkpointRoot 'installer\Assets'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$icon = [System.Drawing.Icon]::new((Join-Path $checkpointRoot 'src\Checkpoint.App\Assets\checkpoint.ico'), 256, 256)
$bitmap = $icon.ToBitmap()
try {
    foreach ($asset in @(@('StoreLogo', 50), @('Square44x44Logo', 44), @('Square150x150Logo', 150))) {
        $image = [System.Drawing.Bitmap]::new([int]$asset[1], [int]$asset[1])
        $graphics = [System.Drawing.Graphics]::FromImage($image)
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.DrawImage($bitmap, 0, 0, $image.Width, $image.Height)
            $image.Save((Join-Path $output ($asset[0] + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $graphics.Dispose(); $image.Dispose()
        }
    }
}
finally {
    $bitmap.Dispose(); $icon.Dispose()
}
Write-Output "Store assets: $output"
