param(
    [ValidateSet('win-x64','win-arm64')][string]$Runtime = 'win-x64',
    [string]$ServiceUrl = '',
    [switch]$Installer
)
$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $checkpointRoot
$version = & "$PSScriptRoot\Get-Version.ps1"
$portableDotnet = Join-Path $checkpointRoot '.tools\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $portableDotnet) { $portableDotnet } else { 'dotnet' }
$env:DOTNET_CLI_HOME = Join-Path $checkpointRoot '.tools\cli-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:NUGET_PACKAGES = Join-Path $checkpointRoot '.tools\nuget'
if ($ServiceUrl) {
    $uri = [Uri]$ServiceUrl
    if (!$uri.IsAbsoluteUri -or $uri.Scheme -ne 'https' -or $uri.AbsolutePath -ne '/' -or $uri.UserInfo -or $uri.Query -or $uri.Fragment) { throw 'ServiceUrl debe ser un origen HTTPS.' }
}
& $dotnet restore Checkpoint.slnx --configfile NuGet.config
if ($LASTEXITCODE -ne 0) { throw 'Falló restore.' }
& $dotnet build Checkpoint.slnx -c Release --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación.' }
& $dotnet run --project tests/Checkpoint.Tests -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de la biblioteca.' }
& node --test server/test/service.test.mjs
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del servicio.' }
$output = Join-Path $checkpointRoot "dist\$version\$Runtime"
& $dotnet publish src/Checkpoint.App/Checkpoint.App.csproj -c Release -r $Runtime --self-contained true -o $output --nologo -p:RestoreConfigFile="$checkpointRoot\NuGet.config" -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Falló publish.' }
@{ serviceUrl = $ServiceUrl } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'service-config.json') -Encoding utf8
Copy-Item -LiteralPath LICENSE,THIRD-PARTY-NOTICES.md -Destination $output
Copy-Item -LiteralPath licenses -Destination $output -Recurse -Force
$zip = Join-Path $checkpointRoot "dist\Checkpoint-$version-$Runtime.zip"
Compress-Archive -Path "$output\*" -DestinationPath $zip -Force
Get-FileHash -LiteralPath $zip -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLowerInvariant())  $(Split-Path $zip -Leaf)" } | Set-Content -LiteralPath "$zip.sha256" -Encoding ascii
Write-Output "Portable: $zip"
# Native WPF tests run against the extracted distribution, not the SDK build folder.
& "$PSScriptRoot\Verify-Package.ps1" -ZipPath $zip -Runtime $Runtime
if ($Installer) { & "$PSScriptRoot\Build-Installer.ps1" -Runtime $Runtime }
