# Comprueba la presencia de WebView2 y permite preparar el runtime en el entorno de CI.

param([switch]$InstallForCI)
$ErrorActionPreference = 'Stop'
$runtimeRoots = @((Join-Path ${env:ProgramFiles(x86)} 'Microsoft\EdgeWebView\Application'), (Join-Path $env:LOCALAPPDATA 'Microsoft\EdgeWebView\Application'))
foreach ($runtimeRoot in $runtimeRoots) {
    if (Test-Path -LiteralPath $runtimeRoot) {
        $runtime = Get-ChildItem -LiteralPath $runtimeRoot -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'msedgewebview2.exe') } | Select-Object -First 1
        if ($runtime) {
            Write-Output "WebView2 Runtime available: $($runtime.Name)"; return
        }
    }
}
if (!$InstallForCI -or $env:GITHUB_ACTIONS -ne 'true') {
    throw 'Install Microsoft Edge WebView2 Evergreen Runtime from https://developer.microsoft.com/microsoft-edge/webview2/ before running CSS interface tests.'
}
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$installer = Join-Path $checkpointRoot '.tools\MicrosoftEdgeWebview2Setup.exe'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $installer) | Out-Null
Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $installer
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') {
    throw 'The WebView2 installer does not have a valid Microsoft signature.'
}
$process = Start-Process -FilePath $installer -ArgumentList '/silent', '/install' -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) {
    throw "WebView2 Runtime installation failed: $($process.ExitCode)"
}
