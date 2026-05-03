using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

[GeneratedComInterface, Guid("06636C29-5A17-458D-8EA2-2422D997A922")]
partial interface IXamlSourceTransparency
{
    void GetIids(out int iidCount, out nint iids);
    void GetRuntimeClassName(out nint className);
    void GetTrustLevel(out int trustLevel);

    [return: MarshalAs(UnmanagedType.I1)] bool GetIsBackgroundTransparent();
    void SetIsBackgroundTransparent([MarshalAs(UnmanagedType.I1)] bool isBackgroundTransparent);
}

static class IXamlSourceTransparencyExtensions
{
    extension(IXamlSourceTransparency arg)
    {
        public bool IsBackgroundTransparent
        {
            get => arg.GetIsBackgroundTransparent();
            set => arg.SetIsBackgroundTransparent(value);
        }
    }
}

[GeneratedComInterface, Guid("45D64A29-A63E-4CB6-B498-5781D298CB4F")]
partial interface ICoreWindowInterop
{
    [PreserveSig]
    int GetWindowHandle(out HWND hwnd);
}

[GeneratedComInterface, Guid("e3dcd8c7-3057-4692-99c3-7b7720afda31")]
partial interface IDesktopWindowXamlSourceNative2
{
    [PreserveSig]
    int AttachToWindow(HWND hwnd);

    [PreserveSig]
    int GetWindowHandle(out HWND hwnd);

    [PreserveSig]
    int PreTranslateMessage(ref MSG msg, [MarshalAs(UnmanagedType.Bool)] out bool result);
}

[GeneratedComInterface, Guid("B3AB45D8-6A4E-4E76-A00D-32D4643A9F1A")]
partial interface IFrameworkApplicationPrivate
{
    void Reserved3();
    void Reserved4();
    void Reserved5();
    void Reserved6();
    void Reserved7();
    void Reserved8();
    void Reserved9();
    void Reserved10();

    [PreserveSig]
    int SetSynchronizationWindow(HWND hwnd);
}
