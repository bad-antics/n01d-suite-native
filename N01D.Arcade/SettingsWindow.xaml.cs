using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace N01D.Arcade;

public partial class SettingsWindow : Window
{
    public ArcadeConfig Config { get; private set; }
    private readonly List<EmulatorSystem> _systems;

    public SettingsWindow(ArcadeConfig config, List<EmulatorSystem> systems)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        Config = new ArcadeConfig
        {
            RomsPath = config.RomsPath,
            EmulatorOverrides = new Dictionary<string, string>(config.EmulatorOverrides)
        };
        _systems = systems;

        TxtRomsPath.Text = Config.RomsPath;
        PopulateEmulatorList();
    }

    private void PopulateEmulatorList()
    {
        LstEmulators.Items.Clear();
        foreach (var sys in _systems)
        {
            var installed = File.Exists(sys.EmulatorPath);
            var status = installed ? "✓ INSTALLED" : "✗ NOT FOUND";
            var statusColor = installed ? "#00ff88" : "#ff4444";

            var item = new ListBoxItem
            {
                Content = $"{sys.Icon}  {sys.Name,-22} {sys.EmulatorName,-28} {status}",
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(statusColor)),
                FontSize = 11,
                Padding = new Thickness(4, 3, 4, 3),
                ToolTip = $"Path: {sys.EmulatorPath}\nROMs: {sys.RomPath}\nFormats: {string.Join(", ", sys.Extensions)}"
            };
            LstEmulators.Items.Add(item);
        }
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select ROM Library Root Folder",
            SelectedPath = TxtRomsPath.Text,
            ShowNewFolderButton = true
        };

        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtRomsPath.Text = dlg.SelectedPath;
        }
    }

    private void BtnRescan_Click(object sender, RoutedEventArgs e)
    {
        Config.RomsPath = TxtRomsPath.Text;
        DialogResult = true;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        Config.RomsPath = TxtRomsPath.Text;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
