<# 
    Trims the Microsoft.UI.Xaml resources.pri by removing XBF variants
    for obsolete Windows versions (compact, v1, rs2~rs5), keeping only
    19h1 and 21h1 XBF resources. Mirrors Magpie's WinUI.targets approach.
#>
param(
    [Parameter(Mandatory)] [string] $InputPri,
    [Parameter(Mandatory)] [string] $OutputPri
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $InputPri)) {
    Write-Host "Trim-MuxPri: input not found, skipping: $InputPri"
    exit 0
}

# Resolve to absolute path (MSBuild may pass relative paths)
$InputPri = (Resolve-Path $InputPri).Path
$OutputPri = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPri)

# Locate makepri.exe from the Windows SDK build tools in NuGet cache
$makepri = $null

# Search NuGet cache for makePRI
$sdkDirs = @(Get-ChildItem "$env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools" -Directory -ErrorAction SilentlyContinue | 
    Sort-Object Name -Descending)
foreach ($d in $sdkDirs) {
    $candidate = Join-Path $d.FullName "bin\10.0.*\x64\makepri.exe" -Resolve -ErrorAction SilentlyContinue
    if ($candidate) { $makepri = $candidate; break }
}

if (-not $makepri) {
    # Fallback: Windows SDK
    $sdkBin = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    $sdkVer = @(Get-ChildItem $sdkBin -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending)
    if ($sdkVer.Count -gt 0) {
        $makepri = Join-Path $sdkBin "$($sdkVer[0].Name)\x64\makepri.exe"
    }
}

if (-not $makepri -or -not (Test-Path $makepri)) {
    throw "Trim-MuxPri: cannot find makepri.exe"
}

$workDir = Split-Path $InputPri -Parent
$dumpXml = Join-Path $workDir "resources.pri.xml"
$configXml = Join-Path $workDir "mux_priconfig.xml"

Write-Host "Trim-MuxPri: dumping $InputPri ..."

# Step 1: Dump PRI to XML (makepri always outputs to resources.pri.xml)
Push-Location $workDir
try {
    $null = & $makepri dump /if $InputPri /dt detailed /o 2>&1
} finally { Pop-Location }
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $dumpXml)) {
    throw "Trim-MuxPri: dump failed"
}

# Step 2: Strip obsolete XBF variants (same logic as Magpie's WinUI.targets)
[xml] $xml = Get-Content $dumpXml -Encoding UTF8

foreach ($node in $xml.SelectNodes("//NamedResource")) {
    $name = $node.GetAttribute("name")
    if (-not $name -or -not $name.EndsWith(".xbf")) { continue }

    # Remove variants for compact themes, v1, and rs2~rs5 (keep 19h1, 21h1 only)
    $strip = $false
    foreach ($key in @("compact", "Compact", "v1", "rs2", "rs3", "rs4", "rs5")) {
        if ($name.Contains($key)) {
            $base64 = $node.SelectSingleNode("Candidate/Base64Value")
            if ($base64) {
                # Replace content with a single space (Base64 "IA==") to keep structure but minimize size
                $base64.InnerText = "IA=="
            }
            $strip = $true
            break
        }
    }
}

# Also strip the Scale=100 variants that duplicate Scale=200
# (keeps file smaller — not in original Magpie but safe)
foreach ($node in $xml.SelectNodes("//NamedResource")) {
    $name = $node.GetAttribute("name")
    if ($name -and $name.Contains("scale-100")) {
        $base64 = $node.SelectSingleNode("Candidate/Base64Value")
        if ($base64) { $base64.InnerText = "IA==" }
    }
}

$xml.Save($dumpXml)

# Step 3: Create priconfig.xml
@"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<resources targetOsVersion="10.0.0" majorVersion="1">
  <packaging>
    <autoResourcePackage qualifier="Scale" />
    <autoResourcePackage qualifier="DXFeatureLevel" />
  </packaging>
  <index startIndexAt="resources.pri.xml" root="">
    <default>
      <qualifier name="Language" value="en-US" />
      <qualifier name="Contrast" value="standard" />
      <qualifier name="Scale" value="200" />
      <qualifier name="HomeRegion" value="001" />
      <qualifier name="TargetSize" value="256" />
      <qualifier name="LayoutDirection" value="LTR" />
      <qualifier name="DXFeatureLevel" value="DX9" />
      <qualifier name="Configuration" value="" />
      <qualifier name="AlternateForm" value="" />
      <qualifier name="Platform" value="UAP" />
    </default>
    <indexer-config type="priinfo" emitStrings="true" emitPaths="true" emitEmbeddedData="true" />
  </index>
</resources>
"@ | Set-Content $configXml -Encoding UTF8

# Step 4: Repack trimmed PRI
Write-Host "Trim-MuxPri: repacking ..."
Push-Location $workDir
try {
    $outName = Split-Path $OutputPri -Leaf
    $null = & $makepri new /pr . /of $outName /cf mux_priconfig.xml /in Microsoft.UI.Xaml /o 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Trim-MuxPri: repack failed"
    }
} finally {
    Pop-Location
}

# Step 5: Cleanup temp files
Remove-Item $dumpXml -Force -ErrorAction SilentlyContinue
Remove-Item $configXml -Force -ErrorAction SilentlyContinue

$inSize = [math]::Round((Get-Item $InputPri).Length / 1KB, 1)
$outSize = [math]::Round((Get-Item $OutputPri).Length / 1KB, 1)
Write-Host "Trim-MuxPri: done — ${inSize}KB → ${outSize}KB"
