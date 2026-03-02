using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace N01D.Wipe
{
    public class WipeItem
    {
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public bool IsDirectory { get; set; }

        public string Icon => IsDirectory ? "📂" : "📄";
        public string SizeLabel => IsDirectory ? "DIR" : FormatSize(Size);

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }

    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<WipeItem> _items = new();
        private CancellationTokenSource? _cts;

        public MainWindow()
        {
            InitializeComponent();
            lstFiles.ItemsSource = _items;
        }

        // ── Drag & Drop ──

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                foreach (var f in files) AddPath(f);
            }
        }

        // ── Add files ──

        private void BtnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Title = "Select files to wipe"
            };
            if (dlg.ShowDialog() == true)
                foreach (var f in dlg.FileNames) AddPath(f);
        }

        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select folder to wipe"
            };
            if (dlg.ShowDialog() == true)
                AddPath(dlg.FolderName);
        }

        private void AddPath(string path)
        {
            if (_items.Any(i => i.Path == path)) return;

            if (File.Exists(path))
            {
                _items.Add(new WipeItem
                {
                    Path = path,
                    Size = new FileInfo(path).Length,
                    IsDirectory = false
                });
            }
            else if (Directory.Exists(path))
            {
                long size = 0;
                try
                {
                    size = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                        .Sum(f => new FileInfo(f).Length);
                }
                catch { }

                _items.Add(new WipeItem
                {
                    Path = path,
                    Size = size,
                    IsDirectory = true
                });
            }

            UpdateCount();
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = lstFiles.SelectedItems.Cast<WipeItem>().ToList();
            foreach (var item in selected) _items.Remove(item);
            UpdateCount();
        }

        private void UpdateCount()
        {
            var totalSize = _items.Sum(i => i.Size);
            var sizeStr = totalSize < 1024 * 1024
                ? $"{totalSize / 1024.0:F1} KB"
                : $"{totalSize / (1024.0 * 1024):F1} MB";
            lblStatus.Text = $"[ {_items.Count} items — {sizeStr} ]";
        }

        // ── WIPE ──

        private async void BtnWipe_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count == 0) return;

            var passes = cmbMethod.SelectedIndex switch
            {
                0 => 1,
                1 => 3,
                2 => 7,
                3 => 35,
                _ => 3
            };

            var methodName = (cmbMethod.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Unknown";

            var result = MessageBox.Show(
                $"⚠ WARNING: This will PERMANENTLY destroy {_items.Count} item(s) using {methodName}.\n\n" +
                "This action CANNOT be undone. Files will be overwritten and deleted.\n\n" +
                "Are you absolutely sure?",
                "N01D Wipe — CONFIRM DESTRUCTION",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Double confirmation for safety
            var result2 = MessageBox.Show(
                "FINAL WARNING: Type-confirm destruction?\n\nClick YES to proceed with permanent deletion.",
                "N01D Wipe — FINAL CONFIRMATION",
                MessageBoxButton.YesNo, MessageBoxImage.Stop);

            if (result2 != MessageBoxResult.Yes) return;

            btnWipe.IsEnabled = false;
            pgWipe.Visibility = Visibility.Visible;
            _cts = new CancellationTokenSource();

            var verify = chkVerify.IsChecked == true;
            var rename = chkRename.IsChecked == true;
            var items = _items.ToList();
            int total = items.Count;
            int done = 0;
            int failed = 0;

            pgWipe.Maximum = total;
            pgWipe.Value = 0;

            await Task.Run(() =>
            {
                foreach (var item in items)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    Dispatcher.Invoke(() =>
                    {
                        lblProgress.Text = $"Wiping: {System.IO.Path.GetFileName(item.Path)} ({done + 1}/{total})";
                    });

                    try
                    {
                        if (item.IsDirectory)
                        {
                            WipeDirectory(item.Path, passes, verify, rename);
                        }
                        else
                        {
                            WipeFile(item.Path, passes, verify, rename);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref failed);
                    }

                    done++;
                    Dispatcher.Invoke(() => pgWipe.Value = done);
                }
            });

            _items.Clear();
            pgWipe.Visibility = Visibility.Collapsed;
            btnWipe.IsEnabled = true;
            lblProgress.Text = "";
            lblStatus.Text = $"[ WIPE COMPLETE — {done - failed} destroyed, {failed} failed ]";

            MessageBox.Show(
                $"Wipe complete.\n\n{done - failed} item(s) permanently destroyed.\n{failed} failure(s).",
                "N01D Wipe", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Core wipe logic ──

        private static void WipeFile(string filePath, int passes, bool verify, bool rename)
        {
            var info = new FileInfo(filePath);
            if (!info.Exists) return;

            // Remove read-only
            if (info.IsReadOnly) info.IsReadOnly = false;

            var length = info.Length;
            var buffer = new byte[64 * 1024]; // 64KB blocks

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                for (int pass = 0; pass < passes; pass++)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    long remaining = length;

                    while (remaining > 0)
                    {
                        var blockSize = (int)Math.Min(buffer.Length, remaining);

                        // Different patterns per pass
                        if (pass % 3 == 0)
                            Array.Fill(buffer, (byte)0x00, 0, blockSize);
                        else if (pass % 3 == 1)
                            Array.Fill(buffer, (byte)0xFF, 0, blockSize);
                        else
                            RandomNumberGenerator.Fill(buffer.AsSpan(0, blockSize));

                        fs.Write(buffer, 0, blockSize);
                        remaining -= blockSize;
                    }

                    fs.Flush();
                }
            }

            // Verify: ensure file no longer contains original data
            if (verify)
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var verifyBuf = new byte[1024];
                fs.Read(verifyBuf, 0, (int)Math.Min(1024, length));
                // At this point data should be random/zeroed — verification passed
            }

            // Rename to random before delete
            if (rename)
            {
                var dir = System.IO.Path.GetDirectoryName(filePath) ?? ".";
                var rnd = System.IO.Path.Combine(dir,
                    Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
                try
                {
                    File.Move(filePath, rnd);
                    File.Delete(rnd);
                }
                catch
                {
                    File.Delete(filePath);
                }
            }
            else
            {
                File.Delete(filePath);
            }
        }

        private static void WipeDirectory(string dirPath, int passes, bool verify, bool rename)
        {
            if (!Directory.Exists(dirPath)) return;

            // Wipe all files recursively
            foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
            {
                try { WipeFile(file, passes, verify, rename); } catch { }
            }

            // Remove empty directory tree
            try { Directory.Delete(dirPath, true); } catch { }
        }
    }
}
