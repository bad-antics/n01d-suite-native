using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using N01D.Overwatch.Models;
using N01D.Overwatch.Services;

namespace N01D.Overwatch
{
    public partial class MainWindow : Window
    {
        private readonly RssFeedService _rss = new();
        private readonly FlightTrackingService _flights = new();
        private readonly ShipTrackingService _ships = new();
        private readonly AlertService _alerts = new();
        private readonly EclipseService _eclipse = new();
        private readonly MissileDefenseService _missiles = new();
        private readonly WarMonitoringService _warOps = new();

        private readonly ObservableCollection<EventViewModel> _timelineItems = new();
        private readonly List<ConflictEvent> _allEvents = new();
        private DispatcherTimer? _autoRefreshTimer;
        private DispatcherTimer? _eclipseTimer;
        private bool _isLoading;
        private bool _initialized;

        public MainWindow()
        {
            InitializeComponent();
            lstTimeline.ItemsSource = _timelineItems;
            LoadAlertRules();
            LoadEclipseData();
            LoadMissileDefenseData();
            _initialized = true;
            Loaded += async (_, _) => await RefreshAllAsync();
            InitMap();
            StartEclipseCountdown();
        }

        // ═══════════════════════════════════════════
        //  REFRESH ALL DATA SOURCES
        // ═══════════════════════════════════════════

        private async Task RefreshAllAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            txtStatus.Text = "⟳ Scanning feeds...";
            btnRefresh.IsEnabled = false;

            try
            {
                var tasks = new List<Task>();

                // RSS Feeds
                var rssTask = _rss.FetchAllAsync();
                tasks.Add(rssTask);

                // Ships
                var shipTask = _ships.FetchVesselsAsync();
                tasks.Add(shipTask);

                await Task.WhenAll(tasks);

                // Process RSS events
                var rssEvents = await rssTask;
                foreach (var ev in rssEvents)
                {
                    _alerts.EvaluateEvent(ev);
                    if (!_allEvents.Any(e => e.Title == ev.Title && e.Source == ev.Source))
                        _allEvents.Add(ev);
                }

                // Process ship data
                var vessels = await shipTask;
                _allEvents.RemoveAll(e => e.DataSource == DataSource.ShipTracker);
                foreach (var v in vessels)
                    _allEvents.Add(_ships.ToConflictEvent(v));

                dgVessels.ItemsSource = vessels;

                ApplyFilters();
                UpdateAlertCount();
                UpdateThreatLevel();
                UpdateMap();

                txtLastUpdate.Text = $"Last update: {DateTime.Now:HH:mm:ss}";
                txtStatus.Text = $"✓ {_allEvents.Count} events tracked";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"⚠ Error: {ex.Message}";
            }
            finally
            {
                _isLoading = false;
                btnRefresh.IsEnabled = true;
            }
        }

        // ═══════════════════════════════════════════
        //  FLIGHT SCANNING
        // ═══════════════════════════════════════════

        private async void BtnScanFlights_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "✈ Scanning Middle East airspace...";
            try
            {
                var flights = await _flights.FetchMilitaryFlightsAsync();
                dgFlights.ItemsSource = flights;
                txtFlightCount.Text = $"{flights.Count} military aircraft detected";

                // Add to timeline
                _allEvents.RemoveAll(e2 => e2.DataSource == DataSource.FlightTracker);
                foreach (var f in flights)
                {
                    var ev = _flights.ToConflictEvent(f);
                    if (ev != null)
                    {
                        _alerts.EvaluateEvent(ev);
                        _allEvents.Add(ev);
                    }
                }

                ApplyFilters();
                UpdateMap();
                UpdateMapFlights(flights);
                txtStatus.Text = $"✈ {flights.Count} military flights tracked";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"⚠ Flight scan error: {ex.Message}";
            }
        }

        private async void BtnScanShips_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "🚢 Scanning maritime zones...";
            try
            {
                var vessels = await _ships.FetchVesselsAsync();
                dgVessels.ItemsSource = vessels;
                txtStatus.Text = $"🚢 {vessels.Count} maritime contacts";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"⚠ Maritime scan error: {ex.Message}";
            }
        }

        // ═══════════════════════════════════════════
        //  WAR OPS MONITORING
        // ═══════════════════════════════════════════

        private async void BtnScanWarOps_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "⚔ Scanning war operations feeds...";
            try
            {
                var events = await _warOps.FetchAllAsync();

                // Classify and add to timeline
                foreach (var ev in events)
                {
                    _alerts.EvaluateEvent(ev);
                    if (!_allEvents.Any(e2 => e2.Title == ev.Title && e2.Source == ev.Source))
                        _allEvents.Add(ev);
                }

                // Populate sub-tabs
                var sigacts = events.Where(e2 => e2.Tags.Contains("SIGACT")).Select(e2 => new EventViewModel(e2)).ToList();
                var sanctions = events.Where(e2 => e2.Tags.Contains("SANCTIONS")).Select(e2 => new EventViewModel(e2)).ToList();
                var cyberOps = events.Where(e2 => e2.Tags.Contains("CYBER")).Select(e2 => new EventViewModel(e2)).ToList();
                var proxy = events.Where(e2 => e2.Tags.Contains("PROXY")).Select(e2 => new EventViewModel(e2)).ToList();

                lstSigact.ItemsSource = sigacts;
                lstSanctions.ItemsSource = sanctions;
                lstCyberOps.ItemsSource = cyberOps;
                lstProxy.ItemsSource = proxy;

                var total = events.Count;
                txtWarOpsCount.Text = $"{total} war actions detected";

                ApplyFilters();
                UpdateMap();
                txtStatus.Text = $"⚔ {total} war actions tracked ({sigacts.Count} SIGACT, {sanctions.Count} sanctions, {cyberOps.Count} cyber, {proxy.Count} proxy)";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"⚠ War ops scan error: {ex.Message}";
            }
        }

        // ═══════════════════════════════════════════
        //  FILTERING & TIMELINE
        // ═══════════════════════════════════════════

        private void ApplyFilters()
        {
            if (!_initialized) return;
            var search = txtSearch.Text?.Trim().ToLowerInvariant() ?? "";
            var enabledCategories = new HashSet<EventCategory>();
            if (chkMilitary.IsChecked == true) enabledCategories.Add(EventCategory.Military);
            if (chkDiplomatic.IsChecked == true) enabledCategories.Add(EventCategory.Diplomatic);
            if (chkEconomic.IsChecked == true) enabledCategories.Add(EventCategory.Economic);
            if (chkNuclear.IsChecked == true) enabledCategories.Add(EventCategory.Nuclear);
            if (chkCyber.IsChecked == true) enabledCategories.Add(EventCategory.Cyber);
            if (chkHumanitarian.IsChecked == true) enabledCategories.Add(EventCategory.Humanitarian);
            if (chkIntelligence.IsChecked == true) enabledCategories.Add(EventCategory.Intelligence);

            var filtered = _allEvents
                .Where(e => enabledCategories.Contains(e.Category))
                .Where(e => string.IsNullOrEmpty(search) ||
                            e.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            e.Summary.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            e.Source.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            e.Tags.Any(t => t.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(e => e.IsAlert)
                .ThenByDescending(e => e.Severity)
                .ThenByDescending(e => e.Timestamp)
                .Take(500)
                .ToList();

            _timelineItems.Clear();
            foreach (var e in filtered)
                _timelineItems.Add(new EventViewModel(e));
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        // ═══════════════════════════════════════════
        //  EVENT DETAIL
        // ═══════════════════════════════════════════

        private void LstTimeline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTimeline.SelectedItem is EventViewModel vm)
            {
                txtDetailTitle.Text = vm.Title;
                txtDetailSeverity.Text = $"Severity: {vm.SeverityLabel}";
                txtDetailSeverity.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(vm.SeverityColor)!);
                txtDetailCategory.Text = $"Category: {vm.Event.Category}";
                txtDetailSource.Text = $"Source: {vm.Source}";
                txtDetailTime.Text = $"Time: {vm.Event.Timestamp:yyyy-MM-dd HH:mm:ss UTC}";
                txtDetailLocation.Text = string.IsNullOrEmpty(vm.Event.Location) ? "" : $"Location: {vm.Event.Location}";
                txtDetailSummary.Text = vm.Summary;
                txtDetailTags.Text = vm.TagsDisplay;
                btnOpenUrl.Visibility = string.IsNullOrEmpty(vm.Event.SourceUrl) ? Visibility.Collapsed : Visibility.Visible;
                btnOpenUrl.Tag = vm.Event.SourceUrl;

                // Fly map to event location if it has coordinates
                if (vm.Event.Latitude.HasValue && vm.Event.Longitude.HasValue)
                {
                    try
                    {
                        webMap.CoreWebView2?.ExecuteScriptAsync(
                            $"flyTo({vm.Event.Latitude},{vm.Event.Longitude},8)");
                    }
                    catch { }
                }
            }
        }

        private void BtnOpenUrl_Click(object sender, RoutedEventArgs e)
        {
            if (btnOpenUrl.Tag is string url && !string.IsNullOrEmpty(url))
            {
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            }
        }

        // ═══════════════════════════════════════════
        //  ALERTS
        // ═══════════════════════════════════════════

        private void LoadAlertRules()
        {
            var vms = _alerts.Rules.Select(r => new AlertRuleViewModel(r)).ToList();
            lstAlerts.ItemsSource = vms;
        }

        private void UpdateAlertCount()
        {
            var count = _allEvents.Count(e => e.IsAlert);
            txtAlertCount.Text = $"⚠ {count} ALERT{(count != 1 ? "S" : "")}";
        }

        private void UpdateThreatLevel()
        {
            var criticals = _allEvents.Count(e => e.Severity == SeverityLevel.Critical);
            var highs = _allEvents.Count(e => e.Severity == SeverityLevel.High);

            string level;
            string color;
            if (criticals > 0) { level = "CRITICAL"; color = "#EE3333"; }
            else if (highs > 5) { level = "HIGH"; color = "#EE3333"; }
            else if (highs > 0) { level = "ELEVATED"; color = "#FF8833"; }
            else { level = "GUARDED"; color = "#33CC33"; }

            txtThreatLevel.Text = $"THREAT LEVEL: {level}";
            txtThreatLevel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        }

        // ═══════════════════════════════════════════
        //  EXPORT
        // ═══════════════════════════════════════════

        private void BtnExportJson_Click(object sender, RoutedEventArgs e)
        {
            _alerts.ExportArchive(_allEvents, "json");
            txtStatus.Text = $"📥 Exported {_allEvents.Count} events to JSON — {_alerts.GetArchivePath()}";
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            _alerts.ExportArchive(_allEvents, "csv");
            txtStatus.Text = $"📥 Exported {_allEvents.Count} events to CSV — {_alerts.GetArchivePath()}";
        }

        // ═══════════════════════════════════════════
        //  MAP (WebView2 — Leaflet.js)
        // ═══════════════════════════════════════════

        private async void InitMap()
        {
            try
            {
                await webMap.EnsureCoreWebView2Async();
                webMap.CoreWebView2.NavigateToString(BuildMapHtml());

                // Handle messages from map JavaScript (event clicks, URL opens)
                webMap.CoreWebView2.WebMessageReceived += (_, args) =>
                {
                    try
                    {
                        var json = System.Text.Json.JsonDocument.Parse(args.WebMessageAsJson.Trim('"').Replace("\\\"", "\"").Replace("\\\\", "\\"));
                        var root = json.RootElement;
                        var msgType = root.GetProperty("type").GetString();

                        if (msgType == "selectEvent")
                        {
                            var id = root.GetProperty("id").GetString();
                            var vm = _timelineItems.FirstOrDefault(t => t.Event.Id == id);
                            if (vm != null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    tabMain.SelectedIndex = 0; // Switch to Timeline tab
                                    lstTimeline.SelectedItem = vm;
                                    lstTimeline.ScrollIntoView(vm);
                                });
                            }
                        }
                        else if (msgType == "openUrl")
                        {
                            var url = root.GetProperty("url").GetString();
                            if (!string.IsNullOrEmpty(url))
                                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        }
                        else if (msgType == "selectFlight")
                        {
                            Dispatcher.Invoke(() => tabMain.SelectedIndex = 2); // Switch to Flights tab
                        }
                    }
                    catch { }
                };
            }
            catch { }
        }

        private void UpdateMap()
        {
            try
            {
                // ─── Event markers with rich data ───
                var geoEvents = _allEvents.Where(e => e.Latitude.HasValue && e.Longitude.HasValue).ToList();
                var markers = string.Join(",", geoEvents.Select(e =>
                {
                    var color = e.Severity switch
                    {
                        SeverityLevel.Critical => "#EE3333",
                        SeverityLevel.High => "#FF8833",
                        SeverityLevel.Medium => "#3388FF",
                        _ => "#6A6A80"
                    };
                    var sev = (int)e.Severity;
                    var icon = e.DataSource switch
                    {
                        DataSource.FlightTracker => "✈",
                        DataSource.ShipTracker => "⚓",
                        DataSource.OilPrice => "🛢",
                        _ => e.Category switch
                        {
                            EventCategory.Military => "⚔",
                            EventCategory.Nuclear => "☢",
                            EventCategory.Cyber => "💻",
                            EventCategory.Diplomatic => "🏛",
                            _ => "●"
                        }
                    };
                    var catLabel = e.Category.ToString().ToUpperInvariant();
                    var title = Escape(e.Title);
                    var summary = Escape(e.Summary.Length > 200 ? e.Summary[..200] + "..." : e.Summary);
                    var source = Escape(e.Source);
                    var url = Escape(e.SourceUrl);
                    var time = e.Timestamp.ToString("yyyy-MM-dd HH:mm");
                    var tags = e.Tags.Count > 0 ? Escape(string.Join(" ", e.Tags.Select(t => $"[{t}]"))) : "";

                    return $"{{lat:{e.Latitude},lon:{e.Longitude},title:'{title}',color:'{color}',sev:{sev}," +
                           $"icon:'{icon}',cat:'{catLabel}',summary:'{summary}',source:'{source}'," +
                           $"url:'{url}',time:'{time}',tags:'{tags}',id:'{e.Id}'}}";
                }));

                webMap.CoreWebView2?.ExecuteScriptAsync($"updateMarkers([{markers}])");

                // ─── Heatmap density circles ───
                webMap.CoreWebView2?.ExecuteScriptAsync($"updateHeatmap([{markers}])");

                // ─── Missile sites, defense systems, eclipse paths ───
                UpdateMapMissiles();
                UpdateMapDefense();
                UpdateMapEclipses();
            }
            catch { }
        }

        /// <summary>Updates flight markers on the map with heading-rotated aircraft icons.</summary>
        private void UpdateMapFlights(List<FlightData> flights)
        {
            try
            {
                var flightMarkers = string.Join(",", flights.Select(f =>
                {
                    var sev = FlightTrackingService.ClassifyFlightSeverity(f);
                    return $"{{lat:{f.Latitude},lon:{f.Longitude},callsign:'{Escape(f.Callsign)}'," +
                           $"type:'{Escape(f.AircraftType)}',country:'{Escape(f.Country)}'," +
                           $"alt:{f.Altitude},spd:{f.Speed},hdg:{f.Heading}," +
                           $"icao:'{Escape(f.Registration)}',sev:{(int)sev}}}";
                }));
                webMap.CoreWebView2?.ExecuteScriptAsync($"updateFlights([{flightMarkers}])");
            }
            catch { }
        }

        private static string Escape(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");

        private static string BuildMapHtml()
        {
            return """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8"/>
                <style>
                    * { box-sizing: border-box; }
                    body { margin: 0; background: #080810; font-family: Consolas, monospace; }
                    #map { width: 100vw; height: 100vh; }

                    /* XChat-themed popup */
                    .event-popup { min-width: 280px; max-width: 380px; }
                    .event-popup .popup-header {
                        display: flex; align-items: center; gap: 8px;
                        padding: 8px 10px; border-radius: 4px 4px 0 0;
                        font-weight: bold; font-size: 13px;
                    }
                    .event-popup .popup-body {
                        padding: 8px 10px; font-size: 11px; color: #D0D0D0;
                        background: #10101C; border-radius: 0 0 4px 4px;
                        line-height: 1.6;
                    }
                    .event-popup .popup-body .meta { color: #6A6A80; font-size: 10px; margin-top: 6px; }
                    .event-popup .popup-body .tags { color: #AA55FF; font-size: 10px; margin-top: 3px; }
                    .event-popup .popup-body .open-link {
                        display: inline-block; margin-top: 8px; padding: 3px 10px;
                        background: #161625; color: #3388FF; border: 1px solid #2A2A3E;
                        border-radius: 3px; cursor: pointer; font-size: 10px;
                        text-decoration: none; font-family: Consolas, monospace;
                    }
                    .event-popup .popup-body .open-link:hover { background: #1E1E35; border-color: #3388FF; }

                    /* Severity badge */
                    .sev-badge {
                        padding: 1px 6px; border-radius: 3px; font-size: 9px;
                        font-weight: bold; letter-spacing: 0.5px;
                    }
                    .sev-critical { background: #EE333344; color: #EE3333; border: 1px solid #EE333366; }
                    .sev-high { background: #FF883344; color: #FF8833; border: 1px solid #FF883366; }
                    .sev-medium { background: #3388FF44; color: #3388FF; border: 1px solid #3388FF66; }
                    .sev-low { background: #6A6A8044; color: #6A6A80; border: 1px solid #6A6A8066; }

                    /* Legend panel */
                    .legend {
                        position: absolute; bottom: 12px; left: 12px; z-index: 1000;
                        background: #080810ee; border: 1px solid #2A2A3E; border-radius: 4px;
                        padding: 10px 14px; font-size: 10px; color: #D0D0D0;
                        backdrop-filter: blur(8px); max-width: 200px;
                    }
                    .legend h4 { margin: 0 0 8px 0; color: #3388FF; font-size: 11px; }
                    .legend-item { display: flex; align-items: center; gap: 6px; margin: 4px 0; }
                    .legend-dot { width: 10px; height: 10px; border-radius: 50%; display: inline-block; }
                    .legend-line { width: 16px; height: 0; border-top: 2px dashed; display: inline-block; }

                    /* Stats overlay */
                    .stats-panel {
                        position: absolute; top: 12px; right: 12px; z-index: 1000;
                        background: #080810ee; border: 1px solid #2A2A3E; border-radius: 4px;
                        padding: 10px 14px; font-size: 10px; color: #D0D0D0;
                        backdrop-filter: blur(8px); min-width: 190px;
                    }
                    .stats-panel h4 { margin: 0 0 6px 0; color: #AA55FF; font-size: 11px; }
                    .stat-row { display: flex; justify-content: space-between; margin: 3px 0; }
                    .stat-val { font-weight: bold; }

                    /* Layer control */
                    .layer-control {
                        position: absolute; top: 12px; left: 12px; z-index: 1000;
                        background: #080810ee; border: 1px solid #2A2A3E; border-radius: 4px;
                        padding: 10px 14px; font-size: 10px; color: #D0D0D0;
                        backdrop-filter: blur(8px);
                    }
                    .layer-control h4 { margin: 0 0 6px 0; color: #33CCCC; font-size: 11px; }
                    .layer-toggle { display: flex; align-items: center; gap: 6px; margin: 3px 0; cursor: pointer; }
                    .layer-toggle:hover { color: #3388FF; }
                    .layer-toggle input { accent-color: #3388FF; }

                    /* Pulse animation for critical markers */
                    @keyframes pulse-ring {
                        0% { r: 6; opacity: 1; }
                        100% { r: 20; opacity: 0; }
                    }

                    /* Custom Leaflet overrides */
                    .leaflet-popup-content-wrapper { background: #161625 !important; border: 1px solid #2A2A3E; border-radius: 4px !important; padding: 0 !important; box-shadow: 0 4px 20px rgba(0,0,0,0.6) !important; }
                    .leaflet-popup-content { margin: 0 !important; }
                    .leaflet-popup-tip { background: #161625 !important; border: 1px solid #2A2A3E; }
                    .leaflet-control-zoom a { background: #161625 !important; color: #3388FF !important; border-color: #2A2A3E !important; }
                    .leaflet-control-zoom a:hover { background: #1E1E35 !important; }
                    .leaflet-control-attribution { background: #080810cc !important; color: #6A6A80 !important; font-size: 9px !important; }
                    .leaflet-control-attribution a { color: #3388FF !important; }
                </style>
                <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
                <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
            </head>
            <body>
                <div id="map"></div>

                <!-- Layer Control -->
                <div class="layer-control" id="layerControl">
                    <h4>⚙ LAYERS</h4>
                    <label class="layer-toggle"><input type="checkbox" id="togEvents" checked onchange="toggleLayer('events')"> 📡 Events</label>
                    <label class="layer-toggle"><input type="checkbox" id="togFlights" checked onchange="toggleLayer('flights')"> ✈ Flights</label>
                    <label class="layer-toggle"><input type="checkbox" id="togZones" checked onchange="toggleLayer('zones')"> 🎯 Strategic Zones</label>
                    <label class="layer-toggle"><input type="checkbox" id="togBases" checked onchange="toggleLayer('bases')"> 🏴 Military Bases</label>
                    <label class="layer-toggle"><input type="checkbox" id="togPipelines" checked onchange="toggleLayer('pipelines')"> 🛢 Oil Routes</label>
                    <label class="layer-toggle"><input type="checkbox" id="togMissiles" checked onchange="toggleLayer('missiles')"> 🚀 Missile Sites</label>
                    <label class="layer-toggle"><input type="checkbox" id="togDefense" checked onchange="toggleLayer('defense')"> 🛡 Air Defense</label>
                    <label class="layer-toggle"><input type="checkbox" id="togEclipse" onchange="toggleLayer('eclipse')"> 🌑 Eclipse Paths</label>
                    <label class="layer-toggle"><input type="checkbox" id="togHeatmap" onchange="toggleLayer('heatmap')"> 🔥 Event Density</label>
                </div>

                <!-- Stats -->
                <div class="stats-panel" id="statsPanel">
                    <h4>📊 MAP INTEL</h4>
                    <div class="stat-row"><span>Events plotted:</span><span class="stat-val" id="statEvents">0</span></div>
                    <div class="stat-row"><span>Aircraft tracked:</span><span class="stat-val" id="statFlights">0</span></div>
                    <div class="stat-row"><span>Critical zones:</span><span class="stat-val" id="statZones" style="color:#EE3333">5</span></div>
                    <div class="stat-row"><span>Missile sites:</span><span class="stat-val" id="statMissiles" style="color:#FF8833">0</span></div>
                    <div class="stat-row"><span>Defense systems:</span><span class="stat-val" id="statDefense" style="color:#33CCCC">0</span></div>
                    <div class="stat-row"><span>Coverage area:</span><span class="stat-val">3.2M km²</span></div>
                </div>

                <!-- Legend -->
                <div class="legend">
                    <h4>SEVERITY</h4>
                    <div class="legend-item"><span class="legend-dot" style="background:#EE3333"></span> Critical</div>
                    <div class="legend-item"><span class="legend-dot" style="background:#FF8833"></span> High</div>
                    <div class="legend-item"><span class="legend-dot" style="background:#3388FF"></span> Medium</div>
                    <div class="legend-item"><span class="legend-dot" style="background:#6A6A80"></span> Low</div>
                    <h4 style="margin-top:10px">MARKERS</h4>
                    <div class="legend-item"><span class="legend-dot" style="background:#AA55FF"></span> Nuclear Site</div>
                    <div class="legend-item"><span class="legend-dot" style="background:#33CC33"></span> Military Base</div>
                    <div class="legend-item"><span class="legend-line" style="border-color:#DDCC33"></span> Oil Route</div>
                    <div class="legend-item"><span class="legend-line" style="border-color:#EE3333"></span> Chokepoint</div>
                    <div class="legend-item"><span class="legend-dot" style="background:#FF5555"></span> 🚀 Missile Site</div>
                    <div class="legend-item"><span class="legend-dot" style="background:#33CCCC;border:2px solid #33CCCC;width:6px;height:6px"></span> 🛡 Air Defense</div>
                    <div class="legend-item"><span class="legend-line" style="border-color:#DDCC33;border-top-style:dotted"></span> Eclipse Path</div>
                </div>

                <script>
                    // ═══════════════════════════════════════
                    //  MAP INITIALIZATION
                    // ═══════════════════════════════════════

                    var map = L.map('map', {
                        zoomControl: true,
                        minZoom: 3,
                        maxZoom: 18,
                        zoomSnap: 0.5,
                        wheelPxPerZoomLevel: 120
                    }).setView([28, 50], 5);

                    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
                        attribution: '&copy; CARTO | N01D.Overwatch',
                        maxZoom: 18,
                        subdomains: 'abcd'
                    }).addTo(map);

                    // ═══════════════════════════════════════
                    //  LAYER GROUPS
                    // ═══════════════════════════════════════

                    var eventLayer = L.layerGroup().addTo(map);
                    var flightLayer = L.layerGroup().addTo(map);
                    var zoneLayer = L.layerGroup().addTo(map);
                    var baseLayer = L.layerGroup().addTo(map);
                    var pipelineLayer = L.layerGroup().addTo(map);
                    var missileLayer = L.layerGroup().addTo(map);
                    var defenseLayer = L.layerGroup().addTo(map);
                    var eclipseLayer = L.layerGroup();
                    var heatmapLayer = L.layerGroup();

                    var layers = {
                        events: eventLayer,
                        flights: flightLayer,
                        zones: zoneLayer,
                        bases: baseLayer,
                        pipelines: pipelineLayer,
                        missiles: missileLayer,
                        defense: defenseLayer,
                        eclipse: eclipseLayer,
                        heatmap: heatmapLayer
                    };

                    function toggleLayer(name) {
                        var layer = layers[name];
                        if (!layer) return;
                        if (map.hasLayer(layer)) { map.removeLayer(layer); }
                        else { map.addLayer(layer); }
                    }

                    // ═══════════════════════════════════════
                    //  STRATEGIC ZONES (Chokepoints + Nuclear)
                    // ═══════════════════════════════════════

                    // Strait of Hormuz — world's most critical oil chokepoint
                    L.circle([26.56, 56.25], {
                        radius: 55000, color: '#EE3333', fillColor: '#EE3333',
                        fillOpacity: 0.06, weight: 2, dashArray: '8,6'
                    }).bindPopup(buildZonePopup('⛽ Strait of Hormuz', '#EE3333',
                        'World\'s most critical oil chokepoint. ~21M barrels/day (~20% of global oil) transit here.',
                        ['21M bbl/day throughput', 'Width: 54km at narrowest', 'Iranian coast to north', 'UAE/Oman coast to south',
                         'IRGCN fast-attack boat patrols', 'US 5th Fleet patrol zone']
                    )).addTo(zoneLayer);

                    // Bab el-Mandeb — Red Sea gateway
                    L.circle([12.58, 43.33], {
                        radius: 55000, color: '#FF8833', fillColor: '#FF8833',
                        fillOpacity: 0.06, weight: 2, dashArray: '8,6'
                    }).bindPopup(buildZonePopup('🚢 Bab el-Mandeb Strait', '#FF8833',
                        'Gateway between Red Sea and Gulf of Aden. Heavily threatened by Houthi anti-ship missiles.',
                        ['Width: 26km', '~6M bbl/day oil transit', 'Houthi drone/missile threat zone',
                         'Yemen to east, Djibouti/Eritrea to west', 'US/UK naval escorts active']
                    )).addTo(zoneLayer);

                    // Suez Canal
                    L.circle([30.45, 32.35], {
                        radius: 30000, color: '#FF8833', fillColor: '#FF8833',
                        fillOpacity: 0.06, weight: 2, dashArray: '8,6'
                    }).bindPopup(buildZonePopup('🏗 Suez Canal', '#FF8833',
                        'Connects Mediterranean to Red Sea. ~12% of global trade. Diversions during Houthi campaign.',
                        ['193km length', '~1M bbl/day oil transit', '~50 ships/day', 'Egypt-controlled',
                         'Revenue: ~$9B/year for Egypt']
                    )).addTo(zoneLayer);

                    // Nuclear facilities
                    var nuclearSites = [
                        { lat: 32.08, lon: 51.68, name: 'Isfahan UCF', desc: 'Uranium Conversion Facility. Converts yellowcake to UF6 for enrichment.', r: 60000 },
                        { lat: 33.72, lon: 51.73, name: 'Natanz FEP', desc: 'Primary uranium enrichment facility. Advanced centrifuge cascades. Previously sabotaged (Stuxnet).', r: 50000 },
                        { lat: 34.38, lon: 50.97, name: 'Fordow FFEP', desc: 'Fortified enrichment plant buried inside mountain. Built to withstand bunker-buster strikes.', r: 35000 },
                        { lat: 28.22, lon: 54.33, name: 'Bushehr NPP', desc: 'Iran\'s only operational nuclear power plant. Russian-built light water reactor.', r: 40000 },
                        { lat: 34.37, lon: 49.24, name: 'Arak IR-40', desc: 'Heavy water research reactor. Redesigned under JCPOA. Potential plutonium production.', r: 35000 }
                    ];
                    nuclearSites.forEach(function(s) {
                        L.circle([s.lat, s.lon], {
                            radius: s.r, color: '#AA55FF', fillColor: '#AA55FF',
                            fillOpacity: 0.04, weight: 1.5, dashArray: '5,5'
                        }).bindPopup(buildZonePopup('☢ ' + s.name, '#AA55FF', s.desc, [
                            'Lat: ' + s.lat.toFixed(3), 'Lon: ' + s.lon.toFixed(3),
                            'IAEA monitored facility', 'Click zone for details'
                        ])).addTo(zoneLayer);
                    });

                    // ═══════════════════════════════════════
                    //  MILITARY BASES & INSTALLATIONS
                    // ═══════════════════════════════════════

                    var milBases = [
                        // US Bases
                        { lat: 26.27, lon: 50.62, name: 'NSA Bahrain (US 5th Fleet HQ)', flag: '🇺🇸', type: 'Naval', desc: 'US Naval Support Activity. HQ of US 5th Fleet & Combined Maritime Forces.' },
                        { lat: 25.12, lon: 56.33, name: 'Al Dhafra Air Base', flag: '🇺🇸', type: 'Air', desc: 'Major USAF base in UAE. F-35, F-22, KC-10, RQ-4 deployments.' },
                        { lat: 29.96, lon: 47.79, name: 'Ali Al Salem Air Base', flag: '🇺🇸', type: 'Air', desc: 'US/Kuwait base. Staging hub for Central Command operations.' },
                        { lat: 25.42, lon: 51.44, name: 'Al Udeid Air Base', flag: '🇺🇸', type: 'Air', desc: 'Largest US base in ME. CENTCOM Forward HQ. 10,000+ personnel.' },
                        { lat: 11.55, lon: 43.15, name: 'Camp Lemonnier', flag: '🇺🇸', type: 'Joint', desc: 'Only permanent US base in Africa. CJTF-HOA. Drone ops over Yemen/Somalia.' },
                        // Regional bases
                        { lat: 27.85, lon: 52.35, name: 'Bandar Abbas Naval', flag: '🇮🇷', type: 'Naval', desc: 'IRIN & IRGCN headquarters. Controls Hormuz from Iranian side. Fast-attack craft, submarines.' },
                        { lat: 36.23, lon: 59.64, name: 'Mashhad Air Base', flag: '🇮🇷', type: 'Air', desc: 'IRIAF base. Su-24 Fencer strike aircraft, F-14 Tomcat interceptors.' },
                        { lat: 32.40, lon: 53.09, name: 'Esfahan AFB (8th TAB)', flag: '🇮🇷', type: 'Air', desc: 'IRIAF tactical air base. F-4 Phantom II, Su-22 Fitter. Near nuclear complex.' },
                        { lat: 30.84, lon: 34.67, name: 'Nevatim Air Base', flag: '🇮🇱', type: 'Air', desc: 'IAF base. F-35I Adir stealth fighters. Long-range strike capability vs Iran.' },
                        { lat: 31.29, lon: 34.28, name: 'Hatzerim Air Base', flag: '🇮🇱', type: 'Air', desc: 'IAF flight school & operational base. F-16I Sufa, AH-64 Apache.' },
                        { lat: 21.66, lon: 39.17, name: 'King Abdulaziz Naval Base', flag: '🇸🇦', type: 'Naval', desc: 'RSNF Western Fleet HQ. Frigate & corvette operations in Red Sea.' },
                        { lat: 37.09, lon: 37.00, name: 'Incirlik Air Base', flag: '🇹🇷', type: 'Air', desc: 'NATO / USAF base in Turkey. KC-135 tankers. B61 nuclear weapons storage.' }
                    ];
                    milBases.forEach(function(b) {
                        var icon = L.divIcon({
                            className: 'base-marker',
                            html: '<div style="width:14px;height:14px;background:#33CC3388;border:2px solid #33CC33;border-radius:2px;transform:rotate(45deg);box-shadow:0 0 6px #33CC3344"></div>',
                            iconSize: [14, 14], iconAnchor: [7, 7]
                        });
                        L.marker([b.lat, b.lon], { icon: icon })
                         .bindPopup(buildBasePopup(b))
                         .addTo(baseLayer);
                    });

                    // ═══════════════════════════════════════
                    //  OIL TRANSIT ROUTES
                    // ═══════════════════════════════════════

                    // Persian Gulf → Hormuz → Indian Ocean
                    L.polyline([
                        [29.5, 48.5], [28.0, 50.5], [26.6, 56.0], [25.0, 58.0], [22.0, 60.0], [18.0, 62.0]
                    ], { color: '#DDCC33', weight: 2, dashArray: '10,6', opacity: 0.5 })
                     .bindPopup(buildZonePopup('🛢 Persian Gulf Oil Route', '#DDCC33',
                         'Primary oil export corridor from Gulf producers through Strait of Hormuz.',
                         ['~21M bbl/day', 'Saudi + UAE + Kuwait + Iraq + Iran exports']))
                     .addTo(pipelineLayer);

                    // Red Sea → Suez route
                    L.polyline([
                        [12.5, 43.5], [15.0, 42.0], [20.0, 38.5], [25.0, 35.0], [30.0, 32.8], [31.5, 32.3]
                    ], { color: '#DDCC33', weight: 2, dashArray: '10,6', opacity: 0.5 })
                     .bindPopup(buildZonePopup('🛢 Red Sea → Suez Route', '#DDCC33',
                         'Oil and LNG transit via Red Sea through Suez Canal to Mediterranean.',
                         ['~4M bbl/day', 'Subject to Houthi interdiction']))
                     .addTo(pipelineLayer);

                    // East-West pipeline (Saudi Arabia)
                    L.polyline([
                        [26.2, 49.5], [26.0, 44.0], [28.5, 36.5]
                    ], { color: '#FF8833', weight: 2, dashArray: '6,4', opacity: 0.4 })
                     .bindPopup(buildZonePopup('🛢 East-West Pipeline (Petroline)', '#FF8833',
                         'Saudi Aramco 1200km pipeline. Bypasses Hormuz. Capacity: 5M bbl/day.',
                         ['Abqaiq → Yanbu', 'Strategic Hormuz bypass', 'Previously attacked by Houthis']))
                     .addTo(pipelineLayer);

                    // ═══════════════════════════════════════
                    //  MARKER & POPUP BUILDERS
                    // ═══════════════════════════════════════

                    function buildZonePopup(title, color, desc, facts) {
                        var factsHtml = facts.map(function(f) { return '<div style="color:#6A6A80;font-size:10px;padding:1px 0">› ' + f + '</div>'; }).join('');
                        return '<div class="event-popup">' +
                            '<div class="popup-header" style="background:' + color + '22;color:' + color + ';border-bottom:1px solid ' + color + '44">' + title + '</div>' +
                            '<div class="popup-body">' + desc + '<div style="margin-top:8px">' + factsHtml + '</div></div></div>';
                    }

                    function buildBasePopup(b) {
                        return '<div class="event-popup">' +
                            '<div class="popup-header" style="background:#33CC3322;color:#33CC33;border-bottom:1px solid #33CC3344">' +
                            b.flag + ' ' + b.name + '</div>' +
                            '<div class="popup-body">' +
                            '<div style="margin-bottom:6px"><span class="sev-badge" style="background:#3388FF44;color:#3388FF;border:1px solid #3388FF66">' + b.type.toUpperCase() + '</span></div>' +
                            b.desc +
                            '<div class="meta">Lat: ' + b.lat.toFixed(3) + ' | Lon: ' + b.lon.toFixed(3) + '</div>' +
                            '</div></div>';
                    }

                    function getSevClass(sev) {
                        switch(sev) {
                            case 3: return 'sev-critical';
                            case 2: return 'sev-high';
                            case 1: return 'sev-medium';
                            default: return 'sev-low';
                        }
                    }
                    function getSevLabel(sev) {
                        switch(sev) {
                            case 3: return 'CRITICAL';
                            case 2: return 'HIGH';
                            case 1: return 'MEDIUM';
                            default: return 'LOW';
                        }
                    }

                    // ═══════════════════════════════════════
                    //  EVENT MARKERS (called from C#)
                    // ═══════════════════════════════════════

                    function updateMarkers(data) {
                        eventLayer.clearLayers();
                        var eventCount = 0;

                        data.forEach(function(d) {
                            eventCount++;
                            var radius = d.sev === 3 ? 9 : (d.sev === 2 ? 7 : 5);

                            // Pulsing outer ring for critical events
                            if (d.sev === 3) {
                                var pulse = L.circleMarker([d.lat, d.lon], {
                                    radius: 16, color: d.color, fillColor: d.color,
                                    fillOpacity: 0.15, weight: 1, opacity: 0.4,
                                    className: 'pulse-marker'
                                });
                                eventLayer.addLayer(pulse);
                            }

                            var circle = L.circleMarker([d.lat, d.lon], {
                                radius: radius, color: d.color, fillColor: d.color,
                                fillOpacity: 0.85, weight: 1.5
                            });

                            var popupContent = '<div class="event-popup">' +
                                '<div class="popup-header" style="background:' + d.color + '22;color:' + d.color + ';border-bottom:1px solid ' + d.color + '44">' +
                                d.icon + ' ' + d.title + '</div>' +
                                '<div class="popup-body">' +
                                '<div style="margin-bottom:6px"><span class="sev-badge ' + getSevClass(d.sev) + '">' + getSevLabel(d.sev) + '</span>' +
                                ' <span style="color:#33CCCC;font-size:10px">' + d.cat + '</span></div>' +
                                '<div>' + d.summary + '</div>' +
                                '<div class="meta">' + d.source + ' • ' + d.time + '</div>';

                            if (d.tags) {
                                popupContent += '<div class="tags">' + d.tags + '</div>';
                            }
                            if (d.url) {
                                popupContent += '<a class="open-link" href="' + d.url + '" target="_blank" onclick="window.chrome.webview.postMessage(JSON.stringify({type:\'openUrl\',url:\'' + d.url.replace(/'/g, "\\'") + '\'})); return false;">🔗 OPEN SOURCE</a>';
                            }
                            popupContent += '</div></div>';

                            circle.bindPopup(popupContent, { maxWidth: 380, className: 'dark-popup' });

                            // Click → also notify C# sidebar
                            circle.on('click', function() {
                                try {
                                    window.chrome.webview.postMessage(JSON.stringify({ type: 'selectEvent', id: d.id }));
                                } catch(e) {}
                            });

                            eventLayer.addLayer(circle);
                        });

                        document.getElementById('statEvents').textContent = eventCount;
                    }

                    // ═══════════════════════════════════════
                    //  FLIGHT MARKERS (called from C#)
                    // ═══════════════════════════════════════

                    function updateFlights(data) {
                        flightLayer.clearLayers();
                        var flightCount = 0;

                        data.forEach(function(f) {
                            flightCount++;
                            var acColor = f.sev === 3 ? '#EE3333' : (f.sev === 2 ? '#FF8833' : '#33CCCC');

                            // Aircraft icon with heading rotation
                            var acIcon = L.divIcon({
                                className: 'flight-icon',
                                html: '<div style="transform:rotate(' + (f.hdg || 0) + 'deg);font-size:16px;text-shadow:0 0 6px ' + acColor + '80" title="' + f.callsign + '">✈</div>',
                                iconSize: [18, 18], iconAnchor: [9, 9]
                            });

                            var marker = L.marker([f.lat, f.lon], { icon: acIcon });

                            var popupContent = '<div class="event-popup">' +
                                '<div class="popup-header" style="background:' + acColor + '22;color:' + acColor + ';border-bottom:1px solid ' + acColor + '44">' +
                                '✈ ' + f.callsign + '</div>' +
                                '<div class="popup-body">' +
                                '<div style="margin-bottom:6px"><span class="sev-badge" style="background:' + acColor + '44;color:' + acColor + ';border:1px solid ' + acColor + '66">' + f.type + '</span></div>' +
                                '<div style="display:grid;grid-template-columns:1fr 1fr;gap:2px 12px;font-size:10px;margin-top:6px">' +
                                '<div>Country:</div><div style="color:#D0D0D0">' + f.country + '</div>' +
                                '<div>Altitude:</div><div style="color:#D0D0D0">' + (f.alt ? Math.round(f.alt).toLocaleString() + ' m' : 'N/A') + '</div>' +
                                '<div>Speed:</div><div style="color:#D0D0D0">' + (f.spd ? Math.round(f.spd) + ' kts' : 'N/A') + '</div>' +
                                '<div>Heading:</div><div style="color:#D0D0D0">' + (f.hdg ? Math.round(f.hdg) + '°' : 'N/A') + '</div>' +
                                '<div>ICAO24:</div><div style="color:#D0D0D0">' + f.icao + '</div>' +
                                '<div>Position:</div><div style="color:#D0D0D0">' + f.lat.toFixed(3) + ', ' + f.lon.toFixed(3) + '</div>' +
                                '</div></div></div>';

                            marker.bindPopup(popupContent, { maxWidth: 320, className: 'dark-popup' });
                            marker.on('click', function() {
                                try {
                                    window.chrome.webview.postMessage(JSON.stringify({ type: 'selectFlight', callsign: f.callsign }));
                                } catch(e) {}
                            });
                            flightLayer.addLayer(marker);
                        });

                        document.getElementById('statFlights').textContent = flightCount;
                    }

                    // ═══════════════════════════════════════
                    //  MISSILE SITES (called from C#)
                    // ═══════════════════════════════════════

                    function updateMissiles(data) {
                        missileLayer.clearLayers();
                        var count = 0;

                        data.forEach(function(m) {
                            count++;

                            // Range circle
                            if (m.range > 0) {
                                L.circle([m.lat, m.lon], {
                                    radius: m.range * 1000,
                                    color: m.color,
                                    fillColor: m.color,
                                    fillOpacity: 0.03,
                                    weight: 1,
                                    dashArray: '6,4',
                                    opacity: 0.4,
                                    interactive: false
                                }).addTo(missileLayer);
                            }

                            // Missile site marker
                            var msIcon = L.divIcon({
                                className: 'missile-marker',
                                html: '<div style="width:12px;height:12px;background:' + m.color + '88;border:2px solid ' + m.color + ';border-radius:50%;box-shadow:0 0 8px ' + m.color + '44" title="' + m.name + '"></div>',
                                iconSize: [12, 12], iconAnchor: [6, 6]
                            });
                            var marker = L.marker([m.lat, m.lon], { icon: msIcon });

                            var popupHtml = '<div class="event-popup">' +
                                '<div class="popup-header" style="background:' + m.color + '22;color:' + m.color + ';border-bottom:1px solid ' + m.color + '44">' +
                                m.icon + ' ' + m.name + '</div>' +
                                '<div class="popup-body">' +
                                '<div style="margin-bottom:6px">' +
                                '<span class="sev-badge" style="background:' + m.color + '44;color:' + m.color + ';border:1px solid ' + m.color + '66">' + m.type + '</span>' +
                                '</div>' +
                                '<div>' + m.desc + '</div>' +
                                '<div style="margin-top:6px;font-size:10px;color:#33CCCC">Missiles: ' + m.missiles + '</div>' +
                                '<div style="font-size:10px;color:#FF8833">Range: ' + m.range + ' km</div>' +
                                (m.underground ? '<div style="font-size:10px;color:#AA55FF">⛰ UNDERGROUND FACILITY</div>' : '') +
                                '<div class="meta">' + m.country + ' — ' + m.operator + '</div>' +
                                '<div class="meta">Lat: ' + m.lat.toFixed(3) + ' | Lon: ' + m.lon.toFixed(3) + '</div>' +
                                '</div></div>';

                            marker.bindPopup(popupHtml, { maxWidth: 400, className: 'dark-popup' });
                            missileLayer.addLayer(marker);
                        });

                        document.getElementById('statMissiles').textContent = count;
                    }

                    // ═══════════════════════════════════════
                    //  AIR DEFENSE SYSTEMS (called from C#)
                    // ═══════════════════════════════════════

                    function updateDefense(data) {
                        defenseLayer.clearLayers();
                        var count = 0;

                        data.forEach(function(d) {
                            count++;

                            // Defense coverage circle
                            L.circle([d.lat, d.lon], {
                                radius: d.range * 1000,
                                color: d.color,
                                fillColor: d.color,
                                fillOpacity: 0.04,
                                weight: 1.5,
                                dashArray: '4,4',
                                opacity: 0.5
                            }).addTo(defenseLayer);

                            // Defense site marker (shield shaped)
                            var defIcon = L.divIcon({
                                className: 'defense-marker',
                                html: '<div style="font-size:14px;text-shadow:0 0 6px ' + d.color + '80" title="' + d.name + '">🛡</div>',
                                iconSize: [16, 16], iconAnchor: [8, 8]
                            });
                            var marker = L.marker([d.lat, d.lon], { icon: defIcon });

                            var popupHtml = '<div class="event-popup">' +
                                '<div class="popup-header" style="background:' + d.color + '22;color:' + d.color + ';border-bottom:1px solid ' + d.color + '44">' +
                                '🛡 ' + d.name + '</div>' +
                                '<div class="popup-body">' +
                                '<div style="margin-bottom:6px">' +
                                '<span class="sev-badge" style="background:' + d.color + '44;color:' + d.color + ';border:1px solid ' + d.color + '66">' + d.system + '</span>' +
                                ' <span style="color:#6A6A80;font-size:10px">' + d.type + '</span>' +
                                '</div>' +
                                '<div>' + d.desc + '</div>' +
                                '<div style="font-size:10px;color:#33CCCC;margin-top:6px">Engagement range: ' + d.range + ' km</div>' +
                                '<div class="meta">' + d.country + '</div>' +
                                '<div class="meta">Lat: ' + d.lat.toFixed(3) + ' | Lon: ' + d.lon.toFixed(3) + '</div>' +
                                '</div></div>';

                            marker.bindPopup(popupHtml, { maxWidth: 380, className: 'dark-popup' });
                            defenseLayer.addLayer(marker);
                        });

                        document.getElementById('statDefense').textContent = count;
                    }

                    // ═══════════════════════════════════════
                    //  ECLIPSE PATHS (called from C#)
                    // ═══════════════════════════════════════

                    function updateEclipses(data) {
                        eclipseLayer.clearLayers();

                        data.forEach(function(e) {
                            if (e.coords.length < 2) return;

                            // Eclipse path line
                            var path = L.polyline(e.coords, {
                                color: e.color,
                                weight: 3,
                                dashArray: '8,4',
                                opacity: 0.7
                            });

                            // Path shadow (wider, transparent)
                            L.polyline(e.coords, {
                                color: e.color,
                                weight: 40,
                                opacity: 0.05,
                                interactive: false
                            }).addTo(eclipseLayer);

                            var popupHtml = '<div class="event-popup">' +
                                '<div class="popup-header" style="background:' + e.color + '22;color:' + e.color + ';border-bottom:1px solid ' + e.color + '44">' +
                                '🌑 ' + e.name + '</div>' +
                                '<div class="popup-body">' +
                                '<div style="margin-bottom:6px">' +
                                '<span class="sev-badge" style="background:#DDCC3344;color:#DDCC33;border:1px solid #DDCC3366">ECLIPSE</span>' +
                                ' <span style="color:#6A6A80;font-size:10px">' + e.date + '</span>' +
                                '</div>' +
                                '<div>' + e.desc + '</div>' +
                                '<div style="margin-top:6px;font-size:10px;color:#FF8833">⚔ ' + e.milSig + '</div>' +
                                '<div class="meta">Magnitude: ' + e.mag.toFixed(3) + ' | ME Coverage: ' + e.coverage + '%</div>' +
                                '</div></div>';

                            path.bindPopup(popupHtml, { maxWidth: 400, className: 'dark-popup' });
                            eclipseLayer.addLayer(path);

                            // Start/end markers
                            var startIcon = L.divIcon({
                                className: 'eclipse-start',
                                html: '<div style="font-size:16px;text-shadow:0 0 8px ' + e.color + '80">🌑</div>',
                                iconSize: [18, 18], iconAnchor: [9, 9]
                            });
                            L.marker(e.coords[0], { icon: startIcon })
                              .bindPopup(popupHtml, { maxWidth: 400, className: 'dark-popup' })
                              .addTo(eclipseLayer);
                        });
                    }

                    // ═══════════════════════════════════════
                    //  HEATMAP (simple density circles)
                    // ═══════════════════════════════════════

                    function updateHeatmap(data) {
                        heatmapLayer.clearLayers();
                        data.forEach(function(d) {
                            L.circle([d.lat, d.lon], {
                                radius: 80000, color: d.color, fillColor: d.color,
                                fillOpacity: 0.08, weight: 0, interactive: false
                            }).addTo(heatmapLayer);
                        });
                    }

                    // ═══════════════════════════════════════
                    //  FLY-TO (called from C# on event select)
                    // ═══════════════════════════════════════

                    function flyTo(lat, lon, zoom) {
                        map.flyTo([lat, lon], zoom || 8, { duration: 1.2 });
                    }

                    // Coordinate display on mouse move
                    var coordDiv = document.createElement('div');
                    coordDiv.style.cssText = 'position:absolute;bottom:12px;right:12px;z-index:1000;background:#080810ee;border:1px solid #2A2A3E;border-radius:3px;padding:4px 8px;font-size:9px;color:#6A6A80;font-family:Consolas,monospace';
                    document.body.appendChild(coordDiv);
                    map.on('mousemove', function(e) {
                        coordDiv.textContent = e.latlng.lat.toFixed(4) + ', ' + e.latlng.lng.toFixed(4);
                    });

                    // Right-click context
                    map.on('contextmenu', function(e) {
                        L.popup()
                         .setLatLng(e.latlng)
                         .setContent('<div style="font-family:Consolas;font-size:11px;color:#D0D0D0;padding:4px"><b style="color:#3388FF">📍 Coordinates</b><br>' +
                             'Lat: ' + e.latlng.lat.toFixed(5) + '<br>Lon: ' + e.latlng.lng.toFixed(5) + '<br>' +
                             '<span style="color:#6A6A80;font-size:9px">Right-click to pin location</span></div>')
                         .openOn(map);
                    });
                </script>
            </body>
            </html>
            """;
        }

        // ═══════════════════════════════════════════
        //  ECLIPSE MONITORING
        // ═══════════════════════════════════════════

        private void LoadEclipseData()
        {
            var eclipses = _eclipse.GetAllEclipses();
            var vms = eclipses.Select(e => new EclipseViewModel(e)).ToList();
            lstEclipses.ItemsSource = vms;
            UpdateEclipseCountdown();
        }

        private void StartEclipseCountdown()
        {
            _eclipseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _eclipseTimer.Tick += (_, _) => UpdateEclipseCountdown();
            _eclipseTimer.Start();
        }

        private void UpdateEclipseCountdown()
        {
            var (next, countdown) = _eclipse.GetNextEclipseCountdown();
            if (next != null)
            {
                var icon = next.Type switch
                {
                    EclipseType.SolarTotal => "🌑",
                    EclipseType.SolarAnnular => "🌗",
                    EclipseType.SolarPartial => "🌘",
                    EclipseType.LunarTotal => "🌕",
                    EclipseType.LunarPartial => "🌔",
                    _ => "🌙"
                };
                txtNextEclipse.Text = $"{icon} {next.Name} — {next.Date:yyyy-MM-dd HH:mm UTC} — Coverage: {next.MECoveragePercent}% ME";
                txtEclipseCountdown.Text = $"T-{countdown.Days}d {countdown.Hours:D2}h {countdown.Minutes:D2}m {countdown.Seconds:D2}s";
                txtEclipseMilSig.Text = next.MilitarySignificance;
            }
            else
            {
                txtNextEclipse.Text = "No upcoming ME-visible eclipses in database";
                txtEclipseCountdown.Text = "";
                txtEclipseMilSig.Text = "";
            }

            // Check for active eclipse — flash alert
            var active = _eclipse.GetActiveEclipse();
            if (active != null)
            {
                txtEclipseCountdown.Text = "⚡ ECLIPSE IN PROGRESS";
                txtEclipseCountdown.Foreground = new SolidColorBrush(Color.FromRgb(0xEE, 0x33, 0x33));
            }
            else
            {
                txtEclipseCountdown.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0x33));
            }
        }

        private void BtnRefreshEclipses_Click(object sender, RoutedEventArgs e) => LoadEclipseData();

        private void LstEclipses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstEclipses.SelectedItem is EclipseViewModel vm && vm.Eclipse.PathCoordinates.Count > 0)
            {
                // Fly to eclipse path center on map
                var mid = vm.Eclipse.PathCoordinates[vm.Eclipse.PathCoordinates.Count / 2];
                try
                {
                    webMap.CoreWebView2?.ExecuteScriptAsync($"flyTo({mid.Lat},{mid.Lon},4)");
                    tabMain.SelectedIndex = 1; // Switch to map tab
                }
                catch { }
            }
        }

        // ═══════════════════════════════════════════
        //  MISSILE / DEFENSE DATA
        // ═══════════════════════════════════════════

        private void LoadMissileDefenseData()
        {
            dgMissiles.ItemsSource = _missiles.GetAllMissileSites();
            dgDefense.ItemsSource = _missiles.GetAllAirDefenseSites();
        }

        private void BtnShowMissilesOnMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateMapMissiles();
                UpdateMapDefense();
                tabMain.SelectedIndex = 1; // Switch to Map tab
                webMap.CoreWebView2?.ExecuteScriptAsync("flyTo(30, 50, 5)");
                txtStatus.Text = "🚀 Missile sites & air defense systems displayed on map";
            }
            catch { }
        }

        private void UpdateMapMissiles()
        {
            try
            {
                var data = _missiles.BuildMissileMapData();
                webMap.CoreWebView2?.ExecuteScriptAsync($"updateMissiles([{data}])");
            }
            catch { }
        }

        private void UpdateMapDefense()
        {
            try
            {
                var data = _missiles.BuildDefenseMapData();
                webMap.CoreWebView2?.ExecuteScriptAsync($"updateDefense([{data}])");
            }
            catch { }
        }

        private void UpdateMapEclipses()
        {
            try
            {
                var eclipses = _eclipse.GetMEVisibleEclipses();
                var data = string.Join(",", eclipses.Where(e => e.PathCoordinates.Count > 0).Select(e =>
                {
                    var coords = string.Join(",", e.PathCoordinates.Select(p => $"[{p.Lat},{p.Lon}]"));
                    var name = Escape(e.Name);
                    var desc = Escape(e.Description);
                    var milSig = Escape(e.MilitarySignificance);
                    var date = e.Date.ToString("yyyy-MM-dd HH:mm UTC");
                    var color = e.Type switch
                    {
                        EclipseType.SolarTotal => "#DDCC33",
                        EclipseType.SolarAnnular => "#FF8833",
                        EclipseType.SolarPartial => "#AA55FF",
                        _ => "#6A6A80"
                    };
                    return $"{{coords:[{coords}],name:'{name}',desc:'{desc}',milSig:'{milSig}'," +
                           $"date:'{date}',color:'{color}',mag:{e.MaxMagnitude},coverage:{e.MECoveragePercent}}}";
                }));
                webMap.CoreWebView2?.ExecuteScriptAsync($"updateEclipses([{data}])");
            }
            catch { }
        }

        // ═══════════════════════════════════════════
        //  AUTO-REFRESH
        // ═══════════════════════════════════════════

        private void CmbInterval_Changed(object sender, SelectionChangedEventArgs e)
        {
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer = null;

            if (cmbInterval.SelectedItem is ComboBoxItem item)
            {
                var text = item.Content?.ToString() ?? "OFF";
                if (text == "OFF") return;

                var minutes = text switch
                {
                    "1 min" => 1,
                    "5 min" => 5,
                    "15 min" => 15,
                    "30 min" => 30,
                    _ => 0
                };

                if (minutes > 0)
                {
                    _autoRefreshTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMinutes(minutes)
                    };
                    _autoRefreshTimer.Tick += async (_, _) => await RefreshAllAsync();
                    _autoRefreshTimer.Start();
                    txtStatus.Text = $"Auto-refresh: every {minutes} min";
                }
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await RefreshAllAsync();
    }

    // ═══════════════════════════════════════════
    //  VIEW MODELS
    // ═══════════════════════════════════════════

    public class EventViewModel
    {
        public ConflictEvent Event { get; }

        public EventViewModel(ConflictEvent e) => Event = e;

        public string Title => Event.Title;
        public string Summary => Event.Summary;
        public string Source => Event.Source;

        public string SeverityLabel => Event.Severity switch
        {
            SeverityLevel.Critical => "CRIT",
            SeverityLevel.High => "HIGH",
            SeverityLevel.Medium => "MED",
            _ => "LOW"
        };

        public string SeverityColor => Event.Severity switch
        {
            SeverityLevel.Critical => "#EE3333",
            SeverityLevel.High => "#FF8833",
            SeverityLevel.Medium => "#3388FF",
            _ => "#6A6A80"
        };

        public string CategoryIcon => Event.Category switch
        {
            EventCategory.Military => "⚔",
            EventCategory.Diplomatic => "🏛",
            EventCategory.Economic => "💰",
            EventCategory.Nuclear => "☢",
            EventCategory.Cyber => "💻",
            EventCategory.Humanitarian => "🏥",
            EventCategory.Intelligence => "🕵",
            _ => "📡"
        };

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - Event.Timestamp;
                if (diff.TotalMinutes < 1) return "just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return $"{(int)diff.TotalDays}d ago";
            }
        }

        public string TagsDisplay => Event.Tags.Count > 0
            ? string.Join(" ", Event.Tags.Select(t => $"[{t}]"))
            : "";
    }

    public class AlertRuleViewModel
    {
        private readonly AlertRule _rule;
        public AlertRuleViewModel(AlertRule r) => _rule = r;

        public string Name => _rule.Name;
        public string StatusText => _rule.Enabled ? "● ACTIVE" : "○ DISABLED";
        public string StatusColor => _rule.Enabled ? "#33CC33" : "#6A6A80";
        public string Description =>
            $"Min severity: {_rule.MinSeverity} | Keywords: {string.Join(", ", _rule.Keywords.Take(5))}" +
            (_rule.Keywords.Count > 5 ? $" +{_rule.Keywords.Count - 5} more" : "");
    }

    public class EclipseViewModel
    {
        public EclipseEvent Eclipse { get; }

        public EclipseViewModel(EclipseEvent e) => Eclipse = e;

        public string Name => Eclipse.Name;
        public string Description => Eclipse.Description;
        public string DateDisplay => Eclipse.Date.ToString("yyyy-MM-dd HH:mm UTC");

        public string Icon => Eclipse.Type switch
        {
            EclipseType.SolarTotal => "🌑",
            EclipseType.SolarAnnular => "🌗",
            EclipseType.SolarPartial => "🌘",
            EclipseType.LunarTotal => "🌕",
            EclipseType.LunarPartial => "🌔",
            _ => "🌙"
        };

        public string StatusColor
        {
            get
            {
                if (Eclipse.Date < DateTime.UtcNow) return "#6A6A80";
                if (Eclipse.MECoveragePercent >= 70) return "#EE3333";
                if (Eclipse.MECoveragePercent >= 40) return "#FF8833";
                if (Eclipse.IsVisibleFromME) return "#3388FF";
                return "#6A6A80";
            }
        }

        public string MELabel => Eclipse.IsVisibleFromME
            ? $"ME: {Eclipse.MECoveragePercent}%"
            : "NOT VISIBLE";

        public string MELabelColor => Eclipse.IsVisibleFromME ? "#33CC33" : "#6A6A80";

        public string CountdownDisplay
        {
            get
            {
                if (Eclipse.Date < DateTime.UtcNow) return "PAST";
                var diff = Eclipse.Date - DateTime.UtcNow;
                if (diff.TotalDays > 365) return $"T-{diff.Days / 365}y {diff.Days % 365}d";
                if (diff.TotalDays > 30) return $"T-{diff.Days}d";
                return $"T-{diff.Days}d {diff.Hours}h";
            }
        }

        public string MagnitudeDisplay => $"Mag: {Eclipse.MaxMagnitude:F3}";
    }
}
