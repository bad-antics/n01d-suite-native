using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace N01D.Clip
{
    public class ClipEntry
    {
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsPinned { get; set; }
        public string Category { get; set; } = "text";

        public string Preview => Content.Length > 120
            ? Content[..120].Replace("\r", "").Replace("\n", "⏎") + "…"
            : Content.Replace("\r", "").Replace("\n", "⏎");
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - Timestamp;
                if (diff.TotalSeconds < 60) return $"{(int)diff.TotalSeconds}s ago";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return Timestamp.ToString("MMM dd HH:mm");
            }
        }
        public string SizeLabel => Content.Length < 1024
            ? $"{Content.Length}B"
            : $"{Content.Length / 1024.0:F1}KB";
        public string CategoryIcon => Category switch
        {
            "url" => "🌐",
            "code" => "💻",
            "number" => "🔢",
            "path" => "📁",
            _ => "📝"
        };
        public string PinIcon => IsPinned ? "📌" : "";
    }

    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<ClipEntry> _clips = new();
        private readonly ObservableCollection<ClipEntry> _filtered = new();
        private readonly DispatcherTimer _pollTimer;
        private string _lastClip = "";
        private bool _monitoring = true;

        public MainWindow()
        {
            InitializeComponent();
            lstClips.ItemsSource = _filtered;

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _pollTimer.Tick += PollClipboard;
            _pollTimer.Start();
        }

        private void PollClipboard(object? sender, EventArgs e)
        {
            if (!_monitoring) return;
            try
            {
                if (!Clipboard.ContainsText()) return;
                var text = Clipboard.GetText();
                if (string.IsNullOrEmpty(text) || text == _lastClip) return;

                _lastClip = text;
                var entry = new ClipEntry
                {
                    Content = text,
                    Category = DetectCategory(text)
                };

                // Remove duplicate if exists 
                var existing = _clips.FirstOrDefault(c => c.Content == text && !c.IsPinned);
                if (existing != null) _clips.Remove(existing);

                _clips.Insert(0, entry);

                // Keep max 200
                while (_clips.Count > 200)
                    _clips.RemoveAt(_clips.Count - 1);

                RefreshList();
                lblStatus.Text = $"[ MONITORING — {_clips.Count} clips ]";
            }
            catch { }
        }

        private void RefreshList()
        {
            var filter = txtSearch.Text.Trim().ToLowerInvariant();
            _filtered.Clear();

            // Pinned first, then by time
            var sorted = _clips
                .Where(c => string.IsNullOrEmpty(filter) ||
                            c.Content.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.IsPinned)
                .ThenByDescending(c => c.Timestamp);

            foreach (var c in sorted)
                _filtered.Add(c);
        }

        private static string DetectCategory(string text)
        {
            if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
                return "url";
            if (text.Contains(":\\") || text.Contains(":/") || text.StartsWith("/"))
                return "path";
            if (text.Contains('{') || text.Contains("=>") || text.Contains("function") ||
                text.Contains("class ") || text.Contains("public ") || text.Contains("var "))
                return "code";
            if (double.TryParse(text.Trim(), out _))
                return "number";
            return "text";
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RefreshList();

        private void LstClips_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstClips.SelectedItem is ClipEntry entry)
                txtPreview.Text = entry.Content;
        }

        private void LstClips_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BtnPaste_Click(sender, e);
        }

        private void BtnPaste_Click(object sender, RoutedEventArgs e)
        {
            if (lstClips.SelectedItem is ClipEntry entry)
            {
                _lastClip = entry.Content; // prevent re-capture
                Clipboard.SetText(entry.Content);
                lblStatus.Text = "[ COPIED TO CLIPBOARD ]";
            }
        }

        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            if (lstClips.SelectedItem is ClipEntry entry)
            {
                entry.IsPinned = !entry.IsPinned;
                RefreshList();
                lblStatus.Text = entry.IsPinned ? "[ PINNED ]" : "[ UNPINNED ]";
            }
        }

        private void BtnDeleteOne_Click(object sender, RoutedEventArgs e)
        {
            if (lstClips.SelectedItem is ClipEntry entry)
            {
                _clips.Remove(entry);
                RefreshList();
                txtPreview.Text = "";
                lblStatus.Text = $"[ DELETED — {_clips.Count} clips ]";
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all clipboard history?", "N01D Clip",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                var pinned = _clips.Where(c => c.IsPinned).ToList();
                _clips.Clear();
                foreach (var p in pinned) _clips.Add(p);
                RefreshList();
                lblStatus.Text = $"[ CLEARED — {_clips.Count} pinned kept ]";
            }
        }

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            _monitoring = !_monitoring;
            btnToggle.Content = _monitoring ? "⏸ PAUSE" : "▶ RESUME";
            lblStatus.Text = _monitoring ? "[ MONITORING ]" : "[ PAUSED ]";
        }
    }
}
