using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

internal static partial class PInvoke // GetWindowLong
{
    // https://github.com/microsoft/CsWin32/issues/528

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static partial int GetWindowLong32(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static partial nint GetWindowLong64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

    public static nint GetWindowLongAnyCPU(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex) =>
        nint.Size == 8
            ? GetWindowLong64(hWnd, nIndex)
            : GetWindowLong32(hWnd, nIndex);
}