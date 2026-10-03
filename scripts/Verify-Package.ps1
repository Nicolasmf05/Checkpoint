param(
    [Parameter(Mandatory)][string]$ZipPath,
    [ValidateSet('win-x64','win-arm64')][string]$Runtime = 'win-x64'
)
$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = & "$PSScriptRoot\Get-Version.ps1"
$caseRoot = Join-Path $checkpointRoot ('.qa\package-' + [Guid]::NewGuid().ToString('N'))
$extractionRoot = Join-Path $caseRoot 'app'
$extracted = Join-Path $extractionRoot 'Checkpoint'
$data = Join-Path $caseRoot 'data'
$render = Join-Path $caseRoot 'render'
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractionRoot
$topLevel = @(Get-ChildItem -LiteralPath $extractionRoot -Force)
if ($topLevel.Count -ne 1 -or !$topLevel[0].PSIsContainer -or $topLevel[0].Name -ne 'Checkpoint') {
    throw 'El portable debe contener una única carpeta Checkpoint en la raíz.'
}
$exe = Join-Path $extracted 'Checkpoint.exe'
foreach ($relative in @('Checkpoint.exe','Checkpoint.dll','service-config.json','supabase-config.json','LICENSE','THIRD-PARTY-NOTICES.md','licenses\DOTNET-LICENSE.txt','READ-ME-FIRST.md','LEEME-PRIMERO.md')) {
    if (!(Test-Path -LiteralPath (Join-Path $extracted $relative))) { throw "El paquete no incluye $relative." }
}
$unexpectedLanguages = @(Get-ChildItem -LiteralPath $extracted -Directory | Where-Object { $_.Name -in @('cs','de','fr','it','ja','ko','pl','pt-BR','ru','tr','zh-Hans','zh-Hant') })
if ($unexpectedLanguages.Count) { throw 'The package contains unused framework language resources.' }
$fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe).ProductVersion.Split('+')[0]
if (!$fileVersion.StartsWith($version + '.') -and $fileVersion -ne $version) { throw "Versión inesperada en el paquete: $fileVersion" }
if ($Runtime -eq 'win-arm64' -and [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() -ne 'Arm64') {
    Write-Output 'Estructura ARM64 comprobada. Ejecuta Verify-Package.ps1 en Windows ARM64 para validar la app nativa.'
    return
}
$stdout = Join-Path $caseRoot 'native.stdout.log'
$stderr = Join-Path $caseRoot 'native.stderr.log'
$arguments = @('--data-dir', ('"' + $data + '"'), '--demo', '--diagnostics', '--smoke-test', ('"' + $render + '"'))
$process = Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $extracted -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
if (!$process.WaitForExit(60000)) {
    $process.Kill(); throw "La prueba nativa superó 60 segundos. Consulta $caseRoot."
}
$process.WaitForExit()
Get-Content -LiteralPath $stdout
if ($process.ExitCode -ne 0) { Get-Content -LiteralPath $stderr; throw "Falló la app del paquete con código $($process.ExitCode). Consulta $caseRoot." }
$reportPath = Join-Path $render 'smoke.json'
if (!(Test-Path -LiteralPath $reportPath)) { throw 'La app no produjo el informe de comprobación.' }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (!$report.ok -or $report.checks -lt 175) { throw 'La comprobación nativa del paquete quedó incompleta.' }
Write-Output "Paquete validado: $($report.checks) comprobaciones. Imágenes e informe: $render"
