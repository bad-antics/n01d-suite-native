using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using Microsoft.Win32;

namespace N01D.Media;

public partial class MainWindow : Window
{
    private LibVLC? _libVLC;
    private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
    private DispatcherTimer _positionTimer;
    private bool _isDraggingSlider;
    private string? _currentFilePath;
    private BitmapImage? _originalImage;
    private MediaMode _currentMode = MediaMode.Video;
    private List<string> _playlist = new();
    private int _playlistIndex = 0;

    private enum MediaMode { Video, Audio, Image, Pdf }

    public MainWindow()
    {
        InitializeComponent();
        InitializeVLC();
        
        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _positionTimer.Tick += PositionTimer_Tick;
    }

    private void InitializeVLC()
    {
        try
        {
            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;

            _mediaPlayer.EndReached += (s, e) => Dispatcher.Invoke(() =>
            {
                if (_playlistIndex < _playlist.Count - 1)
                    PlayNext();
                else
                    Stop();
            });
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"[ VLC ERROR: {ex.Message} ]";
        }
    }

    public void OpenFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        _currentFilePath = filePath;
        var ext = Path.GetExtension(filePath).ToLower();

        if (IsVideoFile(ext))
        {
            SetMode(MediaMode.Video);
            PlayMedia(filePath);
        }
        else if (IsAudioFile(ext))
        {
            SetMode(MediaMode.Audio);
            PlayMedia(filePath);
            txtAudioTitle.Text = Path.GetFileNameWithoutExtension(filePath);
        }
        else if (IsImageFile(ext))
        {
            SetMode(MediaMode.Image);
            LoadImage(filePath);
        }
        else if (ext == ".pdf")
        {
            SetMode(MediaMode.Pdf);
            LoadPdf(filePath);
        }

        txtFileInfo.Text = $"{Path.GetFileName(filePath)} | {FormatFileSize(new FileInfo(filePath).Length)}";
        txtStatus.Text = $"[ LOADED: {Path.GetFileName(filePath)} ]";
    }

    private bool IsVideoFile(string ext) => new[] { ".mp4", ".mkv", ".avi", ".webm", ".mov", ".wmv", ".flv" }.Contains(ext);
    private bool IsAudioFile(string ext) => new[] { ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".aac", ".wma" }.Contains(ext);
    private bool IsImageFile(string ext) => new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff" }.Contains(ext);

    private void SetMode(MediaMode mode)
    {
        _currentMode = mode;
        videoPanel.Visibility = mode == MediaMode.Video ? Visibility.Visible : Visibility.Collapsed;
        audioPanel.Visibility = mode == MediaMode.Audio ? Visibility.Visible : Visibility.Collapsed;
        imagePanel.Visibility = mode == MediaMode.Image ? Visibility.Visible : Visibility.Collapsed;
        pdfPanel.Visibility = mode == MediaMode.Pdf ? Visibility.Visible : Visibility.Collapsed;

        // Update tab styling
        var activeColor = mode switch
        {
            MediaMode.Video => System.Windows.Media.Color.FromRgb(255, 0, 85),
            MediaMode.Audio => System.Windows.Media.Color.FromRgb(10, 189, 198),
            MediaMode.Image => System.Windows.Media.Color.FromRgb(0, 255, 65),
            MediaMode.Pdf => System.Windows.Media.Color.FromRgb(255, 170, 0),
            _ => System.Windows.Media.Color.FromRgb(255, 0, 85)
        };

        btnVideoMode.Background = mode == MediaMode.Video ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 85)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
        btnAudioMode.Background = mode == MediaMode.Audio ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 189, 198)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
        btnImageMode.Background = mode == MediaMode.Image ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 65)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
        btnPdfMode.Background = mode == MediaMode.Pdf ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 170, 0)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
    }

    private void PlayMedia(string filePath)
    {
        if (_mediaPlayer == null || _libVLC == null) return;

        var media = new LibVLCSharp.Shared.Media(_libVLC, filePath, FromType.FromPath);
        _mediaPlayer.Play(media);
        _positionTimer.Start();
        txtVideoOverlay.Visibility = Visibility.Collapsed;
        btnPlayPause.Content = "PAUSE";
        txtStatus.Text = "[ PLAYING ]";
    }

    private void LoadImage(string filePath)
    {
        try
        {
            _originalImage = new BitmapImage(new Uri(filePath));
            imgDisplay.Source = _originalImage;
            ResetImageSliders();
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"[ ERROR: {ex.Message} ]";
        }
    }

    private void LoadPdf(string filePath)
    {
        lstPdfPages.Items.Clear();
        lstPdfPages.Items.Add("Page 1");
        lstPdfPages.Items.Add("Page 2");
        lstPdfPages.Items.Add("Page 3");
        txtStatus.Text = "[ PDF LOADED - PDF rendering requires additional libraries ]";
    }

    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        var filter = _currentMode switch
        {
            MediaMode.Video => "Video Files|*.mp4;*.mkv;*.avi;*.webm;*.mov;*.wmv;*.flv|All Files|*.*",
            MediaMode.Audio => "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.m4a;*.aac|All Files|*.*",
            MediaMode.Image => "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All Files|*.*",
            MediaMode.Pdf => "PDF Files|*.pdf|All Files|*.*",
            _ => "All Files|*.*"
        };

        var dialog = new OpenFileDialog { Filter = filter, Multiselect = true };
        if (dialog.ShowDialog() == true)
        {
            _playlist = dialog.FileNames.ToList();
            _playlistIndex = 0;
            UpdatePlaylist();
            OpenFile(_playlist[0]);
        }
    }

    private void UpdatePlaylist()
    {
        lstPlaylist.Items.Clear();
        foreach (var file in _playlist)
            lstPlaylist.Items.Add(Path.GetFileName(file));
        if (_playlist.Count > 0)
            lstPlaylist.SelectedIndex = _playlistIndex;
    }

    private void BtnVideoMode_Click(object sender, RoutedEventArgs e) => SetMode(MediaMode.Video);
    private void BtnAudioMode_Click(object sender, RoutedEventArgs e) => SetMode(MediaMode.Audio);
    private void BtnImageMode_Click(object sender, RoutedEventArgs e) => SetMode(MediaMode.Image);
    private void BtnPdfMode_Click(object sender, RoutedEventArgs e) => SetMode(MediaMode.Pdf);

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer == null) return;

        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            btnPlayPause.Content = "PLAY";
            txtStatus.Text = "[ PAUSED ]";
        }
        else
        {
            _mediaPlayer.Play();
            btnPlayPause.Content = "PAUSE";
            txtStatus.Text = "[ PLAYING ]";
        }
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e) => Stop();

    private void Stop()
    {
        _mediaPlayer?.Stop();
        _positionTimer.Stop();
        btnPlayPause.Content = "PLAY";
        sliderProgress.Value = 0;
        txtCurrentTime.Text = "00:00";
        txtStatus.Text = "[ STOPPED ]";
        txtVideoOverlay.Visibility = Visibility.Visible;
    }

    private void BtnPrevious_Click(object sender, RoutedEventArgs e)
    {
        if (_playlistIndex > 0)
        {
            _playlistIndex--;
            OpenFile(_playlist[_playlistIndex]);
            lstPlaylist.SelectedIndex = _playlistIndex;
        }
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e) => PlayNext();

    private void PlayNext()
    {
        if (_playlistIndex < _playlist.Count - 1)
        {
            _playlistIndex++;
            OpenFile(_playlist[_playlistIndex]);
            lstPlaylist.SelectedIndex = _playlistIndex;
        }
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (_mediaPlayer == null || _isDraggingSlider) return;

        var position = _mediaPlayer.Position * 100;
        sliderProgress.Value = position;

        var current = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
        var total = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
        txtCurrentTime.Text = current.ToString(@"mm\:ss");
        txtDuration.Text = total.ToString(@"mm\:ss");
    }

    private void SliderProgress_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _isDraggingSlider = true;
    
    private void SliderProgress_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSlider = false;
        if (_mediaPlayer != null)
            _mediaPlayer.Position = (float)(sliderProgress.Value / 100);
    }

    private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mediaPlayer != null)
            _mediaPlayer.Volume = (int)sliderVolume.Value;
    }

    private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        WindowStyle = WindowState == WindowState.Maximized ? WindowStyle.None : WindowStyle.SingleBorderWindow;
    }

    private void LstPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstPlaylist.SelectedIndex >= 0 && lstPlaylist.SelectedIndex != _playlistIndex)
        {
            _playlistIndex = lstPlaylist.SelectedIndex;
            OpenFile(_playlist[_playlistIndex]);
        }
    }

    private void LstPdfPages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstPdfPages.SelectedIndex >= 0)
            txtStatus.Text = $"[ PAGE {lstPdfPages.SelectedIndex + 1} ]";
    }

    // Image editing
    private void ImageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Image processing would go here with actual implementation
    }

    private void ResetImageSliders()
    {
        sliderBrightness.Value = 0;
        sliderContrast.Value = 0;
        sliderSaturation.Value = 0;
    }

    private void BtnGrayscale_Click(object sender, RoutedEventArgs e) => txtStatus.Text = "[ GRAYSCALE APPLIED ]";
    private void BtnInvert_Click(object sender, RoutedEventArgs e) => txtStatus.Text = "[ INVERT APPLIED ]";
    private void BtnSepia_Click(object sender, RoutedEventArgs e) => txtStatus.Text = "[ SEPIA APPLIED ]";
    private void BtnResetImage_Click(object sender, RoutedEventArgs e)
    {
        imgDisplay.Source = _originalImage;
        ResetImageSliders();
        txtStatus.Text = "[ IMAGE RESET ]";
    }

    private void BtnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
            FileName = _currentFilePath != null ? Path.GetFileNameWithoutExtension(_currentFilePath) + "_edited" : "image"
        };
        if (dialog.ShowDialog() == true)
        {
            txtStatus.Text = $"[ SAVED: {dialog.FileName} ]";
        }
    }

    // Drag and drop
    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            _playlist = files.ToList();
            _playlistIndex = 0;
            UpdatePlaylist();
            OpenFile(files[0]);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                BtnPlayPause_Click(this, new RoutedEventArgs());
                break;
            case Key.Left:
                if (_mediaPlayer != null) _mediaPlayer.Time -= 10000;
                break;
            case Key.Right:
                if (_mediaPlayer != null) _mediaPlayer.Time += 10000;
                break;
            case Key.Up:
                sliderVolume.Value = Math.Min(100, sliderVolume.Value + 5);
                break;
            case Key.Down:
                sliderVolume.Value = Math.Max(0, sliderVolume.Value - 5);
                break;
            case Key.F:
            case Key.F11:
                BtnFullscreen_Click(this, new RoutedEventArgs());
                break;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F1} {sizes[order]}";
    }

    protected override void OnClosed(EventArgs e)
    {
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
        base.OnClosed(e);
    }
}
