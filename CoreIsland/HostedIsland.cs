using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    private const string ClassName = "CoreIsland_HostedWnd";
    private const uint WM_CLEANUP = PInvoke.WM_USER + 1;
    private const uint TIMER_ID = 1;
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
        if (msg == PInvoke.WM_NCCREATE)
        {
            var cs = (CREATESTRUCTW*)lParam.Value;
            var pSelf = (nint)cs->lpCreateParams;
            PInvoke.SetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_USERDATA, pSelf);
        }
        else
        {
            var userData = PInvoke.GetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
            if (userData != 0 && GCHandle.FromIntPtr(userData).Target is HostedIsland self)
            {
                if (msg == WM_CLEANUP)
                {
                    Console.WriteLine("CLEAN");
                    self._xamlHost?.Dispose();
                    PInvoke.DestroyWindow(hwnd);
                    return default;
                }

                if (msg == PInvoke.WM_DESTROY)
                {
                    Console.WriteLine("DESTORY");
                    if (self._selfHandle.IsAllocated)
                        self._selfHandle.Free();
                    s_current = null;
                    PInvoke.PostQuitMessage(0);
                    return default;
                }
            }
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}

public unsafe partial class HostedIsland
{
    private readonly DesktopWindowXamlSource _xamlHost = new();
    private readonly GCHandle _selfHandle;
    private readonly HWND _xamlHwnd;
    private readonly HWND _hostHwnd;
    private readonly HWND _fakeHwnd;
    private readonly UnhookWinEventSafeHandle _winEventHook;
    private bool _detached;

    private static HostedIsland? s_current;

    public HostedIsland(nint hostHwnd)
    {
        _hostHwnd = (HWND)hostHwnd;

        _selfHandle = GCHandle.Alloc(this);
        _fakeHwnd = PInvoke.CreateWindowEx(
            dwExStyle: WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW,
            lpClassName: ClassName,
            lpWindowName: null,
            dwStyle: WINDOW_STYLE.WS_POPUP,
            X: 0,
            Y: 0,
            nWidth: 1,
            nHeight: 1,
            hWndParent: default,
            hMenu: default,
            hInstance: s_hModule,
            lpParam: (void*)GCHandle.ToIntPtr(_selfHandle));

        if (_fakeHwnd.IsNull)
            throw new Win32Exception();

        var nativeSource = _xamlHost.As<IDesktopWindowXamlSourceNative2>();
        nativeSource.AttachToWindow(_fakeHwnd);
        nativeSource.GetWindowHandle(out _xamlHwnd);

        PInvoke.SetParent(_xamlHwnd, _hostHwnd);

        s_current = this;
        var tid = PInvoke.GetWindowThreadProcessId(_hostHwnd, out var pid);
        _winEventHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_HIDE,
            PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
            default,
            (delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void>)&WinEventProc,
            idProcess: pid,
            idThread: tid,
            PInvoke.WINEVENT_OUTOFCONTEXT | PInvoke.WINEVENT_SKIPOWNPROCESS);

        EnableResizeLayoutSynchronization(_hostHwnd, true);

        PInvoke.GetClientRect(_hostHwnd, out RECT cr);
        PInvoke.SetWindowPos(_xamlHwnd, default, cr.X, cr.Y, cr.Width, cr.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
    }

    public UIElement Content
    {
        get => _xamlHost.Content;
        set => _xamlHost.Content = value;
    }

    private IFrameworkApplicationPrivate FrameworkAppPrivate { get; } = Windows.UI.Xaml.Application.Current.As<IFrameworkApplicationPrivate>();

    private void Detach()
    {
        if (_detached)
            return;
        _detached = true;

        PInvoke.SetParent(_xamlHwnd, _fakeHwnd);

        try { _winEventHook.Close(); } catch (Exception ex) { Debug.WriteLine(ex.Message); }

        PInvoke.PostMessage(_fakeHwnd, WM_CLEANUP, default, default);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (s_current is not { } self || hwnd != self._hostHwnd || idObject != 0)
            return;

        if (eventType is PInvoke.EVENT_OBJECT_HIDE or PInvoke.EVENT_OBJECT_DESTROY)
        {
            Console.WriteLine(eventType.ToString());
            self.Detach();
            return;
        }

        // EVENT_OBJECT_LOCATIONCHANGE: host moved or resized → re-layout children
        PInvoke.GetClientRect(self._hostHwnd, out RECT cr);
        PInvoke.SetWindowPos(self._xamlHwnd, default, cr.X, cr.Y, cr.Width, cr.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);

        nint lParam = (cr.Height << 16) | (cr.Width & 0xFFFF);
        PInvoke.SendMessage(Application.CoreHwnd, PInvoke.WM_SIZE, (WPARAM)0 /* SIZE_RESTORED */, (LPARAM)lParam);

        self.FrameworkAppPrivate.SetSynchronizationWindow(self._hostHwnd);
    }

    [LibraryImport("user32.dll", EntryPoint = "#2615", SetLastError = false)]
    private static partial void EnableResizeLayoutSynchronization(HWND hwnd, [MarshalAs(UnmanagedType.Bool)] bool enable);
}
