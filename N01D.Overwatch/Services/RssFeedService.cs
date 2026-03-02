using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using N01D.Overwatch.Models;

namespace N01D.Overwatch.Services
{
    public class RssFeedService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private readonly HashSet<string> _seenIds = new();

        public static List<RssFeedConfig> DefaultFeeds => new()
        {
            new() { Name = "Reuters World", Url = "https://feeds.reuters.com/Reuters/worldNews", DefaultCategory = EventCategory.Military },
            new() { Name = "AP Top News", Url = "https://rsshub.app/apnews/topics/apf-topnews", DefaultCategory = EventCategory.Military },
            new() { Name = "BBC World", Url = "https://feeds.bbci.co.uk/news/world/middle_east/rss.xml", DefaultCategory = EventCategory.Military },
            new() { Name = "Al Jazeera", Url = "https://www.aljazeera.com/xml/rss/all.xml", DefaultCategory = EventCategory.Military },
            new() { Name = "Times of Israel", Url = "https://www.timesofisrael.com/feed/", DefaultCategory = EventCategory.Military },
            new() { Name = "Iran International", Url = "https://www.iranintl.com/en/feed", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Defense One", Url = "https://www.defenseone.com/rss/", DefaultCategory = EventCategory.Military },
            new() { Name = "CSIS", Url = "https://www.csis.org/analysis/feed", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Oil Price", Url = "https://oilprice.com/rss/main", DefaultCategory = EventCategory.Economic },
            new() { Name = "Liveuamap ME", Url = "https://liveuamap.com/rss/middleeast", DefaultCategory = EventCategory.Military },
        };

        private static readonly string[] _iranKeywords = {
            "iran", "iranian", "tehran", "irgc", "quds", "hezbollah", "houthi",
            "strait of hormuz", "persian gulf", "nuclear", "enrichment", "centrifuge",
            "proxy", "militia", "drone strike", "ballistic missile", "cruise missile",
            "sanctions", "jcpoa", "iaea", "natanz", "fordow", "isfahan", "bushehr",
            "khamenei", "raisi", "zarif", "revolutionary guard", "basij",
            "israel", "idf", "mossad", "netanyahu", "gaza", "lebanon",
            "middle east conflict", "escalation", "retaliation", "airstrike",
            "red sea", "yemen", "iraq", "syria", "axis of resistance"
        };

        public async Task<List<ConflictEvent>> FetchAllAsync(List<RssFeedConfig>? feeds = null)
        {
            feeds ??= DefaultFeeds;
            var events = new List<ConflictEvent>();
            var tasks = feeds.Where(f => f.Enabled).Select(f => FetchFeedAsync(f));
            var results = await Task.WhenAll(tasks);
            foreach (var batch in results)
                events.AddRange(batch);
            return events.OrderByDescending(e => e.Timestamp).ToList();
        }

        private async Task<List<ConflictEvent>> FetchFeedAsync(RssFeedConfig config)
        {
            var events = new List<ConflictEvent>();
            try
            {
                var xml = await _http.GetStringAsync(config.Url);
                using var reader = XmlReader.Create(new System.IO.StringReader(xml));
                var feed = SyndicationFeed.Load(reader);
                if (feed == null) return events;

                foreach (var item in feed.Items.Take(30))
                {
                    var title = item.Title?.Text ?? "";
                    var summary = item.Summary?.Text ?? "";
                    var combined = $"{title} {summary}".ToLowerInvariant();

                    // Filter for Iran/Middle East conflict relevance
                    if (!_iranKeywords.Any(k => combined.Contains(k)))
                        continue;

                    var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                    var id = $"{config.Name}:{item.Id ?? link}";
                    if (_seenIds.Contains(id)) continue;
                    _seenIds.Add(id);

                    var ev = new ConflictEvent
                    {
                        Title = StripHtml(title),
                        Summary = StripHtml(summary).Length > 500
                            ? StripHtml(summary)[..500] + "…"
                            : StripHtml(summary),
                        Source = config.Name,
                        SourceUrl = link,
                        DataSource = DataSource.RSS,
                        Category = ClassifyCategory(combined, config.DefaultCategory),
                        Severity = ClassifySeverity(combined),
                        Timestamp = item.PublishDate.UtcDateTime != default
                            ? item.PublishDate.UtcDateTime
                            : DateTime.UtcNow
                    };

                    ev.Tags = ExtractTags(combined);
                    ev.IsAlert = ev.Severity >= SeverityLevel.High;
                    events.Add(ev);
                }
            }
            catch { /* Feed unavailable — skip silently */ }
            return events;
        }

        private static EventCategory ClassifyCategory(string text, EventCategory fallback)
        {
            if (text.Contains("nuclear") || text.Contains("enrichment") || text.Contains("centrifuge") || text.Contains("iaea"))
                return EventCategory.Nuclear;
            if (text.Contains("cyber") || text.Contains("hack") || text.Contains("malware"))
                return EventCategory.Cyber;
            if (text.Contains("sanction") || text.Contains("oil") || text.Contains("economic") || text.Contains("trade"))
                return EventCategory.Economic;
            if (text.Contains("diplomat") || text.Contains("negotiat") || text.Contains("treaty") || text.Contains("talk"))
                return EventCategory.Diplomatic;
            if (text.Contains("humanitarian") || text.Contains("refugee") || text.Contains("civilian") || text.Contains("aid"))
                return EventCategory.Humanitarian;
            if (text.Contains("intelligen") || text.Contains("espionage") || text.Contains("spy") || text.Contains("mossad"))
                return EventCategory.Intelligence;
            if (text.Contains("strike") || text.Contains("missile") || text.Contains("military") || text.Contains("attack") ||
                text.Contains("drone") || text.Contains("bomb") || text.Contains("war"))
                return EventCategory.Military;
            return fallback;
        }

        private static SeverityLevel ClassifySeverity(string text)
        {
            var critical = new[] { "war declared", "nuclear weapon", "invasion", "full-scale",
                "mass casualt", "chemical weapon", "nuclear strike", "declaration of war" };
            var high = new[] { "airstrike", "missile launch", "drone strike", "killed",
                "bombing", "explosion", "retaliation", "escalat", "attack on",
                "shot down", "intercept", "casualties", "dead", "wound" };
            var medium = new[] { "sanction", "deploy", "military buildup", "threat",
                "warning", "mobiliz", "tension", "condemn", "proxy" };

            if (critical.Any(k => text.Contains(k))) return SeverityLevel.Critical;
            if (high.Any(k => text.Contains(k))) return SeverityLevel.High;
            if (medium.Any(k => text.Contains(k))) return SeverityLevel.Medium;
            return SeverityLevel.Low;
        }

        private static List<string> ExtractTags(string text)
        {
            var tags = new List<string>();
            var tagWords = new[] { "iran", "israel", "hezbollah", "houthi", "irgc", "hamas",
                "nuclear", "missile", "drone", "sanctions", "oil", "yemen", "lebanon",
                "syria", "iraq", "gaza", "cyber", "navy", "idf" };
            foreach (var t in tagWords)
                if (text.Contains(t)) tags.Add(t.ToUpperInvariant());
            return tags.Distinct().Take(6).ToList();
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
                .Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
                .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ")
                .Trim();
        }

        public void ClearSeen() => _seenIds.Clear();
    }
}
