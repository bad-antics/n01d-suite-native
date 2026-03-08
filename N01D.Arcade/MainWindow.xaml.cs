using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Newtonsoft.Json;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace N01D.Arcade;

public partial class MainWindow : Window
{
    private readonly List<EmulatorSystem> _systems = new();
    private readonly List<GameEntry> _allGames = new();
    private List<GameEntry> _filteredGames = new();
    private EmulatorSystem? _selectedSystem;
    private GameEntry? _selectedGame;
    private readonly string _configPath;
    private ArcadeConfig _config;

    public MainWindow()
    {
        InitializeComponent();
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "N01D", "arcade-config.json");
        _config = LoadConfig();
        InitializeSystems();
        ScanAllGames();
        PopulateSystemList();
        UpdateCounts();
        TxtStatus.Text = "Ready — select a system to browse games";
    }

    // ═══════════════════════════════════════════════════════════════
    // SYSTEM DEFINITIONS
    // ═══════════════════════════════════════════════════════════════

    private void InitializeSystems()
    {
        _systems.Clear();

        // Detect all installed emulators
        var retroarch = FindExe(
            @"C:\RetroArch-Win64\retroarch.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RetroArch", "retroarch.exe"));

        var project64 = FindExe(
            @"C:\Program Files (x86)\Project64 3.0\Project64.exe",
            @"C:\Program Files\Project64 3.0\Project64.exe");

        var duckstation = FindExe(
            FindInWingetPackages("Stenzek.DuckStation", "duckstation-qt-x64-ReleaseLTCG.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "DuckStation", "duckstation-qt-x64-ReleaseLTCG.exe"));

        var dolphin = FindExe(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Dolphin", "Dolphin.exe"),
            @"C:\Program Files (x86)\Dolphin\Dolphin.exe");

        var ppsspp = FindExe(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PPSSPP", "PPSSPPWindows64.exe"),
            @"C:\Program Files (x86)\PPSSPP\PPSSPPWindows64.exe");

        var mgba = FindExe(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "mGBA", "mGBA.exe"),
            @"C:\Program Files (x86)\mGBA\mGBA.exe");

        var pcsx2 = FindExe(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PCSX2", "pcsx2-qt.exe"),
            @"C:\Program Files (x86)\PCSX2\pcsx2-qt.exe");

        var cemu = FindExe(
            FindInWingetPackages("Cemu.Cemu", "Cemu.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Cemu", "Cemu.exe"));

        var azahar = FindExe(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Azahar", "azahar.exe"),
            @"C:\Program Files (x86)\Azahar\azahar.exe");

        var fsuae = FindExe(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "FS-UAE", "FS-UAE", "Windows", "x86-64", "fs-uae.exe"));

        var romsRoot = _config.RomsPath;

        // ── NINTENDO ──
        _systems.Add(new EmulatorSystem
        {
            Name = "Nintendo 64",
            ShortName = "N64",
            Icon = "🎮",
            EmulatorName = "Project64",
            EmulatorPath = project64 ?? "",
            RomPath = Path.Combine(romsRoot, "N64"),
            Extensions = new[] { ".z64", ".n64", ".v64", ".rom" },
            LaunchArgs = "\"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "NES",
            ShortName = "NES",
            Icon = "🕹️",
            EmulatorName = "RetroArch (FCEUmm)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "NES"),
            Extensions = new[] { ".nes", ".unf", ".unif" },
            LaunchArgs = "-L cores\\fceumm_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "SNES",
            ShortName = "SNES",
            Icon = "🎮",
            EmulatorName = "RetroArch (Snes9x)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "SNES"),
            Extensions = new[] { ".smc", ".sfc", ".fig", ".swc" },
            LaunchArgs = "-L cores\\snes9x_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Game Boy",
            ShortName = "GB",
            Icon = "🟢",
            EmulatorName = "mGBA",
            EmulatorPath = mgba ?? "",
            RomPath = Path.Combine(romsRoot, "Game-Boy"),
            Extensions = new[] { ".gb" },
            LaunchArgs = "\"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Game Boy Color",
            ShortName = "GBC",
            Icon = "🟣",
            EmulatorName = "mGBA",
            EmulatorPath = mgba ?? "",
            RomPath = Path.Combine(romsRoot, "Game-Boy-Color"),
            Extensions = new[] { ".gbc" },
            LaunchArgs = "\"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Game Boy Advance",
            ShortName = "GBA",
            Icon = "🔵",
            EmulatorName = "mGBA",
            EmulatorPath = mgba ?? "",
            RomPath = Path.Combine(romsRoot, "Game-Boy-Advance"),
            Extensions = new[] { ".gba" },
            LaunchArgs = "\"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Nintendo DS",
            ShortName = "NDS",
            Icon = "📱",
            EmulatorName = "RetroArch (DeSmuME)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "Nintendo-DS"),
            Extensions = new[] { ".nds", ".dsi" },
            LaunchArgs = "-L cores\\desmume_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Nintendo 3DS",
            ShortName = "3DS",
            Icon = "🎲",
            EmulatorName = "Azahar",
            EmulatorPath = azahar ?? "",
            RomPath = Path.Combine(romsRoot, "Nintendo-3DS"),
            Extensions = new[] { ".3ds", ".cia", ".cxi", ".3dsx" },
            LaunchArgs = "\"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "GameCube",
            ShortName = "GCN",
            Icon = "🟪",
            EmulatorName = "Dolphin",
            EmulatorPath = dolphin ?? "",
            RomPath = Path.Combine(romsRoot, "GameCube"),
            Extensions = new[] { ".iso", ".gcm", ".gcz", ".ciso", ".rvz" },
            LaunchArgs = "/b /e \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Wii",
            ShortName = "Wii",
            Icon = "⬜",
            EmulatorName = "Dolphin",
            EmulatorPath = dolphin ?? "",
            RomPath = Path.Combine(romsRoot, "Wii"),
            Extensions = new[] { ".iso", ".wbfs", ".gcz", ".ciso", ".rvz", ".wad" },
            LaunchArgs = "/b /e \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Wii U",
            ShortName = "WiiU",
            Icon = "🔲",
            EmulatorName = "Cemu",
            EmulatorPath = cemu ?? "",
            RomPath = Path.Combine(romsRoot, "Wii-U"),
            Extensions = new[] { ".wud", ".wux", ".rpx", ".wua" },
            LaunchArgs = "-g \"{ROM}\""
        });

        // ── SONY ──
        _systems.Add(new EmulatorSystem
        {
            Name = "PlayStation 1",
            ShortName = "PS1",
            Icon = "⚫",
            EmulatorName = "DuckStation",
            EmulatorPath = duckstation ?? "",
            RomPath = Path.Combine(romsRoot, "PS1"),
            Extensions = new[] { ".bin", ".cue", ".iso", ".img", ".chd", ".pbp", ".ecm", ".mds" },
            LaunchArgs = "\"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "PlayStation 2",
            ShortName = "PS2",
            Icon = "🔵",
            EmulatorName = "PCSX2",
            EmulatorPath = pcsx2 ?? "",
            RomPath = Path.Combine(romsRoot, "PS2"),
            Extensions = new[] { ".iso", ".bin", ".chd", ".cso", ".gz" },
            LaunchArgs = "\"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "PSP",
            ShortName = "PSP",
            Icon = "⬛",
            EmulatorName = "PPSSPP",
            EmulatorPath = ppsspp ?? "",
            RomPath = Path.Combine(romsRoot, "PSP"),
            Extensions = new[] { ".iso", ".cso", ".pbp" },
            LaunchArgs = "\"{ROM}\""
        });

        // ── SEGA ──
        _systems.Add(new EmulatorSystem
        {
            Name = "Sega Master System",
            ShortName = "SMS",
            Icon = "🔴",
            EmulatorName = "RetroArch (Genesis Plus GX)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "Master-System"),
            Extensions = new[] { ".sms", ".gg" },
            LaunchArgs = "-L cores\\genesis_plus_gx_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Genesis / Mega Drive",
            ShortName = "GEN",
            Icon = "🟡",
            EmulatorName = "RetroArch (Genesis Plus GX)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "Genesis-MegaDrive"),
            Extensions = new[] { ".md", ".gen", ".bin", ".smd" },
            LaunchArgs = "-L cores\\genesis_plus_gx_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Sega Saturn",
            ShortName = "SAT",
            Icon = "🪐",
            EmulatorName = "RetroArch (Beetle Saturn)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "Saturn"),
            Extensions = new[] { ".iso", ".bin", ".cue", ".chd" },
            LaunchArgs = "-L cores\\mednafen_saturn_libretro.dll \"{ROM}\""
        });

        // ── OTHER ──
        _systems.Add(new EmulatorSystem
        {
            Name = "TurboGrafx-16",
            ShortName = "TG16",
            Icon = "🟠",
            EmulatorName = "RetroArch (Beetle PCE)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "TurboGrafx-16"),
            Extensions = new[] { ".pce", ".cue", ".ccd", ".chd" },
            LaunchArgs = "-L cores\\mednafen_pce_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Arcade (MAME)",
            ShortName = "MAME",
            Icon = "🕹️",
            EmulatorName = "RetroArch (MAME)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "MAME"),
            Extensions = new[] { ".zip", ".7z" },
            LaunchArgs = "-L cores\\mame_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Atari 2600",
            ShortName = "2600",
            Icon = "🟤",
            EmulatorName = "RetroArch (Stella)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "Atari-2600"),
            Extensions = new[] { ".a26", ".bin" },
            LaunchArgs = "-L cores\\stella_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Atari 7800",
            ShortName = "7800",
            Icon = "🟫",
            EmulatorName = "RetroArch (ProSystem)",
            EmulatorPath = retroarch ?? "",
            RomPath = Path.Combine(romsRoot, "Atari-7800"),
            Extensions = new[] { ".a78", ".bin" },
            LaunchArgs = "-L cores\\prosystem_libretro.dll \"{ROM}\""
        });

        _systems.Add(new EmulatorSystem
        {
            Name = "Commodore Amiga",
            ShortName = "Amiga",
            Icon = "💾",
            EmulatorName = "FS-UAE",
            EmulatorPath = fsuae ?? "",
            RomPath = Path.Combine(romsRoot, "Amiga"),
            Extensions = new[] { ".adf", ".ipf", ".dms", ".adz", ".hdf", ".lha" },
            LaunchArgs = "--floppy_drive_0=\"{ROM}\""
        });

        // Create ROM directories if they don't exist
        foreach (var sys in _systems)
        {
            Directory.CreateDirectory(sys.RomPath);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // ROM SCANNING
    // ═══════════════════════════════════════════════════════════════

    private void ScanAllGames()
    {
        _allGames.Clear();
        foreach (var sys in _systems)
        {
            if (!Directory.Exists(sys.RomPath)) continue;
            try
            {
                var files = Directory.GetFiles(sys.RomPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => sys.Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => Path.GetFileNameWithoutExtension(f));

                foreach (var file in files)
                {
                    _allGames.Add(new GameEntry
                    {
                        Title = CleanGameTitle(Path.GetFileNameWithoutExtension(file)),
                        FileName = Path.GetFileName(file),
                        FullPath = file,
                        FileSize = new FileInfo(file).Length,
                        System = sys
                    });
                }
            }
            catch { /* skip inaccessible dirs */ }
        }
    }

    private static string CleanGameTitle(string filename)
    {
        // Remove common ROM naming conventions like (U), [!], (Europe), etc.
        var title = filename;
        // Remove region/version tags in parentheses/brackets but keep the game name
        var cleaned = System.Text.RegularExpressions.Regex.Replace(title, @"\s*[\(\[][^\)\]]*[\)\]]", "").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? title : cleaned;
    }

    // ═══════════════════════════════════════════════════════════════
    // UI POPULATION
    // ═══════════════════════════════════════════════════════════════

    private void PopulateSystemList()
    {
        LstSystems.Items.Clear();

        // "All Systems" entry
        var allItem = new ListBoxItem
        {
            Content = $"🎯  ALL SYSTEMS ({_allGames.Count})",
            Tag = (EmulatorSystem?)null,
            FontWeight = FontWeights.Bold
        };
        LstSystems.Items.Add(allItem);

        foreach (var sys in _systems)
        {
            var gameCount = _allGames.Count(g => g.System == sys);
            var status = File.Exists(sys.EmulatorPath) ? "✓" : "✗";
            var statusColor = File.Exists(sys.EmulatorPath) ? "" : " [NO EMU]";

            var item = new ListBoxItem
            {
                Content = $"{sys.Icon}  {sys.Name} ({gameCount}){statusColor}",
                Tag = sys,
                ToolTip = $"Emulator: {sys.EmulatorName}\nPath: {sys.RomPath}\nFormats: {string.Join(", ", sys.Extensions)}"
            };
            LstSystems.Items.Add(item);
        }
    }

    private void PopulateGameList(IEnumerable<GameEntry> games)
    {
        LstGames.Items.Clear();
        _filteredGames = games.ToList();

        foreach (var game in _filteredGames)
        {
            var item = new ListBoxItem
            {
                Content = $"{game.System.Icon}  {game.Title}",
                Tag = game,
                ToolTip = $"{game.FileName}\n{FormatSize(game.FileSize)}\n{game.System.Name}"
            };
            LstGames.Items.Add(item);
        }
    }

    private void UpdateCounts()
    {
        var installedCount = _systems.Count(s => File.Exists(s.EmulatorPath));
        TxtSystemCount.Text = $"{installedCount}/{_systems.Count} systems";
        TxtGameCount.Text = $"{_allGames.Count} games";
    }

    // ═══════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════════

    private void LstSystems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstSystems.SelectedItem is not ListBoxItem item) return;
        _selectedSystem = item.Tag as EmulatorSystem;

        if (_selectedSystem == null)
        {
            // "All Systems" selected
            PopulateGameList(_allGames);
            TxtStatus.Text = $"Showing all {_allGames.Count} games across {_systems.Count} systems";
        }
        else
        {
            var games = _allGames.Where(g => g.System == _selectedSystem);
            PopulateGameList(games);
            TxtStatus.Text = $"{_selectedSystem.Name} — {games.Count()} games | Emulator: {_selectedSystem.EmulatorName}";

            TxtEmulatorName.Text = _selectedSystem.EmulatorName;
            TxtFileFormats.Text = string.Join(", ", _selectedSystem.Extensions);
            TxtRomPath.Text = _selectedSystem.RomPath;
        }

        // Clear game selection
        _selectedGame = null;
        BtnLaunch.IsEnabled = false;
        TxtGameTitle.Text = "SELECT A GAME";
        TxtGameSystem.Text = "";
        TxtGameFile.Text = "";
        TxtGameSize.Text = "";
    }

    private void LstGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstGames.SelectedItem is not ListBoxItem item) return;
        if (item.Tag is not GameEntry game) return;

        _selectedGame = game;
        TxtGameTitle.Text = game.Title;
        TxtGameSystem.Text = $"{game.System.Icon} {game.System.Name}";
        TxtGameFile.Text = game.FileName;
        TxtGameSize.Text = FormatSize(game.FileSize);
        TxtEmulatorName.Text = game.System.EmulatorName;
        TxtFileFormats.Text = string.Join(", ", game.System.Extensions);
        TxtRomPath.Text = game.System.RomPath;

        BtnLaunch.IsEnabled = File.Exists(game.System.EmulatorPath);

        if (!File.Exists(game.System.EmulatorPath))
        {
            TxtStatus.Text = $"⚠ Emulator not found: {game.System.EmulatorName}";
        }
        else
        {
            TxtStatus.Text = $"Ready to launch: {game.Title}";
        }
    }

    private void LstGames_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_selectedGame != null && File.Exists(_selectedGame.System.EmulatorPath))
        {
            LaunchGame(_selectedGame);
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = TxtSearch.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(query))
        {
            if (_selectedSystem != null)
                PopulateGameList(_allGames.Where(g => g.System == _selectedSystem));
            else
                PopulateGameList(_allGames);
            return;
        }

        var source = _selectedSystem != null
            ? _allGames.Where(g => g.System == _selectedSystem)
            : _allGames;

        var filtered = source.Where(g =>
            g.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            g.FileName.Contains(query, StringComparison.OrdinalIgnoreCase));

        PopulateGameList(filtered);
        TxtStatus.Text = $"Search: {_filteredGames.Count} results for \"{query}\"";
    }

    private void BtnLaunch_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame != null)
            LaunchGame(_selectedGame);
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = _selectedGame?.System.RomPath ?? _selectedSystem?.RomPath ?? _config.RomsPath;
        if (Directory.Exists(path))
            Process.Start("explorer.exe", path);
        else
            MessageBox.Show($"Directory not found:\n{path}", "N01D Arcade", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void BtnOpenEmulator_Click(object sender, RoutedEventArgs e)
    {
        var sys = _selectedGame?.System ?? _selectedSystem;
        if (sys == null) return;

        if (File.Exists(sys.EmulatorPath))
            Process.Start(new ProcessStartInfo(sys.EmulatorPath) { UseShellExecute = true });
        else
            MessageBox.Show($"Emulator not found:\n{sys.EmulatorName}\n\nExpected at:\n{sys.EmulatorPath}",
                "N01D Arcade", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(_config, _systems);
        if (dlg.ShowDialog() == true)
        {
            _config = dlg.Config;
            SaveConfig();
            InitializeSystems();
            ScanAllGames();
            PopulateSystemList();
            UpdateCounts();
            TxtStatus.Text = "Settings saved — systems refreshed";
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // GAME LAUNCHING
    // ═══════════════════════════════════════════════════════════════

    private void LaunchGame(GameEntry game)
    {
        if (!File.Exists(game.System.EmulatorPath))
        {
            MessageBox.Show($"Emulator not found:\n{game.System.EmulatorName}\n\n{game.System.EmulatorPath}",
                "N01D Arcade", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var args = game.System.LaunchArgs.Replace("{ROM}", game.FullPath);
            var psi = new ProcessStartInfo
            {
                FileName = game.System.EmulatorPath,
                Arguments = args,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(game.System.EmulatorPath) ?? ""
            };

            Process.Start(psi);
            TxtStatus.Text = $"▶ Launched: {game.Title} via {game.System.EmulatorName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch game:\n{ex.Message}", "N01D Arcade",
                MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = $"✗ Launch failed: {ex.Message}";
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private static string? FindExe(params string?[] paths)
    {
        foreach (var p in paths)
        {
            if (!string.IsNullOrEmpty(p) && File.Exists(p))
                return p;
        }
        return null;
    }

    private static string? FindInWingetPackages(string packageId, string exeName)
    {
        try
        {
            var wingetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Packages");
            if (!Directory.Exists(wingetDir)) return null;

            var matches = Directory.GetFiles(wingetDir, exeName, SearchOption.AllDirectories);
            return matches.FirstOrDefault(f => f.Contains(packageId, StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:F1} {units[unit]}";
    }

    // ═══════════════════════════════════════════════════════════════
    // CONFIG
    // ═══════════════════════════════════════════════════════════════

    private ArcadeConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonConvert.DeserializeObject<ArcadeConfig>(json) ?? ArcadeConfig.Default();
            }
        }
        catch { }
        return ArcadeConfig.Default();
    }

    private void SaveConfig()
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_configPath, JsonConvert.SerializeObject(_config, Formatting.Indented));
        }
        catch { }
    }
}

// ═══════════════════════════════════════════════════════════════
// DATA MODELS
// ═══════════════════════════════════════════════════════════════

public class EmulatorSystem
{
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string Icon { get; set; } = "🎮";
    public string EmulatorName { get; set; } = "";
    public string EmulatorPath { get; set; } = "";
    public string RomPath { get; set; } = "";
    public string[] Extensions { get; set; } = Array.Empty<string>();
    public string LaunchArgs { get; set; } = "\"{ROM}\"";
}

public class GameEntry
{
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FullPath { get; set; } = "";
    public long FileSize { get; set; }
    public EmulatorSystem System { get; set; } = new();
}

public class ArcadeConfig
{
    public string RomsPath { get; set; } = @"W:\Emulation\ROMs";
    public Dictionary<string, string> EmulatorOverrides { get; set; } = new();

    public static ArcadeConfig Default() => new()
    {
        RomsPath = @"W:\Emulation\ROMs",
        EmulatorOverrides = new()
    };
}

// ═══════════════════════════════════════════════════════════════
// CONVERTERS
// ═══════════════════════════════════════════════════════════════

public class ZeroToVisibleConverter : IValueConverter
{
    public static readonly ZeroToVisibleConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int len)
            return len == 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
