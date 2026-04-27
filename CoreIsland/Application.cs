using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Markup;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CoreIsland;

public partial class Application : Windows.UI.Xaml.Application
{
    private static WindowsXamlManager? s_xamlManager;

    /// <summary>MUX metadata provider — injected into generated XamlTypeInfo as a fallback.</summary>
    public static IXamlMetadataProvider? s_muxProvider;

    public static IXamlType? MuxResolveType(string fullName) =>
        s_muxProvider?.GetXamlType(fullName);

    public static IXamlType? MuxResolveType(Type type) =>
        s_muxProvider?.GetXamlType(type);

    public static XmlnsDefinition[] MuxGetXmlnsDefinitions() =>
        s_muxProvider?.GetXmlnsDefinitions() ?? [];

    public void Initialize()
    {
        s_xamlManager = WindowsXamlManager.InitializeForCurrentThread();
    }

    public int Run()
    {
        MSG msg;
        while (PInvoke.GetMessage(out msg, default, 0, 0).Value > 0)
        {
            PInvoke.TranslateMessage(in msg);
            PInvoke.DispatchMessage(in msg);
        }

        return (int)msg.wParam.Value;
    }
}
