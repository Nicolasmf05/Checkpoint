# Prepara el manifiesto y los archivos de un paquete MSIX; permite una vista previa sin firma.

param(
    [ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64',
    [switch]$Preview,
    [string]$IdentityName,
    [string]$Publisher,
    [string]$PublisherDisplayName,
    [string]$DisplayName = 'Checkpoint',
    [string]$PackageVersion,
    [string]$MakeAppxPath
)
$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = & "$PSScriptRoot\Get-Version.ps1"
# Store reserves the last field and requires a nonzero major version.
# Keep the preview app version independent from the Store package version.
if (!$PackageVersion) {
    $appVersion = [version]$version
    $PackageVersion = "$($appVersion.Major + 1).$($appVersion.Minor).$($appVersion.Build).0"
}
if ($PackageVersion -notmatch '^[1-9]\d*\.\d+\.\d+\.0$' -or
    @($PackageVersion.Split('.') | Where-Object { [decimal]$_ -gt 65535 }).Count) {
    throw 'PackageVersion must have four fields, a nonzero major version, a final zero and fields <= 65535.'
}
if ($Preview) {
    if ($IdentityName -or $Publisher -or $PublisherDisplayName) {
        throw 'Preview must not use a Store identity.'
    }
    $IdentityName = 'Checkpoint.PackagingPreview'
    $Publisher = 'CN=Checkpoint Packaging Preview'
    $PublisherDisplayName = 'Checkpoint Packaging Preview'
}
elseif (!$IdentityName -or !$Publisher -or !$PublisherDisplayName) {
    throw 'Copy IdentityName, Publisher and PublisherDisplayName from Partner Center, or use -Preview for local packaging validation.'
}
if ($IdentityName -notmatch '^[A-Za-z0-9.-]{3,50}$' -or $IdentityName -eq 'Checkpoint.PackagingPreview' -and !$Preview) {
    throw 'Invalid Store identity name. Use the exact value from Partner Center.'
}
if (!$MakeAppxPath) {
    $sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $MakeAppxPath = Get-ChildItem -LiteralPath $sdkRoot -Directory |
        Where-Object { $_.Name -match '^10\.0\.\d+\.0$' } |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName 'x64\makeappx.exe' } |
        Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$MakeAppxPath -or !(Test-Path -LiteralPath $MakeAppxPath)) {
    throw 'Install the Windows SDK with MakeAppx, or pass -MakeAppxPath.'
}
$publish = Join-Path $checkpointRoot "dist\$version\$Runtime\Checkpoint"
foreach ($required in @('Checkpoint.exe', 'Checkpoint.dll', 'service-config.json', 'supabase-config.json', 'LICENSE', 'licenses\DOTNET-LICENSE.txt')) {
    if (!(Test-Path -LiteralPath (Join-Path $publish $required))) {
        throw "Build the portable first: missing $required."
    }
}
$productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $publish 'Checkpoint.exe')).ProductVersion.Split('+')[0]
if ($productVersion -ne $version -and !$productVersion.StartsWith($version + '.')) {
    throw 'Published app version does not match Directory.Build.props.'
}
$stage = Join-Path $checkpointRoot ('artifacts\msix-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath $publish -Destination $stage -Recurse
Copy-Item -LiteralPath (Join-Path $checkpointRoot 'installer\Assets') -Destination $stage -Recurse
$manifest = [xml](Get-Content -LiteralPath (Join-Path $checkpointRoot 'installer\AppxManifest.xml') -Raw)
$manifest.Package.Identity.SetAttribute('Name', $IdentityName)
$manifest.Package.Identity.SetAttribute('Publisher', $Publisher)
$manifest.Package.Identity.SetAttribute('Version', $PackageVersion)
$manifest.Package.Identity.SetAttribute('ProcessorArchitecture', $Runtime.Replace('win-', ''))
$manifest.Package.Properties.DisplayName = $DisplayName
$manifest.Package.Properties.PublisherDisplayName = $PublisherDisplayName
$visual = $manifest.GetElementsByTagName('VisualElements', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')[0]
$visual.SetAttribute('DisplayName', $DisplayName)
$manifest.Save((Join-Path $stage 'AppxManifest.xml'))
$suffix = if ($Preview) {
    'preview-unsigned'
}
else {
    'store-unsigned'
}
$output = Join-Path $checkpointRoot "dist\Checkpoint-$version-$Runtime-$suffix.msix"
# Keep SDK schema/content validation enabled. No /nv, certificate creation or system registration.
# The log must be outside the content directory so it cannot enter the package.
$log = "$stage.makeappx.log"
& $MakeAppxPath pack /d $stage /p $output /o /h SHA256 *> $log
if ($LASTEXITCODE -ne 0) {
    Get-Content -LiteralPath $log -Tail 20; throw 'MakeAppx package validation failed.'
}
Get-FileHash -LiteralPath $output -Algorithm SHA256 | ForEach-Object {
    "$($_.Hash.ToLowerInvariant())  $(Split-Path $output -Leaf)"
} | Set-Content -LiteralPath "$output.sha256" -Encoding ascii
Write-Output "Unsigned MSIX: $output"
Write-Output "Package identity: $IdentityName ($PackageVersion). App version: $version."
Write-Output 'This is not an end-user download. Store signing and certification are still required.'
& "$PSScriptRoot\Verify-MSIX.ps1" -PackagePath $output -PublishedPath $publish -MakeAppxPath $MakeAppxPath
