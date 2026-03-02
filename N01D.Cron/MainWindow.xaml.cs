using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace N01D.Cron
{
    public enum JobScheduleType { Once, Interval, Daily, Hourly }

    public class CronJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "New Job";
        public string Command { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public JobScheduleType ScheduleType { get; set; } = JobScheduleType.Interval;
        public int IntervalSeconds { get; set; } = 300;
        public TimeSpan DailyTime { get; set; } = TimeSpan.FromHours(0);
        public bool IsEnabled { get; set; } = true;
        public DateTime? LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public int RunCount { get; set; }
        public int LastExitCode { get; set; }
        public string LastOutput { get; set; } = "";

        // Display helpers
        public string StatusIcon => !IsEnabled ? "⏸" : (LastExitCode == 0 || RunCount == 0) ? "✅" : "❌";
        public string Schedule => ScheduleType switch
        {
            JobScheduleType.Once => "Once",
            JobScheduleType.Interval => $"Every {FormatInterval(IntervalSeconds)}",
            JobScheduleType.Daily => $"Daily @ {DailyTime:hh\\:mm}",
            JobScheduleType.Hourly => "Every hour",
            _ => "?"
        };
        public string LastRunDisplay => LastRun?.ToString("MM/dd HH:mm:ss") ?? "—";
        public string NextRunDisplay => NextRun?.ToString("MM/dd HH:mm:ss") ?? "—";

        private static string FormatInterval(int secs) => secs switch
        {
            < 60 => $"{secs}s",
            < 3600 => $"{secs / 60}m",
            _ => $"{secs / 3600}h {secs % 3600 / 60}m"
        };
    }

    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<CronJob> _jobs = new();
        private readonly DispatcherTimer _scheduler;
        private readonly string _configDir;
        private readonly string _configFile;

        public MainWindow()
        {
            InitializeComponent();
            _configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "N01D", "Cron");
            _configFile = Path.Combine(_configDir, "jobs.json");
            Directory.CreateDirectory(_configDir);

            dgJobs.ItemsSource = _jobs;
            LoadJobs();

            _scheduler = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _scheduler.Tick += SchedulerTick;
        }

        private void LoadJobs()
        {
            if (!File.Exists(_configFile)) return;
            try
            {
                var json = File.ReadAllText(_configFile);
                var jobs = JsonSerializer.Deserialize<CronJob[]>(json);
                if (jobs != null)
                    foreach (var j in jobs) _jobs.Add(j);
            }
            catch { }
        }

        private void SaveJobs()
        {
            var json = JsonSerializer.Serialize(_jobs.ToArray(),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFile, json);
        }

        private void SchedulerTick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            foreach (var job in _jobs.Where(j => j.IsEnabled && j.NextRun.HasValue && j.NextRun <= now))
            {
                _ = RunJobAsync(job);
            }
            lblStatus.Text = $"[ RUNNING — {_jobs.Count(j => j.IsEnabled)} active jobs | {now:HH:mm:ss} ]";
        }

        private async Task RunJobAsync(CronJob job)
        {
            job.LastRun = DateTime.Now;
            job.RunCount++;
            CalculateNextRun(job);
            dgJobs.Items.Refresh();

            AppendLog($"[{DateTime.Now:HH:mm:ss}] ▶ {job.Name}: {job.Command}");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{job.Command.Replace("\"", "\\\"")}\"",
                    WorkingDirectory = string.IsNullOrEmpty(job.WorkingDirectory)
                        ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                        : job.WorkingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return;

                var output = await proc.StandardOutput.ReadToEndAsync();
                var error = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                job.LastExitCode = proc.ExitCode;
                job.LastOutput = string.IsNullOrEmpty(error) ? output : $"{output}\r\nERROR:\r\n{error}";

                var status = proc.ExitCode == 0 ? "✅" : "❌";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] {status} {job.Name}: exit={proc.ExitCode}");
                if (!string.IsNullOrWhiteSpace(output))
                    AppendLog(output.TrimEnd());
            }
            catch (Exception ex)
            {
                job.LastExitCode = -1;
                job.LastOutput = ex.Message;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] ❌ {job.Name}: {ex.Message}");
            }

            dgJobs.Items.Refresh();
            SaveJobs();
        }

        private static void CalculateNextRun(CronJob job)
        {
            var now = DateTime.Now;
            job.NextRun = job.ScheduleType switch
            {
                JobScheduleType.Interval => now.AddSeconds(job.IntervalSeconds),
                JobScheduleType.Hourly => now.AddHours(1),
                JobScheduleType.Daily => now.Date.Add(job.DailyTime) <= now
                    ? now.Date.AddDays(1).Add(job.DailyTime)
                    : now.Date.Add(job.DailyTime),
                JobScheduleType.Once => null,
                _ => null
            };
        }

        private void AppendLog(string text)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText(text + "\r\n");
                txtLog.ScrollToEnd();
            });
        }

        // ── Buttons ──

        private void BtnNewJob_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new JobDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                var job = dlg.Job;
                CalculateNextRun(job);
                _jobs.Add(job);
                SaveJobs();
                dgJobs.Items.Refresh();
            }
        }

        private void BtnStartAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var j in _jobs)
            {
                j.IsEnabled = true;
                CalculateNextRun(j);
            }
            _scheduler.Start();
            dgJobs.Items.Refresh();
            SaveJobs();
            lblStatus.Text = $"[ STARTED — {_jobs.Count} jobs ]";
        }

        private void BtnStopAll_Click(object sender, RoutedEventArgs e)
        {
            _scheduler.Stop();
            foreach (var j in _jobs) j.IsEnabled = false;
            dgJobs.Items.Refresh();
            SaveJobs();
            lblStatus.Text = "[ STOPPED ]";
        }

        private async void BtnRunNow_Click(object sender, RoutedEventArgs e)
        {
            if (dgJobs.SelectedItem is CronJob job)
                await RunJobAsync(job);
        }

        private void BtnDeleteJob_Click(object sender, RoutedEventArgs e)
        {
            if (dgJobs.SelectedItem is CronJob job)
            {
                if (MessageBox.Show($"Delete job \"{job.Name}\"?", "N01D Cron",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _jobs.Remove(job);
                    SaveJobs();
                }
            }
        }

        private void DgJobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgJobs.SelectedItem is CronJob job && !string.IsNullOrEmpty(job.LastOutput))
                txtLog.Text = $"--- Last output from {job.Name} ---\r\n{job.LastOutput}";
        }
    }
}
