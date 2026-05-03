// See https://aka.ms/new-console-template for more information
using HostWindowTest;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

var app = new App();

var notepad = Process.Start("notepad");

notepad.WaitForInputIdle();
var mainWindowHandle = notepad.MainWindowHandle;

var attachWindow = new CoreIsland.HostedIsland(mainWindowHandle)
{ 
    Content = new TextBlock() 
    {
        Text = "sjdalfkjaslkdfj",
        HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right,
    }
};


app.Run();