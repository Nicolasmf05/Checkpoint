# Genera el MSI por usuario a partir de la distribución portable y produce su SHA-256.

param([ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$checkpointRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = & "$PSScriptRoot\Get-Version.ps1"
$publish = Join-Path $checkpointRoot "dist\$version\$Runtime\Checkpoint"
$wix = Join-Path $checkpointRoot '.tools\wix\wix.exe'
if (!(Test-Path -LiteralPath $wix)) {
    throw 'Instala WiX 5.0.2 con dotnet tool install wix --version 5.0.2 --tool-path .tools/wix --configfile NuGet.config.'
}
if (!(Test-Path -LiteralPath (Join-Path $publish 'Checkpoint.exe'))) {
    throw 'Genera primero el paquete con scripts/Build.ps1.'
}
New-Item -ItemType Directory -Path (Join-Path $checkpointRoot 'artifacts\installer') -Force | Out-Null
$document = [System.Xml.XmlDocument]::new()
$ns = 'http://wixtoolset.org/schemas/v4/wxs'
$root = $document.CreateElement('Wix', $ns); [void]$document.AppendChild($root)
$fragment = $document.CreateElement('Fragment', $ns); [void]$root.AppendChild($fragment)
$directories = $document.CreateElement('DirectoryRef', $ns); $directories.SetAttribute('Id', 'INSTALLFOLDER'); [void]$fragment.AppendChild($directories)
$group = $document.CreateElement('ComponentGroup', $ns); $group.SetAttribute('Id', 'AppFiles'); [void]$fragment.AppendChild($group)
$knownDirectories = @{ '' = @{ Id = 'INSTALLFOLDER'; Element = $directories } }
function Get-StableId([string]$text) {
    $bytes = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($text.ToLowerInvariant()))
    return [Convert]::ToHexString($bytes).Substring(0, 24)
}
function Get-Directory([string]$relative) {
    if ($knownDirectories.ContainsKey($relative)) {
        return $knownDirectories[$relative].Id
    }
    $parentPath = [System.IO.Path]::GetDirectoryName($relative)
    $parentId = Get-Directory $parentPath
    $element = $document.CreateElement('Directory', $ns)
    $id = 'd_' + (Get-StableId $relative)
    $element.SetAttribute('Id', $id); $element.SetAttribute('Name', [System.IO.Path]::GetFileName($relative))
    [void]$knownDirectories[$parentPath].Element.AppendChild($element)
    $knownDirectories[$relative] = @{ Id = $id; Element = $element }
    return $id
}
foreach ($file in (Get-ChildItem -LiteralPath $publish -Recurse -File | Sort-Object FullName)) {
    $relative = [System.IO.Path]::GetRelativePath($publish, $file.FullName)
    $hash = Get-StableId $relative
    $directoryId = Get-Directory ([System.IO.Path]::GetDirectoryName($relative))
    $component = $document.CreateElement('Component', $ns)
    $guidBytes = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes("Checkpoint|$Runtime|$relative".ToLowerInvariant()))
    $componentGuid = [Guid]::new([byte[]]$guidBytes[0..15]).ToString()
    $component.SetAttribute('Id', "c_$hash"); $component.SetAttribute('Guid', $componentGuid); $component.SetAttribute('Directory', $directoryId)
    $item = $document.CreateElement('File', $ns)
    $item.SetAttribute('Id', "f_$hash"); $item.SetAttribute('Source', $file.FullName); $item.SetAttribute('KeyPath', 'no'); [void]$component.AppendChild($item)
    $registry = $document.CreateElement('RegistryValue', $ns)
    $registry.SetAttribute('Root', 'HKCU'); $registry.SetAttribute('Key', 'Software\Checkpoint\InstallerFiles'); $registry.SetAttribute('Name', $hash); $registry.SetAttribute('Type', 'integer'); $registry.SetAttribute('Value', '1'); $registry.SetAttribute('KeyPath', 'yes'); [void]$component.AppendChild($registry)
    $remove = $document.CreateElement('RemoveFolder', $ns); $remove.SetAttribute('Id', "r_$hash"); $remove.SetAttribute('Directory', $directoryId); $remove.SetAttribute('On', 'uninstall'); [void]$component.AppendChild($remove)
    [void]$group.AppendChild($component)
}
$harvest = Join-Path $checkpointRoot 'artifacts\installer\Files.wxs'; $document.Save($harvest)
$architecture = if ($Runtime -eq 'win-arm64') {
    'arm64'
}
else {
    'x64'
}
$msi = Join-Path $checkpointRoot "dist\Checkpoint-$version-$Runtime.msi"
& $wix build "$checkpointRoot\installer\Package.wxs" $harvest -arch $architecture -d "AppVersion=$version" -o $msi
if ($LASTEXITCODE -ne 0) {
    throw 'Falló la compilación del MSI.'
}
Get-FileHash -LiteralPath $msi -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLowerInvariant())  $(Split-Path $msi -Leaf)" } | Set-Content -LiteralPath "$msi.sha256" -Encoding ascii
& "$PSScriptRoot\Verify-Uninstaller.ps1" -MsiPath $msi
Write-Output "Instalador: $msi"
