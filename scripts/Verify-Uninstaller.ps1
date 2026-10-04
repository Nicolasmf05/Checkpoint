# Inspecciona tablas y accesos directos del MSI para verificar la desinstalación y sus idiomas.

param([Parameter(Mandatory)][string]$MsiPath)
$ErrorActionPreference = 'Stop'
# Inspect MSI tables only. Never install or remove the user's application.
$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $installer.OpenDatabase([System.IO.Path]::GetFullPath($MsiPath), 0)
function Read-MsiRows([string]$Query, [int]$Columns) {
    $view = $database.OpenView($Query)
    [void]$view.Execute()
    try {
        while ($record = $view.Fetch()) {
            $values = for ($column = 1; $column -le $Columns; $column++) {
                $record.StringData($column)
            }
            , $values
        }
    }
    finally {
        [void]$view.Close()
    }
}
$properties = @{}
foreach ($row in (Read-MsiRows 'SELECT `Property`, `Value` FROM `Property`' 2)) {
    $properties[$row[0]] = $row[1]
}
if ($properties['ProductCode'] -notmatch '^\{[0-9A-Fa-f-]{36}\}$') {
    throw 'Missing MSI product identity.'
}
$version = & "$PSScriptRoot\Get-Version.ps1"
if ($properties['ProductVersion'] -ne $version) {
    throw 'Unexpected MSI version.'
}
$shortcuts = @(Read-MsiRows 'SELECT `Shortcut`, `Directory_`, `Name`, `Target`, `Arguments`, `Component_` FROM `Shortcut`' 6 | Where-Object { $_[0] -like 'Uninstall*' })
if ($shortcuts.Count -ne 4) {
    throw 'Missing localized Start menu or installed-folder uninstall shortcuts.'
}
foreach ($row in $shortcuts) {
    $spanish = $row[0].EndsWith('Spanish')
    $expectedName = if ($spanish) {
        'Desinstalar Checkpoint'
    }
    else {
        'Uninstall Checkpoint'
    }
    $expectedDirectory = if ($row[0].StartsWith('UninstallMenu')) {
        'CheckpointMenu'
    }
    else {
        'INSTALLFOLDER'
    }
    if ($row[1] -ne $expectedDirectory -or $row[2].Split('|')[-1] -ne $expectedName -or
        $row[3] -ne '[System64Folder]msiexec.exe' -or $row[4] -ne '/x [ProductCode]' -or
        $row[5] -ne ('Uninstall' + $(if ($spanish) {
                    'Spanish'
                }
                else {
                    'English'
                }))) {
        throw 'Uninstall shortcut must target this MSI product with normal confirmation.'
    }
}
$conditions = @{}
foreach ($row in (Read-MsiRows 'SELECT `Component`, `Condition` FROM `Component`' 2)) {
    $conditions[$row[0]] = $row[1]
}
$spanishCondition = $conditions['UninstallSpanish']
if (!$spanishCondition -or $conditions['UninstallEnglish'] -ne "NOT ($spanishCondition)") {
    throw 'Uninstall shortcut languages must be mutually exclusive.'
}
# Spanish Spain and Mexico, plus English, exercise the MSI language predicates.
foreach ($language in @(1034, 3082, 2058, 1033, 2057)) {
    $matches = [regex]::Matches($spanishCondition, 'UserLanguageID = ([0-9]+)')
    $spanish = @($matches | ForEach-Object { [int]$_.Groups[1].Value }) -contains $language
    if ($spanish -ne ($language -in @(1034, 3082, 2058))) {
        throw 'Unexpected shortcut locale selection.'
    }
}
$directories = @{}
foreach ($row in (Read-MsiRows 'SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`' 3)) {
    $directories[$row[0]] = $row
}
if ($directories['INSTALLFOLDER'][1] -ne 'UserPrograms' -or $directories['UserPrograms'][1] -ne 'LocalAppDataFolder') {
    throw 'App installation must remain separate from the user library.'
}
foreach ($row in (Read-MsiRows 'SELECT `FileName`, `DirProperty` FROM `RemoveFile`' 2)) {
    if ($row[0] -and $row[0].Split('|')[-1] -ne 'Checkpoint.lnk') {
        throw 'Unexpected data cleanup in MSI uninstall.'
    }
}
Write-Output 'Uninstaller verified: four localized shortcuts, current product, normal confirmation, separate library path.'
