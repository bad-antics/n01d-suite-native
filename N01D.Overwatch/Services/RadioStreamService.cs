using System;
using System.Collections.Generic;
using System.Linq;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Provides live warzone press radio stream URLs for conflict zone monitoring.
    /// Streams are publicly available press and news broadcasts covering
    /// Middle East and global conflict zones.
    /// </summary>
    public class RadioStreamService
    {
        public List<RadioStream> GetAllStreams()
        {
            return new List<RadioStream>
            {
                // ── Major International News (Live) ──
                new RadioStream
                {
                    Name = "Al Jazeera English",
                    Url = "https://live-hls-web-aje.getaj.net/AJE/01.m3u8",
                    WebUrl = "https://www.aljazeera.com/live",
                    Category = RadioCategory.LiveNews,
                    Region = "Global / Middle East",
                    Language = "English",
                    Description = "24/7 live news — primary Middle East conflict coverage",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "Al Jazeera Arabic",
                    Url = "https://live-hls-web-aja.getaj.net/AJA/01.m3u8",
                    WebUrl = "https://www.aljazeera.net/live",
                    Category = RadioCategory.LiveNews,
                    Region = "Middle East",
                    Language = "Arabic",
                    Description = "24/7 live Arabic news — ground-level conflict reporting",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "BBC World Service",
                    Url = "https://stream.live.vc.bbcmedia.co.uk/bbc_world_service",
                    WebUrl = "https://www.bbc.co.uk/sounds/play/live:bbc_world_service",
                    Category = RadioCategory.LiveNews,
                    Region = "Global",
                    Language = "English",
                    Description = "BBC World Service radio — global conflict and crisis coverage",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "France 24 English",
                    Url = "https://stream.france24.com/f24_en/live.m3u8",
                    WebUrl = "https://www.france24.com/en/live",
                    Category = RadioCategory.LiveNews,
                    Region = "Global / Middle East",
                    Language = "English",
                    Description = "France 24 English live — European perspective on ME conflicts",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "Voice of America – Middle East",
                    Url = "https://voa-ingest.akamaized.net/hls/live/2035200/voa-audio/index.m3u8",
                    WebUrl = "https://www.voanews.com/listen/live",
                    Category = RadioCategory.LiveNews,
                    Region = "Middle East",
                    Language = "English",
                    Description = "VOA Middle East desk — US government perspective on regional events",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "i24NEWS English",
                    Url = "https://bcovlive-a.akamaihd.net/r/0x0/v1/hls/live/i24news-en.m3u8",
                    WebUrl = "https://www.i24news.tv/en/live",
                    Category = RadioCategory.LiveNews,
                    Region = "Israel / Middle East",
                    Language = "English",
                    Description = "Israeli 24-hour international news — IDF operations, Israel security",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "TRT World",
                    Url = "https://tv-trtworld.medya.trt.com.tr/master.m3u8",
                    WebUrl = "https://www.trtworld.com/live",
                    Category = RadioCategory.LiveNews,
                    Region = "Turkey / Middle East",
                    Language = "English",
                    Description = "Turkish state broadcaster — Turkey, Syria, northern Iraq ops",
                    IsLive = true
                },

                // ── Regional / Conflict-Specific ──
                new RadioStream
                {
                    Name = "Rudaw English",
                    Url = "https://livestream.rudaw.net/live/smil:live.smil/playlist.m3u8",
                    WebUrl = "https://www.rudaw.net/english/live",
                    Category = RadioCategory.Regional,
                    Region = "Kurdistan / Iraq",
                    Language = "English",
                    Description = "Kurdish media — Peshmerga, PKK, northern Iraq/Syria frontlines",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "Iran International",
                    Url = "https://live.iranintl.com/hls/live.m3u8",
                    WebUrl = "https://www.iranintl.com/en/live",
                    Category = RadioCategory.Regional,
                    Region = "Iran",
                    Language = "English / Persian",
                    Description = "Iranian opposition media — IRGC activity, protests, proxy operations",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "Sky News Arabia",
                    Url = "https://stream.skynewsarabia.com/live/sna.m3u8",
                    WebUrl = "https://www.skynewsarabia.com/live-tv",
                    Category = RadioCategory.Regional,
                    Region = "Gulf / Middle East",
                    Language = "Arabic",
                    Description = "Abu Dhabi-based — Gulf perspective on Yemen, Libya, Iran",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "Al Arabiya",
                    Url = "https://live.alarabiya.net/alarabiapublish/alarabiya.smil/playlist.m3u8",
                    WebUrl = "https://www.alarabiya.net/live",
                    Category = RadioCategory.Regional,
                    Region = "Saudi Arabia / Middle East",
                    Language = "Arabic",
                    Description = "Saudi-owned — Riyadh perspective, Houthi/Yemen coverage",
                    IsLive = true
                },

                // ── Military / Defense Press Briefings ──
                new RadioStream
                {
                    Name = "CENTCOM Press Briefings",
                    Url = "",
                    WebUrl = "https://www.centcom.mil/MEDIA/",
                    Category = RadioCategory.MilitaryBriefing,
                    Region = "US CENTCOM AOR",
                    Language = "English",
                    Description = "US Central Command press releases and video briefings — check MEDIA page for live events",
                    IsLive = false
                },
                new RadioStream
                {
                    Name = "Pentagon Press Briefings",
                    Url = "",
                    WebUrl = "https://www.defense.gov/Multimedia/Videos/",
                    Category = RadioCategory.MilitaryBriefing,
                    Region = "US DoD",
                    Language = "English",
                    Description = "Department of Defense press briefings — SECDEF, Joint Staff updates",
                    IsLive = false
                },
                new RadioStream
                {
                    Name = "NATO Channel",
                    Url = "",
                    WebUrl = "https://www.natomultimedia.tv/",
                    Category = RadioCategory.MilitaryBriefing,
                    Region = "NATO",
                    Language = "English",
                    Description = "NATO Multimedia — press conferences, exercise footage, briefings",
                    IsLive = false
                },
                new RadioStream
                {
                    Name = "IDF Spokesperson",
                    Url = "",
                    WebUrl = "https://www.idf.il/en/",
                    Category = RadioCategory.MilitaryBriefing,
                    Region = "Israel",
                    Language = "English / Hebrew",
                    Description = "IDF official spokesperson updates — operation announcements, press statements",
                    IsLive = false
                },

                // ── Scanner / OSINT Audio ──
                new RadioStream
                {
                    Name = "LiveATC – Middle East",
                    Url = "",
                    WebUrl = "https://www.liveatc.net/",
                    Category = RadioCategory.Scanner,
                    Region = "Middle East ATC",
                    Language = "English / Arabic",
                    Description = "Live air traffic control audio — monitor NOTAMs, diversions, military traffic",
                    IsLive = false
                },
                new RadioStream
                {
                    Name = "Broadcastify – Military",
                    Url = "",
                    WebUrl = "https://www.broadcastify.com/listen/cat/65",
                    Category = RadioCategory.Scanner,
                    Region = "US Military",
                    Language = "English",
                    Description = "Military scanner feeds — base comms, guard frequencies when available",
                    IsLive = false
                },
                new RadioStream
                {
                    Name = "WebSDR HF – Netherlands",
                    Url = "",
                    WebUrl = "http://websdr.ewi.utwente.nl:8901/",
                    Category = RadioCategory.Scanner,
                    Region = "Global HF",
                    Language = "Various",
                    Description = "Web-based HF SDR receiver — monitor military HF, HFGCS, NATO comms on shortwave",
                    IsLive = true
                },
                new RadioStream
                {
                    Name = "KiwiSDR Network",
                    Url = "",
                    WebUrl = "http://kiwisdr.com/public/",
                    Category = RadioCategory.Scanner,
                    Region = "Global",
                    Language = "Various",
                    Description = "Network of web SDR receivers — find receivers near conflict zones for local HF/VHF monitoring",
                    IsLive = true
                },

                // ── Humanitarian / Field ──
                new RadioStream
                {
                    Name = "ICRC Updates",
                    Url = "",
                    WebUrl = "https://www.icrc.org/en/where-we-work/middle-east",
                    Category = RadioCategory.Humanitarian,
                    Region = "Middle East",
                    Language = "English",
                    Description = "International Red Cross — humanitarian corridor status, civilian protection updates",
                    IsLive = false
                },
                new RadioStream
                {
                    Name = "UNHCR – Middle East",
                    Url = "",
                    WebUrl = "https://www.unhcr.org/middle-east-and-north-africa.html",
                    Category = RadioCategory.Humanitarian,
                    Region = "Middle East / North Africa",
                    Language = "English",
                    Description = "UN Refugee Agency — displacement data, refugee camp situations, border crossings",
                    IsLive = false
                },
                new RadioStream
                {
                    Name = "OCHA ReliefWeb",
                    Url = "",
                    WebUrl = "https://reliefweb.int/",
                    Category = RadioCategory.Humanitarian,
                    Region = "Global",
                    Language = "English",
                    Description = "UN OCHA — situation reports, flash updates, humanitarian access snapshots",
                    IsLive = false
                },
            };
        }

        public List<RadioStream> GetByCategory(RadioCategory category)
        {
            return GetAllStreams().Where(s => s.Category == category).ToList();
        }

        public List<RadioStream> GetLiveStreams()
        {
            return GetAllStreams().Where(s => s.IsLive && !string.IsNullOrEmpty(s.Url)).ToList();
        }

        public (int Total, int Live, int News, int Regional, int Military, int Scanner, int Humanitarian) GetStats()
        {
            var all = GetAllStreams();
            return (
                all.Count,
                all.Count(s => s.IsLive && !string.IsNullOrEmpty(s.Url)),
                all.Count(s => s.Category == RadioCategory.LiveNews),
                all.Count(s => s.Category == RadioCategory.Regional),
                all.Count(s => s.Category == RadioCategory.MilitaryBriefing),
                all.Count(s => s.Category == RadioCategory.Scanner),
                all.Count(s => s.Category == RadioCategory.Humanitarian)
            );
        }
    }

    // ── Models ──

    public enum RadioCategory
    {
        LiveNews,
        Regional,
        MilitaryBriefing,
        Scanner,
        Humanitarian
    }

    public class RadioStream
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string WebUrl { get; set; } = "";
        public RadioCategory Category { get; set; }
        public string Region { get; set; } = "";
        public string Language { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsLive { get; set; }

        public string StatusDisplay => IsLive && !string.IsNullOrEmpty(Url) ? "● LIVE" : "○ ON-DEMAND";
        public string CategoryDisplay => Category switch
        {
            RadioCategory.LiveNews => "📡 LIVE NEWS",
            RadioCategory.Regional => "🌍 REGIONAL",
            RadioCategory.MilitaryBriefing => "🎖 MIL BRIEF",
            RadioCategory.Scanner => "📻 SCANNER",
            RadioCategory.Humanitarian => "🏥 HUMANITARIAN",
            _ => "📡 OTHER"
        };
    }
}
