using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Net.NetworkInformation;
using LiveChartsCore.Defaults;

namespace N01D.SysMon;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly PerformanceCounter _cpuCounter;
    private readonly List<ObservableValue> _cpuValues = new();
    private long _lastBytesReceived;
    private long _lastBytesSent;
    private DateTime _lastNetworkCheck;
    private readonly DateTime _startTime;

    public MainWindow()
    {
        InitializeComponent();
        _startTime = DateTime.Now;
        
        // Initialize CPU counter
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _cpuCounter.NextValue(); // First call returns 0
        
        // Initialize CPU chart
        for (int i = 0; i < 60; i++)
            _cpuValues.Add(new ObservableValue(0));
            
        cpuChart.Series = new ISeries[]
        {
            new LineSeries<ObservableValue>
            {
                Values = _cpuValues,
                Fill = new SolidColorPaint(SKColors.LimeGreen.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.LimeGreen, 2),
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };
        
        cpuChart.XAxes = new Axis[] { new Axis { IsVisible = false } };
        cpuChart.YAxes = new Axis[] 
        { 
            new Axis 
            { 
                MinLimit = 0, 
                MaxLimit = 100,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                Labeler = v => $"{v}%"
            } 
        };

        // Initialize memory chart
        memChart.Series = new ISeries[]
        {
            new PieSeries<double> { Values = new double[] { 50 }, Fill = new SolidColorPaint(new SKColor(10, 189, 198)), Name = "Used" },
            new PieSeries<double> { Values = new double[] { 50 }, Fill = new SolidColorPaint(new SKColor(51, 51, 51)), Name = "Free" }
        };
        
        // Get initial network stats
        var stats = GetNetworkStats();
        _lastBytesReceived = stats.received;
        _lastBytesSent = stats.sent;
        _lastNetworkCheck = DateTime.Now;

        // Set hostname
        txtHostname.Text = $"@{Environment.MachineName}";
        
        // Update CPU info
        UpdateCpuInfo();
        
        // Start timer
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        
        // Initial update
        UpdateAllStats();
    }

    private void UpdateCpuInfo()
    {
        try
        {
            int cores = Environment.ProcessorCount;
            int logicalProcessors = Environment.ProcessorCount;
            txtCpuInfo.Text = $"Cores: {cores} | Threads: {logicalProcessors}";
        }
        catch { }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateAllStats();
    }

    private void UpdateAllStats()
    {
        try
        {
            // Update uptime
            var uptime = DateTime.Now - _startTime;
            txtUptime.Text = $"Session: {uptime:hh\\:mm\\:ss}";
            
            // Update CPU
            float cpuUsage = _cpuCounter.NextValue();
            _cpuValues.RemoveAt(0);
            _cpuValues.Add(new ObservableValue(cpuUsage));
            cpuProgress.Value = cpuUsage;
            txtCpuPercent.Text = $"{cpuUsage:F1}%";
            
            // Update Memory
            UpdateMemory();
            
            // Update Disk
            UpdateDisks();
            
            // Update Network
            UpdateNetwork();
            
            // Update Processes
            UpdateProcesses();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update error: {ex.Message}");
        }
    }

    private void UpdateMemory()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemory = gcInfo.TotalAvailableMemoryBytes;
            
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalMem = Convert.ToDouble(obj["TotalVisibleMemorySize"]) * 1024;
                var freeMem = Convert.ToDouble(obj["FreePhysicalMemory"]) * 1024;
                var usedMem = totalMem - freeMem;
                var usedPercent = (usedMem / totalMem) * 100;
                
                memProgress.Value = usedPercent;
                txtMemPercent.Text = $"{usedPercent:F1}%";
                txtMemInfo.Text = $"Used: {FormatBytes(usedMem)} / Total: {FormatBytes(totalMem)}";
                
                ((PieSeries<double>)memChart.Series.ElementAt(0)).Values = new double[] { usedMem };
                ((PieSeries<double>)memChart.Series.ElementAt(1)).Values = new double[] { freeMem };
            }
        }
        catch { }
    }

    private void UpdateDisks()
    {
        diskPanel.Children.Clear();
        
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            try
            {
                var usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                var usedPercent = (double)usedSpace / drive.TotalSize * 100;
                
                var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
                
                var header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                
                var label = new TextBlock 
                { 
                    Text = $"{drive.Name} ({drive.DriveType})", 
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 85)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12
                };
                Grid.SetColumn(label, 0);
                
                var size = new TextBlock 
                { 
                    Text = $"{FormatBytes(usedSpace)} / {FormatBytes(drive.TotalSize)}", 
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(size, 2);
                
                header.Children.Add(label);
                header.Children.Add(size);
                
                var progress = new ProgressBar
                {
                    Value = usedPercent,
                    Height = 15,
                    Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                    Foreground = usedPercent > 90 ? new SolidColorBrush(Color.FromRgb(255, 51, 51)) :
                                 usedPercent > 75 ? new SolidColorBrush(Color.FromRgb(255, 170, 0)) :
                                                    new SolidColorBrush(Color.FromRgb(255, 0, 85)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 5, 0, 0)
                };
                
                panel.Children.Add(header);
                panel.Children.Add(progress);
                diskPanel.Children.Add(panel);
            }
            catch { }
        }
    }

    private (long received, long sent) GetNetworkStats()
    {
        long totalReceived = 0, totalSent = 0;
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                var stats = ni.GetIPv4Statistics();
                totalReceived += stats.BytesReceived;
                totalSent += stats.BytesSent;
            }
        }
        return (totalReceived, totalSent);
    }

    private void UpdateNetwork()
    {
        var stats = GetNetworkStats();
        var elapsed = (DateTime.Now - _lastNetworkCheck).TotalSeconds;
        
        if (elapsed > 0)
        {
            var downloadSpeed = (stats.received - _lastBytesReceived) / elapsed;
            var uploadSpeed = (stats.sent - _lastBytesSent) / elapsed;
            
            txtDownload.Text = FormatSpeed(downloadSpeed);
            txtUpload.Text = FormatSpeed(uploadSpeed);
        }
        
        _lastBytesReceived = stats.received;
        _lastBytesSent = stats.sent;
        _lastNetworkCheck = DateTime.Now;
    }

    private void UpdateProcesses()
    {
        processPanel.Children.Clear();
        
        var header = new TextBlock 
        { 
            Text = "TOP PROCESSES (by CPU)", 
            Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 10)
        };
        processPanel.Children.Add(header);
        
        try
        {
            var processes = Process.GetProcesses()
                .Where(p => { try { return p.WorkingSet64 > 0; } catch { return false; } })
                .OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0L; } })
                .Take(8);
            
            foreach (var proc in processes)
            {
                try
                {
                    var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    
                    var name = new TextBlock 
                    { 
                        Text = proc.ProcessName.Length > 20 ? proc.ProcessName.Substring(0, 20) + "..." : proc.ProcessName,
                        Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 11
                    };
                    Grid.SetColumn(name, 0);
                    
                    var mem = new TextBlock 
                    { 
                        Text = FormatBytes(proc.WorkingSet64),
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 11
                    };
                    Grid.SetColumn(mem, 1);
                    
                    grid.Children.Add(name);
                    grid.Children.Add(mem);
                    processPanel.Children.Add(grid);
                }
                catch { }
            }
        }
        catch { }
    }

    private static string FormatBytes(double bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (bytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytes /= 1024;
        }
        return $"{bytes:F1} {sizes[order]}";
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        if (bytesPerSecond >= 1024)
            return $"{bytesPerSecond / 1024:F1} KB/s";
        return $"{bytesPerSecond:F0} B/s";
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer?.Stop();
        _cpuCounter?.Dispose();
        base.OnClosed(e);
    }
}
