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
    /// <summary>
    /// Monitors kinetic and non-kinetic war actions across the Middle East theater.
    /// Categories: SIGACT (significant activity), Sanctions/Arms, Cyber Operations, Proxy/Militia.
    /// </summary>
    public class WarMonitoringService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        // ══════════════════════════════════════════
        //  WAR OPS RSS FEED SOURCES
        // ══════════════════════════════════════════

        private static readonly List<WarOpsFeed> _feeds = new()
        {
            // ── SIGACT / Kinetic Operations ──
            new WarOpsFeed("Liveuamap ME", "https://liveuamap.com/rss/middleeast", WarOpsCategory.SIGACT,
                "Live conflict map — drone strikes, airstrikes, artillery, ground ops"),
            new WarOpsFeed("Janes Defence", "https://www.janes.com/feeds/news", WarOpsCategory.SIGACT,
                "Defense industry intelligence — military operations, weapons deployments"),
            new WarOpsFeed("CENTCOM Press", "https://www.centcom.mil/MEDIA/Press-Releases/RSS/", WarOpsCategory.SIGACT,
                "US Central Command official press releases"),
            new WarOpsFeed("IDF Spokesperson", "https://www.idf.il/en/mini-sites/idf-rss-feeds/", WarOpsCategory.SIGACT,
                "Israeli Defense Forces official updates"),
            new WarOpsFeed("Defense One", "https://www.defenseone.com/rss/", WarOpsCategory.SIGACT,
                "US defense policy and operations reporting"),
            new WarOpsFeed("War on the Rocks", "https://warontherocks.com/feed/", WarOpsCategory.SIGACT,
                "Strategic analysis and military operations commentary"),
            new WarOpsFeed("The War Zone", "https://www.thedrive.com/the-war-zone/feed", WarOpsCategory.SIGACT,
                "Military aviation, naval ops, and weapons analysis"),
            new WarOpsFeed("Long War Journal", "https://www.longwarjournal.org/feed", WarOpsCategory.SIGACT,
                "Tracking global war on terror operations"),
            new WarOpsFeed("Bellingcat", "https://www.bellingcat.com/feed/", WarOpsCategory.SIGACT,
                "Open-source investigative journalism — conflict verification"),
            new WarOpsFeed("ACLED Conflict", "https://acleddata.com/feed/", WarOpsCategory.SIGACT,
                "Armed Conflict Location & Event Data — incident tracking"),
            new WarOpsFeed("Airwars", "https://airwars.org/feed/", WarOpsCategory.SIGACT,
                "Airstrike tracking and civilian casualty monitoring"),
            new WarOpsFeed("SOFREP", "https://sofrep.com/feed/", WarOpsCategory.SIGACT,
                "Special operations and military intelligence reporting"),

            // ── Sanctions & Arms Transfers ──
            new WarOpsFeed("SIPRI News", "https://www.sipri.org/rss/news", WarOpsCategory.Sanctions,
                "Stockholm International Peace Research Institute — arms transfers"),
            new WarOpsFeed("OFAC / Treasury", "https://home.treasury.gov/system/files/126/rss.xml", WarOpsCategory.Sanctions,
                "US Treasury OFAC sanctions designations"),
            new WarOpsFeed("EU Sanctions Map", "https://www.sanctionsmap.eu/api/v1/rss", WarOpsCategory.Sanctions,
                "European Union sanctions updates"),
            new WarOpsFeed("Arms Control Assoc.", "https://www.armscontrol.org/rss.xml", WarOpsCategory.Sanctions,
                "Arms control agreements, treaties, and nonproliferation"),
            new WarOpsFeed("UN News - Peace", "https://news.un.org/feed/subscribe/en/news/topic/peace-and-security/feed/rss.xml", WarOpsCategory.Sanctions,
                "UN Security Council resolutions and peace operations"),
            new WarOpsFeed("IAEA News", "https://www.iaea.org/feeds/news", WarOpsCategory.Sanctions,
                "International Atomic Energy Agency — nuclear safeguards"),

            // ── Cyber Operations ──
            new WarOpsFeed("The Record", "https://therecord.media/feed", WarOpsCategory.Cyber,
                "Cybersecurity news — nation-state hacking, APT groups"),
            new WarOpsFeed("Krebs on Security", "https://krebsonsecurity.com/feed/", WarOpsCategory.Cyber,
                "Cybersecurity investigations — state-sponsored attacks"),
            new WarOpsFeed("Dark Reading", "https://www.darkreading.com/rss.xml", WarOpsCategory.Cyber,
                "Enterprise cybersecurity — cyber warfare operations"),
            new WarOpsFeed("Mandiant Blog", "https://www.mandiant.com/resources/blog/rss.xml", WarOpsCategory.Cyber,
                "Threat intelligence — Iranian/Russian/Chinese APT groups"),
            new WarOpsFeed("CyberScoop", "https://www.cyberscoop.com/feed/", WarOpsCategory.Cyber,
                "Government cybersecurity — offensive cyber operations"),
            new WarOpsFeed("BleepingComputer", "https://www.bleepingcomputer.com/feed/", WarOpsCategory.Cyber,
                "Ransomware, wipers, and destructive cyber attacks"),
            new WarOpsFeed("Recorded Future", "https://www.recordedfuture.com/feed", WarOpsCategory.Cyber,
                "Threat intelligence — state-sponsored cyber operations"),

            // ── Proxy / Militia Activity ──
            new WarOpsFeed("Al Monitor", "https://www.al-monitor.com/rss", WarOpsCategory.Proxy,
                "Middle East policy — Hezbollah, Houthi, PMU militia coverage"),
            new WarOpsFeed("Middle East Eye", "https://www.middleeasteye.net/rss", WarOpsCategory.Proxy,
                "Regional reporting — proxy warfare, militia movements"),
            new WarOpsFeed("Al Jazeera English", "https://www.aljazeera.com/xml/rss/all.xml", WarOpsCategory.Proxy,
                "Regional news — conflict areas, proxy group activities"),
            new WarOpsFeed("Iran International", "https://www.iranintl.com/en/feed", WarOpsCategory.Proxy,
                "Iranian opposition media — IRGC/proxy operations"),
            new WarOpsFeed("South Front", "https://southfront.press/feed/", WarOpsCategory.Proxy,
                "Military analysis — Syria, Iraq, Yemen proxy conflicts"),
            new WarOpsFeed("Terrorism Monitor", "https://jamestown.org/programs/tm/feed/", WarOpsCategory.Proxy,
                "Jamestown Foundation — terrorism and insurgency tracking"),
            new WarOpsFeed("CSIS Missile Threat", "https://missilethreat.csis.org/feed/", WarOpsCategory.SIGACT,
                "Missile launches, tests, and ballistic missile defense events"),
            new WarOpsFeed("NK News", "https://www.nknews.org/feed/", WarOpsCategory.Proxy,
                "North Korea proliferation — Iran-DPRK weapons cooperation"),
        };

        // ══════════════════════════════════════════
        //  KEYWORD CLASSIFIERS
        // ══════════════════════════════════════════

        private static readonly string[] _sigactKeywords = {
            "airstrike", "air strike", "drone strike", "bombing", "bombardment",
            "shelling", "artillery", "rocket attack", "missile launch", "missile strike",
            "IED", "ambush", "firefight", "clashes", "skirmish", "offensive",
            "counter-offensive", "invasion", "incursion", "raid", "special operation",
            "killed", "casualties", "wounded", "KIA", "WIA", "MIA",
            "SIGACT", "TIC", "troops in contact", "engagement", "intercepted",
            "shoot down", "shot down", "destroyed", "neutralized", "eliminated",
            "detonation", "explosion", "suicide bomb", "VBIED", "SVBIED",
            "naval engagement", "maritime intercept", "boarding operation",
            "no-fly zone", "air defense", "SAM launch", "AAA fire",
            "ground operation", "mechanized", "armored", "tank", "IFV",
            "sniper", "mortar", "MLRS", "HIMARS", "Grad", "Qassam",
            "Iron Dome intercept", "Arrow intercept", "Patriot intercept",
            "NOTAM", "airspace closure", "TFR", "DACT",
            "hostage", "kidnapping", "prisoner exchange", "POW",
            "chemical attack", "chemical weapons", "white phosphorus",
            "cluster munitions", "thermobaric", "bunker buster",
            "assassination", "targeted killing", "decapitation strike"
        };

        private static readonly string[] _sanctionsKeywords = {
            "sanctions", "sanctioned", "designated", "OFAC", "SDN list",
            "arms embargo", "weapons embargo", "trade restriction",
            "asset freeze", "travel ban", "export control",
            "proliferation", "technology transfer", "dual-use",
            "arms deal", "weapons sale", "military aid", "defense package",
            "JCPOA", "nuclear deal", "enrichment", "centrifuge",
            "IAEA inspection", "safeguards", "nuclear material",
            "UN resolution", "Security Council", "Chapter VII",
            "weapons of mass destruction", "WMD", "CBRN",
            "nonproliferation", "arms control", "treaty violation",
            "war crimes", "ICC", "international humanitarian law", "IHL",
            "Geneva Convention", "Hague Convention", "Rome Statute"
        };

        private static readonly string[] _cyberKeywords = {
            "cyber attack", "cyberattack", "hack", "hacking", "breach",
            "APT", "advanced persistent threat", "nation-state",
            "ransomware", "wiper", "malware", "zero-day", "0-day",
            "Charming Kitten", "APT33", "APT34", "APT35", "APT42",
            "MuddyWater", "Shamoon", "OilRig", "Lyceum", "Agrius",
            "Sandworm", "Fancy Bear", "Cozy Bear", "Lazarus",
            "infrastructure attack", "SCADA", "ICS", "critical infrastructure",
            "power grid", "water treatment", "oil refinery",
            "DDoS", "defacement", "espionage", "exfiltration",
            "signal intelligence", "SIGINT", "ELINT", "COMINT",
            "electronic warfare", "jamming", "spoofing", "GPS jamming",
            "information warfare", "disinformation", "deepfake",
            "offensive cyber", "CNE", "CNA", "computer network",
            "Stuxnet", "Olympic Games", "Nitro Zeus",
            "dark web", "Iranian hackers", "Israeli cyber", "Unit 8200"
        };

        private static readonly string[] _proxyKeywords = {
            "Hezbollah", "Hizballah", "Houthi", "Ansar Allah",
            "PMU", "PMF", "Popular Mobilization", "Hashd al-Shaabi",
            "Kata'ib Hezbollah", "Kataib", "Asa'ib Ahl al-Haq",
            "Badr Organization", "Harakat Hezbollah al-Nujaba",
            "Islamic Jihad", "Hamas", "PIJ",
            "IRGC Quds Force", "Quds Force", "Qasem Soleimani",
            "militia", "proxy force", "proxy war", "axis of resistance",
            "resistance axis", "tunnel", "weapons smuggling",
            "arms shipment", "weapons cache", "rocket arsenal",
            "Shahed drone", "loitering munition", "suicide drone",
            "maritime militia", "fast attack craft", "IRGCN",
            "border violation", "cross-border", "infiltration",
            "IDF operation", "operation", "campaign",
            "ceasefire violation", "truce breach", "escalation",
            "retaliation", "retaliatory", "response operation",
            "Syrian Democratic Forces", "SDF", "YPG", "PKK",
            "ISIS", "ISIL", "Daesh", "al-Qaeda", "AQAP",
            "Taliban", "TTP", "Jaish", "Lashkar"
        };

        // ══════════════════════════════════════════
        //  GEO-LOCATION DATABASE
        // ══════════════════════════════════════════

        private static readonly Dictionary<string, (double Lat, double Lon, string Region)> _geoLocations = new(StringComparer.OrdinalIgnoreCase)
        {
            // Major conflict zones
            ["Gaza"] = (31.35, 34.30, "Gaza Strip"),
            ["Gaza Strip"] = (31.35, 34.30, "Gaza Strip"),
            ["Khan Younis"] = (31.35, 34.30, "Gaza Strip"),
            ["Rafah"] = (31.28, 34.24, "Gaza-Egypt Border"),
            ["West Bank"] = (31.95, 35.20, "West Bank"),
            ["Jenin"] = (32.46, 35.30, "West Bank"),
            ["Nablus"] = (32.22, 35.26, "West Bank"),
            ["Hebron"] = (31.53, 35.10, "West Bank"),
            ["Ramallah"] = (31.90, 35.20, "West Bank"),
            ["Jerusalem"] = (31.77, 35.23, "Jerusalem"),
            ["Tel Aviv"] = (32.08, 34.78, "Israel"),
            ["Haifa"] = (32.79, 34.99, "Israel"),
            ["Golan Heights"] = (33.00, 35.80, "Golan Heights"),
            // Lebanon
            ["Beirut"] = (33.89, 35.50, "Lebanon"),
            ["South Lebanon"] = (33.27, 35.46, "Lebanon"),
            ["Bekaa Valley"] = (33.85, 36.10, "Lebanon"),
            ["Baalbek"] = (34.01, 36.21, "Lebanon"),
            // Syria
            ["Damascus"] = (33.51, 36.29, "Syria"),
            ["Aleppo"] = (36.20, 37.15, "Syria"),
            ["Idlib"] = (35.93, 36.63, "Syria"),
            ["Deir ez-Zor"] = (35.34, 40.14, "Syria"),
            ["Al-Tanf"] = (33.50, 38.63, "Syria"),
            ["Tartus"] = (34.89, 35.89, "Syria"),
            ["Latakia"] = (35.52, 35.78, "Syria"),
            ["Homs"] = (34.73, 36.72, "Syria"),
            ["Raqqa"] = (35.95, 39.01, "Syria"),
            // Iraq
            ["Baghdad"] = (33.31, 44.37, "Iraq"),
            ["Erbil"] = (36.19, 44.01, "Iraq"),
            ["Mosul"] = (36.34, 43.14, "Iraq"),
            ["Basra"] = (30.51, 47.81, "Iraq"),
            ["Al Asad"] = (33.78, 42.44, "Iraq"),
            ["Kirkuk"] = (35.47, 44.39, "Iraq"),
            ["Sulaymaniyah"] = (35.56, 45.43, "Iraq"),
            ["Sinjar"] = (36.32, 41.87, "Iraq"),
            ["Taji"] = (33.53, 44.26, "Iraq"),
            // Yemen
            ["Sanaa"] = (15.37, 44.19, "Yemen"),
            ["Aden"] = (12.78, 45.02, "Yemen"),
            ["Hodeidah"] = (14.80, 42.95, "Yemen"),
            ["Marib"] = (15.46, 45.32, "Yemen"),
            ["Saada"] = (16.94, 43.76, "Yemen"),
            // Iran
            ["Tehran"] = (35.69, 51.39, "Iran"),
            ["Isfahan"] = (32.65, 51.68, "Iran"),
            ["Shiraz"] = (29.59, 52.58, "Iran"),
            ["Tabriz"] = (38.08, 46.30, "Iran"),
            ["Bushehr"] = (28.97, 50.84, "Iran"),
            ["Bandar Abbas"] = (27.19, 56.28, "Iran"),
            ["Natanz"] = (33.72, 51.73, "Iran"),
            ["Fordow"] = (34.38, 50.97, "Iran"),
            ["Kharg Island"] = (29.24, 50.31, "Iran"),
            // Gulf States
            ["Riyadh"] = (24.71, 46.67, "Saudi Arabia"),
            ["Jeddah"] = (21.49, 39.19, "Saudi Arabia"),
            ["Abu Dhabi"] = (24.45, 54.65, "UAE"),
            ["Dubai"] = (25.20, 55.27, "UAE"),
            ["Doha"] = (25.29, 51.53, "Qatar"),
            ["Manama"] = (26.23, 50.59, "Bahrain"),
            ["Kuwait City"] = (29.38, 47.99, "Kuwait"),
            ["Muscat"] = (23.59, 58.54, "Oman"),
            // Chokepoints
            ["Strait of Hormuz"] = (26.56, 56.25, "Strait of Hormuz"),
            ["Bab el-Mandeb"] = (12.58, 43.33, "Bab el-Mandeb"),
            ["Suez Canal"] = (30.45, 32.35, "Suez Canal"),
            ["Red Sea"] = (20.0, 38.5, "Red Sea"),
            ["Persian Gulf"] = (27.0, 51.0, "Persian Gulf"),
            // Turkey
            ["Ankara"] = (39.93, 32.86, "Turkey"),
            ["Istanbul"] = (41.01, 28.98, "Turkey"),
            ["Incirlik"] = (37.09, 37.00, "Turkey"),
            // Egypt
            ["Cairo"] = (30.04, 31.24, "Egypt"),
            ["Sinai"] = (29.50, 33.80, "Egypt"),
            // Libya
            ["Tripoli"] = (32.90, 13.18, "Libya"),
            ["Benghazi"] = (32.12, 20.09, "Libya"),
            // Sudan
            ["Khartoum"] = (15.60, 32.53, "Sudan"),
            // Somalia
            ["Mogadishu"] = (2.05, 45.32, "Somalia"),
            // Djibouti
            ["Djibouti"] = (11.55, 43.15, "Djibouti"),
        };

        // ══════════════════════════════════════════
        //  FETCH & CLASSIFY
        // ══════════════════════════════════════════

        public async Task<List<ConflictEvent>> FetchAllAsync()
        {
            var allEvents = new List<ConflictEvent>();
            var tasks = _feeds.Select(f => FetchFeedAsync(f)).ToList();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch { }

            foreach (var t in tasks)
            {
                if (t.IsCompletedSuccessfully)
                    allEvents.AddRange(t.Result);
            }

            return allEvents
                .OrderByDescending(e => e.Severity)
                .ThenByDescending(e => e.Timestamp)
                .Take(500)
                .ToList();
        }

        private async Task<List<ConflictEvent>> FetchFeedAsync(WarOpsFeed feed)
        {
            var events = new List<ConflictEvent>();
            try
            {
                var response = await _http.GetStringAsync(feed.Url);
                using var reader = XmlReader.Create(new System.IO.StringReader(response));
                var syndFeed = SyndicationFeed.Load(reader);
                if (syndFeed == null) return events;

                foreach (var item in syndFeed.Items.Take(25))
                {
                    var title = item.Title?.Text ?? "";
                    var summary = item.Summary?.Text ?? "";
                    var cleanSummary = System.Text.RegularExpressions.Regex.Replace(summary, "<.*?>", "").Trim();
                    if (cleanSummary.Length > 500) cleanSummary = cleanSummary[..500] + "...";

                    var combined = $"{title} {cleanSummary}".ToLowerInvariant();

                    // Check if this is relevant to war operations
                    var relevanceScore = CalculateRelevance(combined, feed.Category);
                    if (relevanceScore < 2) continue; // Skip low-relevance articles

                    var url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                    var timestamp = item.PublishDate.UtcDateTime;
                    if (timestamp == DateTime.MinValue) timestamp = DateTime.UtcNow;

                    var category = ClassifyCategory(combined, feed.Category);
                    var severity = ClassifySeverity(combined, relevanceScore);
                    var tags = BuildTags(combined, feed.Category);
                    var (lat, lon, location) = GeoLocate(combined);

                    events.Add(new ConflictEvent
                    {
                        Title = title,
                        Summary = cleanSummary,
                        Source = feed.Name,
                        SourceUrl = url,
                        DataSource = DataSource.RSS,
                        Category = category,
                        Severity = severity,
                        Timestamp = timestamp,
                        Latitude = lat,
                        Longitude = lon,
                        Location = location,
                        Tags = tags
                    });
                }
            }
            catch { }
            return events;
        }

        private int CalculateRelevance(string text, WarOpsCategory feedCat)
        {
            int score = 0;
            var keywords = feedCat switch
            {
                WarOpsCategory.SIGACT => _sigactKeywords,
                WarOpsCategory.Sanctions => _sanctionsKeywords,
                WarOpsCategory.Cyber => _cyberKeywords,
                WarOpsCategory.Proxy => _proxyKeywords,
                _ => _sigactKeywords
            };

            foreach (var kw in keywords)
                if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    score++;

            // Cross-category bonuses
            foreach (var kw in _sigactKeywords.Take(20))
                if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    score++;

            return score;
        }

        private EventCategory ClassifyCategory(string text, WarOpsCategory feedCat)
        {
            // Check for specific categories
            if (_cyberKeywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)) > 2)
                return EventCategory.Cyber;
            if (_sanctionsKeywords.Any(k => k.Contains("nuclear") || k.Contains("IAEA") || k.Contains("enrichment"))
                && _sanctionsKeywords.Count(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)) > 1)
                return EventCategory.Nuclear;

            return feedCat switch
            {
                WarOpsCategory.SIGACT => EventCategory.Military,
                WarOpsCategory.Sanctions => EventCategory.Diplomatic,
                WarOpsCategory.Cyber => EventCategory.Cyber,
                WarOpsCategory.Proxy => EventCategory.Military,
                _ => EventCategory.Intelligence
            };
        }

        private SeverityLevel ClassifySeverity(string text, int relevanceScore)
        {
            var criticalTerms = new[] {
                "nuclear", "chemical attack", "invasion", "declaration of war",
                "massive", "unprecedented", "DEFCON", "nuclear launch",
                "WMD", "thermonuclear", "genocide", "ethnic cleansing",
                "assassination", "decapitation strike", "regime change"
            };
            var highTerms = new[] {
                "killed", "casualties", "airstrike", "missile strike",
                "offensive", "escalation", "breach", "APT", "zero-day",
                "intercept", "shoot down", "destroyed", "detonation",
                "sanctions", "embargo", "war crime"
            };

            if (criticalTerms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase)))
                return SeverityLevel.Critical;
            if (highTerms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase)) || relevanceScore >= 8)
                return SeverityLevel.High;
            if (relevanceScore >= 4)
                return SeverityLevel.Medium;
            return SeverityLevel.Low;
        }

        private List<string> BuildTags(string text, WarOpsCategory category)
        {
            var tags = new List<string>();

            // Primary category tag
            tags.Add(category.ToString().ToUpperInvariant());

            // Action type tags
            if (text.Contains("airstrike") || text.Contains("air strike")) tags.Add("AIRSTRIKE");
            if (text.Contains("drone strike") || text.Contains("drone attack")) tags.Add("DRONE-STRIKE");
            if (text.Contains("missile")) tags.Add("MISSILE");
            if (text.Contains("rocket")) tags.Add("ROCKET");
            if (text.Contains("artillery") || text.Contains("shelling")) tags.Add("ARTILLERY");
            if (text.Contains("naval") || text.Contains("maritime")) tags.Add("NAVAL");
            if (text.Contains("cyber") || text.Contains("hack")) tags.Add("CYBER");
            if (text.Contains("sanction")) tags.Add("SANCTIONS");
            if (text.Contains("nuclear") || text.Contains("enrichment")) tags.Add("NUCLEAR");
            if (text.Contains("chemical")) tags.Add("CBRN");
            if (text.Contains("hostage") || text.Contains("prisoner")) tags.Add("HOSTAGE");
            if (text.Contains("ceasefire") || text.Contains("truce")) tags.Add("CEASEFIRE");
            if (text.Contains("escalat")) tags.Add("ESCALATION");

            // Actor tags
            if (text.Contains("hezbollah") || text.Contains("hizballah")) tags.Add("HEZBOLLAH");
            if (text.Contains("houthi") || text.Contains("ansar allah")) tags.Add("HOUTHI");
            if (text.Contains("hamas")) tags.Add("HAMAS");
            if (text.Contains("irgc") || text.Contains("quds")) tags.Add("IRGC");
            if (text.Contains("isis") || text.Contains("daesh")) tags.Add("ISIS");
            if (text.Contains("idf") || text.Contains("israel")) tags.Add("ISRAEL");
            if (text.Contains("iran")) tags.Add("IRAN");
            if (text.Contains("russia")) tags.Add("RUSSIA");
            if (text.Contains("united states") || text.Contains("pentagon") || text.Contains("centcom")) tags.Add("US");
            if (text.Contains("nato")) tags.Add("NATO");

            return tags.Distinct().Take(8).ToList();
        }

        private (double? Lat, double? Lon, string Location) GeoLocate(string text)
        {
            // Try to find a known location in the text
            foreach (var (name, geo) in _geoLocations)
            {
                if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return (geo.Lat, geo.Lon, $"{name}, {geo.Region}");
            }
            return (null, null, "");
        }
    }

    // ══════════════════════════════════════════
    //  WAR OPS MODELS
    // ══════════════════════════════════════════

    public enum WarOpsCategory
    {
        SIGACT,      // Significant Activity — kinetic operations
        Sanctions,   // Sanctions, arms transfers, treaties
        Cyber,       // Cyber operations, hacking, EW
        Proxy        // Proxy forces, militia activity
    }

    public class WarOpsFeed
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public WarOpsCategory Category { get; set; }
        public string Description { get; set; }

        public WarOpsFeed(string name, string url, WarOpsCategory category, string description)
        {
            Name = name;
            Url = url;
            Category = category;
            Description = description;
        }
    }
}
