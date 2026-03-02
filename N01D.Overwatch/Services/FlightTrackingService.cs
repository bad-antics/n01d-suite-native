using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using N01D.Overwatch.Models;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Fetches military and notable aircraft positions from the OpenSky Network API (free, no key required).
    /// Monitors a bounding box covering the Middle East region.
    /// </summary>
    public class FlightTrackingService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

        // Middle East bounding box: lat 12-42, lon 24-64
        private const double LatMin = 12.0, LatMax = 42.0, LonMin = 24.0, LonMax = 64.0;
        private const string OpenSkyUrl = "https://opensky-network.org/api/states/all";

        // Known military callsign prefixes — comprehensive list
        private static readonly string[] _milPrefixes = {
            // ── US Military ──
            "RCH", "REACH",       // AMC strategic airlift
            "DUKE",               // Special operations
            "EVAC",               // Aeromedical evacuation
            "BOLT",               // Fighter deployments
            "VIPER",              // F-16 ops
            "HAWK",               // Various fighter
            "FURY",               // Strike missions
            "FORTE",              // RQ-4 Global Hawk ISR
            "DRACO",              // ISR/Recon
            "IRON",               // Iron Hand SEAD
            "SNTRY", "AWACS",     // E-3 Sentry AWACS
            "MAGIC",              // E-3 AWACS callsign
            "DRAGN",              // U-2 Dragon Lady
            "NCHO", "TOPCT",      // Special ops
            "VADER",              // F-22 Raptor
            "CONVOY",             // Ground logistics air
            "ATLAS", "REACH",     // C-17 Globemaster III
            "GIANT",              // C-5 Galaxy
            "HOMER",              // E-6B Mercury (nuclear C2)
            "GORDO",              // KC-135 Stratotanker
            "SHELL", "TEAL",      // Aerial refueling
            "ARCT",               // KC-46A Pegasus
            "STEEL",              // KC-10 Extender
            "NEON",               // EC-130H Compass Call EW
            "JSTAR",              // E-8 JSTARS ground surveil
            "RAIDR", "DEATH",     // B-2 Spirit
            "BONE",               // B-1B Lancer
            "DOOM", "GRIM",       // MQ-9 Reaper
            "REAPER",             // MQ-9 Reaper
            "KING",               // HC-130J rescue
            "PEDRO", "JOLLY",     // HH-60 Pave Hawk CSAR
            "SLIP", "BLADE",      // CV-22 Osprey
            "NIGHT",              // AC-130 gunship
            "SPOOK",              // AC-130J Ghostrider
            "HAVE",               // Classified programs
            "COBRA",              // AH-1 Cobra / ops
            "TIGER",              // Fighter mixed
            "RAPTOR",             // F-22
            "STRIKE",             // Strike package
            "WEASEL",             // SEAD/DEAD
            "THUD",               // Wild Weasel heritage
            "CYLON",              // RQ-170 Sentinel
            "BAT",                // Stealth ops
            "SAM",                // SAR missions
            "CANOE",              // Navy P-8 Poseidon
            "TRIDENT",            // Navy operations
            "RED",                // Red Flag exercises
            "BULL",               // MV-22 / USMC
            "RAIDER",             // USMC ops
            "VMFA",               // USMC fighter attack
            "VMGR",               // USMC refueler/transport
            "HMH", "HMM", "HML", // USMC helo squadrons
            "MARLN",              // P-8A Poseidon maritime patrol
            "LMTR",               // Navy P-8 / EP-3
            "MPRA",               // Maritime patrol recon

            // ── US Special / Strategic ──
            "THORN",              // Special operations
            "KNIFE",              // SOF direct action
            "WRATH",              // Strike ops
            "ORDER", "ORDEN",     // STRATCOM
            "SKY",                // E-4B Nightwatch (Doomsday)
            "NAVY",               // USN misc
            "NAVY",               // USN callsign block

            // ── Iranian Military ──
            "IAF", "IRIAF", "IRGC", "IIAF",
            "IRI",                // Islamic Republic of Iran
            "EP", "EPA",          // Iran Air Force prefix
            "IRG",                // IRGC Air
            "QFZ",                // Quds Force

            // ── Israeli Military ──
            "IDAL",               // Israeli AF
            "ISR",                // Israeli military

            // ── UK Military ──
            "RRR", "RAF",         // Royal Air Force
            "ASCOT",              // RAF transport
            "TARTAN",             // RAF Typhoon
            "LOSSIE",             // RAF Lossiemouth
            "TYPHON",             // RAF Typhoon

            // ── French Military ──
            "FAF", "CTM",         // French Air Force
            "RAFALE",             // Rafale ops
            "COTAM",              // French mil transport

            // ── Other NATO / Coalition ──
            "GAF",                // German Air Force
            "BAF",                // Belgian AF
            "CNV",                // French Navy
            "ITAL",               // Italian AF
            "MMF",                // NATO Multinational MRTT
            "NATO",               // NATO AWACS

            // ── Gulf States──
            "CAMEL",              // Gulf state ops
            "UAE", "UAEAF",       // UAE Air Force
            "QAF",                // Qatar Air Force
            "RSAF",               // Royal Saudi AF
            "SAF",                // Saudi AF
            "BAAH",               // Bahrain AF
            "KAF",                // Kuwaiti AF
            "OMAN",               // Royal Air Force of Oman
            "JOAF",               // Jordanian AF

            // ── Other Regional ──
            "TRK", "THK",         // Turkish AF
            "EGY",                // Egyptian AF
            "PAF",                // Pakistan AF
            "INDIAN",             // Indian AF
        };

        // Known military aircraft ICAO24 hex ranges
        private static readonly (long Start, long End)[] _milHexRanges = {
            // US military
            (0xADF7C7, 0xAFFFFF), // USAF
            (0xA00001, 0xADF7C6), // US Army / other DoD
            // UK military
            (0x43C000, 0x43CFFF), // RAF
            // France military  
            (0x3B0000, 0x3BFFFF), // French military
            // Israel military
            (0x738000, 0x738FFF), // IAF
            // Iran military
            (0x730000, 0x730FFF), // IRIAF partial range
            // Turkey military
            (0x4B8000, 0x4B8FFF), // THK
            // Saudi military
            (0x710000, 0x710FFF), // RSAF partial
            // UAE military
            (0x896000, 0x896FFF), // UAEAF partial
        };

        public async Task<List<FlightData>> FetchMilitaryFlightsAsync()
        {
            var flights = new List<FlightData>();
            try
            {
                var url = $"{OpenSkyUrl}?lamin={LatMin}&lomin={LonMin}&lamax={LatMax}&lomax={LonMax}";
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("states", out var states) || states.ValueKind != JsonValueKind.Array)
                    return flights;

                foreach (var s in states.EnumerateArray())
                {
                    if (s.GetArrayLength() < 17) continue;

                    var icao24 = s[0].GetString() ?? "";
                    var callsign = (s[1].GetString() ?? "").Trim();
                    var country = s[2].GetString() ?? "";
                    var lon = s[5].ValueKind == JsonValueKind.Number ? s[5].GetDouble() : (double?)null;
                    var lat = s[6].ValueKind == JsonValueKind.Number ? s[6].GetDouble() : (double?)null;
                    var alt = s[7].ValueKind == JsonValueKind.Number ? s[7].GetDouble() : 0.0;
                    var speed = s[9].ValueKind == JsonValueKind.Number ? s[9].GetDouble() : 0.0;
                    var heading = s[10].ValueKind == JsonValueKind.Number ? s[10].GetDouble() : 0.0;

                    if (lat == null || lon == null) continue;

                    bool isMil = IsMilitaryCallsign(callsign) || IsMilitaryHex(icao24) ||
                                 IsMilitaryCountryOfInterest(country);

                    if (!isMil) continue;

                    flights.Add(new FlightData
                    {
                        Callsign = callsign,
                        Registration = icao24,
                        AircraftType = GuessAircraftType(callsign),
                        Country = country,
                        Latitude = lat.Value,
                        Longitude = lon.Value,
                        Altitude = alt,
                        Speed = speed * 1.944, // m/s → knots
                        Heading = heading,
                        IsMilitary = true,
                        LastSeen = DateTime.UtcNow
                    });
                }
            }
            catch { /* API may rate-limit — fail silently */ }
            return flights;
        }

        private static bool IsMilitaryCallsign(string cs)
        {
            if (string.IsNullOrWhiteSpace(cs)) return false;
            var upper = cs.ToUpperInvariant();
            return _milPrefixes.Any(p => upper.StartsWith(p));
        }

        private static bool IsMilitaryHex(string icao24)
        {
            if (string.IsNullOrWhiteSpace(icao24)) return false;
            if (long.TryParse(icao24, System.Globalization.NumberStyles.HexNumber, null, out var hex))
                return _milHexRanges.Any(r => hex >= r.Start && hex <= r.End);
            return false;
        }

        private static bool IsMilitaryCountryOfInterest(string country)
        {
            var milCountries = new[] {
                "Iran", "Israel", "United States",
                "United Kingdom", "France", "Turkey",
                "Saudi Arabia", "United Arab Emirates",
                "Russian Federation"
            };
            return milCountries.Any(c => country.Contains(c, StringComparison.OrdinalIgnoreCase));
        }

        private static string GuessAircraftType(string callsign)
        {
            var cs = callsign.ToUpperInvariant();
            // ISR / Recon
            if (cs.StartsWith("FORTE")) return "RQ-4 Global Hawk (ISR)";
            if (cs.StartsWith("DRAGN")) return "U-2 Dragon Lady (ISR)";
            if (cs.StartsWith("CYLON")) return "RQ-170 Sentinel (Stealth ISR)";
            if (cs.StartsWith("DOOM") || cs.StartsWith("GRIM") || cs.StartsWith("REAPER")) return "MQ-9 Reaper (Armed ISR)";
            if (cs.StartsWith("CANOE") || cs.StartsWith("MARLN") || cs.StartsWith("MPRA")) return "P-8A Poseidon (Maritime Patrol)";
            if (cs.StartsWith("LMTR")) return "EP-3E Aries II (SIGINT)";
            // AWACS / C2 / Strategic
            if (cs.StartsWith("SNTRY") || cs.StartsWith("AWACS") || cs.StartsWith("MAGIC")) return "E-3 Sentry (AWACS)";
            if (cs.StartsWith("JSTAR")) return "E-8C JSTARS (Ground Surveillance)";
            if (cs.StartsWith("HOMER")) return "E-6B Mercury (Nuclear C2)";
            if (cs.StartsWith("SKY")) return "E-4B Nightwatch (Doomsday Plane)";
            if (cs.StartsWith("NEON")) return "EC-130H Compass Call (EW)";
            // Bombers
            if (cs.StartsWith("RAIDR") || cs.StartsWith("DEATH") || cs.StartsWith("BAT")) return "B-2 Spirit (Stealth Bomber)";
            if (cs.StartsWith("BONE")) return "B-1B Lancer (Bomber)";
            // Fighters
            if (cs.StartsWith("VADER") || cs.StartsWith("RAPTOR")) return "F-22 Raptor";
            if (cs.StartsWith("BOLT") || cs.StartsWith("VIPER")) return "F-16 Viper";
            if (cs.StartsWith("STRIKE")) return "F-15E Strike Eagle";
            if (cs.StartsWith("HAWK")) return "Fighter (Type Unknown)";
            if (cs.StartsWith("WEASEL") || cs.StartsWith("THUD")) return "F-16CJ (SEAD/DEAD)";
            if (cs.StartsWith("RAFALE")) return "Rafale (French AF)";
            if (cs.StartsWith("TYPHON") || cs.StartsWith("TARTAN")) return "Eurofighter Typhoon";
            // Gunship
            if (cs.StartsWith("NIGHT") || cs.StartsWith("SPOOK")) return "AC-130J Ghostrider (Gunship)";
            // Transport / Airlift
            if (cs.StartsWith("RCH") || cs.StartsWith("REACH") || cs.StartsWith("ATLAS")) return "C-17 Globemaster III";
            if (cs.StartsWith("GIANT")) return "C-5M Super Galaxy";
            if (cs.StartsWith("EVAC")) return "C-130J Hercules";
            if (cs.StartsWith("ASCOT") || cs.StartsWith("COTAM")) return "Military Transport";
            // Tankers
            if (cs.StartsWith("GORDO") || cs.StartsWith("SHEL") || cs.StartsWith("TEAL")) return "KC-135 Stratotanker";
            if (cs.StartsWith("STEEL")) return "KC-10 Extender";
            if (cs.StartsWith("ARCT")) return "KC-46A Pegasus";
            // Rotary / SOF
            if (cs.StartsWith("PEDRO") || cs.StartsWith("JOLLY")) return "HH-60 Pave Hawk (CSAR)";
            if (cs.StartsWith("KING")) return "HC-130J (Combat Rescue)";
            if (cs.StartsWith("SLIP") || cs.StartsWith("BLADE")) return "CV-22 Osprey (SOF)";
            if (cs.StartsWith("DUKE") || cs.StartsWith("THORN") || cs.StartsWith("KNIFE")) return "SOF Aircraft";
            if (cs.StartsWith("COBRA")) return "Attack Helicopter";
            // Misc
            if (cs.StartsWith("BULL") || cs.StartsWith("RAIDER")) return "USMC Aviation";
            if (cs.StartsWith("TRIDENT") || cs.StartsWith("NAVY") || cs.StartsWith("NAVY")) return "USN Aviation";
            if (cs.StartsWith("NATO")) return "NATO AWACS (E-3A)";
            if (cs.StartsWith("MMF")) return "A330 MRTT (NATO Tanker)";
            return "Military Aircraft";
        }

        /// <summary>
        /// Classifies how significant a tracked aircraft is — high-value assets get elevated severity.
        /// </summary>
        public static SeverityLevel ClassifyFlightSeverity(string callsign, string aircraftType)
        {
            var cs = callsign.ToUpperInvariant();
            // Critical — strategic / nuclear C2 / stealth
            if (cs.StartsWith("HOMER") || cs.StartsWith("SKY") ||
                cs.StartsWith("RAIDR") || cs.StartsWith("DEATH") ||
                cs.StartsWith("ORDER"))
                return SeverityLevel.Critical;
            // High — bombers, gunships, ISR drones, SEAD
            if (cs.StartsWith("BONE") || cs.StartsWith("FORTE") || cs.StartsWith("DRAGN") ||
                cs.StartsWith("CYLON") || cs.StartsWith("NIGHT") || cs.StartsWith("SPOOK") ||
                cs.StartsWith("WEASEL") || cs.StartsWith("DOOM") || cs.StartsWith("GRIM") ||
                cs.StartsWith("REAPER") || cs.StartsWith("JSTAR") || cs.StartsWith("NEON") ||
                cs.StartsWith("VADER") || cs.StartsWith("RAPTOR") ||
                aircraftType.Contains("Stealth") || aircraftType.Contains("Bomber") ||
                aircraftType.Contains("Gunship") || aircraftType.Contains("SEAD"))
                return SeverityLevel.High;
            // Medium — fighters, patrol, tankers, AWACS
            if (cs.StartsWith("SNTRY") || cs.StartsWith("AWACS") || cs.StartsWith("MAGIC") ||
                cs.StartsWith("CANOE") || cs.StartsWith("MARLN") ||
                cs.StartsWith("STRIKE") || cs.StartsWith("BOLT") || cs.StartsWith("VIPER") ||
                cs.StartsWith("GORDO") || cs.StartsWith("STEEL") || cs.StartsWith("ARCT"))
                return SeverityLevel.Medium;
            return SeverityLevel.Low;
        }

        /// <summary>Convenience overload accepting a FlightData object.</summary>
        public static SeverityLevel ClassifyFlightSeverity(FlightData f) =>
            ClassifyFlightSeverity(f.Callsign, f.AircraftType);

        public ConflictEvent? ToConflictEvent(FlightData flight)
        {
            if (!flight.IsMilitary) return null;
            var severity = ClassifyFlightSeverity(flight.Callsign, flight.AircraftType);
            var icon = severity switch
            {
                SeverityLevel.Critical => "🔴✈",
                SeverityLevel.High => "🟠✈",
                SeverityLevel.Medium => "🟡✈",
                _ => "✈"
            };
            return new ConflictEvent
            {
                Title = $"{icon} {flight.Callsign} — {flight.AircraftType}",
                Summary = $"{flight.Country} | Alt: {flight.Altitude:N0}m | Speed: {flight.Speed:N0}kts | Heading: {flight.Heading:N0}° | ICAO: {flight.Registration}",
                Source = "OpenSky Network",
                DataSource = DataSource.FlightTracker,
                Category = EventCategory.Military,
                Severity = severity,
                Latitude = flight.Latitude,
                Longitude = flight.Longitude,
                Location = $"{flight.Latitude:F2}, {flight.Longitude:F2}",
                Timestamp = flight.LastSeen,
                Tags = new List<string> { "FLIGHT", flight.Country.ToUpperInvariant().Split(',')[0], flight.AircraftType.Split(' ')[0].ToUpperInvariant() }
            };
        }
    }
}
