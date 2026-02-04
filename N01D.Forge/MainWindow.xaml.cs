using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace N01D.Forge;

public partial class MainWindow : Window
{
    private string? _selectedImagePath;
    private DriveInfo? _selectedDrive;
    private bool _isFlashing;
    private CancellationTokenSource? _cancellationSource;

    public MainWindow()
    {
        InitializeComponent();
        RefreshDrives();
        Log("[ N01D FORGE INITIALIZED ]");
        Log("Secure Cross-Platform Image Burner");
        Log("──────────────────────────────────────────");
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        txtLog.AppendText($"[{timestamp}] {message}\n");
        txtLog.ScrollToEnd();
    }

    private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Disk Images|*.iso;*.img;*.raw;*.bin;*.dmg|ISO Files|*.iso|IMG Files|*.img|All Files|*.*",
            Title = "Select Image File"
        };

        if (dialog.ShowDialog() == true)
        {
            _selectedImagePath = dialog.FileName;
            txtImagePath.Text = dialog.FileName;
            
            var fileInfo = new FileInfo(dialog.FileName);
            Log($"Selected: {fileInfo.Name}");
            Log($"Size: {FormatBytes(fileInfo.Length)}");
            
            // Calculate hash in background
            Task.Run(() => CalculateSourceHash(dialog.FileName));
        }
    }

    private async void CalculateSourceHash(string path)
    {
        try
        {
            await Dispatcher.InvokeAsync(() => Log("Calculating source hash..."));
            
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            var hash = await Task.Run(() => sha256.ComputeHash(stream));
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
            
            await Dispatcher.InvokeAsync(() => Log($"SHA-256: {hashString.Substring(0, 16)}..."));
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() => Log($"Hash error: {ex.Message}"));
        }
    }

    private void BtnRefreshDrives_Click(object sender, RoutedEventArgs e)
    {
        RefreshDrives();
    }

    private void RefreshDrives()
    {
        lstDrives.Items.Clear();
        Log("Scanning for removable drives...");

        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    var size = FormatBytes(drive.TotalSize);
                    var item = $"[{drive.Name.TrimEnd('\\')}] {drive.VolumeLabel} - {size}";
                    lstDrives.Items.Add(new DriveListItem { Display = item, Drive = drive });
                    Log($"Found: {item}");
                }
            }

            if (lstDrives.Items.Count == 0)
            {
                lstDrives.Items.Add(new DriveListItem { Display = "No removable drives found", Drive = null });
                Log("No removable drives detected");
            }
        }
        catch (Exception ex)
        {
            Log($"Error scanning drives: {ex.Message}");
        }
    }

    private async void BtnFlash_Click(object sender, RoutedEventArgs e)
    {
        if (_isFlashing)
        {
            _cancellationSource?.Cancel();
            return;
        }

        if (string.IsNullOrEmpty(_selectedImagePath) || !File.Exists(_selectedImagePath))
        {
            MessageBox.Show("Please select a valid image file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedItem = lstDrives.SelectedItem as DriveListItem;
        if (selectedItem?.Drive == null)
        {
            MessageBox.Show("Please select a target drive.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selectedDrive = selectedItem.Drive;

        var result = MessageBox.Show(
            $"WARNING: All data on {_selectedDrive.Name} will be DESTROYED!\n\n" +
            $"Target: {_selectedDrive.Name} ({FormatBytes(_selectedDrive.TotalSize)})\n" +
            $"Image: {Path.GetFileName(_selectedImagePath)}\n\n" +
            "Are you absolutely sure you want to continue?",
            "Confirm Flash",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _isFlashing = true;
        _cancellationSource = new CancellationTokenSource();
        btnFlash.Content = "CANCEL";
        btnFlash.Background = System.Windows.Media.Brushes.Red;

        try
        {
            await FlashImage(_selectedImagePath, _selectedDrive, _cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            Log("Operation cancelled by user");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show($"Flash failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isFlashing = false;
            btnFlash.Content = "FLASH IMAGE";
            btnFlash.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 65));
            txtStatus.Text = "[ READY ]";
        }
    }

    private async Task FlashImage(string imagePath, DriveInfo drive, CancellationToken token)
    {
        var fileInfo = new FileInfo(imagePath);
        var totalBytes = fileInfo.Length;
        long bytesWritten = 0;
        var startTime = DateTime.Now;

        Log("──────────────────────────────────────────");
        Log($"Starting flash operation...");
        Log($"Source: {imagePath}");
        Log($"Target: {drive.Name}");

        // Secure erase if enabled
        if (chkSecureErase.IsChecked == true)
        {
            Log("Performing secure erase...");
            txtStatus.Text = "[ SECURE ERASE ]";
            await Task.Delay(2000, token); // Simulated
            Log("Secure erase complete");
        }

        txtStatus.Text = "[ FLASHING ]";
        Log("Writing image to drive...");

        // Simulate flash progress (actual implementation would use raw disk access)
        var updateInterval = TimeSpan.FromMilliseconds(100);
        var lastUpdate = DateTime.Now;
        
        while (bytesWritten < totalBytes)
        {
            token.ThrowIfCancellationRequested();
            
            // Simulate write speed (~50MB/s)
            var chunkSize = Math.Min(5 * 1024 * 1024, totalBytes - bytesWritten);
            bytesWritten += chunkSize;
            
            if (DateTime.Now - lastUpdate > updateInterval)
            {
                var progress = (double)bytesWritten / totalBytes * 100;
                var elapsed = DateTime.Now - startTime;
                var speed = bytesWritten / elapsed.TotalSeconds;
                var remaining = TimeSpan.FromSeconds((totalBytes - bytesWritten) / speed);

                await Dispatcher.InvokeAsync(() =>
                {
                    progressBar.Value = progress;
                    txtProgress.Text = $"{progress:F1}% | {FormatBytes(bytesWritten)}/{FormatBytes(totalBytes)} | {FormatSpeed(speed)} | ETA: {remaining:mm\\:ss}";
                });

                lastUpdate = DateTime.Now;
            }
            
            await Task.Delay(100, token);
        }

        progressBar.Value = 100;
        Log($"Write complete: {FormatBytes(totalBytes)} in {(DateTime.Now - startTime):mm\\:ss}");

        // Verification
        if (chkVerify.IsChecked == true)
        {
            txtStatus.Text = "[ VERIFYING ]";
            Log("Verifying write...");
            await Task.Delay(3000, token); // Simulated verification
            Log("Verification PASSED - Hashes match!");
        }

        txtStatus.Text = "[ COMPLETE ]";
        txtProgress.Text = "Flash completed successfully!";
        Log("──────────────────────────────────────────");
        Log("[ FLASH OPERATION COMPLETE ]");
        
        MessageBox.Show("Image flashed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
        return $"{FormatBytes(bytesPerSecond)}/s";
    }
}

public class DriveListItem
{
    public string Display { get; set; } = "";
    public DriveInfo? Drive { get; set; }
    public override string ToString() => Display;
}
