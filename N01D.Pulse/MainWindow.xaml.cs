using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace N01D.Pulse
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _cts;

        public MainWindow()
        {
            InitializeComponent();
            LoadLocalIP();
        }

        private void LoadLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ips = host.AddressList
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.ToString());
                lblLocalIP.Text = $"Local: {string.Join(" | ", ips)}";
            }
            catch { lblLocalIP.Text = "Local: unknown"; }
        }

        // ── PING ──

        private async void BtnPing_Click(object sender, RoutedEventArgs e)
        {
            var host = txtPingHost.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;

            txtPingOutput.Text = $"Pinging {host}...\r\n";
            lblStatus.Text = "[ PINGING ]";

            _cts = new CancellationTokenSource();
            btnStopPing.IsEnabled = true;

            try
            {
                using var ping = new Ping();
                for (int i = 0; i < 10 && !_cts.Token.IsCancellationRequested; i++)
                {
                    var reply = await ping.SendPingAsync(host, 3000);
                    var line = reply.Status == IPStatus.Success
                        ? $"Reply from {reply.Address}: bytes={reply.Buffer.Length} time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl}"
                        : $"Request timed out ({reply.Status})";
                    txtPingOutput.AppendText(line + "\r\n");
                    txtPingOutput.ScrollToEnd();
                    await Task.Delay(500, _cts.Token);
                }
            }
            catch (OperationCanceledException) { txtPingOutput.AppendText("-- Stopped --\r\n"); }
            catch (Exception ex) { txtPingOutput.AppendText($"Error: {ex.Message}\r\n"); }

            btnStopPing.IsEnabled = false;
            lblStatus.Text = "[ DONE ]";
        }

        private async void BtnPingSweep_Click(object sender, RoutedEventArgs e)
        {
            var host = txtPingHost.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;

            // Determine /24 subnet
            string subnet;
            if (IPAddress.TryParse(host, out var ip))
            {
                var parts = ip.ToString().Split('.');
                subnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            else
            {
                var entry = await Dns.GetHostEntryAsync(host);
                var resolved = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (resolved == null) { txtPingOutput.Text = "Cannot resolve host.\r\n"; return; }
                var parts = resolved.ToString().Split('.');
                subnet = $"{parts[0]}.{parts[1]}.{parts[2]}";
            }

            txtPingOutput.Text = $"Sweeping {subnet}.0/24...\r\n";
            lblStatus.Text = "[ SWEEPING ]";

            _cts = new CancellationTokenSource();
            btnStopPing.IsEnabled = true;

            int alive = 0;
            var tasks = Enumerable.Range(1, 254).Select(async i =>
            {
                if (_cts.Token.IsCancellationRequested) return;
                var target = $"{subnet}.{i}";
                using var p = new Ping();
                try
                {
                    var r = await p.SendPingAsync(target, 1000);
                    if (r.Status == IPStatus.Success)
                    {
                        Interlocked.Increment(ref alive);
                        Dispatcher.Invoke(() =>
                        {
                            txtPingOutput.AppendText($"  ✓ {target} — {r.RoundtripTime}ms\r\n");
                            txtPingOutput.ScrollToEnd();
                        });
                    }
                }
                catch { }
            });

            await Task.WhenAll(tasks);
            txtPingOutput.AppendText($"\r\nSweep complete: {alive} hosts alive.\r\n");
            btnStopPing.IsEnabled = false;
            lblStatus.Text = $"[ SWEEP DONE — {alive} hosts ]";
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            btnStopPing.IsEnabled = false;
        }

        // ── PORT SCAN ──

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            var host = txtScanHost.Text.Trim();
            var portRange = txtPorts.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;

            int startPort = 1, endPort = 1024;
            if (portRange.Contains('-'))
            {
                var pts = portRange.Split('-');
                int.TryParse(pts[0], out startPort);
                int.TryParse(pts[1], out endPort);
            }

            txtScanOutput.Text = $"Scanning {host} ports {startPort}-{endPort}...\r\n";
            pgScan.Visibility = Visibility.Visible;
            pgScan.Maximum = endPort - startPort + 1;
            pgScan.Value = 0;
            lblStatus.Text = "[ SCANNING ]";

            int openCount = 0;
            int progress = 0;
            var sb = new StringBuilder();

            var semaphore = new SemaphoreSlim(200);
            var tasks = Enumerable.Range(startPort, endPort - startPort + 1).Select(async port =>
            {
                await semaphore.WaitAsync();
                try
                {
                    using var client = new TcpClient();
                    var cts = new CancellationTokenSource(500);
                    try
                    {
                        await client.ConnectAsync(host, port, cts.Token);
                        Interlocked.Increment(ref openCount);
                        var svc = GetServiceName(port);
                        Dispatcher.Invoke(() =>
                        {
                            txtScanOutput.AppendText($"  ✓ Port {port}/tcp OPEN  {svc}\r\n");
                            txtScanOutput.ScrollToEnd();
                        });
                    }
                    catch { }
                }
                finally
                {
                    semaphore.Release();
                    var p = Interlocked.Increment(ref progress);
                    if (p % 50 == 0)
                        Dispatcher.Invoke(() => pgScan.Value = p);
                }
            });

            await Task.WhenAll(tasks);
            pgScan.Value = pgScan.Maximum;
            txtScanOutput.AppendText($"\r\nScan complete: {openCount} open ports.\r\n");
            pgScan.Visibility = Visibility.Collapsed;
            lblStatus.Text = $"[ SCAN DONE — {openCount} open ]";
        }

        // ── TRACEROUTE ──

        private async void BtnTrace_Click(object sender, RoutedEventArgs e)
        {
            var host = txtTraceHost.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;

            txtTraceOutput.Text = $"Tracing route to {host}...\r\n\r\n";
            lblStatus.Text = "[ TRACING ]";

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo("tracert", $"-d -w 1000 {host}")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc == null) return;
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        var line = proc.StandardOutput.ReadLine();
                        if (line != null)
                            Dispatcher.Invoke(() =>
                            {
                                txtTraceOutput.AppendText(line + "\r\n");
                                txtTraceOutput.ScrollToEnd();
                            });
                    }
                    proc.WaitForExit();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => txtTraceOutput.AppendText($"Error: {ex.Message}\r\n"));
                }
            });

            lblStatus.Text = "[ TRACE DONE ]";
        }

        // ── DNS LOOKUP ──

        private async void BtnDns_Click(object sender, RoutedEventArgs e)
        {
            var host = txtDnsHost.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;

            txtDnsOutput.Text = $"DNS Lookup: {host}\r\n\r\n";
            lblStatus.Text = "[ RESOLVING ]";

            try
            {
                var entry = await Dns.GetHostEntryAsync(host);
                txtDnsOutput.AppendText($"Host: {entry.HostName}\r\n");
                txtDnsOutput.AppendText($"Aliases: {string.Join(", ", entry.Aliases)}\r\n\r\n");
                txtDnsOutput.AppendText("Addresses:\r\n");
                foreach (var addr in entry.AddressList)
                {
                    var type = addr.AddressFamily == AddressFamily.InterNetworkV6 ? "IPv6" : "IPv4";
                    txtDnsOutput.AppendText($"  {type}: {addr}\r\n");
                }
            }
            catch (Exception ex)
            {
                txtDnsOutput.AppendText($"Error: {ex.Message}\r\n");
            }

            lblStatus.Text = "[ DNS DONE ]";
        }

        // ── WHOIS ──

        private async void BtnWhois_Click(object sender, RoutedEventArgs e)
        {
            var host = txtDnsHost.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;

            txtDnsOutput.Text = $"WHOIS Lookup: {host}\r\n\r\n";
            lblStatus.Text = "[ WHOIS QUERY ]";

            await Task.Run(() =>
            {
                try
                {
                    using var client = new TcpClient("whois.iana.org", 43);
                    using var stream = client.GetStream();
                    var query = Encoding.ASCII.GetBytes(host + "\r\n");
                    stream.Write(query, 0, query.Length);

                    var buffer = new byte[8192];
                    var sb = new StringBuilder();
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                    // Find refer server
                    var result = sb.ToString();
                    var referLine = result.Split('\n')
                        .FirstOrDefault(l => l.StartsWith("refer:", StringComparison.OrdinalIgnoreCase));

                    if (referLine != null)
                    {
                        var referServer = referLine.Split(':')[1].Trim();
                        using var client2 = new TcpClient(referServer, 43);
                        using var stream2 = client2.GetStream();
                        stream2.Write(query, 0, query.Length);
                        sb.Clear();
                        while ((bytesRead = stream2.Read(buffer, 0, buffer.Length)) > 0)
                            sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                        result = sb.ToString();
                    }

                    Dispatcher.Invoke(() =>
                    {
                        txtDnsOutput.AppendText(result);
                        txtDnsOutput.ScrollToEnd();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => txtDnsOutput.AppendText($"Error: {ex.Message}\r\n"));
                }
            });

            lblStatus.Text = "[ WHOIS DONE ]";
        }

        // ── Helpers ──

        private static string GetServiceName(int port) => port switch
        {
            21 => "(FTP)", 22 => "(SSH)", 23 => "(Telnet)", 25 => "(SMTP)",
            53 => "(DNS)", 80 => "(HTTP)", 110 => "(POP3)", 143 => "(IMAP)",
            443 => "(HTTPS)", 445 => "(SMB)", 993 => "(IMAPS)", 995 => "(POP3S)",
            3306 => "(MySQL)", 3389 => "(RDP)", 5432 => "(PostgreSQL)",
            5900 => "(VNC)", 6379 => "(Redis)", 8080 => "(HTTP-ALT)",
            8443 => "(HTTPS-ALT)", 27017 => "(MongoDB)",
            _ => ""
        };
    }
}
