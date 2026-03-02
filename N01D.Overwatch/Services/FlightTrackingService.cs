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

        // Known military callsign prefixes
        private static readonly string[] _milPrefixes = {
            "RCH", "DUKE", "EVAC", "REACH", "BOLT", "VIPER", "HAWK", "FURY",
            "FORTE", "DRACO", "IRON", "SNTRY", "AWACS", "MAGIC", "DRAGN",
            "NCHO", "TOPCT", "VADER", "CONVOY", "ATLAS", "GIANT", "HOMER",
            "IAF", "IRIAF", "IRGC", "IIAF", // Iranian
            "RRR", "RAF", "BAF", // British/Belgian
            "GAF", "CNV", // German/French Navy
            "CAMEL", "UAE", "QAF", "RSAF", "SAF", // Gulf states
        };

        // Known military aircraft hex ranges (partial — US military)
        private static readonly (long Start, long End)[] _milHexRanges = {
            (0xADF7C7, 0xAFFFFF), // USAF
            (0xA00001, 0xADF7C6), // US Army partial
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
            // Flag aircraft from Iranian military origins
            var milCountries = new[] { "Iran, Islamic Republic of" };
            return milCountries.Any(c => country.Contains(c, StringComparison.OrdinalIgnoreCase));
        }

        private static string GuessAircraftType(string callsign)
        {
            var cs = callsign.ToUpperInvariant();
            if (cs.StartsWith("FORTE")) return "RQ-4 Global Hawk";
            if (cs.StartsWith("RCH") || cs.StartsWith("REACH")) return "C-17 Globemaster";
            if (cs.StartsWith("EVAC")) return "C-130 Hercules";
            if (cs.StartsWith("SNTRY") || cs.StartsWith("AWACS")) return "E-3 Sentry AWACS";
            if (cs.StartsWith("HOMER")) return "E-6B Mercury";
            if (cs.StartsWith("GIANT")) return "C-5 Galaxy";
            if (cs.StartsWith("ATLAS")) return "C-17 Globemaster";
            return "Unknown Military";
        }

        public ConflictEvent? ToConflictEvent(FlightData flight)
        {
            if (!flight.IsMilitary) return null;
            return new ConflictEvent
            {
                Title = $"✈ Military Aircraft: {flight.Callsign} ({flight.AircraftType})",
                Summary = $"{flight.Country} | Alt: {flight.Altitude:N0}m | Speed: {flight.Speed:N0}kts | Heading: {flight.Heading:N0}° | ICAO: {flight.Registration}",
                Source = "OpenSky Network",
                DataSource = DataSource.FlightTracker,
                Category = EventCategory.Military,
                Severity = SeverityLevel.Medium,
                Latitude = flight.Latitude,
                Longitude = flight.Longitude,
                Location = $"{flight.Latitude:F2}, {flight.Longitude:F2}",
                Timestamp = flight.LastSeen,
                Tags = new List<string> { "FLIGHT", flight.Country.ToUpperInvariant().Split(',')[0] }
            };
        }
    }
}
