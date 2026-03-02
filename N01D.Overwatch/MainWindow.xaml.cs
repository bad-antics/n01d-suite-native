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
        private readonly OilPriceService _oil = new();
        private readonly AlertService _alerts = new();

        private readonly ObservableCollection<EventViewModel> _timelineItems = new();
        private readonly List<ConflictEvent> _allEvents = new();
        private DispatcherTimer? _autoRefreshTimer;
        private bool _isLoading;

        public MainWindow()
        {
            InitializeComponent();
            lstTimeline.ItemsSource = _timelineItems;
            LoadAlertRules();
            Loaded += async (_, _) => await RefreshAllAsync();
            InitMap();
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

                // Oil Prices
                var oilTask = _oil.FetchPricesAsync();
                tasks.Add(oilTask);

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

                // Process oil data
                var oilData = await oilTask;
                foreach (var o in oilData)
                {
                    var ev = _oil.ToConflictEvent(o);
                    _alerts.EvaluateEvent(ev);
                    // Replace existing oil events
                    _allEvents.RemoveAll(e => e.DataSource == DataSource.OilPrice && e.Source == ev.Source);
                    _allEvents.Add(ev);
                }
                UpdateOilPanel(oilData);

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

        private async void BtnRefreshOil_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "🛢 Fetching oil prices...";
            try
            {
                var prices = await _oil.FetchPricesAsync();
                UpdateOilPanel(prices);
                txtStatus.Text = "🛢 Oil prices updated";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"⚠ Oil price error: {ex.Message}";
            }
        }

        // ═══════════════════════════════════════════
        //  OIL PANEL
        // ═══════════════════════════════════════════

        private void UpdateOilPanel(List<OilPriceData> prices)
        {
            pnlOil.Children.Clear();
            if (prices.Count == 0)
            {
                pnlOil.Children.Add(new TextBlock
                {
                    Text = "Unable to fetch oil prices (market may be closed)",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128))
                });
                return;
            }

            foreach (var p in prices)
            {
                var isUp = p.Change >= 0;
                var arrow = isUp ? "▲" : "▼";
                var color = isUp
                    ? (Math.Abs(p.ChangePercent) > 3 ? "#FF0055" : "#00FF41")
                    : (Math.Abs(p.ChangePercent) > 3 ? "#FF0055" : "#FFAA00");

                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)),
                    CornerRadius = new CornerRadius(5),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(20, 16, 20, 16),
                    Margin = new Thickness(0, 0, 0, 12),
                    Width = 500,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = p.Name.ToUpperInvariant(),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xBD, 0xC6)),
                    FontWeight = FontWeights.Bold
                });

                var pricePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                pricePanel.Children.Add(new TextBlock
                {
                    Text = $"${p.Price:F2}",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 28,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                    FontWeight = FontWeights.Bold
                });
                pricePanel.Children.Add(new TextBlock
                {
                    Text = $"  {arrow} {p.Change:+0.00;-0.00} ({p.ChangePercent:+0.00;-0.00}%)",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 16,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(10, 0, 0, 4)
                });
                sp.Children.Add(pricePanel);

                sp.Children.Add(new TextBlock
                {
                    Text = $"Last updated: {p.Timestamp:HH:mm:ss UTC}",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    Margin = new Thickness(0, 6, 0, 0)
                });

                card.Child = sp;
                pnlOil.Children.Add(card);
            }
        }

        // ═══════════════════════════════════════════
        //  FILTERING & TIMELINE
        // ═══════════════════════════════════════════

        private void ApplyFilters()
        {
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
            if (criticals > 0) { level = "CRITICAL"; color = "#FF0055"; }
            else if (highs > 5) { level = "HIGH"; color = "#FF0055"; }
            else if (highs > 0) { level = "ELEVATED"; color = "#FFAA00"; }
            else { level = "GUARDED"; color = "#00FF41"; }

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
            }
            catch { }
        }

        private void UpdateMap()
        {
            try
            {
                var geoEvents = _allEvents.Where(e => e.Latitude.HasValue && e.Longitude.HasValue).ToList();
                var markers = string.Join(",", geoEvents.Select(e =>
                {
                    var color = e.Severity switch
                    {
                        SeverityLevel.Critical => "#FF0055",
                        SeverityLevel.High => "#FFAA00",
                        SeverityLevel.Medium => "#0ABDC6",
                        _ => "#808080"
                    };
                    var icon = e.DataSource switch
                    {
                        DataSource.FlightTracker => "✈",
                        DataSource.ShipTracker => "⚓",
                        _ => "●"
                    };
                    var title = e.Title.Replace("'", "\\'").Replace("\"", "\\\"");
                    return $"{{lat:{e.Latitude},lon:{e.Longitude},title:'{icon} {title}',color:'{color}'}}";
                }));

                webMap.CoreWebView2?.ExecuteScriptAsync($"updateMarkers([{markers}])");
            }
            catch { }
        }

        private static string BuildMapHtml()
        {
            return """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8"/>
                <style>
                    body { margin: 0; background: #0D0D0D; }
                    #map { width: 100vw; height: 100vh; }
                </style>
                <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"/>
                <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
            </head>
            <body>
                <div id="map"></div>
                <script>
                    var map = L.map('map', { zoomControl: true }).setView([28, 50], 5);
                    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
                        attribution: '&copy; CARTO',
                        maxZoom: 18
                    }).addTo(map);

                    var markerLayer = L.layerGroup().addTo(map);

                    function updateMarkers(data) {
                        markerLayer.clearLayers();
                        data.forEach(function(d) {
                            var circle = L.circleMarker([d.lat, d.lon], {
                                radius: 6, color: d.color, fillColor: d.color,
                                fillOpacity: 0.8, weight: 1
                            });
                            circle.bindPopup('<div style="font-family:Consolas;font-size:12px;color:#E0E0E0;background:#1A1A1A;padding:6px;border:1px solid '+d.color+';border-radius:3px">' + d.title + '</div>', {
                                className: 'dark-popup'
                            });
                            markerLayer.addLayer(circle);
                        });
                    }

                    // Strategic zone overlays
                    L.circle([26.56, 56.25], {radius: 50000, color: '#FF0055', fillOpacity: 0.05, weight: 1, dashArray: '5,5'})
                     .bindPopup('<b style="color:#FF0055">Strait of Hormuz</b>').addTo(map);
                    L.circle([12.58, 43.33], {radius: 50000, color: '#FFAA00', fillOpacity: 0.05, weight: 1, dashArray: '5,5'})
                     .bindPopup('<b style="color:#FFAA00">Bab el-Mandeb</b>').addTo(map);
                    L.circle([32.08, 51.68], {radius: 80000, color: '#0ABDC6', fillOpacity: 0.03, weight: 1, dashArray: '5,5'})
                     .bindPopup('<b style="color:#0ABDC6">Isfahan — Nuclear</b>').addTo(map);
                    L.circle([33.72, 51.73], {radius: 40000, color: '#0ABDC6', fillOpacity: 0.03, weight: 1, dashArray: '5,5'})
                     .bindPopup('<b style="color:#0ABDC6">Natanz — Nuclear</b>').addTo(map);
                    L.circle([34.38, 50.97], {radius: 30000, color: '#0ABDC6', fillOpacity: 0.03, weight: 1, dashArray: '5,5'})
                     .bindPopup('<b style="color:#0ABDC6">Fordow — Nuclear</b>').addTo(map);
                </script>
            </body>
            </html>
            """;
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
            SeverityLevel.Critical => "#FF0055",
            SeverityLevel.High => "#FFAA00",
            SeverityLevel.Medium => "#0ABDC6",
            _ => "#808080"
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
        public string StatusColor => _rule.Enabled ? "#00FF41" : "#808080";
        public string Description =>
            $"Min severity: {_rule.MinSeverity} | Keywords: {string.Join(", ", _rule.Keywords.Take(5))}" +
            (_rule.Keywords.Count > 5 ? $" +{_rule.Keywords.Count - 5} more" : "");
    }
}
