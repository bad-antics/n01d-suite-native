using System.IO;
using System.Windows;
using System.Windows.Threading;
using Markdig;
using Microsoft.Win32;

namespace N01D.Notes;

public partial class MainWindow : Window
{
    private string? _currentFilePath;
    private bool _isModified;
    private bool _previewVisible = true;
    private readonly DispatcherTimer _previewTimer;
    private readonly MarkdownPipeline _pipeline;

    private const string HtmlTemplate = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        * { box-sizing: border-box; }
        body {
            background: #0D0D0D;
            color: #E0E0E0;
            font-family: 'Segoe UI', Arial, sans-serif;
            font-size: 14px;
            line-height: 1.6;
            padding: 20px;
            margin: 0;
        }
        h1, h2, h3, h4, h5, h6 {
            color: #00FF41;
            border-bottom: 1px solid #333;
            padding-bottom: 5px;
            margin-top: 20px;
        }
        h1 { font-size: 2em; }
        h2 { font-size: 1.5em; color: #0ABDC6; }
        h3 { font-size: 1.2em; color: #FF0055; }
        a { color: #0ABDC6; text-decoration: none; }
        a:hover { text-decoration: underline; }
        code {
            background: #1A1A1A;
            color: #00FF41;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Consolas', monospace;
        }
        pre {
            background: #1A1A1A;
            border: 1px solid #333;
            border-left: 3px solid #00FF41;
            padding: 15px;
            overflow-x: auto;
            border-radius: 5px;
        }
        pre code {
            background: transparent;
            padding: 0;
        }
        blockquote {
            border-left: 3px solid #0ABDC6;
            margin: 10px 0;
            padding: 10px 20px;
            background: #1A1A1A;
            color: #808080;
        }
        table {
            border-collapse: collapse;
            width: 100%;
            margin: 15px 0;
        }
        th, td {
            border: 1px solid #333;
            padding: 10px;
            text-align: left;
        }
        th {
            background: #1A1A1A;
            color: #00FF41;
        }
        tr:nth-child(even) { background: #1A1A1A; }
        ul, ol { padding-left: 25px; }
        li { margin: 5px 0; }
        li::marker { color: #00FF41; }
        hr {
            border: none;
            border-top: 1px solid #333;
            margin: 20px 0;
        }
        img {
            max-width: 100%;
            border-radius: 5px;
        }
        ::-webkit-scrollbar { width: 8px; height: 8px; }
        ::-webkit-scrollbar-track { background: #1A1A1A; }
        ::-webkit-scrollbar-thumb { background: #333; border-radius: 4px; }
        ::-webkit-scrollbar-thumb:hover { background: #00FF41; }
    </style>
</head>
<body>
{0}
</body>
</html>";

    public MainWindow()
    {
        InitializeComponent();
        
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        
        _previewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _previewTimer.Tick += PreviewTimer_Tick;
        
        InitializeWebView();
        
        // Set initial content
        txtEditor.Text = @"# Welcome to N01D Notes

A **cyberpunk** markdown editor with live preview.

## Features

- Real-time markdown rendering
- Dark hacker aesthetics
- Full GFM support
- Keyboard shortcuts

## Quick Start

| Shortcut | Action |
|----------|--------|
| Ctrl+N | New note |
| Ctrl+O | Open |
| Ctrl+S | Save |
| Ctrl+P | Toggle preview |

```python
# Sample code block
def hack():
    print('[ N01D :: INITIALIZED ]')
```

> ""The matrix has you..."" - N01D

---

Made by [bad-antics](https://github.com/bad-antics)
";
    }

    private async void InitializeWebView()
    {
        try
        {
            await webPreview.EnsureCoreWebView2Async();
            UpdatePreview();
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"WebView2 error: {ex.Message}";
        }
    }

    private void TxtEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _isModified = true;
        UpdateTitle();
        UpdateStats();
        
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        _previewTimer.Stop();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (!_previewVisible || webPreview.CoreWebView2 == null) return;
        
        try
        {
            var markdown = txtEditor.Text ?? "";
            var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
            var fullHtml = string.Format(HtmlTemplate, html);
            webPreview.NavigateToString(fullHtml);
        }
        catch { }
    }

    private void UpdateStats()
    {
        var text = txtEditor.Text ?? "";
        txtLines.Text = text.Split('\n').Length.ToString();
        txtWords.Text = string.IsNullOrWhiteSpace(text) ? "0" : 
            text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length.ToString();
        txtChars.Text = text.Length.ToString();
    }

    private void UpdateTitle()
    {
        var filename = string.IsNullOrEmpty(_currentFilePath) ? "untitled.md" : Path.GetFileName(_currentFilePath);
        txtFilename.Text = _isModified ? $"{filename} *" : filename;
        Title = $"N01D Notes - {filename}";
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscard()) return;
        
        txtEditor.Text = "";
        _currentFilePath = null;
        _isModified = false;
        UpdateTitle();
        txtStatus.Text = "New note created";
    }

    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscard()) return;
        
        var dialog = new OpenFileDialog
        {
            Filter = "Markdown Files (*.md;*.markdown)|*.md;*.markdown|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "Open Note"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                txtEditor.Text = File.ReadAllText(dialog.FileName);
                _currentFilePath = dialog.FileName;
                _isModified = false;
                UpdateTitle();
                txtStatus.Text = $"Opened: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath))
            BtnSaveAs_Click(sender, e);
        else
            SaveFile(_currentFilePath);
    }

    private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Markdown Files (*.md)|*.md|All Files (*.*)|*.*",
            Title = "Save Note As",
            DefaultExt = ".md"
        };
        
        if (dialog.ShowDialog() == true)
            SaveFile(dialog.FileName);
    }

    private void SaveFile(string path)
    {
        try
        {
            File.WriteAllText(path, txtEditor.Text);
            _currentFilePath = path;
            _isModified = false;
            UpdateTitle();
            txtStatus.Text = $"Saved: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool ConfirmDiscard()
    {
        if (!_isModified) return true;
        
        var result = MessageBox.Show(
            "You have unsaved changes. Do you want to save before continuing?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            BtnSave_Click(this, new RoutedEventArgs());
            return !_isModified;
        }
        
        return result == MessageBoxResult.No;
    }

    private void InsertMarkdown(string before, string after = "")
    {
        var selStart = txtEditor.SelectionStart;
        var selLength = txtEditor.SelectionLength;
        var selectedText = txtEditor.SelectedText;
        
        txtEditor.Text = txtEditor.Text.Insert(selStart + selLength, after);
        txtEditor.Text = txtEditor.Text.Insert(selStart, before);
        
        txtEditor.SelectionStart = selStart + before.Length;
        txtEditor.SelectionLength = selLength;
        txtEditor.Focus();
    }

    private void BtnBold_Click(object sender, RoutedEventArgs e) => InsertMarkdown("**", "**");
    private void BtnItalic_Click(object sender, RoutedEventArgs e) => InsertMarkdown("*", "*");
    private void BtnCode_Click(object sender, RoutedEventArgs e) => InsertMarkdown("`", "`");
    private void BtnLink_Click(object sender, RoutedEventArgs e) => InsertMarkdown("[", "](url)");
    private void BtnList_Click(object sender, RoutedEventArgs e) => InsertMarkdown("- ");

    private void BtnTogglePreview_Click(object sender, RoutedEventArgs e)
    {
        _previewVisible = !_previewVisible;
        
        if (_previewVisible)
        {
            PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitterColumn.Width = GridLength.Auto;
            previewBorder.Visibility = Visibility.Visible;
            UpdatePreview();
        }
        else
        {
            PreviewColumn.Width = new GridLength(0);
            SplitterColumn.Width = new GridLength(0);
            previewBorder.Visibility = Visibility.Collapsed;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!ConfirmDiscard())
            e.Cancel = true;
        base.OnClosing(e);
    }
}
