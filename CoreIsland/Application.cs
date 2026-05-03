using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace CoreIsland;

public partial class Application : Windows.UI.Xaml.Application
{
    private static WindowsXamlManager? s_xamlManager;

    public void Initialize()
    {
        s_xamlManager = WindowsXamlManager.InitializeForCurrentThread();

        var win = Windows.UI.Xaml.Window.Current;
        win.As<IXamlSourceTransparency>().IsBackgroundTransparent = true;
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
