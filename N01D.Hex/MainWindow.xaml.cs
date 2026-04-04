using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace N01D.Hex
{
    public partial class MainWindow : Window
    {
        // ── State ──────────────────────────────────────────────────────────────
        private byte[]  _bytes    = Array.Empty<byte>();
        private bool    _dirty    = false;
        private string? _filePath = null;
        private bool    _syncing  = false;

        private const int BYTES_PER_ROW = 16;

        // ── Known binary signatures ────────────────────────────────────────────
        private static readonly Dictionary<string, byte[]> Patterns = new()
        {
            ["PNG Header"]  = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            ["JPEG Header"] = new byte[] { 0xFF, 0xD8, 0xFF },
            ["ELF Header"]  = new byte[] { 0x7F, 0x45, 0x4C, 0x46 },
            ["PE Header"]   = new byte[] { 0x4D, 0x5A },          // MZ
            ["ZIP Header"]  = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            ["Null Bytes"]  = new byte[] { 0x00 },
        };

        public MainWindow()
        {
            InitializeComponent();
            Title = "N01D.Hex";
            RenderEmpty();
        }

        // ── Rendering ─────────────────────────────────────────────────────────

        private void RenderEmpty()
        {
            txtOffsets.Text = "";
            txtHex.Text     = "";
            txtAscii.Text   = "";
            lblSize.Text    = "Size: 0 bytes";
            lblOffset.Text  = "Offset: 0x00000000";
            lblSelection.Text = "Sel: none";
            lblStatus.Text  = "No file loaded";
        }

        private void RenderBytes()
        {
            if (_bytes.Length == 0) { RenderEmpty(); return; }

            var offsetSb = new StringBuilder();
            var hexSb    = new StringBuilder();
            var asciiSb  = new StringBuilder();

            for (int row = 0; row < _bytes.Length; row += BYTES_PER_ROW)
            {
                offsetSb.AppendLine($"{row:X8}");

                var hexLine   = new StringBuilder();
                var asciiLine = new StringBuilder();

                for (int col = 0; col < BYTES_PER_ROW; col++)
                {
                    int idx = row + col;
                    if (idx < _bytes.Length)
                    {
                        hexLine.Append($"{_bytes[idx]:X2} ");
                        byte b = _bytes[idx];
                        asciiLine.Append(b >= 0x20 && b <= 0x7E ? (char)b : '.');
                    }
                    else
                    {
                        hexLine.Append("   ");
                        asciiLine.Append(' ');
                    }

                    if (col == 7) hexLine.Append(' ');
                }

                hexSb.AppendLine(hexLine.ToString().TrimEnd());
                asciiSb.AppendLine(asciiLine.ToString());
            }

            _syncing = true;
            try
            {
                txtOffsets.Text = offsetSb.ToString();
                txtHex.Text     = hexSb.ToString();
                txtAscii.Text   = asciiSb.ToString();
            }
            finally { _syncing = false; }

            var fn = _filePath is null ? "untitled" : Path.GetFileName(_filePath);
            lblSize.Text   = $"Size: {_bytes.Length:N0} bytes  ({FormatSize(_bytes.Length)})";
            lblStatus.Text = $"[ {fn} ]  {_bytes.Length:N0} bytes";
            Title = $"N01D.Hex — {fn}{(_dirty ? " *" : "")}";
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F2} MB";
        }

        // ── Scroll sync ───────────────────────────────────────────────────────

        private void SvHex_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_syncing) return;
            svOffset.ScrollToVerticalOffset(e.VerticalOffset);
            svAscii.ScrollToVerticalOffset(e.VerticalOffset);
        }

        // ── File I/O ──────────────────────────────────────────────────────────

        private void BtnOpen_Click(object sender, RoutedEventArgs e) => OpenFile();
        private void OpenFile()
        {
            var dlg = new OpenFileDialog { Title = "Open binary file", Filter = "All files (*.*)|*.*" };
            if (dlg.ShowDialog() != true) return;
            LoadFile(dlg.FileName);
        }

        private void LoadFile(string path)
        {
            try
            {
                _bytes    = File.ReadAllBytes(path);
                _filePath = path;
                _dirty    = false;
                btnSave.IsEnabled   = true;
                btnSaveAs.IsEnabled = true;
                RenderBytes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open file:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_filePath is null) { BtnSaveAs_Click(sender, e); return; }
            SaveTo(_filePath);
        }

        private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Title = "Save binary file", Filter = "All files (*.*)|*.*",
                                           FileName = _filePath is null ? "untitled.bin" : Path.GetFileName(_filePath) };
            if (dlg.ShowDialog() != true) return;
            _filePath = dlg.FileName;
            SaveTo(_filePath);
        }

        private void SaveTo(string path)
        {
            try
            {
                SyncHexToBytes();
                File.WriteAllBytes(path, _bytes);
                _dirty         = false;
                lblStatus.Text = $"Saved: {Path.GetFileName(path)}";
                Title = $"N01D.Hex — {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Hex editing ───────────────────────────────────────────────────────

        private void TxtHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncing) return;
            _dirty = true;
            Title  = $"N01D.Hex — {(_filePath is null ? "untitled" : Path.GetFileName(_filePath))} *";
        }

        private void SyncHexToBytes()
        {
            var hexChars = txtHex.Text
                .Where(c => "0123456789abcdefABCDEF".Contains(c))
                .ToArray();

            var result = new List<byte>();
            for (int i = 0; i + 1 < hexChars.Length; i += 2)
                result.Add(Convert.ToByte(new string(hexChars, i, 2), 16));

            _bytes = result.ToArray();
        }

        // ── Selection → offset label ──────────────────────────────────────────

        private void TxtHex_SelectionChanged(object sender, RoutedEventArgs e)
        {
            int caretPos  = txtHex.CaretIndex;
            int lineChars = (BYTES_PER_ROW * 3) + 1 + Environment.NewLine.Length;
            int row       = caretPos / lineChars;
            int col       = (caretPos % lineChars) / 3;
            long offset   = (long)row * BYTES_PER_ROW + col;
            if (offset >= 0 && offset < _bytes.LongLength)
            {
                lblOffset.Text = $"Offset: 0x{offset:X8}  ({offset})";
                byte b = _bytes[offset];
                lblSelection.Text = $"Byte: 0x{b:X2}  ({b})  '{(b >= 32 && b <= 126 ? (char)b : '.')}'";
            }
        }

        // ── Goto ──────────────────────────────────────────────────────────────

        private void BtnGoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new GotoDialog { Owner = this };
            if (dlg.ShowDialog() != true) return;
            long offset = dlg.Offset;
            if (offset < 0 || offset >= _bytes.LongLength)
            {
                lblStatus.Text = $"Offset 0x{offset:X} out of range";
                return;
            }
            // Scroll to the row containing offset
            int row      = (int)(offset / BYTES_PER_ROW);
            int lineHeight = 18;
            svHex.ScrollToVerticalOffset(row * lineHeight);
            lblOffset.Text = $"Offset: 0x{offset:X8}  ({offset})";
        }

        // ── Find ──────────────────────────────────────────────────────────────

        private void BtnFind_Click(object sender, RoutedEventArgs e)
            => findBar.Visibility = findBar.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;

        private void BtnFindClose_Click(object sender, RoutedEventArgs e)
            => findBar.Visibility = Visibility.Collapsed;

        private void TxtFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnFindNext_Click(sender, e);
        }

        private int _findStart = 0;

        private void BtnFindNext_Click(object sender, RoutedEventArgs e)
            => DoFind(forward: true);

        private void BtnFindPrev_Click(object sender, RoutedEventArgs e)
            => DoFind(forward: false);

        private void DoFind(bool forward)
        {
            var pattern = ParseHexPattern(txtFind.Text.Trim());
            if (pattern is null || pattern.Length == 0)
            {
                lblFindResult.Text = "invalid hex pattern";
                return;
            }
            int idx = forward
                ? FindPattern(_bytes, pattern, _findStart + 1)
                : FindPatternBackward(_bytes, pattern, _findStart - 1);

            if (idx < 0)
            {
                lblFindResult.Text = "not found";
                return;
            }
            _findStart         = idx;
            lblFindResult.Text = $"found at 0x{idx:X8}";
            lblOffset.Text     = $"Offset: 0x{idx:X8}  ({idx})";

            int row = idx / BYTES_PER_ROW;
            svHex.ScrollToVerticalOffset(row * 18);
        }

        private static byte[]? ParseHexPattern(string s)
        {
            var hexChars = s.Replace(" ", "").Replace("0x", "");
            if (hexChars.Length % 2 != 0) hexChars = "0" + hexChars;
            try
            {
                return Enumerable.Range(0, hexChars.Length / 2)
                    .Select(i => Convert.ToByte(hexChars.Substring(i * 2, 2), 16))
                    .ToArray();
            }
            catch { return null; }
        }

        private static int FindPattern(byte[] data, byte[] pattern, int startFrom)
        {
            for (int i = Math.Max(0, startFrom); i <= data.Length - pattern.Length; i++)
                if (data.Skip(i).Take(pattern.Length).SequenceEqual(pattern)) return i;
            return -1;
        }

        private static int FindPatternBackward(byte[] data, byte[] pattern, int startFrom)
        {
            for (int i = Math.Min(data.Length - pattern.Length, startFrom); i >= 0; i--)
                if (data.Skip(i).Take(pattern.Length).SequenceEqual(pattern)) return i;
            return -1;
        }

        // ── Pattern highlight ─────────────────────────────────────────────────

        private void CmbPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (cmbPattern.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selected is null || selected == "None" || !Patterns.ContainsKey(selected)) return;
            var pattern = Patterns[selected];
            int idx     = FindPattern(_bytes, pattern, 0);
            if (idx < 0)
            {
                lblStatus.Text = $"Pattern "{selected}" not found";
                return;
            }
            _findStart         = idx;
            lblStatus.Text     = $"Pattern "{selected}" at 0x{idx:X8}";
            lblOffset.Text     = $"Offset: 0x{idx:X8}  ({idx})";
            svHex.ScrollToVerticalOffset((idx / BYTES_PER_ROW) * 18);
        }
    }

    // ── Goto dialog ───────────────────────────────────────────────────────────

    public class GotoDialog : Window
    {
        public long Offset { get; private set; }

        private TextBox _txt;

        public GotoDialog()
        {
            Title             = "Goto Offset";
            Width             = 320;
            Height            = 120;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background        = (System.Windows.Media.Brush)Application.Current.Resources["N01DBackgroundBrush"];
            Foreground        = (System.Windows.Media.Brush)Application.Current.Resources["N01DTextBrush"];
            FontFamily        = (System.Windows.Media.FontFamily)Application.Current.Resources["N01DMonoFont"];
            ResizeMode        = ResizeMode.NoResize;

            var panel = new StackPanel { Margin = new Thickness(12) };
            panel.Children.Add(new TextBlock
            {
                Text       = "Enter offset (decimal or 0xHex):",
                Foreground = (System.Windows.Media.Brush)Application.Current.Resources["N01DTextDimBrush"],
                Margin     = new Thickness(0, 0, 0, 6)
            });

            _txt = new TextBox
            {
                Background   = (System.Windows.Media.Brush)Application.Current.Resources["N01DBackgroundAltBrush"],
                Foreground   = (System.Windows.Media.Brush)Application.Current.Resources["N01DPrimaryBrush"],
                BorderBrush  = (System.Windows.Media.Brush)Application.Current.Resources["N01DBorderBrush"],
                CaretBrush   = (System.Windows.Media.Brush)Application.Current.Resources["N01DPrimaryBrush"],
                Padding      = new Thickness(4, 2, 4, 2),
                Margin       = new Thickness(0, 0, 0, 8)
            };
            _txt.KeyDown += (s, e) => { if (e.Key == Key.Enter) TryAccept(); };
            panel.Children.Add(_txt);

            var btn = new Button { Content = "[ GO ]", HorizontalAlignment = HorizontalAlignment.Right };
            btn.Click += (s, e) => TryAccept();
            panel.Children.Add(btn);

            Content = panel;
            Loaded += (s, e) => _txt.Focus();
        }

        private void TryAccept()
        {
            var s = _txt.Text.Trim();
            long val;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                val = Convert.ToInt64(s[2..], 16);
            else if (!long.TryParse(s, out val))
            {
                MessageBox.Show("Invalid offset", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Offset      = val;
            DialogResult = true;
        }
    }
}