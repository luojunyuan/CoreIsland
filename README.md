# CoreIsland

A .NET library that enables building **AOT-compiled Win32 desktop apps with WinUI 2** via XAML Islands — no UWP packaging or MSIX required.

It replaces UWP's `Application.Start()` / runtime marshalling with a custom `Application`, a Win32 `Window` hosting `DesktopWindowXamlSource`, source-generated COM interop, and build-time MSBuild extensions — all compatible with .NET Native AOT.

```powershell
# Build PublishAot
msbuild .\App1\App1\App1.csproj /t:Publish /p:PublishProfile=win-x64 /p:Platform=x64 /p:Configuration=Release
```

### Features (what CoreIsland has done)

1. Smooth resize synchronization with the XAML framework
2. Build-time XBF→PRI embedding so XAML resources are not loose files
3. Optional Microsoft.UI.Xaml (WinUI 2) support via NuGet
4. Restore certain Windows 10 behaviors to match Windows 11

### TODO
- [x] reduce "Microsoft.UI.Xaml.pri" size
- [ ] ExtendsContentIntoTitleBar
- [x] release nuget package
- [ ] fix when PublishAot on msix packaged

### Looking for a C++ version?
Also check [Blinue/Xaml-Islands-Cpp](https://github.com/Blinue/Xaml-Islands-Cpp); this project is inspired by it.
