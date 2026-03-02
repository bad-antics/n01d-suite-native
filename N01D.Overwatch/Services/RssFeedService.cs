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
            // ── Major Wire Services ──
            new() { Name = "Reuters World", Url = "https://feeds.reuters.com/Reuters/worldNews", DefaultCategory = EventCategory.Military },
            new() { Name = "AP Top News", Url = "https://rsshub.app/apnews/topics/apf-topnews", DefaultCategory = EventCategory.Military },
            new() { Name = "AFP", Url = "https://www.france24.com/en/middle-east/rss", DefaultCategory = EventCategory.Military },

            // ── Broadcast / Major Outlets ──
            new() { Name = "BBC Middle East", Url = "https://feeds.bbci.co.uk/news/world/middle_east/rss.xml", DefaultCategory = EventCategory.Military },
            new() { Name = "CNN World", Url = "http://rss.cnn.com/rss/edition_world.rss", DefaultCategory = EventCategory.Military },
            new() { Name = "Sky News World", Url = "https://feeds.skynews.com/feeds/rss/world.xml", DefaultCategory = EventCategory.Military },
            new() { Name = "NPR World", Url = "https://feeds.npr.org/1004/rss.xml", DefaultCategory = EventCategory.Military },
            new() { Name = "Guardian World", Url = "https://www.theguardian.com/world/middleeast/rss", DefaultCategory = EventCategory.Military },

            // ── Regional / Middle East Focused ──
            new() { Name = "Al Jazeera", Url = "https://www.aljazeera.com/xml/rss/all.xml", DefaultCategory = EventCategory.Military },
            new() { Name = "Al Monitor", Url = "https://www.al-monitor.com/rss", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Middle East Eye", Url = "https://www.middleeasteye.net/rss", DefaultCategory = EventCategory.Military },
            new() { Name = "Arab News", Url = "https://www.arabnews.com/rss.xml", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Times of Israel", Url = "https://www.timesofisrael.com/feed/", DefaultCategory = EventCategory.Military },
            new() { Name = "Jerusalem Post", Url = "https://www.jpost.com/rss/rssfeedsmiddleeast", DefaultCategory = EventCategory.Military },
            new() { Name = "Iran International", Url = "https://www.iranintl.com/en/feed", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Tehran Times", Url = "https://www.tehrantimes.com/rss", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Press TV", Url = "https://www.presstv.ir/RSS", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Haaretz", Url = "https://www.haaretz.com/cmlink/1.628765", DefaultCategory = EventCategory.Military },

            // ── Defense / Military / Intel ──
            new() { Name = "Defense One", Url = "https://www.defenseone.com/rss/", DefaultCategory = EventCategory.Military },
            new() { Name = "War on the Rocks", Url = "https://warontherocks.com/feed/", DefaultCategory = EventCategory.Military },
            new() { Name = "Breaking Defense", Url = "https://breakingdefense.com/feed/", DefaultCategory = EventCategory.Military },
            new() { Name = "Defense News", Url = "https://www.defensenews.com/arc/outboundfeeds/rss/?outputType=xml", DefaultCategory = EventCategory.Military },
            new() { Name = "Military Times", Url = "https://www.militarytimes.com/arc/outboundfeeds/rss/?outputType=xml", DefaultCategory = EventCategory.Military },
            new() { Name = "The Drive/War Zone", Url = "https://www.thedrive.com/the-war-zone/feed", DefaultCategory = EventCategory.Military },
            new() { Name = "Janes", Url = "https://www.janes.com/feeds/news", DefaultCategory = EventCategory.Intelligence },
            new() { Name = "Stars & Stripes ME", Url = "https://www.stripes.com/theaters/middle_east.rss", DefaultCategory = EventCategory.Military },

            // ── Think Tanks / Analysis ──
            new() { Name = "CSIS", Url = "https://www.csis.org/analysis/feed", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Brookings ME", Url = "https://www.brookings.edu/topic/middle-east-north-africa/feed/", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "RAND", Url = "https://www.rand.org/topics/middle-east.xml", DefaultCategory = EventCategory.Intelligence },
            new() { Name = "CFR", Url = "https://www.cfr.org/rss.xml", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "Atlantic Council", Url = "https://www.atlanticcouncil.org/feed/", DefaultCategory = EventCategory.Diplomatic },
            new() { Name = "ISW", Url = "https://www.understandingwar.org/rss.xml", DefaultCategory = EventCategory.Military },

            // ── OSINT / Conflict Trackers ──
            new() { Name = "Liveuamap ME", Url = "https://liveuamap.com/rss/middleeast", DefaultCategory = EventCategory.Military },
            new() { Name = "Bellingcat", Url = "https://www.bellingcat.com/feed/", DefaultCategory = EventCategory.Intelligence },
            new() { Name = "ACLED", Url = "https://acleddata.com/feed/", DefaultCategory = EventCategory.Military },
            new() { Name = "Conflict Armament", Url = "https://www.conflictarm.com/feed/", DefaultCategory = EventCategory.Intelligence },

            // ── Nuclear / Arms Control ──
            new() { Name = "Arms Control Assoc", Url = "https://www.armscontrol.org/rss.xml", DefaultCategory = EventCategory.Nuclear },
            new() { Name = "IAEA News", Url = "https://www.iaea.org/feeds/press-releases", DefaultCategory = EventCategory.Nuclear },
            new() { Name = "NTI", Url = "https://www.nti.org/rss/all.xml", DefaultCategory = EventCategory.Nuclear },
            new() { Name = "Bulletin Atomic", Url = "https://thebulletin.org/feed/", DefaultCategory = EventCategory.Nuclear },

            // ── Economic / Energy ──
            new() { Name = "Oil Price", Url = "https://oilprice.com/rss/main", DefaultCategory = EventCategory.Economic },
            new() { Name = "OPEC News", Url = "https://www.opec.org/opec_web/en/press_room/28.htm?rss", DefaultCategory = EventCategory.Economic },
            new() { Name = "S&P Global Commodity", Url = "https://www.spglobal.com/commodityinsights/en/rss-feed/oil", DefaultCategory = EventCategory.Economic },

            // ── Cyber / Infosec ──
            new() { Name = "The Record", Url = "https://therecord.media/feed", DefaultCategory = EventCategory.Cyber },
            new() { Name = "CyberScoop", Url = "https://cyberscoop.com/feed/", DefaultCategory = EventCategory.Cyber },
            new() { Name = "Recorded Future", Url = "https://www.recordedfuture.com/feed", DefaultCategory = EventCategory.Cyber },

            // ── Humanitarian ──
            new() { Name = "UNHCR", Url = "https://www.unhcr.org/rss/news.xml", DefaultCategory = EventCategory.Humanitarian },
            new() { Name = "ReliefWeb ME", Url = "https://reliefweb.int/updates/rss.xml?search=middle+east", DefaultCategory = EventCategory.Humanitarian },
            new() { Name = "ICRC", Url = "https://www.icrc.org/en/rss", DefaultCategory = EventCategory.Humanitarian },
        };

        private static readonly string[] _iranKeywords = {
            // Iran — Government, Military, Nuclear
            "iran", "iranian", "tehran", "irgc", "quds", "quds force",
            "revolutionary guard", "basij", "artesh", "iriaf", "irin",
            "khamenei", "raisi", "pezeshkian", "zarif", "jalili", "shamkhani",
            "natanz", "fordow", "isfahan", "bushehr", "arak", "parchin",
            "enrichment", "centrifuge", "nuclear", "uranium", "plutonium",
            "jcpoa", "iaea", "breakout time", "weapons-grade",
            "strait of hormuz", "persian gulf", "chabahar", "bandar abbas",
            "kharg island", "lavan island",

            // Proxies & Allies
            "hezbollah", "houthi", "ansar allah", "hamas", "pij",
            "islamic jihad", "kata'ib hezbollah", "kata'ib sayyid",
            "popular mobilization", "pmu", "hashd al-shaabi",
            "axis of resistance", "proxy", "militia",

            // Israel & IDF
            "israel", "idf", "mossad", "shin bet", "netanyahu", "gallant",
            "iron dome", "arrow", "david's sling", "dimona",
            "gaza", "west bank", "golan heights", "negev",

            // Military Operations
            "airstrike", "air strike", "drone strike", "missile launch",
            "ballistic missile", "cruise missile", "hypersonic",
            "precision strike", "targeted killing", "sortie",
            "carrier strike group", "csg", "amphibious ready group",
            "b-52", "b-1", "b-2", "f-35", "f-22", "f-15", "f-16",
            "mq-9", "rq-4", "global hawk", "reaper",
            "tomahawk", "jdam", "agm-154", "agm-158",
            "shahed", "fateh", "emad", "khorramshahr", "sejjil",

            // Conflict & Escalation
            "escalation", "retaliation", "response", "counterattack",
            "ceasefire", "truce", "negotiations", "de-escalation",
            "middle east conflict", "regional war", "wider war",
            "red sea", "bab el-mandeb", "suez canal",

            // Geography
            "yemen", "iraq", "syria", "lebanon", "bahrain", "qatar",
            "saudi", "uae", "oman", "kuwait", "jordan",
            "damascus", "beirut", "baghdad", "sanaa", "aden",

            // Naval & Maritime
            "5th fleet", "navcent", "centcom", "eucom",
            "aircraft carrier", "destroyer", "frigate",
            "tanker seizure", "tanker attack", "maritime security",
            "freedom of navigation",

            // Sanctions & Economic
            "sanctions", "ofac", "oil embargo", "oil supply",
            "energy security", "pipeline", "lng",

            // Cyber
            "cyber attack", "apt33", "apt34", "apt35", "apt39",
            "charming kitten", "muddy water", "oilrig",
            "critical infrastructure", "scada", "stuxnet"
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
