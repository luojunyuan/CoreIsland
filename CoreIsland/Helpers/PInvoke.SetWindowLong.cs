using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

internal static partial class PInvoke // SetWindowLong
{
    // https://github.com/microsoft/CsWin32/issues/528

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static partial int SetWindowLong32(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, int dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static partial nint SetWindowLong64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);

    public static nint SetWindowLongAnyCPU(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong) =>
        nint.Size == 8
            ? SetWindowLong64(hWnd, nIndex, dwNewLong)
            : SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
}