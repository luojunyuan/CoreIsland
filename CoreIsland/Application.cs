using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace CoreIsland;

public partial class Application : Windows.UI.Xaml.Application
{
    internal static HWND CoreHwnd;
    internal new static Application Current { get; private set; } = null!;

    private readonly WindowsXamlManager _xamlManager;

    protected Application()
    {
        Current = this;

        _xamlManager = WindowsXamlManager.InitializeForCurrentThread();
    }

    protected sealed override void OnLaunched(LaunchActivatedEventArgs e)
    {
        global::Windows.UI.Core.CoreWindow.GetForCurrentThread().HideWindowInWin10(out CoreHwnd);

        var xamlWindowBoundToCoreWindow = global::Windows.UI.Xaml.Window.Current;
        xamlWindowBoundToCoreWindow.As<IXamlSourceTransparency>().IsBackgroundTransparent = true;

        OnIslandLaunched(e);
    }

    protected virtual void OnIslandLaunched(LaunchActivatedEventArgs e) { }

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

    public int Run(Window window)
    {
        window.Activate();
        return Run();
    }
}
