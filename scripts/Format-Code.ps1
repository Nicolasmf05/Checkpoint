<#
.SYNOPSIS
Aplica o verifica el formato de todo el código mantenido en el repositorio.
.DESCRIPTION
Usa versiones fijadas de CSharpier, Prettier, Black, SQL Formatter y
PSScriptAnalyzer. -Setup prepara las herramientas locales;
-Check informa de diferencias sin modificar los archivos de código.
#>
param(
    [switch]$Check,
    [switch]$Setup
)

$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $checkpointRoot
try {
    $env:DOTNET_CLI_HOME = Join-Path $checkpointRoot '.tools/cli-home'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:NUGET_PACKAGES = Join-Path $checkpointRoot '.tools/nuget'
    $env:npm_config_cache = Join-Path $checkpointRoot '.tools/npm-cache'
    $moduleRoot = Join-Path $checkpointRoot '.tools/format-powershell'
    $modulePath = Join-Path $moduleRoot 'PSScriptAnalyzer/1.24.0/PSScriptAnalyzer.psd1'
    $prettier = Join-Path $checkpointRoot 'node_modules/prettier/bin/prettier.cjs'
    $dotnet = Join-Path $checkpointRoot '.tools/dotnet/dotnet.exe'
    if (!(Test-Path -LiteralPath $dotnet)) {
        $dotnet = 'dotnet'
    }
    $csharpier = Join-Path $checkpointRoot '.tools/csharpier/csharpier.exe'

    if ($Setup) {
        & npm ci --ignore-scripts --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) {
            throw 'No se pudieron preparar las herramientas de Node.'
        }
        & python -m pip install --target .tools/format-python --upgrade 'black==26.5.1' 'sqlparse==0.6.0'
        if ($LASTEXITCODE -ne 0) {
            throw 'No se pudieron preparar las herramientas de Python.'
        }
        New-Item -ItemType Directory -Path $moduleRoot -Force | Out-Null
        Save-PSResource -Name PSScriptAnalyzer -Version 1.24.0 -Path $moduleRoot -TrustRepository
        if (!(Test-Path -LiteralPath $csharpier)) {
            & $dotnet tool install csharpier --version 1.3.0 --tool-path .tools/csharpier --configfile NuGet.config
            if ($LASTEXITCODE -ne 0) {
                throw 'No se pudo preparar CSharpier.'
            }
        }
    }
    if (!(Test-Path -LiteralPath $prettier) -or !(Test-Path -LiteralPath $modulePath) -or
        !(Test-Path -LiteralPath .tools/format-python/black) -or !(Test-Path -LiteralPath $csharpier)) {
        throw 'Faltan formateadores. Ejecuta ./scripts/Format-Code.ps1 -Setup.'
    }

    # Git limita el trabajo al código propio; las carpetas de salida quedan fuera.
    $files = @(& git ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) {
        throw 'No se pudo enumerar el código del repositorio.'
    }
    $csharpMode = if ($Check) {
        'check'
    }
    else {
        'format'
    }
    $csharpFiles = @($files | Where-Object { $_ -match '\.cs$' })
    $csharpArguments = @($csharpMode) + $csharpFiles
    if (!$Check) {
        $csharpArguments += '--no-cache'
    }
    & $csharpier @csharpArguments
    $failed = $LASTEXITCODE -ne 0

    $webFiles = @($files | Where-Object {
            $_ -match '\.(mjs|js|ts|css|html|svg|xml|xaml|wxs|csproj|props|slnx|manifest|webmanifest|json|yml|yaml|toml|sh)$' -or $_ -eq 'NuGet.config'
        } | Where-Object { $_ -notmatch 'package-lock\.json$' })
    $mode = if ($Check) {
        '--check'
    }
    else {
        '--write'
    }
    & node $prettier $mode @webFiles
    $failed = $failed -or $LASTEXITCODE -ne 0

    # El módulo conserva comentarios y cadenas; la escritura usa UTF-8 y LF.
    Import-Module $modulePath -Force
    $settings = Join-Path $PSScriptRoot 'PSScriptAnalyzerSettings.psd1'
    foreach ($file in ($files | Where-Object { $_ -match '\.(ps1|psd1)$' })) {
        $source = [System.IO.File]::ReadAllText((Join-Path $checkpointRoot $file))
        $formatted = (Invoke-Formatter -ScriptDefinition $source -Settings $settings).Replace("`r`n", "`n").TrimEnd() + "`n"
        $formatted = [regex]::Replace($formatted, '(?m)[ \t]+$', '')
        if ($source -ne $formatted) {
            Write-Output $file
            if ($Check) {
                $failed = $true
            }
            else {
                [System.IO.File]::WriteAllText((Join-Path $checkpointRoot $file), $formatted, [System.Text.UTF8Encoding]::new($false))
            }
        }
    }

    $pythonFiles = @($files | Where-Object { $_ -match '\.(py|sql)$' })
    $pythonArguments = @('scripts/format-python.py')
    if ($Check) {
        $pythonArguments += '--check'
    }
    & python @pythonArguments @pythonFiles
    $failed = $failed -or $LASTEXITCODE -ne 0
    if ($failed) {
        throw 'La comprobación de formato ha detectado diferencias o errores.'
    }
}
finally {
    Pop-Location
}
