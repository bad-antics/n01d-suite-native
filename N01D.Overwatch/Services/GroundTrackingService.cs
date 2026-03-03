using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using N01D.Overwatch.Models;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Ground flock tracking service — monitors ground-level activity across
    /// the Middle East AOR. Tracks force deployments, conflict zones, thermal
    /// hotspots (NASA FIRMS), convoy routes, checkpoints, IDP corridors, and
    /// protest/unrest clusters.
    ///
    /// Data sources: NASA FIRMS (free API), known OSINT force positions,
    /// ACLED-style conflict data, UNHCR displacement feeds.
    /// </summary>
    public class GroundTrackingService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

        // ══════════════════════════════════════════════════════
        //  GROUND FORCE FLOCKS — Known deployments & clusters
        // ══════════════════════════════════════════════════════

        private readonly List<GroundFlock> _flocks = new()
        {
            // ──────────────────────────────────────
            //  IRAN — IRGC Ground Force + Artesh
            // ──────────────────────────────────────
            new() { Id = "GF-IR-01", Name = "IRGC-GF Western Border Deployment",
                Country = "Iran", Force = "IRGC Ground Forces", ForceFlag = "🇮🇷",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 34.31, Longitude = 45.98, RadiusKm = 60,
                EstimatedStrength = "15,000+", Equipment = "T-72S MBT, BMP-2, Safir MRAP, Raad artillery",
                ThreatLevel = SeverityLevel.High,
                Description = "IRGC-GF armored & mechanized units deployed along Iraq border. Kermanshah-Ilam axis. " +
                              "Includes 31st Ashura Division. High readiness posture since 2024 tensions.",
                LastUpdated = DateTime.UtcNow.AddDays(-3), IsActive = true },

            new() { Id = "GF-IR-02", Name = "Artesh 92nd Armored Division",
                Country = "Iran", Force = "IRIAA (Artesh)", ForceFlag = "🇮🇷",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 33.49, Longitude = 48.36, RadiusKm = 35,
                EstimatedStrength = "12,000", Equipment = "T-72S, Zulfiqar-3 MBT, M109 SPH, Boragh APC",
                ThreatLevel = SeverityLevel.Medium,
                Description = "Iran's premier armored division based at Khuzestan. Geared for conventional ground warfare. " +
                              "Significant mechanized capability with 300+ MBTs.",
                LastUpdated = DateTime.UtcNow.AddDays(-7), IsActive = true },

            new() { Id = "GF-IR-03", Name = "IRGC Basij Mobilization Zones",
                Country = "Iran", Force = "Basij Militia", ForceFlag = "🇮🇷",
                FlockType = GroundFlockType.MilitiaCluster,
                Latitude = 35.69, Longitude = 51.39, RadiusKm = 120,
                EstimatedStrength = "90,000+ (mobilizable 600K)",
                Equipment = "G3 rifles, RPG-7, technicals, PKM, 60mm mortars",
                ThreatLevel = SeverityLevel.Medium,
                Description = "Basij paramilitary organization with nationwide cells. Tehran cluster is largest. " +
                              "Rapid mobilization capability. Internal security and crowd control focus.",
                LastUpdated = DateTime.UtcNow.AddDays(-14), IsActive = true },

            new() { Id = "GF-IR-04", Name = "IRGC Quds Force — Syria Forward",
                Country = "Iran", Force = "IRGC Quds Force", ForceFlag = "🇮🇷",
                FlockType = GroundFlockType.AdvisoryMission,
                Latitude = 33.51, Longitude = 36.31, RadiusKm = 40,
                EstimatedStrength = "2,000-4,000 advisors",
                Equipment = "LMVs, technicals, Kornet ATGMs, Toophan, comms gear",
                ThreatLevel = SeverityLevel.High,
                Description = "IRGC-QF advisory and logistic network in Syria. Coordinates with SAA, Hezbollah, " +
                              "and Shia militias. Manages arms pipeline from Iran through Iraq into Syria/Lebanon.",
                LastUpdated = DateTime.UtcNow.AddDays(-5), IsActive = true },

            // ──────────────────────────────────────
            //  IRAQ — ISF + PMF
            // ──────────────────────────────────────
            new() { Id = "GF-IQ-01", Name = "Iraqi Counter-Terrorism Service",
                Country = "Iraq", Force = "CTS / Golden Division", ForceFlag = "🇮🇶",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 33.32, Longitude = 44.37, RadiusKm = 25,
                EstimatedStrength = "10,000", Equipment = "M1A1M Abrams, MRAP, Humvee, M4/HK416",
                ThreatLevel = SeverityLevel.Low,
                Description = "Iraq's elite counter-terrorism force (ISOF/Golden Division). US-trained, best equipped ISF unit. " +
                              "Primary Baghdad rapid response element.",
                LastUpdated = DateTime.UtcNow.AddDays(-10), IsActive = true },

            new() { Id = "GF-IQ-02", Name = "PMF/Hashd al-Shaabi — Northern Axis",
                Country = "Iraq", Force = "Popular Mobilization Forces", ForceFlag = "🇮🇶",
                FlockType = GroundFlockType.MilitiaCluster,
                Latitude = 35.47, Longitude = 43.16, RadiusKm = 80,
                EstimatedStrength = "25,000+", Equipment = "T-55, technicals, 122mm Katyusha, ATGMs, SPGs",
                ThreatLevel = SeverityLevel.High,
                Description = "Iran-aligned PMF brigades along Kirkuk-Mosul corridor. Includes Kataib Hezbollah, " +
                              "Asaib Ahl al-Haq, Badr Organization. Significant anti-US posture. Drone-capable.",
                LastUpdated = DateTime.UtcNow.AddDays(-2), IsActive = true },

            new() { Id = "GF-IQ-03", Name = "PMF — Syrian Border Watch",
                Country = "Iraq", Force = "Popular Mobilization Forces", ForceFlag = "🇮🇶",
                FlockType = GroundFlockType.BorderGuard,
                Latitude = 34.42, Longitude = 40.95, RadiusKm = 90,
                EstimatedStrength = "8,000", Equipment = "Technicals, DShK, SPG-9, IEDs",
                ThreatLevel = SeverityLevel.Medium,
                Description = "PMF border control along Iraq-Syria frontier. Controls Al-Qaim crossing and desert corridor. " +
                              "Facilitates Iran-Syria logistics pipeline. Frequent US/Israeli strikes on convoys.",
                LastUpdated = DateTime.UtcNow.AddDays(-4), IsActive = true },

            // ──────────────────────────────────────
            //  SYRIA — SAA + Proxies
            // ──────────────────────────────────────
            new() { Id = "GF-SY-01", Name = "SAA 4th Armored Division",
                Country = "Syria", Force = "Syrian Arab Army", ForceFlag = "🇸🇾",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 33.50, Longitude = 36.29, RadiusKm = 30,
                EstimatedStrength = "20,000", Equipment = "T-72B3, BMP-1/2, Gvozdika 2S1, Shilka ZSU-23-4",
                ThreatLevel = SeverityLevel.Medium,
                Description = "Elite Republican Guard-tier division under Maher al-Assad. Damascus defense ring. " +
                              "Best equipped SAA formation. Anti-rebel, regime protection mandate.",
                LastUpdated = DateTime.UtcNow.AddDays(-8), IsActive = true },

            new() { Id = "GF-SY-02", Name = "SAA — Deir ez-Zor / Euphrates Front",
                Country = "Syria", Force = "Syrian Arab Army + NDF", ForceFlag = "🇸🇾",
                FlockType = GroundFlockType.FrontLine,
                Latitude = 35.33, Longitude = 40.14, RadiusKm = 70,
                EstimatedStrength = "6,000", Equipment = "T-55, technicals, 130mm artillery, ZU-23-2",
                ThreatLevel = SeverityLevel.High,
                Description = "SAA & NDF positions along Euphrates deconfliction line. Faces SDF/US positions across river. " +
                              "ISIS remnant operations in desert zone. Frequent clashes and IED attacks.",
                LastUpdated = DateTime.UtcNow.AddDays(-1), IsActive = true },

            // ──────────────────────────────────────
            //  HEZBOLLAH — Lebanon/Syria
            // ──────────────────────────────────────
            new() { Id = "GF-HZ-01", Name = "Hezbollah — Southern Lebanon Border",
                Country = "Lebanon", Force = "Hezbollah", ForceFlag = "🇱🇧",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 33.27, Longitude = 35.40, RadiusKm = 40,
                EstimatedStrength = "12,000-20,000",
                Equipment = "Kornet ATGM, Almas, Burkan rockets, tunnels, fortified positions",
                ThreatLevel = SeverityLevel.Critical,
                Description = "Hezbollah's Radwan Force and border units. Extensive tunnel network, fortified bunkers, " +
                              "pre-positioned Kornet ATGMs. Primary ground threat to northern Israel. UNIFIL area.",
                LastUpdated = DateTime.UtcNow.AddDays(-1), IsActive = true },

            new() { Id = "GF-HZ-02", Name = "Hezbollah — Bekaa Valley Logistics",
                Country = "Lebanon", Force = "Hezbollah", ForceFlag = "🇱🇧",
                FlockType = GroundFlockType.LogisticsHub,
                Latitude = 33.84, Longitude = 36.09, RadiusKm = 35,
                EstimatedStrength = "5,000+",
                Equipment = "Arms depots, rocket stockpiles, training camps, medical facilities",
                ThreatLevel = SeverityLevel.High,
                Description = "Hezbollah logistics backbone in Bekaa Valley. Arms transfers from Syria, rocket storage, " +
                              "training camps, and C2 nodes. Primary Israeli airstrike target zone.",
                LastUpdated = DateTime.UtcNow.AddDays(-3), IsActive = true },

            // ──────────────────────────────────────
            //  ISRAEL — IDF Ground
            // ──────────────────────────────────────
            new() { Id = "GF-IL-01", Name = "IDF Northern Command — Golan/Lebanon Border",
                Country = "Israel", Force = "IDF Ground Forces", ForceFlag = "🇮🇱",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 33.00, Longitude = 35.75, RadiusKm = 45,
                EstimatedStrength = "30,000+",
                Equipment = "Merkava IV, Namer APC, Trophy APS, Spike ATGM, Iron Dome batteries",
                ThreatLevel = SeverityLevel.Low,
                Description = "IDF 91st Division (Galilee) + reserves. Heavily fortified border with Lebanon. " +
                              "Permanent Merkava deployments, border surveillance fence, Iron Dome coverage.",
                LastUpdated = DateTime.UtcNow.AddDays(-2), IsActive = true },

            new() { Id = "GF-IL-02", Name = "IDF Gaza Division — Southern Command",
                Country = "Israel", Force = "IDF Ground Forces", ForceFlag = "🇮🇱",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 31.35, Longitude = 34.35, RadiusKm = 30,
                EstimatedStrength = "25,000+",
                Equipment = "Merkava IV, D9 bulldozer, Namer APC, drones, engineering units",
                ThreatLevel = SeverityLevel.Low,
                Description = "IDF ground operations Gaza envelope. Active combat operations since Oct 2023. " +
                              "Brigade-level rotations. Urban warfare, tunnel clearing, humanitarian corridor ops.",
                LastUpdated = DateTime.UtcNow.AddDays(-1), IsActive = true },

            // ──────────────────────────────────────
            //  TURKEY — Border Operations
            // ──────────────────────────────────────
            new() { Id = "GF-TR-01", Name = "TAF — Northern Syria / Operation Claw",
                Country = "Turkey", Force = "Turkish Armed Forces", ForceFlag = "🇹🇷",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 36.60, Longitude = 37.10, RadiusKm = 80,
                EstimatedStrength = "15,000+",
                Equipment = "Leopard 2A4, M60T Sabra, T-155 Firtina SPH, Bayraktar TB2, Kirpi MRAP",
                ThreatLevel = SeverityLevel.Medium,
                Description = "Turkish military presence in NE Syria safe zone and Euphrates Shield/Spring areas. " +
                              "Anti-PKK/YPG operations. Observation posts. SNA proxy forces augment.",
                LastUpdated = DateTime.UtcNow.AddDays(-6), IsActive = true },

            new() { Id = "GF-TR-02", Name = "TAF — Northern Iraq / Claw-Lock Bases",
                Country = "Turkey", Force = "Turkish Armed Forces", ForceFlag = "🇹🇷",
                FlockType = GroundFlockType.ForwardBase,
                Latitude = 37.15, Longitude = 43.90, RadiusKm = 50,
                EstimatedStrength = "5,000",
                Equipment = "Commando brigades, Cobra II, Bayraktar, howitzers, FOBs",
                ThreatLevel = SeverityLevel.Medium,
                Description = "Turkish forward operating bases in Iraqi Kurdistan (Operation Claw-Lock/Sword). " +
                              "Anti-PKK mountain warfare. Dozens of FOBs in Metina, Zap, Avasin-Basyan. " +
                              "Baghdad objects to presence; KRG tolerates.",
                LastUpdated = DateTime.UtcNow.AddDays(-5), IsActive = true },

            // ──────────────────────────────────────
            //  SAUDI ARABIA — Ground Forces
            // ──────────────────────────────────────
            new() { Id = "GF-SA-01", Name = "RSLF — Yemen Border Defense",
                Country = "Saudi Arabia", Force = "Royal Saudi Land Forces", ForceFlag = "🇸🇦",
                FlockType = GroundFlockType.BorderGuard,
                Latitude = 17.59, Longitude = 44.22, RadiusKm = 120,
                EstimatedStrength = "20,000+",
                Equipment = "M1A2S Abrams, LAV-25, Caesar SPH, M2 Bradley, Patriot batteries",
                ThreatLevel = SeverityLevel.Medium,
                Description = "RSLF and Saudi Border Guard along Yemen frontier. Defensive posture against Houthi cross-border raids " +
                              "and drone/missile attacks. Enhanced since 2015 intervention.",
                LastUpdated = DateTime.UtcNow.AddDays(-10), IsActive = true },

            // ──────────────────────────────────────
            //  US — Ground Presence
            // ──────────────────────────────────────
            new() { Id = "GF-US-01", Name = "US Forces — NE Syria (CJTF-OIR)",
                Country = "United States", Force = "US Army / CJTF-OIR", ForceFlag = "🇺🇸",
                FlockType = GroundFlockType.AdvisoryMission,
                Latitude = 36.18, Longitude = 40.25, RadiusKm = 60,
                EstimatedStrength = "900",
                Equipment = "MRAP, Stryker, HIMARS, Bradley, M777 howitzer, Counter-UAS",
                ThreatLevel = SeverityLevel.Low,
                Description = "US forces in NE Syria supporting SDF anti-ISIS ops. Bases at Al-Tanf, Conoco, Green Village, " +
                              "Rmeilan. Frequent PMF drone/rocket harassment. Counter-UAS priority.",
                LastUpdated = DateTime.UtcNow.AddDays(-2), IsActive = true },

            new() { Id = "GF-US-02", Name = "US Forces — Iraq (OIR Advisory)",
                Country = "United States", Force = "US Army / CJTF-OIR", ForceFlag = "🇺🇸",
                FlockType = GroundFlockType.AdvisoryMission,
                Latitude = 33.26, Longitude = 44.23, RadiusKm = 40,
                EstimatedStrength = "2,500",
                Equipment = "C-RAM, Patriot, M-SHORAD, MRAP, advisors",
                ThreatLevel = SeverityLevel.Low,
                Description = "US advisory presence in Iraq. Victoria base complex (Baghdad), Ain al-Asad, Erbil. " +
                              "Training and advising ISF. Subject to PMF militia attacks. Drawdown discussions ongoing.",
                LastUpdated = DateTime.UtcNow.AddDays(-3), IsActive = true },

            // ──────────────────────────────────────
            //  HOUTHIS / ANSAR ALLAH — Yemen
            // ──────────────────────────────────────
            new() { Id = "GF-YE-01", Name = "Ansar Allah — Sana'a Defense / N. Yemen",
                Country = "Yemen", Force = "Ansar Allah (Houthis)", ForceFlag = "🇾🇪",
                FlockType = GroundFlockType.MilitiaCluster,
                Latitude = 15.35, Longitude = 44.21, RadiusKm = 100,
                EstimatedStrength = "100,000+",
                Equipment = "T-55, T-34, technicals, Katyusha, Toophan ATGM, ballistic missiles, drones",
                ThreatLevel = SeverityLevel.High,
                Description = "Houthi/Ansar Allah ground forces controlling northern Yemen and Sana'a. Large conventional-irregular " +
                              "hybrid army. Iranian-supplied drones and missiles. Anti-shipping campaign in Red Sea.",
                LastUpdated = DateTime.UtcNow.AddDays(-1), IsActive = true },

            // ──────────────────────────────────────
            //  SDF — Syrian Democratic Forces
            // ──────────────────────────────────────
            new() { Id = "GF-SD-01", Name = "SDF — Rojava / NE Syria",
                Country = "Syria", Force = "Syrian Democratic Forces", ForceFlag = "🏴",
                FlockType = GroundFlockType.ForceCluster,
                Latitude = 36.68, Longitude = 40.50, RadiusKm = 80,
                EstimatedStrength = "70,000",
                Equipment = "Humvees, technicals, M16/AK-47, MRAPs (US-supplied), ATGMs",
                ThreatLevel = SeverityLevel.Low,
                Description = "Kurdish-led SDF controlling NE Syria autonomous region. US-backed anti-ISIS partner force. " +
                              "Faces Turkish/SNA threat from north, SAA from south. AANES governance structure.",
                LastUpdated = DateTime.UtcNow.AddDays(-4), IsActive = true },
        };

        // ══════════════════════════════════════════════════════
        //  ACTIVE CONFLICT ZONES
        // ══════════════════════════════════════════════════════

        private readonly List<GroundConflictZone> _conflictZones = new()
        {
            new() { Id = "CZ-01", Name = "Gaza — Active Urban Combat",
                Latitude = 31.42, Longitude = 34.37, RadiusKm = 20,
                Intensity = ConflictIntensity.HighIntensity,
                Description = "Active IDF ground operations. Urban warfare, tunnel clearing. Massive displacement.",
                Belligerents = "IDF vs Hamas/PIJ", LastUpdated = DateTime.UtcNow.AddHours(-6) },

            new() { Id = "CZ-02", Name = "Southern Lebanon — Border Escalation",
                Latitude = 33.10, Longitude = 35.30, RadiusKm = 25,
                Intensity = ConflictIntensity.MediumIntensity,
                Description = "Daily cross-border exchanges between Hezbollah and IDF. Artillery, ATGM, drone strikes.",
                Belligerents = "IDF vs Hezbollah", LastUpdated = DateTime.UtcNow.AddHours(-3) },

            new() { Id = "CZ-03", Name = "Deir ez-Zor / Euphrates Badiya",
                Latitude = 35.00, Longitude = 40.30, RadiusKm = 60,
                Intensity = ConflictIntensity.LowIntensity,
                Description = "ISIS remnant insurgency. IED attacks on SAA/SDF. Desert ambushes. Coalition strikes.",
                Belligerents = "SAA/SDF vs ISIS remnants", LastUpdated = DateTime.UtcNow.AddDays(-1) },

            new() { Id = "CZ-04", Name = "Marib — Yemen Front Line",
                Latitude = 15.47, Longitude = 45.33, RadiusKm = 45,
                Intensity = ConflictIntensity.MediumIntensity,
                Description = "Houthi-government front line around Marib city. Strategic for oil/gas infrastructure.",
                Belligerents = "Ansar Allah vs Yemen Gov/Coalition", LastUpdated = DateTime.UtcNow.AddDays(-2) },

            new() { Id = "CZ-05", Name = "Idlib / NW Syria — De-escalation Zone",
                Latitude = 35.76, Longitude = 36.63, RadiusKm = 40,
                Intensity = ConflictIntensity.LowIntensity,
                Description = "HTS-controlled NW Syria. Turkish observation posts. Periodic SAA/Russian strikes.",
                Belligerents = "HTS vs SAA/Russia, Turkish buffer", LastUpdated = DateTime.UtcNow.AddDays(-5) },

            new() { Id = "CZ-06", Name = "Turkish-PKK — Northern Iraq Mountains",
                Latitude = 37.00, Longitude = 43.50, RadiusKm = 55,
                Intensity = ConflictIntensity.MediumIntensity,
                Description = "TAF Claw operations against PKK mountain positions. Air strikes, commando raids, FOBs.",
                Belligerents = "Turkish Armed Forces vs PKK", LastUpdated = DateTime.UtcNow.AddDays(-3) },

            new() { Id = "CZ-07", Name = "Red Sea — Houthi Anti-Shipping Zone",
                Latitude = 14.50, Longitude = 42.00, RadiusKm = 200,
                Intensity = ConflictIntensity.HighIntensity,
                Description = "Houthi drone and missile attacks on commercial shipping. US/UK naval response. " +
                              "Massive disruption to Suez trade route. Ships rerouting around Africa.",
                Belligerents = "Ansar Allah vs US/UK, commercial shipping", LastUpdated = DateTime.UtcNow.AddHours(-12) },
        };

        // ══════════════════════════════════════════════════════
        //  THERMAL HOTSPOTS — NASA FIRMS-style anomalies
        // ══════════════════════════════════════════════════════

        private readonly List<ThermalHotspot> _hotspots = new()
        {
            new() { Id = "TH-01", Name = "Gaza City — Thermal Cluster",
                Latitude = 31.51, Longitude = 34.44, Confidence = 95, BrightnessK = 380,
                Source = "VIIRS-SNPP", SatelliteName = "Suomi NPP",
                Description = "Persistent thermal anomalies consistent with structural fires and ordnance detonation.",
                DetectedUtc = DateTime.UtcNow.AddHours(-4) },

            new() { Id = "TH-02", Name = "Khan Younis — Strike Aftermath",
                Latitude = 31.35, Longitude = 34.30, Confidence = 88, BrightnessK = 340,
                Source = "MODIS-Aqua", SatelliteName = "Aqua",
                Description = "Multiple hotspots indicating recent airstrike impact zone and secondary fires.",
                DetectedUtc = DateTime.UtcNow.AddHours(-8) },

            new() { Id = "TH-03", Name = "Southern Lebanon — Border Exchange",
                Latitude = 33.11, Longitude = 35.27, Confidence = 75, BrightnessK = 315,
                Source = "VIIRS-SNPP", SatelliteName = "Suomi NPP",
                Description = "Thermal signatures at known Hezbollah launch positions. Possible rocket launches or strikes.",
                DetectedUtc = DateTime.UtcNow.AddHours(-6) },

            new() { Id = "TH-04", Name = "Deir ez-Zor — Airstrike Signature",
                Latitude = 35.32, Longitude = 40.15, Confidence = 82, BrightnessK = 325,
                Source = "MODIS-Terra", SatelliteName = "Terra",
                Description = "Isolated thermal anomaly in desert near known ISIS camps. Probable coalition strike.",
                DetectedUtc = DateTime.UtcNow.AddHours(-18) },

            new() { Id = "TH-05", Name = "Marib — Artillery Exchange Signatures",
                Latitude = 15.45, Longitude = 45.30, Confidence = 70, BrightnessK = 310,
                Source = "VIIRS-SNPP", SatelliteName = "Suomi NPP",
                Description = "Linear thermal pattern consistent with artillery barrage along front line.",
                DetectedUtc = DateTime.UtcNow.AddHours(-12) },

            new() { Id = "TH-06", Name = "Yemen Coast — Houthi Launch Site",
                Latitude = 14.82, Longitude = 42.95, Confidence = 60, BrightnessK = 300,
                Source = "VIIRS-NOAA20", SatelliteName = "NOAA-20",
                Description = "Brief thermal spike at known mobile launcher position. Possible anti-ship missile launch.",
                DetectedUtc = DateTime.UtcNow.AddHours(-2) },

            new() { Id = "TH-07", Name = "Baghdad Green Zone — C-RAM Activations",
                Latitude = 33.30, Longitude = 44.39, Confidence = 55, BrightnessK = 290,
                Source = "VIIRS-SNPP", SatelliteName = "Suomi NPP",
                Description = "Thermal events near US Embassy complex. Consistent with C-RAM intercept flares.",
                DetectedUtc = DateTime.UtcNow.AddHours(-36) },

            new() { Id = "TH-08", Name = "Rafah — Border Zone Activity",
                Latitude = 31.28, Longitude = 34.24, Confidence = 90, BrightnessK = 365,
                Source = "MODIS-Aqua", SatelliteName = "Aqua",
                Description = "Large thermal footprint at Rafah border area. Ground operations and structure demolition.",
                DetectedUtc = DateTime.UtcNow.AddHours(-5) },
        };

        // ══════════════════════════════════════════════════════
        //  CHECKPOINTS & FOBs
        // ══════════════════════════════════════════════════════

        private readonly List<GroundCheckpoint> _checkpoints = new()
        {
            new() { Id = "CP-01", Name = "Al-Tanf Garrison (US)", Latitude = 33.49, Longitude = 38.77,
                Controller = "US Army", Type = CheckpointType.FOB,
                Description = "Strategic US garrison at Syria-Iraq-Jordan tri-border. 55km deconfliction zone. Blocks Iran's land bridge." },

            new() { Id = "CP-02", Name = "Al-Qaim / Al-Bukamal Crossing", Latitude = 34.38, Longitude = 40.93,
                Controller = "PMF / SAA", Type = CheckpointType.BorderCrossing,
                Description = "Iran-Iraq-Syria corridor crossing. PMF-controlled on Iraqi side. Key node in Tehran-Beirut land bridge." },

            new() { Id = "CP-03", Name = "Conoco Gas Plant (US FOB)", Latitude = 35.15, Longitude = 40.10,
                Controller = "US Army", Type = CheckpointType.FOB,
                Description = "US forward base at Conoco gas facility, NE Syria. SDF partnered. Frequent drone/rocket attacks by PMF." },

            new() { Id = "CP-04", Name = "Green Village (US FOB)", Latitude = 34.40, Longitude = 40.35,
                Controller = "US Army", Type = CheckpointType.FOB,
                Description = "US FOB near Deir ez-Zor. Houses CJTF-OIR personnel supporting SDF operations." },

            new() { Id = "CP-05", Name = "Ain al-Asad Air Base", Latitude = 33.80, Longitude = 42.44,
                Controller = "US / Iraqi", Type = CheckpointType.AirBase,
                Description = "Major coalition base in Anbar. Target of Iran IRBM strike Jan 2020. Enhanced air defense post-attack." },

            new() { Id = "CP-06", Name = "T4 (Tiyas) Airbase", Latitude = 34.52, Longitude = 37.63,
                Controller = "SAA / IRGC", Type = CheckpointType.AirBase,
                Description = "Strategic Syrian airbase used by IRGC and SAA. Frequent Israeli airstrikes targeting Iranian assets." },

            new() { Id = "CP-07", Name = "Erez Crossing (Gaza-Israel)", Latitude = 31.55, Longitude = 34.49,
                Controller = "IDF", Type = CheckpointType.BorderCrossing,
                Description = "Primary pedestrian crossing between Gaza and Israel. Heavily fortified. Limited humanitarian access." },

            new() { Id = "CP-08", Name = "Rafah Crossing (Gaza-Egypt)", Latitude = 31.24, Longitude = 34.25,
                Controller = "Egypt / IDF contested", Type = CheckpointType.BorderCrossing,
                Description = "Gaza-Egypt border crossing. Critical humanitarian corridor. Buffer zone expanding under IDF pressure." },

            new() { Id = "CP-09", Name = "Kerem Abu Salem (Kerem Shalom)", Latitude = 31.22, Longitude = 34.27,
                Controller = "IDF", Type = CheckpointType.BorderCrossing,
                Description = "Primary cargo/goods crossing into Gaza. Bottleneck for humanitarian aid delivery." },

            new() { Id = "CP-10", Name = "Tower 22 (Jordan)", Latitude = 33.38, Longitude = 38.81,
                Controller = "US Army", Type = CheckpointType.FOB,
                Description = "US logistics facility near Al-Tanf at Jordan border. Target of fatal drone strike Jan 2024 (3 KIA)." },
        };

        // ══════════════════════════════════════════════════════
        //  IDP / REFUGEE MOVEMENT CORRIDORS
        // ══════════════════════════════════════════════════════

        private readonly List<DisplacementCorridor> _idpCorridors = new()
        {
            new() { Id = "IDP-01", Name = "Gaza North → South Displacement",
                StartLat = 31.52, StartLon = 34.44, EndLat = 31.25, EndLon = 34.28,
                EstimatedPersons = 1_100_000, Status = "ACTIVE — ONGOING",
                Description = "Mass civilian displacement from northern Gaza southward following IDF evacuation orders." },

            new() { Id = "IDP-02", Name = "Southern Lebanon → Bekaa / Beirut",
                StartLat = 33.15, StartLon = 35.35, EndLat = 33.89, EndLon = 35.50,
                EstimatedPersons = 90_000, Status = "ACTIVE",
                Description = "Civilian evacuation from southern Lebanon border villages due to Hezbollah-IDF exchanges." },

            new() { Id = "IDP-03", Name = "NE Syria — Turkey Border IDP Camps",
                StartLat = 36.40, StartLon = 40.00, EndLat = 36.85, EndLon = 40.20,
                EstimatedPersons = 250_000, Status = "SEMI-PERMANENT",
                Description = "Displaced populations in NE Syria IDP camps near Turkish border." },

            new() { Id = "IDP-04", Name = "Marib — Displacement from Front Line",
                StartLat = 15.50, StartLon = 45.40, EndLat = 15.47, EndLon = 45.32,
                EstimatedPersons = 70_000, Status = "ONGOING",
                Description = "IDPs fleeing Houthi-government front line around Marib governorate." },
        };

        // ══════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════

        public List<GroundFlock> GetAllFlocks() => _flocks.OrderByDescending(f => f.ThreatLevel).ToList();

        public List<GroundFlock> GetActiveFlocks() => _flocks.Where(f => f.IsActive).ToList();

        public List<GroundFlock> GetFlocksByCountry(string country) =>
            _flocks.Where(f => f.Country.Equals(country, StringComparison.OrdinalIgnoreCase)).ToList();

        public List<GroundFlock> GetFlocksByType(GroundFlockType type) =>
            _flocks.Where(f => f.FlockType == type).ToList();

        public List<GroundConflictZone> GetAllConflictZones() => _conflictZones;

        public List<GroundConflictZone> GetActiveConflictZones() =>
            _conflictZones.Where(z => z.Intensity >= ConflictIntensity.MediumIntensity).ToList();

        public List<ThermalHotspot> GetAllHotspots() => _hotspots.OrderByDescending(h => h.Confidence).ToList();

        public List<ThermalHotspot> GetRecentHotspots(int hoursBack = 24) =>
            _hotspots.Where(h => h.DetectedUtc > DateTime.UtcNow.AddHours(-hoursBack)).ToList();

        /// <summary>Merges live NASA FIRMS satellite hotspots into the tracking list, replacing older FIRMS entries.</summary>
        public void MergeFirmsHotspots(List<ThermalHotspot> firmsData)
        {
            // Remove old FIRMS entries; keep manually-curated static hotspots
            _hotspots.RemoveAll(h => h.Id.StartsWith("FIRMS-"));
            _hotspots.AddRange(firmsData);
        }

        public List<GroundCheckpoint> GetAllCheckpoints() => _checkpoints;

        public List<DisplacementCorridor> GetAllCorridors() => _idpCorridors;

        public GroundStats GetStats()
        {
            return new GroundStats
            {
                TotalFlocks = _flocks.Count,
                ActiveFlocks = _flocks.Count(f => f.IsActive),
                ConflictZones = _conflictZones.Count,
                HighIntensityZones = _conflictZones.Count(z => z.Intensity == ConflictIntensity.HighIntensity),
                ThermalHotspots = _hotspots.Count,
                Checkpoints = _checkpoints.Count,
                IdpCorridors = _idpCorridors.Count,
                TotalDisplaced = _idpCorridors.Sum(c => c.EstimatedPersons),
                Countries = _flocks.Select(f => f.Country).Distinct().Count()
            };
        }

        /// <summary>Builds JavaScript-ready map data for ground flocks.</summary>
        public string BuildFlockMapData()
        {
            return string.Join(",", _flocks.Where(f => f.IsActive).Select(f =>
            {
                var color = f.ThreatLevel switch
                {
                    SeverityLevel.Critical => "#EE3333",
                    SeverityLevel.High => "#FF8833",
                    SeverityLevel.Medium => "#DDCC33",
                    _ => "#33CC33"
                };
                var icon = f.FlockType switch
                {
                    GroundFlockType.ForceCluster => "🪖",
                    GroundFlockType.MilitiaCluster => "⚔",
                    GroundFlockType.BorderGuard => "🛡",
                    GroundFlockType.FrontLine => "🎯",
                    GroundFlockType.ForwardBase => "🏴",
                    GroundFlockType.LogisticsHub => "📦",
                    GroundFlockType.AdvisoryMission => "🎖",
                    _ => "👥"
                };
                var name = Esc(f.Name);
                var desc = Esc(f.Description);
                var equip = Esc(f.Equipment);
                var strength = Esc(f.EstimatedStrength);

                return $"{{lat:{f.Latitude.ToString(CultureInfo.InvariantCulture)}," +
                       $"lon:{f.Longitude.ToString(CultureInfo.InvariantCulture)}," +
                       $"r:{f.RadiusKm},name:'{name}',desc:'{desc}',color:'{color}',icon:'{icon}'," +
                       $"country:'{Esc(f.Country)}',force:'{Esc(f.Force)}',flag:'{f.ForceFlag}'," +
                       $"strength:'{strength}',equip:'{equip}',type:'{f.FlockType}'," +
                       $"sev:{(int)f.ThreatLevel}}}";
            }));
        }

        /// <summary>Builds JavaScript-ready map data for conflict zones.</summary>
        public string BuildConflictZoneMapData()
        {
            return string.Join(",", _conflictZones.Select(z =>
            {
                var color = z.Intensity switch
                {
                    ConflictIntensity.HighIntensity => "#EE3333",
                    ConflictIntensity.MediumIntensity => "#FF8833",
                    _ => "#DDCC33"
                };
                return $"{{lat:{z.Latitude.ToString(CultureInfo.InvariantCulture)}," +
                       $"lon:{z.Longitude.ToString(CultureInfo.InvariantCulture)}," +
                       $"r:{z.RadiusKm},name:'{Esc(z.Name)}',desc:'{Esc(z.Description)}'," +
                       $"color:'{color}',belligerents:'{Esc(z.Belligerents)}'," +
                       $"intensity:'{z.Intensity}'}}";
            }));
        }

        /// <summary>Builds JavaScript-ready map data for thermal hotspots.</summary>
        public string BuildHotspotMapData()
        {
            return string.Join(",", _hotspots.Select(h =>
            {
                var color = h.Confidence >= 80 ? "#EE3333" : (h.Confidence >= 60 ? "#FF8833" : "#DDCC33");
                return $"{{lat:{h.Latitude.ToString(CultureInfo.InvariantCulture)}," +
                       $"lon:{h.Longitude.ToString(CultureInfo.InvariantCulture)}," +
                       $"name:'{Esc(h.Name)}',desc:'{Esc(h.Description)}'," +
                       $"color:'{color}',confidence:{h.Confidence},brightness:{h.BrightnessK}," +
                       $"source:'{Esc(h.Source)}',satellite:'{Esc(h.SatelliteName)}'," +
                       $"time:'{h.DetectedUtc:yyyy-MM-dd HH:mm}'}}";
            }));
        }

        /// <summary>Builds JavaScript-ready map data for checkpoints and FOBs.</summary>
        public string BuildCheckpointMapData()
        {
            return string.Join(",", _checkpoints.Select(c =>
            {
                var icon = c.Type switch
                {
                    CheckpointType.FOB => "🏴",
                    CheckpointType.AirBase => "🛫",
                    CheckpointType.BorderCrossing => "🚧",
                    _ => "📍"
                };
                var color = c.Type switch
                {
                    CheckpointType.FOB => "#FF8833",
                    CheckpointType.AirBase => "#3388FF",
                    CheckpointType.BorderCrossing => "#DDCC33",
                    _ => "#6A6A80"
                };
                return $"{{lat:{c.Latitude.ToString(CultureInfo.InvariantCulture)}," +
                       $"lon:{c.Longitude.ToString(CultureInfo.InvariantCulture)}," +
                       $"name:'{Esc(c.Name)}',desc:'{Esc(c.Description)}'," +
                       $"icon:'{icon}',color:'{color}',type:'{c.Type}'," +
                       $"controller:'{Esc(c.Controller)}'}}";
            }));
        }

        /// <summary>Builds JavaScript-ready map data for IDP displacement corridors.</summary>
        public string BuildCorridorMapData()
        {
            return string.Join(",", _idpCorridors.Select(c =>
            {
                return $"{{startLat:{c.StartLat.ToString(CultureInfo.InvariantCulture)}," +
                       $"startLon:{c.StartLon.ToString(CultureInfo.InvariantCulture)}," +
                       $"endLat:{c.EndLat.ToString(CultureInfo.InvariantCulture)}," +
                       $"endLon:{c.EndLon.ToString(CultureInfo.InvariantCulture)}," +
                       $"name:'{Esc(c.Name)}',desc:'{Esc(c.Description)}'," +
                       $"persons:{c.EstimatedPersons},status:'{Esc(c.Status)}'}}";
            }));
        }

        /// <summary>Fetches real thermal anomaly data from NASA FIRMS API for the Middle East region.</summary>
        public async Task<List<ThermalHotspot>> FetchFirmsDataAsync()
        {
            var results = new List<ThermalHotspot>();
            try
            {
                // NASA FIRMS VIIRS active fire data — ME bounding box
                // Free API with MAP_KEY=ENTER_YOUR_KEY or use "DEMO" for limited requests
                var url = "https://firms.modaps.eosdis.nasa.gov/api/area/csv/DEMO/VIIRS_SNPP_NRT/25,30,55,40/1";
                var csv = await _http.GetStringAsync(url);
                var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                for (int i = 1; i < lines.Length; i++) // Skip header
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length < 10) continue;

                    if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                        double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var brightness) &&
                        int.TryParse(parts[8], out var conf))
                    {
                        results.Add(new ThermalHotspot
                        {
                            Id = $"FIRMS-{i}",
                            Name = $"FIRMS Hotspot ({lat:F2}, {lon:F2})",
                            Latitude = lat, Longitude = lon,
                            BrightnessK = brightness, Confidence = conf,
                            Source = "VIIRS-SNPP", SatelliteName = "Suomi NPP",
                            Description = $"Satellite-detected thermal anomaly. Brightness: {brightness}K, Confidence: {conf}%",
                            DetectedUtc = DateTime.UtcNow
                        });
                    }
                }
            }
            catch
            {
                // API unavailable — fall back to static data
            }

            return results;
        }

        private static string Esc(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
    }

    // ══════════════════════════════════════════════════════
    //  STATS MODEL
    // ══════════════════════════════════════════════════════

    public class GroundStats
    {
        public int TotalFlocks { get; set; }
        public int ActiveFlocks { get; set; }
        public int ConflictZones { get; set; }
        public int HighIntensityZones { get; set; }
        public int ThermalHotspots { get; set; }
        public int Checkpoints { get; set; }
        public int IdpCorridors { get; set; }
        public int TotalDisplaced { get; set; }
        public int Countries { get; set; }
    }
}
