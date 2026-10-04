# Prepara y envía la configuración del servicio Steam al servidor indicado por el operador.

param(
    [Parameter(Mandatory)][ValidatePattern('^[a-zA-Z0-9.-]+$')][string]$ServerHost,
    [Parameter(Mandatory)][string]$IdentityFile,
    [Parameter(Mandatory)][ValidatePattern('^https://[a-zA-Z0-9.-]+$')][string]$ServiceOrigin,
    [ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$RemoteUser = 'ubuntu'
)
$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$checkpointIdentity = (Resolve-Path -LiteralPath $IdentityFile).Path
$checkpointQa = Join-Path $checkpointRoot '.qa'
New-Item -ItemType Directory -Path $checkpointQa -Force | Out-Null
$checkpointKnownHosts = Join-Path $checkpointQa 'ssh_known_hosts'
$checkpointVersion = & (Join-Path $PSScriptRoot 'Get-Version.ps1')
$checkpointRelease = "$checkpointVersion-$([DateTime]::UtcNow.ToString('yyyyMMddTHHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0,8))"
$checkpointArchive = Join-Path $checkpointQa "server-$checkpointRelease.tar.gz"
$checkpointDestination = "$RemoteUser@$ServerHost"
$checkpointRemoteStage = "/tmp/checkpoint-upload-$checkpointRelease"
$checkpointSsh = @('-i', $checkpointIdentity, '-o', 'IdentitiesOnly=yes', '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=20', '-o', 'StrictHostKeyChecking=accept-new', '-o', "UserKnownHostsFile=$checkpointKnownHosts")

& node --test (Join-Path $checkpointRoot 'server\test\service.test.mjs')
if ($LASTEXITCODE -ne 0) {
    throw 'Fallaron las pruebas del servicio.'
}
& tar -czf $checkpointArchive -C $checkpointRoot server/src server/test server/package.json deploy LICENSE
if ($LASTEXITCODE -ne 0) {
    throw 'Falló la creación del paquete del servidor.'
}
$checkpointHash = (Get-FileHash -LiteralPath $checkpointArchive -Algorithm SHA256).Hash.ToLowerInvariant()
& ssh @checkpointSsh $checkpointDestination "umask 077 && mkdir '$checkpointRemoteStage'"
if ($LASTEXITCODE -ne 0) {
    throw 'No se pudo crear el directorio remoto.'
}
& scp @checkpointSsh $checkpointArchive "${checkpointDestination}:$checkpointRemoteStage/release.tar.gz"
if ($LASTEXITCODE -ne 0) {
    throw 'Falló la transferencia.'
}
$checkpointInstall = @"
set -eu
cd '$checkpointRemoteStage'
printf '%s  %s\n' '$checkpointHash' release.tar.gz | sha256sum --check
tar -xzf release.tar.gz
sudo -n bash deploy/install-server.sh '$ServiceOrigin' '$checkpointRelease'
"@
$checkpointInstall.Replace("`r", '') | & ssh @checkpointSsh $checkpointDestination 'tr -d "\r" | bash -s'
if ($LASTEXITCODE -ne 0) {
    throw 'El despliegue requiere revisar el resultado remoto.'
}
Write-Output "Release instalada: $checkpointRelease"
Write-Output 'Comprobar acceso externo y activar HTTPS antes de usar la app con este servicio.'
