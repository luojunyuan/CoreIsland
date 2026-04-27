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
