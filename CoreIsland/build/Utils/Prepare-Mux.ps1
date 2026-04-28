param(
    [Parameter(Mandatory)] [string] $MuxPackageDir,
    [Parameter(Mandatory)] [string] $Architecture,
    [Parameter(Mandatory)] [string] $AppManifest,
    [Parameter(Mandatory)] [string] $OutDir
)

$ErrorActionPreference = 'Stop'

$archFolder = switch ($Architecture.ToLower()) {
    'x64'   { 'x64' }
    'arm64' { 'arm64' }
    'x86'   { 'x86' }
    default { throw "Unsupported Architecture '$Architecture'" }
}

$appxPath = Join-Path $MuxPackageDir "tools\AppX\$archFolder\Release\Microsoft.UI.Xaml.2.8.appx"
if (-not (Test-Path $appxPath)) { throw "Missing MUX appx: $appxPath" }

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$stageDir = Join-Path $OutDir '_appxStage'
if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

$zipPath = Join-Path $stageDir 'mux.zip'
Copy-Item $appxPath $zipPath -Force
Expand-Archive -Path $zipPath -DestinationPath $stageDir -Force

# Copy runtime payload: rename resources.pri to Microsoft.UI.Xaml.pri
Copy-Item (Join-Path $stageDir 'Microsoft.UI.Xaml.dll') (Join-Path $OutDir 'Microsoft.UI.Xaml.dll') -Force
Copy-Item (Join-Path $stageDir 'resources.pri') (Join-Path $OutDir 'Microsoft.UI.Xaml.pri') -Force

# Merge activatableClass entries from AppxManifest into app manifest
[xml] $appx = Get-Content (Join-Path $stageDir 'AppxManifest.xml') -Encoding UTF8
$ns = New-Object System.Xml.XmlNamespaceManager($appx.NameTable)
$ns.AddNamespace('a', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$ips = $appx.SelectSingleNode('//a:InProcessServer', $ns)
if (-not $ips) { throw 'No <InProcessServer> in MUX AppxManifest' }

$fileName = $ips.SelectSingleNode('a:Path', $ns).InnerText

# Read base manifest (might not exist; create minimal if needed)
if (-not (Test-Path $AppManifest)) {
    $xml = [xml] '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0"><trustInfo xmlns="urn:schemas-microsoft-com:asm.v3"><security><requestedPrivileges><requestedExecutionLevel level="asInvoker" uiAccess="false"/></requestedPrivileges></security></trustInfo></assembly>'
    $xml.Save($AppManifest)
}
[xml] $app = Get-Content $AppManifest -Encoding UTF8
$asmNs = 'urn:schemas-microsoft-com:asm.v1'
$winrtNs = 'urn:schemas-microsoft-com:winrt.v1'
$root = $app.DocumentElement

$fileElem = $app.CreateElement('file', $asmNs)
$fileElem.SetAttribute('name', $fileName)

foreach ($ac in $ips.SelectNodes('a:ActivatableClass', $ns)) {
    $classId = $ac.GetAttribute('ActivatableClassId')
    $threading = $ac.GetAttribute('ThreadingModel')
    if ([string]::IsNullOrEmpty($threading)) { $threading = 'both' }
    $ace = $app.CreateElement('activatableClass', $winrtNs)
    $ace.SetAttribute('name', $classId)
    $ace.SetAttribute('threadingModel', $threading.ToLower())
    [void] $fileElem.AppendChild($ace)
}

[void] $root.AppendChild($fileElem)

$mergedPath = Join-Path $OutDir 'app.merged.manifest'
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$settings.OmitXmlDeclaration = $false
$xw = [System.Xml.XmlWriter]::Create($mergedPath, $settings)
$app.Save($xw)
$xw.Dispose()

Remove-Item $stageDir -Recurse -Force
Write-Host "Mux payload prepared in $OutDir (arch=$Architecture)"
