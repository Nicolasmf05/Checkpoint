# Inspecciona el paquete MSIX generado y comprueba manifiesto, archivos y evidencias de validación.

param(
    [Parameter(Mandatory)][string]$PackagePath,
    [Parameter(Mandatory)][string]$PublishedPath,
    [Parameter(Mandatory)][string]$MakeAppxPath
)
$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$caseRoot = Join-Path $checkpointRoot ('.qa\msix-' + [Guid]::NewGuid().ToString('N'))
$unpacked = Join-Path $caseRoot 'unpacked'
New-Item -ItemType Directory -Path $caseRoot -Force | Out-Null
& $MakeAppxPath unpack /p ([System.IO.Path]::GetFullPath($PackagePath)) /d $unpacked /o *> (Join-Path $caseRoot 'unpack.log')
if ($LASTEXITCODE -ne 0) {
    throw "MSIX unpack validation failed. See $caseRoot."
}
$manifest = [xml](Get-Content -LiteralPath (Join-Path $unpacked 'AppxManifest.xml') -Raw)
$executable = [string]$manifest.Package.Applications.Application.Executable
if ($executable -ne 'Checkpoint\Checkpoint.exe') {
    throw 'Unexpected MSIX entry point.'
}
if ($manifest.Package.Dependencies.TargetDeviceFamily.Name -ne 'Windows.Desktop') {
    throw 'MSIX must target Windows.Desktop.'
}
if ($manifest.Package.Identity.Version -notmatch '^[1-9]\d*\.\d+\.\d+\.0$') {
    throw 'Invalid Store package version.'
}
if ($manifest.GetElementsByTagName('Capability', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities')[0].Name -ne 'runFullTrust') {
    throw 'Desktop WPF capability missing.'
}
$languages = @($manifest.Package.Resources.Resource | ForEach-Object { $_.Language })
if ('en-US' -notin $languages -or 'es-ES' -notin $languages) {
    throw 'Both UI languages must be declared.'
}
$count = 0
foreach ($file in Get-ChildItem -LiteralPath $PublishedPath -Recurse -File) {
    $relative = [System.IO.Path]::GetRelativePath([System.IO.Path]::GetFullPath($PublishedPath), $file.FullName)
    $packedFile = Join-Path (Join-Path $unpacked 'Checkpoint') $relative
    if (!(Test-Path -LiteralPath $packedFile) -or
        (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $packedFile -Algorithm SHA256).Hash) {
        throw "MSIX payload mismatch: $relative"
    }
    $count++
}
$packedCount = @(Get-ChildItem -LiteralPath (Join-Path $unpacked 'Checkpoint') -Recurse -File).Count
if ($packedCount -ne $count) {
    throw 'MSIX contains unexpected application files.'
}
Add-Type -AssemblyName System.Drawing
foreach ($asset in @(@('StoreLogo.png', 50), @('Square44x44Logo.png', 44), @('Square150x150Logo.png', 150))) {
    $image = [System.Drawing.Image]::FromFile((Join-Path $unpacked ('Assets\' + $asset[0])))
    try {
        if ($image.Width -ne $asset[1] -or $image.Height -ne $asset[1]) {
            throw 'Invalid Store icon dimensions.'
        }
    }
    finally {
        $image.Dispose()
    }
}
# Verify the native app host architecture, not just the manifest label.
$bytes = [System.IO.File]::ReadAllBytes((Join-Path $unpacked $executable))
$peOffset = [BitConverter]::ToInt32($bytes, 0x3c)
$machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
$expected = if ($manifest.Package.Identity.ProcessorArchitecture -eq 'arm64') {
    0xAA64
}
else {
    0x8664
}
if ($machine -ne $expected) {
    throw 'MSIX architecture does not match the application.'
}
if (Test-Path -LiteralPath (Join-Path $unpacked 'AppxSignature.p7x')) {
    throw 'Expected an unsigned Store submission package.'
}
@{ applicationFiles = $count; identity = [string]$manifest.Package.Identity.Name;
    packageVersion = [string]$manifest.Package.Identity.Version; architecture = [string]$manifest.Package.Identity.ProcessorArchitecture;
    unsigned = $true; installedTested = $false
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $caseRoot 'verification.json') -Encoding utf8
Write-Output "MSIX verified: $count application files match SHA-256; icons, languages, manifest and PE architecture checked. Report: $caseRoot"
