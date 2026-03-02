using System;
using System.Collections.Generic;

namespace N01D.Overwatch.Models
{
    public enum EventCategory
    {
        Military,
        Diplomatic,
        Economic,
        Humanitarian,
        Cyber,
        Nuclear,
        Intelligence
    }

    public enum SeverityLevel
    {
        Low,        // Routine / background
        Medium,     // Notable development
        High,       // Significant escalation
        Critical    // Major incident / imminent threat
    }

    public enum DataSource
    {
        RSS,
        FlightTracker,
        ShipTracker,
        OilPrice,
        OSINT,
        Social,
        Manual
    }

    public class ConflictEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Source { get; set; } = "";
        public string SourceUrl { get; set; } = "";
        public DataSource DataSource { get; set; }
        public EventCategory Category { get; set; }
        public SeverityLevel Severity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Location { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public bool IsAlert { get; set; }
        public bool IsRead { get; set; }
    }

    public class FlightData
    {
        public string Callsign { get; set; } = "";
        public string AircraftType { get; set; } = "";
        public string Registration { get; set; } = "";
        public string Origin { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }
        public bool IsMilitary { get; set; }
        public string Country { get; set; } = "";
        public DateTime LastSeen { get; set; }
    }

    public class VesselData
    {
        public string Name { get; set; } = "";
        public string MMSI { get; set; } = "";
        public string IMO { get; set; } = "";
        public string VesselType { get; set; } = "";
        public string Flag { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double Course { get; set; }
        public string Destination { get; set; } = "";
        public DateTime LastSeen { get; set; }
    }

    public class OilPriceData
    {
        public string Name { get; set; } = "";   // WTI, Brent
        public double Price { get; set; }
        public double Change { get; set; }
        public double ChangePercent { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AlertRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Name { get; set; } = "";
        public List<string> Keywords { get; set; } = new();
        public SeverityLevel MinSeverity { get; set; } = SeverityLevel.High;
        public List<EventCategory> Categories { get; set; } = new();
        public bool Enabled { get; set; } = true;
        public bool PlaySound { get; set; } = true;
    }

    public class RssFeedConfig
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public EventCategory DefaultCategory { get; set; }
        public bool Enabled { get; set; } = true;
    }

    // ══════════════════════════════════════════
    //  ECLIPSE MODELS
    // ══════════════════════════════════════════

    public enum EclipseType
    {
        SolarTotal,
        SolarAnnular,
        SolarPartial,
        LunarTotal,
        LunarPartial,
        LunarPenumbral
    }

    public class EclipseEvent
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public EclipseType Type { get; set; }
        public DateTime Date { get; set; }
        public DateTime PeakTime { get; set; }
        public int DurationMinutes { get; set; }
        public double MaxMagnitude { get; set; }
        public string Description { get; set; } = "";
        public string MilitarySignificance { get; set; } = "";
        public string VisibilityRegion { get; set; } = "";
        public bool IsVisibleFromME { get; set; }
        public int MECoveragePercent { get; set; }
        public List<(double Lat, double Lon)> PathCoordinates { get; set; } = new();
    }

    // ══════════════════════════════════════════
    //  MISSILE & DEFENSE MODELS
    // ══════════════════════════════════════════

    public enum MissileSiteType
    {
        BallisticMissile,
        CruiseMissile,
        AntiShip,
        TestRange,
        StorageDepot
    }

    public enum AirDefenseType
    {
        ShortRange,     // <40km (Iron Dome, Tor, Pantsir)
        MediumRange,    // 40-200km (Patriot, David's Sling, Buk, Khordad)
        LongRange,      // 200-400km (S-300, S-400, Bavar-373)
        ABM             // Anti-ballistic missile (Arrow, THAAD)
    }

    public class MissileSite
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; } = "";
        public string Operator { get; set; } = "";
        public MissileSiteType Type { get; set; }
        public List<string> MissileTypes { get; set; } = new();
        public int MaxRangeKm { get; set; }
        public string Description { get; set; } = "";
        public SeverityLevel ThreatLevel { get; set; }
        public bool IsUnderground { get; set; }
    }

    public class AirDefenseSite
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; } = "";
        public string Operator { get; set; } = "";
        public string SystemType { get; set; } = "";
        public int MaxRangeKm { get; set; }
        public string Description { get; set; } = "";
        public AirDefenseType DefenseType { get; set; }
        public SeverityLevel ThreatLevel { get; set; }
    }
}
