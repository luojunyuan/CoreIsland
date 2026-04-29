# CoreIsland — AI Agent Reference

XAML Islands host library for unpackaged .NET 10 desktop apps.

- **CoreIsland** — library providing `CoreIsland.Application` + `CoreIsland.Window`.
- **App1** — sample/demo app.

## UWP XAML, NOT WinUI 3

All XAML types are `Windows.UI.Xaml.*`. Never use WinUI 3's `Microsoft.UI.Xaml.*`.

MUXC (WinUI 2, the `Microsoft.UI.Xaml` NuGet package) is an **extension controls library** that layers on top of UWP. It has its own `Microsoft.UI.Xaml.Controls` namespace — this is **MUXC's namespace, not WinUI 3's**, and it's the only `Microsoft.*` namespace used in this project. E.g. `<XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />` pulls Win11 control styles into an otherwise pure UWP app.

## Build

The **startup project** (App1) is a UWP app — it must be built with MSBuild, not `dotnet` CLI.

The CoreIsland library itself can be built with either MSBuild or `dotnet`.

```powershell
# ✅ Correct — MSBuild for the startup project
msbuild CoreIsland.slnx -p:Platform=x64 -p:Configuration=Debug
msbuild CoreIsland.slnx -p:Platform=ARM64 -p:Configuration=Release

# ✅ Also fine — dotnet for the library only
dotnet build CoreIsland\CoreIsland.csproj

# ❌ Wrong — dotnet for the UWP startup project
dotnet build App1\App1\App1.csproj
dotnet build CoreIsland.slnx
```

## File Encoding

Always create or save files with **UTF-8 BOM** encoding and **CRLF** line endings. This is required for compatibility with MSBuild / Windows tooling in this repository.

