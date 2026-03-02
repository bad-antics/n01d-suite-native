using System;
using System.Collections.Generic;
using System.Linq;
using N01D.Overwatch.Models;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Tracks known missile sites, ballistic missile programs, air defense systems,
    /// and provides range/threat analysis for the Middle East theater.
    /// </summary>
    public class MissileDefenseService
    {
        // ══════════════════════════════════════════
        //  IRANIAN MISSILE SITES
        // ══════════════════════════════════════════

        private static readonly List<MissileSite> _missileSites = new()
        {
            // ── Iran — IRGC Aerospace Force Missile Bases ──
            new MissileSite
            {
                Id = "MS-IR-01", Name = "Imam Ali Missile Base",
                Latitude = 34.35, Longitude = 47.16,
                Country = "Iran", Operator = "IRGC-ASF",
                Type = MissileSiteType.BallisticMissile,
                MissileTypes = new() { "Shahab-3", "Ghadr-1", "Emad", "Khorramshahr" },
                MaxRangeKm = 2000,
                Description = "Major IRGC Aerospace Force missile garrison near Kermanshah. Underground storage. Shahab-3 & Ghadr-1 medium-range ballistic missiles.",
                ThreatLevel = SeverityLevel.Critical,
                IsUnderground = true
            },
            new MissileSite
            {
                Id = "MS-IR-02", Name = "Tabriz Missile Base",
                Latitude = 38.08, Longitude = 46.30,
                Country = "Iran", Operator = "IRGC-ASF",
                Type = MissileSiteType.BallisticMissile,
                MissileTypes = new() { "Fateh-110", "Zolfaghar", "Dezful" },
                MaxRangeKm = 1000,
                Description = "IRGC missile facility in northwest Iran. Solid-fuel short/medium-range ballistic missiles. Within range of Turkey, Azerbaijan, and the Caucasus.",
                ThreatLevel = SeverityLevel.High,
                IsUnderground = true
            },
            new MissileSite
            {
                Id = "MS-IR-03", Name = "Khorramabad Underground Base",
                Latitude = 33.49, Longitude = 48.35,
                Country = "Iran", Operator = "IRGC-ASF",
                Type = MissileSiteType.BallisticMissile,
                MissileTypes = new() { "Emad", "Shahab-3", "Sejjil" },
                MaxRangeKm = 2500,
                Description = "Deep underground missile base. Hardened tunnels housing medium-range & potentially IRBM-class missiles. Designed to survive US/Israeli bunker-buster strikes.",
                ThreatLevel = SeverityLevel.Critical,
                IsUnderground = true
            },
            new MissileSite
            {
                Id = "MS-IR-04", Name = "Semnan Missile Test Range",
                Latitude = 35.23, Longitude = 53.95,
                Country = "Iran", Operator = "IRGC-ASF / ISA",
                Type = MissileSiteType.TestRange,
                MissileTypes = new() { "Simorgh SLV", "Qased SLV", "Sejjil", "Khorramshahr-4" },
                MaxRangeKm = 0,
                Description = "Primary Iranian missile & space launch facility. Test site for all IRGC ballistic missile programs and IRGC/ISA space launches. ICBM-concern program.",
                ThreatLevel = SeverityLevel.High,
                IsUnderground = false
            },
            new MissileSite
            {
                Id = "MS-IR-05", Name = "Shahrud Missile Base",
                Latitude = 36.42, Longitude = 55.01,
                Country = "Iran", Operator = "IRGC-ASF",
                Type = MissileSiteType.BallisticMissile,
                MissileTypes = new() { "Shahab-3", "Ghadr-1", "Sejjil-2" },
                MaxRangeKm = 2000,
                Description = "Underground missile base in northeast Iran. Houses medium-range ballistic missiles. Also linked to solid-fuel Sejjil-2 program.",
                ThreatLevel = SeverityLevel.High,
                IsUnderground = true
            },
            new MissileSite
            {
                Id = "MS-IR-06", Name = "Bandar Abbas Coastal Missiles",
                Latitude = 27.19, Longitude = 56.28,
                Country = "Iran", Operator = "IRGCN",
                Type = MissileSiteType.AntiShip,
                MissileTypes = new() { "Noor", "Qader", "Khalij Fars ASBM", "Hormuz-2" },
                MaxRangeKm = 300,
                Description = "IRGC Navy anti-ship missile batteries guarding the Strait of Hormuz. Includes anti-ship ballistic missiles (ASBM) capable of targeting aircraft carriers.",
                ThreatLevel = SeverityLevel.Critical,
                IsUnderground = false
            },
            new MissileSite
            {
                Id = "MS-IR-07", Name = "Abu Musa Island",
                Latitude = 25.87, Longitude = 55.03,
                Country = "Iran", Operator = "IRGCN",
                Type = MissileSiteType.AntiShip,
                MissileTypes = new() { "C-802/Noor", "Khalij Fars" },
                MaxRangeKm = 200,
                Description = "Contested island in Persian Gulf fortified with IRGCN anti-ship missiles. Strategic position controlling Hormuz approaches.",
                ThreatLevel = SeverityLevel.High,
                IsUnderground = false
            },
            new MissileSite
            {
                Id = "MS-IR-08", Name = "Chabahar Coastal Defense",
                Latitude = 25.29, Longitude = 60.64,
                Country = "Iran", Operator = "IRGCN",
                Type = MissileSiteType.AntiShip,
                MissileTypes = new() { "Noor", "Qader", "Fateh Mobin" },
                MaxRangeKm = 250,
                Description = "IRGC Navy missile site on the Gulf of Oman coast. Provides coverage of Indian Ocean approaches and Makran coast.",
                ThreatLevel = SeverityLevel.Medium,
                IsUnderground = false
            },

            // ── Houthi (Yemen) — Iranian-supplied ──
            new MissileSite
            {
                Id = "MS-YE-01", Name = "Sa'ada Missile Storage",
                Latitude = 16.94, Longitude = 43.76,
                Country = "Yemen (Houthi)", Operator = "Ansar Allah",
                Type = MissileSiteType.BallisticMissile,
                MissileTypes = new() { "Toufan (Qiam-variant)", "Burkan-2", "Zulfiqar" },
                MaxRangeKm = 1400,
                Description = "Houthi stronghold in northern Yemen. Launch site for Iranian-supplied medium-range ballistic missiles targeting Saudi Arabia and UAE.",
                ThreatLevel = SeverityLevel.High,
                IsUnderground = true
            },
            new MissileSite
            {
                Id = "MS-YE-02", Name = "Al Hudaydah Anti-Ship",
                Latitude = 14.80, Longitude = 42.95,
                Country = "Yemen (Houthi)", Operator = "Ansar Allah",
                Type = MissileSiteType.AntiShip,
                MissileTypes = new() { "C-802 variant", "Drone boats", "Naval mines" },
                MaxRangeKm = 200,
                Description = "Houthi anti-ship missile launch zone covering Bab el-Mandeb and southern Red Sea. Source of attacks on commercial shipping.",
                ThreatLevel = SeverityLevel.High,
                IsUnderground = false
            },

            // ── Hezbollah (Lebanon) — Iranian-supplied ──
            new MissileSite
            {
                Id = "MS-LB-01", Name = "Bekaa Valley Arsenal",
                Latitude = 33.85, Longitude = 36.10,
                Country = "Lebanon", Operator = "Hezbollah",
                Type = MissileSiteType.BallisticMissile,
                MissileTypes = new() { "Fateh-110", "M-600 (Tishreen)", "Zelzal-2" },
                MaxRangeKm = 300,
                Description = "Hezbollah's suspected precision-guided missile stockpile in the Bekaa Valley. Iranian-supplied Fateh-110 variants capable of striking anywhere in Israel.",
                ThreatLevel = SeverityLevel.Critical,
                IsUnderground = true
            }
        };

        // ══════════════════════════════════════════
        //  AIR DEFENSE SYSTEMS
        // ══════════════════════════════════════════

        private static readonly List<AirDefenseSite> _airDefenseSites = new()
        {
            // ── Israel — Multi-layered missile defense ──
            new AirDefenseSite
            {
                Id = "AD-IL-01", Name = "Iron Dome Battery (Central)",
                Latitude = 32.08, Longitude = 34.78,
                Country = "Israel", Operator = "IDF",
                SystemType = "Iron Dome",
                MaxRangeKm = 70,
                Description = "Short-range air defense intercepting rockets, artillery, and mortars. 90%+ intercept rate. Protects Tel Aviv metropolitan area.",
                DefenseType = AirDefenseType.ShortRange,
                ThreatLevel = SeverityLevel.Medium
            },
            new AirDefenseSite
            {
                Id = "AD-IL-02", Name = "David's Sling Battery",
                Latitude = 31.90, Longitude = 34.85,
                Country = "Israel", Operator = "IDF",
                SystemType = "David's Sling (Stunner)",
                MaxRangeKm = 300,
                Description = "Medium-range interceptor for cruise missiles and short-range ballistic missiles. Bridges gap between Iron Dome and Arrow.",
                DefenseType = AirDefenseType.MediumRange,
                ThreatLevel = SeverityLevel.Medium
            },
            new AirDefenseSite
            {
                Id = "AD-IL-03", Name = "Arrow-3 Battery (Palmachim)",
                Latitude = 31.88, Longitude = 34.69,
                Country = "Israel", Operator = "IDF / IAI",
                SystemType = "Arrow-3",
                MaxRangeKm = 2400,
                Description = "Exo-atmospheric interceptor for long-range ballistic missiles. Can intercept IRBMs outside the atmosphere. Primary defense against Iranian Shahab/Sejjil.",
                DefenseType = AirDefenseType.ABM,
                ThreatLevel = SeverityLevel.Critical
            },
            new AirDefenseSite
            {
                Id = "AD-IL-04", Name = "Arrow-2 Battery (Ein Shemer)",
                Latitude = 32.44, Longitude = 35.02,
                Country = "Israel", Operator = "IDF / IAI",
                SystemType = "Arrow-2",
                MaxRangeKm = 150,
                Description = "Endo-atmospheric interceptor for ballistic missiles. First line of defense against Scud/Shahab-class threats.",
                DefenseType = AirDefenseType.ABM,
                ThreatLevel = SeverityLevel.High
            },

            // ── Saudi Arabia ──
            new AirDefenseSite
            {
                Id = "AD-SA-01", Name = "Patriot PAC-3 (Riyadh)",
                Latitude = 24.71, Longitude = 46.67,
                Country = "Saudi Arabia", Operator = "RSLF",
                SystemType = "Patriot PAC-3 MSE",
                MaxRangeKm = 160,
                Description = "US-supplied Patriot battery defending Saudi capital. Intercepted Houthi ballistic missiles on multiple occasions.",
                DefenseType = AirDefenseType.MediumRange,
                ThreatLevel = SeverityLevel.High
            },
            new AirDefenseSite
            {
                Id = "AD-SA-02", Name = "Patriot PAC-3 (Jeddah/Yanbu)",
                Latitude = 21.49, Longitude = 39.19,
                Country = "Saudi Arabia", Operator = "RSLF",
                SystemType = "Patriot PAC-3",
                MaxRangeKm = 160,
                Description = "Patriot battery protecting Jeddah and the western oil infrastructure. East-West Pipeline terminus defense.",
                DefenseType = AirDefenseType.MediumRange,
                ThreatLevel = SeverityLevel.Medium
            },

            // ── UAE ──
            new AirDefenseSite
            {
                Id = "AD-AE-01", Name = "THAAD Battery (Al Dhafra)",
                Latitude = 24.25, Longitude = 54.55,
                Country = "UAE", Operator = "US Army / UAE",
                SystemType = "THAAD",
                MaxRangeKm = 200,
                Description = "Terminal High Altitude Area Defense. US-deployed system protecting UAE from Iranian MRBM threats. Intercepted Houthi missiles in 2022.",
                DefenseType = AirDefenseType.ABM,
                ThreatLevel = SeverityLevel.Critical
            },

            // ── Iran — Air Defense ──
            new AirDefenseSite
            {
                Id = "AD-IR-01", Name = "S-300PMU2 Battery (Isfahan)",
                Latitude = 32.65, Longitude = 51.68,
                Country = "Iran", Operator = "IRIAF / Khatam al-Anbiya",
                SystemType = "S-300PMU2 Favorit",
                MaxRangeKm = 200,
                Description = "Russian-supplied long-range SAM defending Isfahan nuclear facility. Capable of engaging aircraft, cruise missiles, and ballistic missile RVs.",
                DefenseType = AirDefenseType.LongRange,
                ThreatLevel = SeverityLevel.High
            },
            new AirDefenseSite
            {
                Id = "AD-IR-02", Name = "Bavar-373 (Tehran)",
                Latitude = 35.69, Longitude = 51.39,
                Country = "Iran", Operator = "Khatam al-Anbiya ADA",
                SystemType = "Bavar-373",
                MaxRangeKm = 300,
                Description = "Indigenous Iranian long-range SAM (S-300 equivalent). Defends Tehran. Claimed capable of engaging stealth aircraft.",
                DefenseType = AirDefenseType.LongRange,
                ThreatLevel = SeverityLevel.High
            },
            new AirDefenseSite
            {
                Id = "AD-IR-03", Name = "Khordad-15 (Natanz)",
                Latitude = 33.73, Longitude = 51.73,
                Country = "Iran", Operator = "Khatam al-Anbiya ADA",
                SystemType = "3rd Khordad / Khordad-15",
                MaxRangeKm = 150,
                Description = "Iran's indigenous medium-range SAM system defending Natanz enrichment facility. Claimed to have downed US RQ-4 Global Hawk in 2019.",
                DefenseType = AirDefenseType.MediumRange,
                ThreatLevel = SeverityLevel.Medium
            },

            // ── Turkey ──
            new AirDefenseSite
            {
                Id = "AD-TR-01", Name = "S-400 Battery (Ankara)",
                Latitude = 39.93, Longitude = 32.86,
                Country = "Turkey", Operator = "TSK",
                SystemType = "S-400 Triumf",
                MaxRangeKm = 400,
                Description = "Russian-supplied S-400 SAM. Controversial NATO-member acquisition. Can track stealth aircraft. Caused US F-35 supply suspension.",
                DefenseType = AirDefenseType.LongRange,
                ThreatLevel = SeverityLevel.High
            },

            // ── Qatar ──
            new AirDefenseSite
            {
                Id = "AD-QA-01", Name = "Patriot PAC-3 (Al Udeid)",
                Latitude = 25.12, Longitude = 51.32,
                Country = "Qatar", Operator = "QEAF / US Army",
                SystemType = "Patriot PAC-3",
                MaxRangeKm = 160,
                Description = "Patriot battery defending Al Udeid Air Base (US CENTCOM Forward HQ). Critical force protection asset.",
                DefenseType = AirDefenseType.MediumRange,
                ThreatLevel = SeverityLevel.Medium
            }
        };

        // ══════════════════════════════════════════
        //  PUBLIC ACCESSORS
        // ══════════════════════════════════════════

        public List<MissileSite> GetAllMissileSites() => _missileSites.ToList();
        public List<AirDefenseSite> GetAllAirDefenseSites() => _airDefenseSites.ToList();

        public List<MissileSite> GetMissileSitesByCountry(string country) =>
            _missileSites.Where(s => s.Country.Contains(country, StringComparison.OrdinalIgnoreCase)).ToList();

        public List<AirDefenseSite> GetAirDefenseSitesByCountry(string country) =>
            _airDefenseSites.Where(s => s.Country.Contains(country, StringComparison.OrdinalIgnoreCase)).ToList();

        public List<MissileSite> GetCriticalThreats() =>
            _missileSites.Where(s => s.ThreatLevel >= SeverityLevel.High).ToList();

        /// <summary>Converts a missile site to a timeline ConflictEvent.</summary>
        public ConflictEvent ToConflictEvent(MissileSite site)
        {
            var icon = site.Type switch
            {
                MissileSiteType.BallisticMissile => "🚀",
                MissileSiteType.AntiShip => "🎯",
                MissileSiteType.CruiseMissile => "💨",
                MissileSiteType.TestRange => "🔬",
                _ => "🚀"
            };

            return new ConflictEvent
            {
                Id = site.Id,
                Title = $"{icon} {site.Name}",
                Summary = $"{site.Description}\nMissiles: {string.Join(", ", site.MissileTypes)}\nMax Range: {site.MaxRangeKm}km",
                Source = "OSINT / Missile Threat DB",
                DataSource = DataSource.OSINT,
                Category = EventCategory.Military,
                Severity = site.ThreatLevel,
                Latitude = site.Latitude,
                Longitude = site.Longitude,
                Location = $"{site.Country} ({site.Operator})",
                Timestamp = DateTime.UtcNow,
                Tags = new List<string> { "MISSILE", site.Type.ToString().ToUpperInvariant(), site.Country.ToUpperInvariant() }
            };
        }

        /// <summary>Converts an air defense site to a timeline ConflictEvent.</summary>
        public ConflictEvent ToConflictEvent(AirDefenseSite site)
        {
            return new ConflictEvent
            {
                Id = site.Id,
                Title = $"🛡 {site.Name} ({site.SystemType})",
                Summary = $"{site.Description}\nRange: {site.MaxRangeKm}km",
                Source = "OSINT / Air Defense DB",
                DataSource = DataSource.OSINT,
                Category = EventCategory.Military,
                Severity = site.ThreatLevel,
                Latitude = site.Latitude,
                Longitude = site.Longitude,
                Location = $"{site.Country} ({site.Operator})",
                Timestamp = DateTime.UtcNow,
                Tags = new List<string> { "AIR-DEFENSE", site.SystemType.ToUpperInvariant(), site.Country.ToUpperInvariant() }
            };
        }

        /// <summary>Builds data for the map JavaScript — missile sites with range circles.</summary>
        public string BuildMissileMapData()
        {
            var entries = _missileSites.Select(s =>
            {
                var color = s.ThreatLevel switch
                {
                    SeverityLevel.Critical => "#EE3333",
                    SeverityLevel.High => "#FF8833",
                    SeverityLevel.Medium => "#DDCC33",
                    _ => "#6A6A80"
                };
                var icon = s.Type switch
                {
                    MissileSiteType.BallisticMissile => "🚀",
                    MissileSiteType.AntiShip => "🎯",
                    MissileSiteType.CruiseMissile => "💨",
                    MissileSiteType.TestRange => "🔬",
                    _ => "🚀"
                };
                var typeName = s.Type switch
                {
                    MissileSiteType.BallisticMissile => "BALLISTIC MISSILE",
                    MissileSiteType.AntiShip => "ANTI-SHIP",
                    MissileSiteType.CruiseMissile => "CRUISE MISSILE",
                    MissileSiteType.TestRange => "TEST RANGE",
                    _ => "UNKNOWN"
                };
                var missiles = string.Join(", ", s.MissileTypes).Replace("'", "\\'");
                var desc = (s.Description ?? "").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ");
                var name = (s.Name ?? "").Replace("'", "\\'");
                var country = (s.Country ?? "").Replace("'", "\\'");
                var op = (s.Operator ?? "").Replace("'", "\\'");

                return $"{{lat:{s.Latitude},lon:{s.Longitude},name:'{name}',country:'{country}'," +
                       $"operator:'{op}',type:'{typeName}',icon:'{icon}',color:'{color}'," +
                       $"range:{s.MaxRangeKm},missiles:'{missiles}',desc:'{desc}'," +
                       $"underground:{(s.IsUnderground ? "true" : "false")},sev:{(int)s.ThreatLevel}}}";
            });
            return string.Join(",", entries);
        }

        /// <summary>Builds data for the map JavaScript — air defense systems with coverage circles.</summary>
        public string BuildDefenseMapData()
        {
            var entries = _airDefenseSites.Select(s =>
            {
                var color = s.DefenseType switch
                {
                    AirDefenseType.ABM => "#AA55FF",
                    AirDefenseType.LongRange => "#3388FF",
                    AirDefenseType.MediumRange => "#33CCCC",
                    AirDefenseType.ShortRange => "#33CC33",
                    _ => "#6A6A80"
                };
                var desc = (s.Description ?? "").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ");
                var name = (s.Name ?? "").Replace("'", "\\'");
                var country = (s.Country ?? "").Replace("'", "\\'");
                var system = (s.SystemType ?? "").Replace("'", "\\'");
                var defType = s.DefenseType.ToString().ToUpperInvariant();

                return $"{{lat:{s.Latitude},lon:{s.Longitude},name:'{name}',country:'{country}'," +
                       $"system:'{system}',type:'{defType}',color:'{color}'," +
                       $"range:{s.MaxRangeKm},desc:'{desc}',sev:{(int)s.ThreatLevel}}}";
            });
            return string.Join(",", entries);
        }
    }
}
