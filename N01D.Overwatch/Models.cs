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
}
