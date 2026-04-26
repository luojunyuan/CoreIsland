using System;

namespace App1;

public class Program
{
    [STAThread]
    private static int Main()
    {
        var app = new App();
        app.Initialize();
        return app.Run();
    }
}
