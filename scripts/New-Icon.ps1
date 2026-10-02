param([string]$OutputPath = "$PSScriptRoot\..\src\Checkpoint.App\Assets\checkpoint.ico")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$parent = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $parent | Out-Null
$sizes = @(16, 32, 48, 256)
$images = @()
foreach ($size in $sizes) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 17, 24, 39))
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 140, 235, 198))
    $graphics.FillEllipse($brush, $size * .14, $size * .14, $size * .72, $size * .72)
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 16, 45, 39), $size * .06)
    $points = @([System.Drawing.PointF]::new($size*.36, $size*.71), [System.Drawing.PointF]::new($size*.36, $size*.31), [System.Drawing.PointF]::new($size*.66, $size*.31), [System.Drawing.PointF]::new($size*.57, $size*.43), [System.Drawing.PointF]::new($size*.66, $size*.55), [System.Drawing.PointF]::new($size*.36, $size*.55))
    $graphics.DrawLines($pen, [System.Drawing.PointF[]]$points)
    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += ,$stream.ToArray()
    $graphics.Dispose(); $bitmap.Dispose(); $brush.Dispose(); $pen.Dispose(); $stream.Dispose()
}
$file = [System.IO.File]::Create([System.IO.Path]::GetFullPath($OutputPath))
$writer = [System.IO.BinaryWriter]::new($file)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
$writer.Dispose(); $file.Dispose()
Write-Output "Icono creado: $OutputPath"
