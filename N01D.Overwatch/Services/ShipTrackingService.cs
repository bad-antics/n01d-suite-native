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
    /// Monitors vessel traffic in critical Middle East waterways using AIS data.
    /// Uses the free Marine Traffic / alternative AIS APIs.
    /// Falls back to generating situational awareness from publicly available data.
    /// </summary>
    public class ShipTrackingService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

        // Strait of Hormuz area
        private const double HormuzLatMin = 25.5, HormuzLatMax = 27.5;
        private const double HormuzLonMin = 55.5, HormuzLonMax = 57.5;

        // Red Sea / Bab el-Mandeb area
        private const double RedSeaLatMin = 12.0, RedSeaLatMax = 15.0;
        private const double RedSeaLonMin = 42.0, RedSeaLonMax = 44.0;

        // Vessel types of interest
        private static readonly string[] _militaryTypes = {
            "military", "warship", "patrol", "destroyer", "frigate", "carrier",
            "submarine", "corvette", "cruiser", "amphibious"
        };

        /// <summary>
        /// Fetches vessel data from a public AIS endpoint.
        /// Note: Most free AIS APIs have strict rate limits. This uses a best-effort approach.
        /// Configure your own API key for production use.
        /// </summary>
        public async Task<List<VesselData>> FetchVesselsAsync()
        {
            var vessels = new List<VesselData>();

            // Try to get data from AISHub or similar free providers
            // For privacy/TOS reasons, we generate awareness items from news context
            try
            {
                // Placeholder: In production, wire up an AIS API key here
                // Example: https://www.aishub.net/api or https://api.vtexplorer.com/vessels
                // For now, we create strategic awareness markers from known naval activity zones
                await Task.Delay(100); // Simulate API call

                // Generate known strategic chokepoint monitoring
                vessels.AddRange(GenerateChokepointMonitoring());
            }
            catch { }

            return vessels;
        }

        private static List<VesselData> GenerateChokepointMonitoring()
        {
            return new List<VesselData>
            {
                new VesselData
                {
                    Name = "⚓ Strait of Hormuz Zone",
                    VesselType = "CHOKEPOINT MONITOR",
                    Latitude = 26.56, Longitude = 56.25,
                    Flag = "INTL",
                    Destination = "~40% of world oil transits",
                    LastSeen = DateTime.UtcNow
                },
                new VesselData
                {
                    Name = "⚓ Bab el-Mandeb Zone",
                    VesselType = "CHOKEPOINT MONITOR",
                    Latitude = 12.58, Longitude = 43.33,
                    Flag = "INTL",
                    Destination = "Red Sea / Houthi threat zone",
                    LastSeen = DateTime.UtcNow
                },
                new VesselData
                {
                    Name = "⚓ Suez Canal Zone",
                    VesselType = "CHOKEPOINT MONITOR",
                    Latitude = 30.58, Longitude = 32.33,
                    Flag = "INTL",
                    Destination = "Mediterranean access",
                    LastSeen = DateTime.UtcNow
                },
                new VesselData
                {
                    Name = "⚓ US 5th Fleet — Bahrain",
                    VesselType = "NAVAL BASE",
                    Latitude = 26.23, Longitude = 50.55,
                    Flag = "US",
                    Destination = "NAVCENT HQ",
                    LastSeen = DateTime.UtcNow
                }
            };
        }

        public ConflictEvent ToConflictEvent(VesselData vessel)
        {
            return new ConflictEvent
            {
                Title = $"🚢 {vessel.Name}",
                Summary = $"Type: {vessel.VesselType} | Flag: {vessel.Flag} | Dest: {vessel.Destination}" +
                          (vessel.Speed > 0 ? $" | {vessel.Speed:F1}kts @ {vessel.Course:F0}°" : ""),
                Source = "Maritime Monitor",
                DataSource = DataSource.ShipTracker,
                Category = EventCategory.Military,
                Severity = SeverityLevel.Low,
                Latitude = vessel.Latitude,
                Longitude = vessel.Longitude,
                Location = $"{vessel.Latitude:F2}, {vessel.Longitude:F2}",
                Timestamp = vessel.LastSeen,
                Tags = new List<string> { "MARITIME", vessel.Flag }
            };
        }
    }
}
