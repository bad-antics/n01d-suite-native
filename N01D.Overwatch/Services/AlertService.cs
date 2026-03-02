using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using N01D.Overwatch.Models;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Manages alert rules and evaluates incoming events against them.
    /// Persists alert configuration to disk.
    /// </summary>
    public class AlertService
    {
        private static readonly string _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "N01D", "Overwatch");
        private static readonly string _alertFile = Path.Combine(_configDir, "alerts.json");
        private static readonly string _archiveFile = Path.Combine(_configDir, "archive.json");

        public List<AlertRule> Rules { get; private set; } = new();

        public AlertService()
        {
            Directory.CreateDirectory(_configDir);
            LoadRules();
            if (Rules.Count == 0) InitDefaultRules();
        }

        private void InitDefaultRules()
        {
            Rules = new List<AlertRule>
            {
                new()
                {
                    Name = "Military Escalation",
                    Keywords = new() { "airstrike", "missile launch", "bombing", "invasion", "attack" },
                    MinSeverity = SeverityLevel.High,
                    Categories = new() { EventCategory.Military }
                },
                new()
                {
                    Name = "Nuclear Activity",
                    Keywords = new() { "nuclear", "enrichment", "centrifuge", "iaea", "weapon" },
                    MinSeverity = SeverityLevel.Medium,
                    Categories = new() { EventCategory.Nuclear }
                },
                new()
                {
                    Name = "Oil Disruption",
                    Keywords = new() { "hormuz", "blockade", "tanker seizure", "oil supply" },
                    MinSeverity = SeverityLevel.High,
                    Categories = new() { EventCategory.Economic }
                },
                new()
                {
                    Name = "Cyber Attack",
                    Keywords = new() { "cyber attack", "hack", "critical infrastructure", "malware" },
                    MinSeverity = SeverityLevel.Medium,
                    Categories = new() { EventCategory.Cyber }
                }
            };
            SaveRules();
        }

        public bool EvaluateEvent(ConflictEvent ev)
        {
            foreach (var rule in Rules.Where(r => r.Enabled))
            {
                if (ev.Severity < rule.MinSeverity) continue;

                if (rule.Categories.Count > 0 && !rule.Categories.Contains(ev.Category))
                    continue;

                if (rule.Keywords.Count > 0)
                {
                    var combined = $"{ev.Title} {ev.Summary}".ToLowerInvariant();
                    if (!rule.Keywords.Any(k => combined.Contains(k.ToLowerInvariant())))
                        continue;
                }

                ev.IsAlert = true;
                return true;
            }
            return false;
        }

        public void SaveRules()
        {
            var json = JsonSerializer.Serialize(Rules, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_alertFile, json);
        }

        public void LoadRules()
        {
            try
            {
                if (File.Exists(_alertFile))
                {
                    var json = File.ReadAllText(_alertFile);
                    Rules = JsonSerializer.Deserialize<List<AlertRule>>(json) ?? new();
                }
            }
            catch { Rules = new(); }
        }

        public void ExportArchive(List<ConflictEvent> events, string format = "json")
        {
            Directory.CreateDirectory(_configDir);
            if (format == "json")
            {
                var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_archiveFile, json);
            }
            else if (format == "csv")
            {
                var csvPath = Path.ChangeExtension(_archiveFile, ".csv");
                var lines = new List<string> { "Timestamp,Severity,Category,Source,Title,Summary,Location,URL" };
                foreach (var e in events)
                {
                    var line = $"\"{e.Timestamp:u}\",\"{e.Severity}\",\"{e.Category}\",\"{e.Source}\"," +
                               $"\"{Escape(e.Title)}\",\"{Escape(e.Summary)}\",\"{e.Location}\",\"{e.SourceUrl}\"";
                    lines.Add(line);
                }
                File.WriteAllLines(csvPath, lines);
            }
        }

        private static string Escape(string s) => s.Replace("\"", "\"\"");

        public string GetArchivePath() => _configDir;
    }
}
