using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace CoreIsland;


public unsafe partial class HostedIsland
{
    private const string ClassName = "CoreIsland_Wnd";
    private static readonly FreeLibrarySafeHandle s_hModule = PInvoke.GetModuleHandle();
    private static readonly WNDPROC s_wndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)&StaticWndProc;

    static HostedIsland()
    {
        fixed (char* pClassName = ClassName)
        {
            WNDCLASSEXW wc = new()
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = s_wndProc,
                hInstance = (HINSTANCE)s_hModule.DangerousGetHandle(),
                lpszClassName = pClassName,
            };

            if (PInvoke.RegisterClassEx(in wc) == 0)
                throw new Win32Exception();
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT StaticWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}

public unsafe partial class HostedIsland
{
    private readonly DesktopWindowXamlSource _xamlHost = new();
    private readonly GCHandle _selfHandle;
    private readonly HWND _coreHwnd;
    private readonly HWND _xamlHwnd;
    private readonly HWND _hostHwnd;
    private readonly UnhookWinEventSafeHandle _winEventHook;

    private static HostedIsland? s_current;

    public HostedIsland(nint hostHwnd)
    {
        _hostHwnd = (HWND)hostHwnd;

        CoreWindow.GetForCurrentThread()
            .HideWindowInWin10(out _coreHwnd);

        _selfHandle = GCHandle.Alloc(this);
        var fakeHwnd = PInvoke.CreateWindowEx(
            dwExStyle: WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP,
            lpClassName: ClassName,
            lpWindowName: "FakeHostIsland",
            dwStyle: WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
            X: PInvoke.CW_USEDEFAULT,
            Y: PInvoke.CW_USEDEFAULT,
            nWidth: PInvoke.CW_USEDEFAULT,
            nHeight: PInvoke.CW_USEDEFAULT,
            hWndParent: default,
            hMenu: default,
            hInstance: s_hModule,
            lpParam: (void*)GCHandle.ToIntPtr(_selfHandle));

        if (fakeHwnd.IsNull)
            throw new Win32Exception();

        var nativeSource = _xamlHost.As<IDesktopWindowXamlSourceNative2>();
        nativeSource.AttachToWindow(fakeHwnd);
        nativeSource.GetWindowHandle(out _xamlHwnd);

        // Reparent both XAML island HWND and CoreWindow HWND as children of the host
        SetChildWindow(_xamlHwnd, _hostHwnd);
        SetChildWindow(_coreHwnd, _hostHwnd);

        // Install a WinEvent hook to watch the host for resize/destroy (cross-process safe).
        // idProcess=0 & idThread=0 → monitor all processes/threads; filter by HWND in callback.
        s_current = this;
        _winEventHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_DESTROY,
            PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
            default,
            (delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void>)&WinEventProc,
            idProcess: 0,
            idThread: 0,
            PInvoke.WINEVENT_OUTOFCONTEXT);

        EnableResizeLayoutSynchronization(_hostHwnd, true);
    }

    /// <summary>Remove WS_POPUP, add WS_CHILD, and SetParent to the host.</summary>
    private static void SetChildWindow(HWND hwnd, HWND parent)
    {
        var style = PInvoke.GetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        style &= ~(nint)WINDOW_STYLE.WS_POPUP;
        style |= (nint)WINDOW_STYLE.WS_CHILD;
        PInvoke.SetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, style);
        PInvoke.SetParent(hwnd, parent);
    }

    public UIElement Content
    {
        get => _xamlHost.Content;
        set => _xamlHost.Content = value;
    }

    private IFrameworkApplicationPrivate FrameworkAppPrivate { get; } = Windows.UI.Xaml.Application.Current.As<IFrameworkApplicationPrivate>();

    /// <summary>WinEvent hook — invoked by the message pump when the host window moves, resizes, or is destroyed.</summary>
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (s_current is not { } self || hwnd != self._hostHwnd || idObject != 0)
            return;

        if (eventType == PInvoke.EVENT_OBJECT_DESTROY)
        {
            self._winEventHook.Close();
            self._xamlHost?.Dispose();
            if (self._selfHandle.IsAllocated)
                self._selfHandle.Free();
            s_current = null;
            PInvoke.PostQuitMessage(0);
            return;
        }

        // EVENT_OBJECT_LOCATIONCHANGE: host moved or resized → re-layout children
        PInvoke.GetClientRect(self._hostHwnd, out RECT cr);
        PInvoke.SetWindowPos(self._xamlHwnd, default, cr.X, cr.Y, cr.Width, cr.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);

        nint lParam = (cr.Height << 16) | (cr.Width & 0xFFFF);
        PInvoke.SendMessage(self._coreHwnd, PInvoke.WM_SIZE, (WPARAM)0 /* SIZE_RESTORED */, (LPARAM)lParam);

        self.FrameworkAppPrivate.SetSynchronizationWindow(self._hostHwnd);
    }

    [LibraryImport("user32.dll", EntryPoint = "#2615", SetLastError = false)]
    private static partial void EnableResizeLayoutSynchronization(HWND hwnd, [MarshalAs(UnmanagedType.Bool)] bool enable);
}
