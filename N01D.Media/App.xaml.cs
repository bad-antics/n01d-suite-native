using System.IO;
using System.Windows;
using LibVLCSharp.Shared;

namespace N01D.Media;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handlers for diagnostics
        DispatcherUnhandledException += (s, args) =>
        {
            File.WriteAllText("crash.log", $"[DISPATCHER] {args.Exception}");
            MessageBox.Show($"N01D Media Error:\n{args.Exception.Message}\n\nSee crash.log for details.",
                "N01D Media - Crash", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            File.WriteAllText("crash.log", $"[DOMAIN] {ex}");
        };

        base.OnStartup(e);

        try
        {
            // Initialize LibVLC
            LibVLCSharp.Shared.Core.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize LibVLC:\n{ex.Message}",
                "N01D Media - VLC Error", MessageBoxButton.OK, MessageBoxImage.Error);
            File.WriteAllText("crash.log", $"[VLC_INIT] {ex}");
            Shutdown(1);
            return;
        }

        // Check for file argument (open with support)
        if (e.Args.Length > 0)
        {
            var filePath = e.Args[0];
            ((MainWindow)MainWindow).OpenFile(filePath);
        }
    }
}
