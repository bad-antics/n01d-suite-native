using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace N01D.Term;

public partial class MainWindow : Window
{
    private readonly List<TerminalTab> _tabs = new();
    private int _activeTabIndex = -1;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;
    private readonly string _historyPath;

    private readonly string[] _quickCommands = new[]
    {
        "ls", "dir", "cd ..", "pwd", "cls", "clear",
        "whoami", "hostname", "ipconfig", "ping localhost",
        "netstat -an", "tasklist", "systeminfo",
        "git status", "git log --oneline -5",
        "npm --version", "node --version",
        "python --version", "dotnet --version"
    };

    public MainWindow()
    {
        InitializeComponent();
        
        _historyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".n01d-term-history");
        
        LoadHistory();
        SetupQuickCommands();
        
        txtUser.Text = $"{Environment.UserName}@{Environment.MachineName}";
        
        // Create initial tab
        CreateNewTab();
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                var lines = File.ReadAllLines(_historyPath);
                _history.AddRange(lines.Take(1000));
            }
        }
        catch { }
    }

    private void SaveHistory()
    {
        try
        {
            File.WriteAllLines(_historyPath, _history.TakeLast(1000));
        }
        catch { }
    }

    private void SetupQuickCommands()
    {
        foreach (var cmd in _quickCommands)
        {
            var btn = new Button
            {
                Content = cmd,
                Background = new SolidColorBrush(Color.FromRgb(13, 13, 13)),
                Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 2, 0, 2),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Cursor = Cursors.Hand
            };
            
            btn.Click += (s, e) =>
            {
                if (_activeTabIndex >= 0 && _activeTabIndex < _tabs.Count)
                {
                    var tab = _tabs[_activeTabIndex];
                    tab.InputBox.Text = cmd;
                    tab.InputBox.Focus();
                    tab.InputBox.CaretIndex = cmd.Length;
                }
            };
            
            btn.MouseEnter += (s, e) =>
            {
                btn.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 65));
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 65));
            };
            
            btn.MouseLeave += (s, e) =>
            {
                btn.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            };
            
            quickCommands.Children.Add(btn);
        }
    }

    private void CreateNewTab()
    {
        var tab = new TerminalTab(_tabs.Count + 1);
        
        tab.InputBox.KeyDown += InputBox_KeyDown;
        tab.Process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
                Dispatcher.Invoke(() => AppendOutput(tab, e.Data, false));
        };
        tab.Process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
                Dispatcher.Invoke(() => AppendOutput(tab, e.Data, true));
        };
        
        _tabs.Add(tab);
        UpdateTabBar();
        SwitchToTab(_tabs.Count - 1);
        
        // Show welcome message
        AppendOutput(tab, @"
 ╔═══════════════════════════════════════════════════════════╗
 ║     _   _  ___  _  ____        _____ _____ ____  __  __   ║
 ║    | \ | |/ _ \/ ||  _ \      |_   _| ____|  _ \|  \/  |  ║
 ║    |  \| | | | | || | | |_____  | | |  _| | |_) | |\/| |  ║
 ║    | |\  | |_| | || |_| |_____| | | | |___|  _ <| |  | |  ║
 ║    |_| \_|\___/|_||____/        |_| |_____|_| \_\_|  |_|  ║
 ║                                                           ║
 ║           [ Modern Terminal | bad-antics ]                ║
 ╚═══════════════════════════════════════════════════════════╝
", false);
        AppendOutput(tab, $"Session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}", false);
        AppendOutput(tab, $"Working directory: {tab.WorkingDirectory}\n", false);
    }

    private void UpdateTabBar()
    {
        tabBar.Children.Clear();
        
        for (int i = 0; i < _tabs.Count; i++)
        {
            var index = i;
            var tab = _tabs[i];
            
            var tabButton = new Border
            {
                Background = i == _activeTabIndex 
                    ? new SolidColorBrush(Color.FromRgb(0, 255, 65)) 
                    : new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                BorderThickness = new Thickness(1, 1, 1, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 2, 0),
                Cursor = Cursors.Hand
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var label = new TextBlock
            {
                Text = $"Terminal {tab.TabNumber}",
                Foreground = i == _activeTabIndex 
                    ? new SolidColorBrush(Color.FromRgb(13, 13, 13))
                    : new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);
            
            var closeBtn = new TextBlock
            {
                Text = " ×",
                Foreground = i == _activeTabIndex 
                    ? new SolidColorBrush(Color.FromRgb(13, 13, 13))
                    : new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            Grid.SetColumn(closeBtn, 1);
            
            closeBtn.MouseDown += (s, e) =>
            {
                e.Handled = true;
                CloseTab(index);
            };
            
            grid.Children.Add(label);
            grid.Children.Add(closeBtn);
            tabButton.Child = grid;
            
            tabButton.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    SwitchToTab(index);
            };
            
            tabBar.Children.Add(tabButton);
        }
    }

    private void SwitchToTab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;
        
        _activeTabIndex = index;
        terminalContainer.Children.Clear();
        terminalContainer.Children.Add(_tabs[index].Container);
        
        var tab = _tabs[index];
        txtPid.Text = tab.Process?.Id.ToString() ?? "0";
        
        UpdateTabBar();
        tab.InputBox.Focus();
    }

    private void CloseTab(int index)
    {
        if (_tabs.Count <= 1) return;
        
        var tab = _tabs[index];
        try
        {
            if (!tab.Process.HasExited)
                tab.Process.Kill(true);
        }
        catch { }
        
        _tabs.RemoveAt(index);
        
        if (_activeTabIndex >= _tabs.Count)
            _activeTabIndex = _tabs.Count - 1;
        
        UpdateTabBar();
        SwitchToTab(_activeTabIndex);
    }

    private void BtnNewTab_Click(object sender, RoutedEventArgs e)
    {
        CreateNewTab();
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (_activeTabIndex < 0 || _activeTabIndex >= _tabs.Count) return;
        var tab = _tabs[_activeTabIndex];
        
        switch (e.Key)
        {
            case Key.Enter:
                var command = tab.InputBox.Text;
                if (!string.IsNullOrWhiteSpace(command))
                {
                    _history.Add(command);
                    _historyIndex = _history.Count;
                    ExecuteCommand(tab, command);
                }
                tab.InputBox.Clear();
                e.Handled = true;
                break;
                
            case Key.Up:
                if (_historyIndex > 0)
                {
                    _historyIndex--;
                    tab.InputBox.Text = _history[_historyIndex];
                    tab.InputBox.CaretIndex = tab.InputBox.Text.Length;
                }
                e.Handled = true;
                break;
                
            case Key.Down:
                if (_historyIndex < _history.Count - 1)
                {
                    _historyIndex++;
                    tab.InputBox.Text = _history[_historyIndex];
                    tab.InputBox.CaretIndex = tab.InputBox.Text.Length;
                }
                else
                {
                    _historyIndex = _history.Count;
                    tab.InputBox.Clear();
                }
                e.Handled = true;
                break;
                
            case Key.L when Keyboard.Modifiers == ModifierKeys.Control:
                tab.OutputBox.Clear();
                e.Handled = true;
                break;
        }
    }

    private void ExecuteCommand(TerminalTab tab, string command)
    {
        AppendOutput(tab, $"\n{tab.Prompt}{command}", false);
        
        // Handle built-in commands
        var parts = command.Trim().Split(' ', 2);
        var cmd = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1] : "";
        
        switch (cmd)
        {
            case "cd":
                ChangeDirectory(tab, args);
                return;
            case "clear":
            case "cls":
                tab.OutputBox.Clear();
                return;
            case "exit":
                CloseTab(_activeTabIndex);
                return;
        }
        
        // Execute external command
        try
        {
            tab.Process.StandardInput.WriteLine(command);
        }
        catch (Exception ex)
        {
            AppendOutput(tab, $"Error: {ex.Message}", true);
        }
    }

    private void ChangeDirectory(TerminalTab tab, string path)
    {
        try
        {
            string newPath;
            
            if (string.IsNullOrEmpty(path) || path == "~")
                newPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            else if (path == "..")
                newPath = Directory.GetParent(tab.WorkingDirectory)?.FullName ?? tab.WorkingDirectory;
            else if (Path.IsPathRooted(path))
                newPath = path;
            else
                newPath = Path.GetFullPath(Path.Combine(tab.WorkingDirectory, path));
            
            if (Directory.Exists(newPath))
            {
                tab.WorkingDirectory = newPath;
                tab.Process.StandardInput.WriteLine($"cd \"{newPath}\"");
                AppendOutput(tab, $"Changed directory to: {newPath}", false);
            }
            else
            {
                AppendOutput(tab, $"Directory not found: {path}", true);
            }
        }
        catch (Exception ex)
        {
            AppendOutput(tab, $"Error changing directory: {ex.Message}", true);
        }
    }

    private void AppendOutput(TerminalTab tab, string text, bool isError)
    {
        tab.OutputBox.AppendText(text + "\n");
        tab.OutputBox.ScrollToEnd();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        SaveHistory();
        
        foreach (var tab in _tabs)
        {
            try
            {
                if (!tab.Process.HasExited)
                    tab.Process.Kill(true);
            }
            catch { }
        }
        
        base.OnClosing(e);
    }
}

public class TerminalTab
{
    public int TabNumber { get; }
    public Grid Container { get; }
    public TextBox OutputBox { get; }
    public TextBox InputBox { get; }
    public Process Process { get; }
    public string WorkingDirectory { get; set; }
    
    public string Prompt => $"[N01D@{Environment.MachineName} {Path.GetFileName(WorkingDirectory)}]$ ";

    public TerminalTab(int number)
    {
        TabNumber = number;
        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        Container = new Grid();
        Container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // Output area
        OutputBox = new TextBox
        {
            Background = new SolidColorBrush(Color.FromRgb(13, 13, 13)),
            Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10)
        };
        Grid.SetRow(OutputBox, 0);
        
        // Input area
        var inputGrid = new Grid { Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)) };
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        var promptLabel = new TextBlock
        {
            Text = "$ ",
            Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 65)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(10, 8, 0, 8)
        };
        Grid.SetColumn(promptLabel, 0);
        
        InputBox = new TextBox
        {
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            BorderThickness = new Thickness(0),
            CaretBrush = new SolidColorBrush(Color.FromRgb(0, 255, 65)),
            Padding = new Thickness(5, 8, 10, 8),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(InputBox, 1);
        
        inputGrid.Children.Add(promptLabel);
        inputGrid.Children.Add(InputBox);
        Grid.SetRow(inputGrid, 1);
        
        Container.Children.Add(OutputBox);
        Container.Children.Add(inputGrid);
        
        // Start PowerShell process
        Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoLogo -NoProfile",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = WorkingDirectory
            },
            EnableRaisingEvents = true
        };
        
        Process.Start();
        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
    }
}
