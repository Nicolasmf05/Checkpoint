$ErrorActionPreference = 'Stop'
$propsPath = Join-Path $PSScriptRoot '..\Directory.Build.props'
$props = [xml](Get-Content -LiteralPath $propsPath -Raw)
$version = [string]$props.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'La versión debe tener el formato mayor.menor.revisión.' }
Write-Output $version
