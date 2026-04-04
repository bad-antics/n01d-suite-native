using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace N01D.Scan
{
    public class ScanResult
    {
        public string Host    { get; set; } = "";
        public int    Port    { get; set; }
        public string State   { get; set; } = "closed";
        public string Service { get; set; } = "";
        public string Banner  { get; set; } = "";
        public long   RttMs   { get; set; }
    }

    public partial class MainWindow : Window
    {
        // ── Service name lookup ────────────────────────────────────────────────
        private static readonly Dictionary<int, string> ServiceNames = new()
        {
            [21] = "FTP",   [22] = "SSH",    [23] = "Telnet",
            [25] = "SMTP",  [53] = "DNS",    [80] = "HTTP",
            [110] = "POP3", [143] = "IMAP",  [443] = "HTTPS",
            [445] = "SMB",  [3306] = "MySQL",[3389] = "RDP",
            [5432] = "PostgreSQL", [5900] = "VNC",
            [6379] = "Redis", [8080] = "HTTP-Alt", [8443] = "HTTPS-Alt",
            [27017] = "MongoDB",
        };

        private readonly ObservableCollection<ScanResult> _results = new();
        private CancellationTokenSource? _cts;
        private int _openCount;
        private readonly StringBuilder _logSb = new();

        public MainWindow()
        {
            InitializeComponent();
            lstResults.ItemsSource = _results;
        }

        // ── Scan control ──────────────────────────────────────────────────────

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            var target    = txtTarget.Text.Trim();
            var portsText = txtPorts.Text.Trim();
            if (!int.TryParse(txtTimeout.Text.Trim(), out int timeoutMs))
                timeoutMs = 500;

            List<string> hosts;
            try { hosts = ExpandTarget(target); }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid target: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<int> ports;
            try { ports = ParsePorts(portsText); }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid port spec: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _results.Clear();
            _logSb.Clear();
            _openCount   = 0;
            lblOpen.Text = "Open: 0";
            txtLog.Text  = "";

            btnScan.IsEnabled = false;
            btnStop.IsEnabled = true;

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            int total = hosts.Count * ports.Count;
            int scanned = 0;
            progBar.Maximum  = total;
            progBar.Value    = 0;
            lblProgress.Text = $"0 / {total}";
            lblStatus.Text   = $"Scanning {hosts.Count} host(s) x {ports.Count} port(s)...";

            AppendLog($"Scan started  target={target}  ports={portsText}  timeout={timeoutMs}ms");

            var semaphore = new SemaphoreSlim(128);
            var tasks     = new List<Task>();

            foreach (var host in hosts)
            {
                foreach (var port in ports)
                {
                    if (ct.IsCancellationRequested) break;
                    await semaphore.WaitAsync(ct).ConfigureAwait(false);
                    var h = host; var p = port;
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var result = await ProbeAsync(h, p, timeoutMs, ct);
                            Dispatcher.Invoke(() =>
                            {
                                if (result.State == "open")
                                {
                                    _results.Add(result);
                                    _openCount++;
                                    lblOpen.Text = $"Open: {_openCount}";
                                    AppendLog($"OPEN  {h}:{p}  {result.Service}  {result.RttMs}ms");
                                }
                                scanned++;
                                progBar.Value    = scanned;
                                lblProgress.Text = $"{scanned} / {total}";
                            });
                        }
                        finally { semaphore.Release(); }
                    }, ct));
                }
                if (ct.IsCancellationRequested) break;
            }

            try { await Task.WhenAll(tasks); }
            catch (OperationCanceledException) { }

            Dispatcher.Invoke(() =>
            {
                lblStatus.Text    = ct.IsCancellationRequested
                    ? $"Scan cancelled — {_openCount} open ports found"
                    : $"Scan complete — {_openCount} open ports found";
                btnScan.IsEnabled = true;
                btnStop.IsEnabled = false;
                progBar.Value     = total;
                AppendLog($"Scan finished  open={_openCount}  total_probed={total}");
            });
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            lblStatus.Text    = "Stopping...";
            btnStop.IsEnabled = false;
        }

        // ── TCP probe ─────────────────────────────────────────────────────────

        private static async Task<ScanResult> ProbeAsync(
            string host, int port, int timeoutMs, CancellationToken ct)
        {
            var result = new ScanResult
            {
                Host    = host,
                Port    = port,
                Service = ServiceNames.GetValueOrDefault(port, ""),
                State   = "closed",
            };

            var t0 = DateTime.UtcNow;
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(host, port);
                var delay       = Task.Delay(timeoutMs, ct);
                if (await Task.WhenAny(connectTask, delay) == delay)
                    return result;

                await connectTask;
                result.RttMs = (long)(DateTime.UtcNow - t0).TotalMilliseconds;
                result.State = "open";

                // Quick banner grab
                try
                {
                    tcp.ReceiveTimeout = 150;
                    var stream = tcp.GetStream();
                    if (stream.CanRead)
                    {
                        var buf  = new byte[256];
                        using var bannerCts = new CancellationTokenSource(150);
                        var read = await stream.ReadAsync(buf, 0, buf.Length, bannerCts.Token);
                        if (read > 0)
                        {
                            var raw = Encoding.ASCII.GetString(buf, 0, read)
                                .Replace("\r", " ").Replace("\n", " ").Trim();
                            result.Banner = raw.Length > 100 ? raw[..100] : raw;
                        }
                    }
                }
                catch { }
            }
            catch (SocketException) { }
            catch (OperationCanceledException) { }
            catch { }

            return result;
        }

        // ── Host expansion ────────────────────────────────────────────────────

        private static List<string> ExpandTarget(string target)
        {
            if (target.Contains('/'))
            {
                var parts  = target.Split('/');
                int prefix = int.Parse(parts[1]);
                if (prefix < 0 || prefix > 32) throw new ArgumentException("Invalid CIDR prefix");
                var ip    = IPAddress.Parse(parts[0]);
                var bytes = ip.GetAddressBytes();
                uint baseIp = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16)
                            | ((uint)bytes[2] << 8)  |  (uint)bytes[3];
                uint mask   = prefix == 0 ? 0 : 0xFFFFFFFF << (32 - prefix);
                uint net    = baseIp & mask;
                uint bcast  = net | ~mask;
                var hosts   = new List<string>();
                for (uint addr = net + 1; addr < bcast; addr++)
                {
                    hosts.Add($"{(addr >> 24) & 0xFF}.{(addr >> 16) & 0xFF}"
                             + $".{(addr >> 8) & 0xFF}.{addr & 0xFF}");
                    if (hosts.Count >= 1024) break;
                }
                return hosts;
            }
            return new List<string> { target };
        }

        private static List<int> ParsePorts(string spec)
        {
            var ports = new HashSet<int>();
            foreach (var token in spec.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var t = token.Trim();
                if (t.Contains('-'))
                {
                    var range = t.Split('-');
                    int lo = int.Parse(range[0]);
                    int hi = int.Parse(range[1]);
                    if (lo < 1 || hi > 65535 || lo > hi)
                        throw new ArgumentException($"Invalid range: {t}");
                    for (int p = lo; p <= hi && ports.Count < 10000; p++)
                        ports.Add(p);
                }
                else
                {
                    int p = int.Parse(t);
                    if (p < 1 || p > 65535) throw new ArgumentException($"Invalid port: {p}");
                    ports.Add(p);
                }
            }
            return ports.OrderBy(x => x).ToList();
        }

        // ── Detail panel ──────────────────────────────────────────────────────

        private void LstResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstResults.SelectedItem is not ScanResult r) return;
            lblDetailHost.Text    = $"Host: {r.Host}";
            lblDetailPort.Text    = $"Port: {r.Port}";
            lblDetailState.Text   = $"State: {r.State}";
            lblDetailService.Text = $"Service: {(string.IsNullOrEmpty(r.Service) ? "unknown" : r.Service)}";
            lblDetailRtt.Text     = $"RTT: {r.RttMs} ms";
            txtBanner.Text        = string.IsNullOrEmpty(r.Banner) ? "(no banner)" : r.Banner;
        }

        // ── Log ───────────────────────────────────────────────────────────────

        private void AppendLog(string line)
        {
            _logSb.AppendLine($"[{DateTime.Now:HH:mm:ss}] {line}");
            txtLog.Text = _logSb.ToString();
        }
    }
}
