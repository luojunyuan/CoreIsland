# CoreIsland

XAML Islands host library for unpackaged .NET 10 WinUI/UWP apps.

---

## 1. App Manifest

Add the following to your `app.manifest` to enable XAML Islands support.
The `maxversiontested` entry is required for `WindowsXamlManager` to activate
correctly on Windows 10 1903+.

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1"
          manifestVersion="1.0">
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <!-- Enable XAML Islands -->
      <maxversiontested Id="10.0.18362.0"/>
    </application>
  </compatibility>
</assembly>
```

---

## 2. App.xaml

Replace `<Application>` with `<island:Application>` and declare the
`using:CoreIsland` XML namespace:

```xml
<island:Application
    x:Class="YourApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:island="using:CoreIsland">

</island:Application>
```

---

## 3. App.xaml.cs

Derive from `CoreIsland.Application` and create a `CoreIsland.Window` in
`OnLaunched`. Call `Initialize()` before constructing any XAML types.

```csharp
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;

namespace YourApp
{
    public sealed partial class App : CoreIsland.Application
    {
        private CoreIsland.Window? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = new Frame();
            rootFrame.NavigationFailed += (s, args) =>
                throw new Exception("Failed to load Page " + args.SourcePageType.FullName);

            _window = new CoreIsland.Window()
            {
                Content = rootFrame
            };

            rootFrame.Navigate(typeof(MainPage), e.Arguments);
            _window.Activate();
        }
    }
}
```

---

## 4. .csproj (minimum)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.18362.0</TargetPlatformMinVersion>
    <UseUwp>true</UseUwp>
    <Nullable>enable</Nullable>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <!-- Turn this to true if you want to package your app with WAP -->
    <CoreIslandPackaging>false</CoreIslandPackaging>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CoreIsland" Version="1.0.3" />
  </ItemGroup>
</Project>
```

Learn more: https://github.com/luojunyuan/CoreIsland#readme