using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace CoreIsland;

public unsafe partial class Window
{
    private const string ClassName = "CoreIsland_Wnd";
    private static readonly FreeLibrarySafeHandle s_hModule = PInvoke.GetModuleHandle();
    private static readonly WNDPROC s_wndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)&StaticWndProc;

    static Window()
    {
        fixed (char* pClassName = ClassName)
        {
            WNDCLASSEXW wc = new()
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = s_wndProc,
                hInstance = (HINSTANCE)s_hModule.DangerousGetHandle(),
                //hCursor = PInvoke.LoadCursor(default, PInvoke.IDC_ARROW),
                //hbrBackground = (HBRUSH)(IntPtr)(COLOR_WINDOW + 1),
                lpszClassName = pClassName,
            };

            if (PInvoke.RegisterClassEx(in wc) == 0)
                throw new Win32Exception();
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT StaticWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == PInvoke.WM_NCCREATE)
        {
            var cs = (CREATESTRUCTW*)lParam.Value;
            var pSelf = (nint)cs->lpCreateParams;
            PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_USERDATA, pSelf);

            if (GCHandle.FromIntPtr(pSelf).Target is Window self)
                self._hwnd = hwnd;
        }
        else
        {
            var userData = PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
            if (userData != 0 && GCHandle.FromIntPtr(userData).Target is Window self)
                return self.WndProc(msg, wParam, lParam);
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}

public unsafe partial class Window
{
    private const string DefaultTitle = "CoreIsland";

    private readonly DesktopWindowXamlSource _xamlHost = new();
    private readonly HWND _hwndDWXS;
    private readonly HWND _islandHwnd;
    private readonly GCHandle _selfHandle;
    private HWND _hwnd;

    public Window()
    {
        CoreWindow.GetForCurrentThread().As<ICoreWindowInterop>().GetWindowHandle(out _hwndDWXS);

        _selfHandle = GCHandle.Alloc(this);
        var hwnd = PInvoke.CreateWindowEx(
            dwExStyle: 0,
            lpClassName: ClassName,
            lpWindowName: DefaultTitle,
            dwStyle: WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
            X: PInvoke.CW_USEDEFAULT,
            Y: PInvoke.CW_USEDEFAULT,
            nWidth: PInvoke.CW_USEDEFAULT,
            nHeight: PInvoke.CW_USEDEFAULT,
            hWndParent: default,
            hMenu: default,
            hInstance: s_hModule,
            lpParam: (void*)GCHandle.ToIntPtr(_selfHandle));

        if (hwnd.IsNull)
            throw new Win32Exception();

        var nativeSource = _xamlHost.As<IDesktopWindowXamlSourceNative2>();
        nativeSource.AttachToWindow(_hwnd);
        nativeSource.GetWindowHandle(out _islandHwnd);

        var border = new Windows.UI.Xaml.Controls.Border()
        {
            BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red),
            BorderThickness = new Windows.UI.Xaml.Thickness(1),
            Child = new Windows.UI.Xaml.Controls.TextBlock()
            {
                Text = "XAML Islands 已加载！",
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right,
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Bottom,
            }
        };

        _xamlHost.Content = border;

        EnableResizeLayoutSynchronization(_hwnd, true);
    }

    public UIElement Content 
    { 
        get => _xamlHost.Content; 
        set => _xamlHost.Content = value; 
    }

    public void Show()
    {
        PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        PInvoke.UpdateWindow(_hwnd);
    }

    private IFrameworkApplicationPrivate FrameworkAppPrivate { get; } = Windows.UI.Xaml.Application.Current.As<IFrameworkApplicationPrivate>();

    private LRESULT WndProc(uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_SIZE when wParam.Value != PInvoke.SIZE_MINIMIZED:
                PInvoke.GetClientRect(_hwnd, out RECT cr);
                PInvoke.SetWindowPos(_islandHwnd, default, cr.X, cr.Y, cr.Width, cr.Height,
                    SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);

                PInvoke.SendMessage(_hwndDWXS, PInvoke.WM_SIZE, wParam, lParam);

                FrameworkAppPrivate.SetSynchronizationWindow(_hwnd);
                return default;

            case PInvoke.WM_DESTROY:
                _xamlHost?.Dispose();
                if (_selfHandle.IsAllocated)
                    _selfHandle.Free();
                PInvoke.PostQuitMessage(0);
                return default;
        }

        return PInvoke.DefWindowProc(_hwnd, msg, wParam, lParam);
    }

    [LibraryImport("user32.dll", EntryPoint = "#2615", SetLastError = false)]
    private static partial void EnableResizeLayoutSynchronization(HWND hwnd, [MarshalAs(UnmanagedType.Bool)] bool enable);
}
