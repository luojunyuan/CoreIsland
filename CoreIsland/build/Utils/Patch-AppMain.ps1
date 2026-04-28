<# 
    CoreIsland Patch: replaces auto-generated UWP Application.Start in
    App.g.i.cs with CoreIsland's XAML Islands initialization.
#>
param([Parameter(Mandatory)] [string] $FilePath)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $FilePath)) {
    Write-Host "Patch-AppMain: not found, skipping: $FilePath"
    exit 0
}

$c = Get-Content $FilePath -Raw -Encoding UTF8
$nl = "`r`n"
if ($c -match '\r?\n') { $nl = $matches[0] }

# Match the #if !DISABLE_XAML_GENERATED_MAIN ... #endif block
$pattern = '(?s)#if !DISABLE_XAML_GENERATED_MAIN\s*(.*?)\s*#endif'

$replacement = @"
// CoreIsland Patch: replaced UWP Application.Start with XAML Islands init.
// This file is auto-generated; the patch runs after XAML compilation.
#if !DISABLE_XAML_GENERATED_MAIN
    public static class Program
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 10.0.26100.7705")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.STAThread]
        static int Main(string[] args)
        {
            var app = new App();
            app.Initialize();
            return app.Run();
        }
    }
#endif
"@

if ($c -match $pattern) {
    $c = $c -replace $pattern, $replacement
    Set-Content -Path $FilePath -Value $c -NoNewline
    Write-Host "Patch-AppMain: patched $FilePath"
} else {
    Write-Host "Patch-AppMain: already patched or no Main block, skipping"
}
