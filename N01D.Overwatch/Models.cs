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

    // ══════════════════════════════════════════
    //  EQUIPMENT / ORDER OF BATTLE MODELS
    // ══════════════════════════════════════════

    public enum EquipmentDomain
    {
        AirForce,
        Navy,
        GroundForces,
        AirDefense,
        SpecialForces,
        Drones,
        Missiles,
        CyberElectronicWarfare
    }

    public enum EquipmentStatus
    {
        Operational,
        PartiallyOperational,
        InMaintenance,
        Reserve,
        Retired,
        Unknown
    }

    public enum EquipmentRole
    {
        AirSuperiority,
        MultiRole,
        GroundAttack,
        Bomber,
        ISR,
        AWACS,
        Tanker,
        Transport,
        CSAR,
        EW,
        MaritimePatrol,
        AttackHelicopter,
        UtilityHelicopter,
        UCAV,
        Submarine,
        Destroyer,
        Frigate,
        Corvette,
        FastAttackCraft,
        AmphibiousAssault,
        Carrier,
        MBT,
        IFV,
        APC,
        SelfPropelledArtillery,
        MLRS,
        SpecOps,
        AntiShipMissile,
        BallisticMissile,
        CruiseMissile,
        LoiteringMunition,
        TacticalISR,
        MALE,
        HALE,
        SAM,
        SHORAD,
        CIWS,
        Minesweeper,
        OilTanker,
        SupplyShip,
        PatrolBoat
    }

    public class MilitaryEquipment
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Designation { get; set; } = "";      // NATO/common designation
        public string Country { get; set; } = "";
        public string Operator { get; set; } = "";          // Military branch
        public EquipmentDomain Domain { get; set; }
        public EquipmentRole Role { get; set; }
        public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;
        public int Quantity { get; set; }
        public int QuantityActive { get; set; }
        public string Manufacturer { get; set; } = "";
        public string Origin { get; set; } = "";            // Country of origin
        public int YearIntroduced { get; set; }
        public string Specs { get; set; } = "";             // Key specifications
        public string Description { get; set; } = "";
        public double? BaseLat { get; set; }                // Primary deployment location
        public double? BaseLon { get; set; }
        public string BaseLocation { get; set; } = "";
        public SeverityLevel ThreatRating { get; set; }
        public string ImageIcon { get; set; } = "";         // Emoji icon
    }

    public class ForceComposition
    {
        public string Country { get; set; } = "";
        public string FlagEmoji { get; set; } = "";
        public int TotalPersonnel { get; set; }
        public int ActivePersonnel { get; set; }
        public int ReservePersonnel { get; set; }
        public int ParamilitaryPersonnel { get; set; }
        public double DefenseBudgetBillions { get; set; }
        public int GlobalFirepowerRank { get; set; }
        public string Notes { get; set; } = "";
        public List<MilitaryEquipment> Equipment { get; set; } = new();
    }

    // ══════════════════════════════════════════
    //  GROUND FLOCK TRACKING MODELS
    // ══════════════════════════════════════════

    public enum GroundFlockType
    {
        ForceCluster,
        MilitiaCluster,
        BorderGuard,
        FrontLine,
        ForwardBase,
        LogisticsHub,
        AdvisoryMission,
        Convoy,
        ProtestCluster,
        IDPCamp
    }

    public enum ConflictIntensity
    {
        LowIntensity,
        MediumIntensity,
        HighIntensity
    }

    public enum CheckpointType
    {
        FOB,
        AirBase,
        BorderCrossing,
        MilitaryCheckpoint,
        HumanitarianCorridor
    }

    public class GroundFlock
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public string Force { get; set; } = "";
        public string ForceFlag { get; set; } = "";
        public GroundFlockType FlockType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusKm { get; set; }
        public string EstimatedStrength { get; set; } = "";
        public string Equipment { get; set; } = "";
        public SeverityLevel ThreatLevel { get; set; }
        public string Description { get; set; } = "";
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; } = true;

        // Display helpers
        public string ThreatDisplay => ThreatLevel switch
        {
            SeverityLevel.Critical => "🔴 CRITICAL",
            SeverityLevel.High => "🟠 HIGH",
            SeverityLevel.Medium => "🟡 MEDIUM",
            _ => "🟢 LOW"
        };

        public string TypeDisplay => FlockType switch
        {
            GroundFlockType.ForceCluster => "🪖 Force Cluster",
            GroundFlockType.MilitiaCluster => "⚔ Militia",
            GroundFlockType.BorderGuard => "🛡 Border Guard",
            GroundFlockType.FrontLine => "🎯 Front Line",
            GroundFlockType.ForwardBase => "🏴 Forward Base",
            GroundFlockType.LogisticsHub => "📦 Logistics Hub",
            GroundFlockType.AdvisoryMission => "🎖 Advisory",
            GroundFlockType.Convoy => "🚛 Convoy",
            GroundFlockType.ProtestCluster => "📢 Protest",
            GroundFlockType.IDPCamp => "🏕 IDP Camp",
            _ => "👥 Unknown"
        };

        public string AgeDisplay
        {
            get
            {
                var diff = DateTime.UtcNow - LastUpdated;
                if (diff.TotalHours < 1) return "< 1h ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return $"{(int)diff.TotalDays}d ago";
            }
        }
    }

    public class GroundConflictZone
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusKm { get; set; }
        public ConflictIntensity Intensity { get; set; }
        public string Description { get; set; } = "";
        public string Belligerents { get; set; } = "";
        public DateTime LastUpdated { get; set; }

        public string IntensityDisplay => Intensity switch
        {
            ConflictIntensity.HighIntensity => "🔴 HIGH",
            ConflictIntensity.MediumIntensity => "🟠 MEDIUM",
            _ => "🟡 LOW"
        };
    }

    public class ThermalHotspot
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Confidence { get; set; }
        public double BrightnessK { get; set; }
        public string Source { get; set; } = "";
        public string SatelliteName { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime DetectedUtc { get; set; }

        public string ConfidenceDisplay => Confidence >= 80 ? "🔴 HIGH" :
                                           (Confidence >= 60 ? "🟠 MEDIUM" : "🟡 LOW");
    }

    public class GroundCheckpoint
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Controller { get; set; } = "";
        public CheckpointType Type { get; set; }
        public string Description { get; set; } = "";

        public string TypeDisplay => Type switch
        {
            CheckpointType.FOB => "🏴 FOB",
            CheckpointType.AirBase => "🛫 Air Base",
            CheckpointType.BorderCrossing => "🚧 Border Crossing",
            CheckpointType.MilitaryCheckpoint => "🛑 Checkpoint",
            CheckpointType.HumanitarianCorridor => "🏥 Humanitarian",
            _ => "📍 Other"
        };
    }

    public class DisplacementCorridor
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double StartLat { get; set; }
        public double StartLon { get; set; }
        public double EndLat { get; set; }
        public double EndLon { get; set; }
        public int EstimatedPersons { get; set; }
        public string Status { get; set; } = "";
        public string Description { get; set; } = "";
    }

    // ══════════════════════════════════════════
    //  EQUIPMENT INTEL FEED
    // ══════════════════════════════════════════

    public class EquipmentIntelItem
    {
        public string Title { get; set; } = "";
        public string Source { get; set; } = "";
        public string Url { get; set; } = "";
        public string Summary { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsNew { get; set; }

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - Timestamp;
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return $"{(int)diff.TotalDays}d ago";
            }
        }

        public string NewDisplay => IsNew ? "🆕 NEW" : "";
    }
}
