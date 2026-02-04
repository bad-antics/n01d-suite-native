using System.Windows;
using LibVLCSharp.Shared;

namespace N01D.Media;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize LibVLC
        LibVLCSharp.Shared.Core.Initialize();
        
        // Check for file argument (open with support)
        if (e.Args.Length > 0)
        {
            var filePath = e.Args[0];
            ((MainWindow)MainWindow).OpenFile(filePath);
        }
    }
}
