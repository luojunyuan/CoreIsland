using Windows.UI.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

internal static class Win10Compact
{
    // REMARK: After InitializeForCurrentThread CoreWindow show in the taskbar and always has a size of 1x1
    public static CoreWindow HideWindowInWin10(this CoreWindow coreWindow, out HWND hwnd)
    {
        coreWindow.As<ICoreWindowInterop>().GetWindowHandle(out hwnd);

        if (Environment.OSVersion.Version.Build < 22000)
        {
            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
        }

        return coreWindow;
    }
}
