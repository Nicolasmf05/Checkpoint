param([Parameter(Mandatory)][string]$ExePath)
$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$caseRoot = Join-Path $checkpointRoot ('.qa\web-package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $caseRoot | Out-Null
$data = Join-Path $caseRoot 'data'
$render = Join-Path $caseRoot 'render'
$stdout = Join-Path $caseRoot 'web.stdout.log'
$stderr = Join-Path $caseRoot 'web.stderr.log'
$arguments = @('--data-dir', ('"' + $data + '"'), '--demo', '--diagnostics', '--web-smoke-test', ('"' + $render + '"'))
$process = Start-Process -FilePath $ExePath -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
if (!$process.WaitForExit(90000)) { $process.Kill(); throw "CSS validation timed out. See $caseRoot" }
$process.WaitForExit()
Get-Content -LiteralPath $stdout
if ($process.ExitCode -ne 0) { Get-Content -LiteralPath $stderr; throw "CSS application validation failed. See $caseRoot" }
$report = Get-Content -LiteralPath (Join-Path $render 'web-smoke.json') -Raw | ConvertFrom-Json
if (!$report.ok -or $report.checks -lt 30) { throw 'The CSS interface validation is incomplete.' }
Write-Output "CSS interface verified: $($report.checks) checks. Evidence: $render"
