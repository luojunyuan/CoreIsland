# Patches generated XamlTypeInfo.g.cs — injects MUX fallback into
# GetXamlTypeByName, GetXamlTypeByType, and GetXmlnsDefinitions.

param([Parameter(Mandatory)] [string] $FilePath)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $FilePath)) {
    Write-Host "Patch-XamlTypeInfo: file not found, skipping: $FilePath"
    exit 0
}

$content = Get-Content $FilePath -Raw

# Determine line ending from the file
$nl = "`r`n"
if ($content -match '\r?\n') { $nl = $matches[0] }

# Patch GetXamlTypeByType: insert fallback before "return xamlType;"
$old1 = "            return xamlType;${nl}        }${nl}${nl}        public global::Windows.UI.Xaml.Markup.IXamlType GetXamlTypeByName"
if ($content -match [regex]::Escape($old1)) {
    $new1 = "            if (xamlType == null) xamlType = global::CoreIsland.Application.MuxResolveType(type);${nl}            return xamlType;${nl}        }${nl}${nl}        public global::Windows.UI.Xaml.Markup.IXamlType GetXamlTypeByName"
    $content = $content.Replace($old1, $new1)
}

# Patch GetXamlTypeByName: insert fallback before "return xamlType;"
$old2 = "            return xamlType;${nl}        }${nl}${nl}        public global::Windows.UI.Xaml.Markup.IXamlMember GetMemberByLongName"
if ($content -match [regex]::Escape($old2)) {
    $new2 = "            if (xamlType == null) xamlType = global::CoreIsland.Application.MuxResolveType(typeName);${nl}            return xamlType;${nl}        }${nl}${nl}        public global::Windows.UI.Xaml.Markup.IXamlMember GetMemberByLongName"
    $content = $content.Replace($old2, $new2)
}

# Patch GetXmlnsDefinitions: replace empty array return
$old3 = "return new global::Windows.UI.Xaml.Markup.XmlnsDefinition[0];"
$new3 = "return global::CoreIsland.Application.MuxGetXmlnsDefinitions();"
$content = $content.Replace($old3, $new3)

Set-Content -Path $FilePath -Value $content -NoNewline
Write-Host "Patch-XamlTypeInfo: patched $FilePath"
