$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $checkpointRoot
$version = & "$PSScriptRoot\Get-Version.ps1"
Add-Type -AssemblyName System.IO.Compression
$sourceFiles = & rg --files --hidden --no-require-git
if ($LASTEXITCODE -ne 0) { throw 'No se pudo enumerar el código con ripgrep.' }
New-Item -ItemType Directory -Path (Join-Path $checkpointRoot 'dist') -Force | Out-Null
$destination = Join-Path $checkpointRoot "dist\Checkpoint-source-$version.zip"
$stream = [System.IO.File]::Create($destination)
$archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($relative in $sourceFiles) {
        $full = [System.IO.Path]::GetFullPath((Join-Path $checkpointRoot $relative))
        if (!$full.StartsWith($checkpointRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Archivo fuera del proyecto.' }
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $full, $relative.Replace('\','/'), [System.IO.Compression.CompressionLevel]::Optimal)
    }
}
finally { $archive.Dispose(); $stream.Dispose() }
Write-Output "Código: $destination"
Get-FileHash -LiteralPath $destination -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLowerInvariant())  $(Split-Path $destination -Leaf)" } | Set-Content -LiteralPath "$destination.sha256" -Encoding ascii
