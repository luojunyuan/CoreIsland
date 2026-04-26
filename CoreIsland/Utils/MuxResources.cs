using System;
using System.Runtime.InteropServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using WinRT;

namespace CoreIsland.Utils;

/// <summary>
/// Brings up the WinUI 2.8 (Microsoft.UI.Xaml) control styles in an unpackaged .NET 10 app.
///
/// We ship Microsoft.UI.Xaml.dll + resources.pri next to the exe, plus an embedded
/// Fusion side-by-side activation manifest listing every MUX activatable class so
/// Windows 10 1903+ can resolve RoGetActivationFactory without a packaged AppX context.
/// The resources.pri is the MUX framework's own PRI renamed so the MRT loader picks
/// it up as the current exe's resource map, letting ms-appx:///Microsoft.UI.Xaml/...
/// URIs embedded in themeresources.xbf resolve.
/// </summary>
public static unsafe partial class MuxResources
{
    private static readonly Guid IID_IXamlControlsResources3 =
        new("3BA1468F-22AB-520B-8D17-9CBB3EEE950C");

    private const int VtblPutControlsResourcesVersion = 7;
    private const int ControlsResourcesVersion_Version2 = 1;

    private static bool s_applied;

    public static void Apply()
    {
        if (s_applied) return;

        if (NativeMethods.LoadLibraryExW("Microsoft.UI.Xaml.dll", default, 0) == 0)
        {
            int err = Marshal.GetLastPInvokeError();
            throw new InvalidOperationException(
                $"Failed to load Microsoft.UI.Xaml.dll (GetLastError=0x{err:X8}).");
        }

        nint hstring = 0;
        Marshal.ThrowExceptionForHR(NativeMethods.WindowsCreateString(
            "Microsoft.UI.Xaml.Controls.XamlControlsResources",
            (uint)"Microsoft.UI.Xaml.Controls.XamlControlsResources".Length,
            out hstring));

        nint pFactory = 0;
        nint pInstance = 0;
        try
        {
            Guid iidActivationFactory = new("00000035-0000-0000-C000-000000000046");
            int hr = NativeMethods.RoGetActivationFactory(hstring, in iidActivationFactory, out pFactory);
            if (hr < 0)
            {
                throw new InvalidOperationException(
                    $"RoGetActivationFactory(XamlControlsResources) failed hr=0x{hr:X8}.");
            }

            void** vtblFactory = *(void***)pFactory;
            var activateInstance = (delegate* unmanaged<nint, nint*, int>)vtblFactory[6];
            hr = activateInstance(pFactory, &pInstance);
            Marshal.ThrowExceptionForHR(hr);

            Guid iid3 = IID_IXamlControlsResources3;
            nint pRes3 = 0;
            hr = Marshal.QueryInterface(pInstance, in iid3, out pRes3);
            if (hr >= 0 && pRes3 != 0)
            {
                try
                {
                    void** vtblRes3 = *(void***)pRes3;
                    var putVersion = (delegate* unmanaged<nint, int, int>)
                        vtblRes3[VtblPutControlsResourcesVersion];
                    Marshal.ThrowExceptionForHR(putVersion(pRes3, ControlsResourcesVersion_Version2));
                }
                finally { Marshal.Release(pRes3); }
            }

            nint transferred = pInstance;
            pInstance = 0;
            var rd = MarshalInspectable<ResourceDictionary>.FromAbi(transferred);
            // FromAbi takes ownership of our AddRef; do NOT call Marshal.Release.

            Application.Current.Resources.MergedDictionaries.Add(rd);
            s_applied = true;
        }
        finally
        {
            if (pInstance != 0) Marshal.Release(pInstance);
            if (pFactory != 0) Marshal.Release(pFactory);
            if (hstring != 0) NativeMethods.WindowsDeleteString(hstring);
        }
    }

    /// <summary>
    /// 激活 MUX 自带的 XAML 元数据提供者
    /// <c>Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider</c>，
    /// 用于告诉 WUX XAML 解析器（<see cref="XamlReader.Load(string)"/>）哪些命名空间
    /// 由 MUX 实现。C++ 版里由 cppwinrt 生成的 XamlMetaDataProvider 会聚合这个类型；
    /// 这里手工创建以便 <c>App.xaml</c> 引用 <c>muxc:XamlControlsResources</c> 时
    /// 解析器能找到对应类型。
    /// </summary>
    public static IXamlMetadataProvider CreateMuxMetadataProvider()
    {
        if (NativeMethods.LoadLibraryExW("Microsoft.UI.Xaml.dll", default, 0) == 0)
        {
            int err = Marshal.GetLastPInvokeError();
            throw new InvalidOperationException(
                $"Failed to load Microsoft.UI.Xaml.dll (GetLastError=0x{err:X8}).");
        }

        const string className = "Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider";
        nint hstring = 0;
        Marshal.ThrowExceptionForHR(NativeMethods.WindowsCreateString(
            className, (uint)className.Length, out hstring));

        nint pFactory = 0;
        nint pInstance = 0;
        try
        {
            Guid iidActivationFactory = new("00000035-0000-0000-C000-000000000046");
            int hr = NativeMethods.RoGetActivationFactory(hstring, in iidActivationFactory, out pFactory);
            if (hr < 0)
            {
                throw new InvalidOperationException(
                    $"RoGetActivationFactory({className}) failed hr=0x{hr:X8}.");
            }

            void** vtblFactory = *(void***)pFactory;
            var activateInstance = (delegate* unmanaged<nint, nint*, int>)vtblFactory[6];
            hr = activateInstance(pFactory, &pInstance);
            Marshal.ThrowExceptionForHR(hr);

            nint transferred = pInstance;
            pInstance = 0;
            return MarshalInspectable<IXamlMetadataProvider>.FromAbi(transferred);
        }
        finally
        {
            if (pInstance != 0) Marshal.Release(pInstance);
            if (pFactory != 0) Marshal.Release(pFactory);
            if (hstring != 0) NativeMethods.WindowsDeleteString(hstring);
        }
    }

    private static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll", SetLastError = false)]
        public static partial nint LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName, nint hFile, uint dwFlags);

        [LibraryImport("combase.dll")]
        public static partial int RoGetActivationFactory(
            nint activatableClassId,
            in Guid iid,
            out nint factory);

        [LibraryImport("combase.dll")]
        public static partial int WindowsCreateString(
            [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
            uint length,
            out nint hstring);

        [LibraryImport("combase.dll")]
        public static partial int WindowsDeleteString(nint hstring);
    }
}
