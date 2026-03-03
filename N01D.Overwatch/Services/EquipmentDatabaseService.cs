using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using N01D.Overwatch.Models;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Comprehensive Middle East military order of battle database.
    /// Tracks active equipment inventories, capabilities, deployment locations,
    /// and force compositions for all major regional actors.
    /// Sources: IISS Military Balance 2024, Jane's, GlobalFirepower, SIPRI.
    /// </summary>
    public class EquipmentDatabaseService
    {
        // ══════════════════════════════════════════════════════
        //  FORCE COMPOSITIONS — REGIONAL POWERS
        // ══════════════════════════════════════════════════════

        private readonly List<ForceComposition> _forces = new()
        {
            // ────────────────────────────────────────
            //  IRAN
            // ────────────────────────────────────────
            new ForceComposition
            {
                Country = "Iran", FlagEmoji = "🇮🇷",
                TotalPersonnel = 610_000, ActivePersonnel = 420_000,
                ReservePersonnel = 350_000, ParamilitaryPersonnel = 220_000,
                DefenseBudgetBillions = 6.8, GlobalFirepowerRank = 14,
                Notes = "IRIAF aging fleet but massive IRGC missile/drone arsenal. Basij militia ~600K mobilizable.",
                Equipment = new()
                {
                    // ── IRIAF — Fighters & Attack ──
                    new() { Id = "IR-F14", Name = "F-14A Tomcat", Designation = "F-14A", Country = "Iran", Operator = "IRIAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.AirSuperiority,
                        Quantity = 44, QuantityActive = 20, Manufacturer = "Grumman", Origin = "United States",
                        YearIntroduced = 1976, ThreatRating = SeverityLevel.Medium,
                        Specs = "Max speed: Mach 2.34 | Range: 3,200 km | Radar: AN/AWG-9 (modified) | Weapons: AIM-54 Phoenix (Iranian Fakour-90), Hawk conversions",
                        Description = "Iran's premier air superiority fighter. Only non-US operator. Heavily maintained with indigenous parts. Fakour-90 replaces Phoenix. Estimated 20 flyable.",
                        BaseLat = 32.37, BaseLon = 51.69, BaseLocation = "Isfahan-Khatami AFB", ImageIcon = "✈" },
                    new() { Id = "IR-F4E", Name = "F-4E Phantom II", Designation = "F-4E", Country = "Iran", Operator = "IRIAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.GroundAttack,
                        Quantity = 63, QuantityActive = 30, Manufacturer = "McDonnell Douglas", Origin = "United States",
                        YearIntroduced = 1971, ThreatRating = SeverityLevel.Low,
                        Specs = "Max speed: Mach 2.23 | Range: 2,600 km | Payload: 8,400 kg bombs/missiles | Upgraded with Ghods Yasir pod",
                        Description = "Workhorse ground attack platform. Upgraded with indigenous avionics. Used for anti-ship with C-802/Noor missiles.",
                        BaseLat = 27.19, BaseLon = 56.28, BaseLocation = "Bandar Abbas", ImageIcon = "✈" },
                    new() { Id = "IR-MIG29", Name = "MiG-29A/UB Fulcrum", Designation = "MiG-29A", Country = "Iran", Operator = "IRIAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.AirSuperiority,
                        Quantity = 36, QuantityActive = 25, Manufacturer = "Mikoyan", Origin = "Russia",
                        YearIntroduced = 1990, ThreatRating = SeverityLevel.Medium,
                        Specs = "Max speed: Mach 2.25 | Range: 1,500 km | Weapons: R-73, R-27 | Highly maneuverable dogfighter",
                        Description = "Most capable short-range fighter in IRIAF inventory. Iraqi defectors + direct purchases. R-73 IR missiles.",
                        BaseLat = 38.08, BaseLon = 46.30, BaseLocation = "Tabriz AFB", ImageIcon = "✈" },
                    new() { Id = "IR-SU24", Name = "Su-24MK Fencer", Designation = "Su-24MK", Country = "Iran", Operator = "IRIAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.GroundAttack,
                        Quantity = 30, QuantityActive = 20, Manufacturer = "Sukhoi", Origin = "Russia",
                        YearIntroduced = 1991, ThreatRating = SeverityLevel.Medium,
                        Specs = "Max speed: Mach 1.35 | Range: 2,800 km | Payload: 8,000 kg | All-weather strike",
                        Description = "Deep strike/interdiction bomber. Iraqi defectors during Gulf War. Can carry Russian/Iranian PGMs.",
                        BaseLat = 30.83, BaseLon = 49.27, BaseLocation = "Omidiyeh AFB", ImageIcon = "✈" },
                    new() { Id = "IR-SU35", Name = "Su-35S Flanker-E", Designation = "Su-35S", Country = "Iran", Operator = "IRIAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.AirSuperiority,
                        Quantity = 24, QuantityActive = 12, Manufacturer = "Sukhoi", Origin = "Russia",
                        YearIntroduced = 2024, ThreatRating = SeverityLevel.Critical,
                        Specs = "Max speed: Mach 2.25 | Range: 3,600 km | Radar: Irbis-E PESA | Weapons: R-77, R-73, R-27, KAB PGMs | 3D TVC",
                        Description = "Iran's newest and most capable fighter. Delivered 2024 under Russia-Iran defense pact. Transforms IRIAF air superiority capability. Irbis-E radar can track 30 targets.",
                        BaseLat = 35.69, BaseLon = 51.39, BaseLocation = "Tehran-Mehrabad", ImageIcon = "🔴✈" },
                    new() { Id = "IR-KOWSAR", Name = "HESA Kowsar", Designation = "Kowsar", Country = "Iran", Operator = "IRIAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 12, QuantityActive = 10, Manufacturer = "HESA", Origin = "Iran",
                        YearIntroduced = 2018, ThreatRating = SeverityLevel.Low,
                        Specs = "Based on F-5 airframe | Indigenous avionics | 4th-gen HUD | Weapons: AIM-9 equiv, bombs",
                        Description = "Indigenous light fighter based on F-5E Tiger II. Indigenous avionics, HUD, weapons computer. Limited capability but propaganda value.",
                        BaseLat = 32.37, BaseLon = 51.69, BaseLocation = "Isfahan", ImageIcon = "✈" },

                    // ── IRGC — Drones / UAS ──
                    new() { Id = "IR-SHAHED136", Name = "Shahed-136 / Geran-2", Designation = "Shahed-136", Country = "Iran", Operator = "IRGC-ASF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.LoiteringMunition,
                        Quantity = 3000, QuantityActive = 2500, Manufacturer = "IACI", Origin = "Iran",
                        YearIntroduced = 2021, ThreatRating = SeverityLevel.Critical,
                        Specs = "Range: 2,500 km | Speed: 185 km/h | Warhead: 40-50 kg | Delta wing, GPS/INS guided | Cost: ~$20,000-50,000",
                        Description = "Mass-produced one-way attack drone. Exported to Russia (Geran-2) for Ukraine war. Prolific in Houthi/Hezbollah attacks. GPS/INS + potential TV terminal guidance.",
                        BaseLat = 32.65, BaseLon = 51.68, BaseLocation = "Isfahan IACI", ImageIcon = "🛩" },
                    new() { Id = "IR-SHAHED129", Name = "Shahed-129", Designation = "Shahed-129", Country = "Iran", Operator = "IRGC-ASF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.MALE,
                        Quantity = 100, QuantityActive = 60, Manufacturer = "IACI", Origin = "Iran",
                        YearIntroduced = 2012, ThreatRating = SeverityLevel.High,
                        Specs = "Endurance: 24 hrs | Range: 1,700 km | Payload: Sadid-345 PGM | EO/IR sensor | Wingspan: 16m",
                        Description = "MALE UCAV comparable to MQ-1 Predator. Armed with Sadid-345 precision munitions. Operated extensively over Syria and Iraq.",
                        BaseLat = 33.51, BaseLon = 36.29, BaseLocation = "Syria/Iran bases", ImageIcon = "🛩" },
                    new() { Id = "IR-MOHAJER6", Name = "Mohajer-6", Designation = "Mohajer-6", Country = "Iran", Operator = "IRGC/IRIAF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.TacticalISR,
                        Quantity = 150, QuantityActive = 100, Manufacturer = "Qods Aviation", Origin = "Iran",
                        YearIntroduced = 2017, ThreatRating = SeverityLevel.High,
                        Specs = "Endurance: 12 hrs | Range: 200 km | Payload: Qaem PGM, EO/IR | Ceiling: 18,000 ft",
                        Description = "Tactical UCAV. Exported to Russia, Ethiopia, Venezuela. Armed with Qaem-series smart bombs. Widely used Iran proxy network.",
                        BaseLat = 35.69, BaseLon = 51.39, BaseLocation = "Multiple bases", ImageIcon = "🛩" },
                    new() { Id = "IR-ABABIL3", Name = "Ababil-3", Designation = "Ababil-3", Country = "Iran", Operator = "IRGC",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.TacticalISR,
                        Quantity = 200, QuantityActive = 150, Manufacturer = "IACI", Origin = "Iran",
                        YearIntroduced = 2014, ThreatRating = SeverityLevel.Medium,
                        Specs = "Range: 150 km | Endurance: 4 hrs | EO payload | Can be armed with submunitions",
                        Description = "Mass-produced tactical drone supplied to Hezbollah, Houthis, Iraqi PMU. ISR and loitering munition variants.",
                        BaseLat = 33.85, BaseLon = 36.10, BaseLocation = "Distributed to proxies", ImageIcon = "🛩" },
                    new() { Id = "IR-KARRAR", Name = "Karrar UCAV", Designation = "Karrar", Country = "Iran", Operator = "IRGC-ASF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.UCAV,
                        Quantity = 100, QuantityActive = 70, Manufacturer = "IACI", Origin = "Iran",
                        YearIntroduced = 2010, ThreatRating = SeverityLevel.High,
                        Specs = "Max speed: 900 km/h | Range: 1,000 km | Jet-powered | Can carry Mk-82 bombs, Kowsar anti-ship missiles",
                        Description = "Jet-powered attack drone. Can function as cruise missile or recoverable UCAV. High-speed target drone capability.",
                        BaseLat = 27.19, BaseLon = 56.28, BaseLocation = "Bandar Abbas / Gulf coast", ImageIcon = "🛩" },

                    // ── Iran Navy & IRGCN ──
                    new() { Id = "IR-FATEH", Name = "Fateh-class Submarine", Designation = "Fateh", Country = "Iran", Operator = "IRIN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.Submarine,
                        Quantity = 1, QuantityActive = 1, Manufacturer = "MDSI", Origin = "Iran",
                        YearIntroduced = 2019, ThreatRating = SeverityLevel.High,
                        Specs = "Displacement: 593 tons | Torpedoes: 4 x 533mm | Cruise missiles capable | Depth: 200m",
                        Description = "Iran's most advanced indigenous submarine. Can fire cruise missiles, lay mines, deploy naval commandos. Persian Gulf operations.",
                        BaseLat = 27.19, BaseLon = 56.28, BaseLocation = "Bandar Abbas", ImageIcon = "🔱" },
                    new() { Id = "IR-KILO", Name = "Kilo-class Submarine (877EKM)", Designation = "Kilo", Country = "Iran", Operator = "IRIN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.Submarine,
                        Quantity = 3, QuantityActive = 2, Manufacturer = "Admiralty Shipyard", Origin = "Russia",
                        YearIntroduced = 1992, ThreatRating = SeverityLevel.High,
                        Specs = "Displacement: 3,076 tons | Torpedoes: 6 x 533mm | Range: 6,000 nm | Extremely quiet diesel-electric",
                        Description = "Russian-built diesel-electric attack subs. 'Black hole' quiet signature. Iran's most capable undersea platform. Can mine Strait of Hormuz.",
                        BaseLat = 27.19, BaseLon = 56.28, BaseLocation = "Bandar Abbas", ImageIcon = "🔱" },
                    new() { Id = "IR-JAMARAN", Name = "Jamaran-class Frigate", Designation = "Moudge", Country = "Iran", Operator = "IRIN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.Frigate,
                        Quantity = 3, QuantityActive = 3, Manufacturer = "MDSI", Origin = "Iran",
                        YearIntroduced = 2010, ThreatRating = SeverityLevel.Medium,
                        Specs = "Displacement: 1,500 tons | Weapons: Noor AShM, 76mm gun, torpedoes | Helicopter deck",
                        Description = "Indigenous corvette/light frigate. Equipped with C-802/Noor anti-ship missile. Limited air defense (Fajr-27 SAM).",
                        BaseLat = 27.19, BaseLon = 56.28, BaseLocation = "Bandar Abbas", ImageIcon = "🚢" },
                    new() { Id = "IR-FAC", Name = "IRGCN Fast Attack Craft", Designation = "Various", Country = "Iran", Operator = "IRGCN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.FastAttackCraft,
                        Quantity = 350, QuantityActive = 300, Manufacturer = "Various", Origin = "Iran/China",
                        YearIntroduced = 1990, ThreatRating = SeverityLevel.High,
                        Specs = "Speed: 45-65 kts | Weapons: RPG, machine guns, C-802 AShM on larger boats | Swarm tactics",
                        Description = "IRGC Navy's primary offensive arm. 350+ fast attack boats for swarm tactics in Persian Gulf. Can overwhelm larger naval vessels.",
                        BaseLat = 26.56, BaseLon = 56.25, BaseLocation = "Strait of Hormuz islands", ImageIcon = "🚤" },

                    // ── Iran Ground Forces ──
                    new() { Id = "IR-T72S", Name = "T-72S Shilden", Designation = "T-72S", Country = "Iran", Operator = "IRGC-GF / IA",
                        Domain = EquipmentDomain.GroundForces, Role = EquipmentRole.MBT,
                        Quantity = 480, QuantityActive = 400, Manufacturer = "Uralvagonzavod", Origin = "Russia",
                        YearIntroduced = 1993, ThreatRating = SeverityLevel.Medium,
                        Specs = "125mm smoothbore | Kontakt-5 ERA option | 1,000 hp engine | Fire control upgraded locally",
                        Description = "Iran's most modern MBT. Local upgrade programs include improved FCS, ERA, and communications. Main IRGC Ground Force tank.",
                        BaseLat = 33.49, BaseLon = 48.35, BaseLocation = "Western garrisons", ImageIcon = "🪖" },
                    new() { Id = "IR-KARRAR-T", Name = "Karrar MBT", Designation = "Karrar", Country = "Iran", Operator = "IA",
                        Domain = EquipmentDomain.GroundForces, Role = EquipmentRole.MBT,
                        Quantity = 150, QuantityActive = 100, Manufacturer = "DIO", Origin = "Iran",
                        YearIntroduced = 2017, ThreatRating = SeverityLevel.Medium,
                        Specs = "125mm smoothbore | ERA Relikt-type | Electro-optical FCS | Laser warning | 1,000 hp",
                        Description = "Indigenous MBT based on T-72 hull with significant upgrades. ERA, new turret design, indigenous FCS. Claimed comparable to T-90.",
                        BaseLat = 35.69, BaseLon = 51.39, BaseLocation = "Central garrisons", ImageIcon = "🪖" },
                }
            },

            // ────────────────────────────────────────
            //  ISRAEL
            // ────────────────────────────────────────
            new ForceComposition
            {
                Country = "Israel", FlagEmoji = "🇮🇱",
                TotalPersonnel = 634_000, ActivePersonnel = 170_000,
                ReservePersonnel = 465_000, ParamilitaryPersonnel = 8_000,
                DefenseBudgetBillions = 23.4, GlobalFirepowerRank = 17,
                Notes = "Qualitative military edge (QME). F-35I Adir stealth fighter. Multi-layered missile defense (Iron Dome → David's Sling → Arrow).",
                Equipment = new()
                {
                    // ── IAF Fighters ──
                    new() { Id = "IL-F35I", Name = "F-35I Adir", Designation = "F-35I", Country = "Israel", Operator = "IAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 50, QuantityActive = 46, Manufacturer = "Lockheed Martin", Origin = "United States",
                        YearIntroduced = 2016, ThreatRating = SeverityLevel.Critical,
                        Specs = "Max speed: Mach 1.6 | Combat radius: 1,093 km | Stealth | AESA radar: AN/APG-81 | Israeli modifications: EW suite, fuel tanks, weapons integration",
                        Description = "5th-gen stealth multirole fighter with Israeli-specific modifications. Indigenous EW, datalinks, conformal fuel tanks. Has conducted strikes in Iran, Syria, Iraq.",
                        BaseLat = 31.29, BaseLon = 34.39, BaseLocation = "Nevatim AFB", ImageIcon = "🔴✈" },
                    new() { Id = "IL-F15I", Name = "F-15I Ra'am", Designation = "F-15I", Country = "Israel", Operator = "IAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.GroundAttack,
                        Quantity = 25, QuantityActive = 25, Manufacturer = "Boeing/MDD", Origin = "United States",
                        YearIntroduced = 1998, ThreatRating = SeverityLevel.Critical,
                        Specs = "Max speed: Mach 2.5 | Range: 4,450 km | Payload: 11,000 kg | CFTs | LANTIRN/Litening pods | GBU-28 bunker buster capable",
                        Description = "Long-range strike variant for deep interdiction. Can reach Iran with aerial refueling. Primary bunker-buster delivery platform. Israeli-specific avionics.",
                        BaseLat = 30.12, BaseLon = 34.87, BaseLocation = "Ramon AFB", ImageIcon = "✈" },
                    new() { Id = "IL-F15C", Name = "F-15C/D Baz", Designation = "F-15C/D", Country = "Israel", Operator = "IAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.AirSuperiority,
                        Quantity = 55, QuantityActive = 49, Manufacturer = "McDonnell Douglas", Origin = "United States",
                        YearIntroduced = 1976, ThreatRating = SeverityLevel.High,
                        Specs = "Max speed: Mach 2.5 | Range: 3,500 km | Radar: AN/APG-63(V)1 | Python-5 + Derby BVR",
                        Description = "Air superiority fighters with decades of aerial victories. Heavily upgraded Israeli avionics. Python-5 5th-gen dogfight missile.",
                        BaseLat = 32.60, BaseLon = 35.23, BaseLocation = "Tel Nof AFB", ImageIcon = "✈" },
                    new() { Id = "IL-F16I", Name = "F-16I Sufa", Designation = "F-16I", Country = "Israel", Operator = "IAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 100, QuantityActive = 98, Manufacturer = "Lockheed Martin", Origin = "United States",
                        YearIntroduced = 2004, ThreatRating = SeverityLevel.High,
                        Specs = "Max speed: Mach 2.0 | Range: 4,200 km with CFTs | Israeli EW suite | SPICE 1000/2000 PGMs",
                        Description = "IAF backbone multirole fighter. Block 52+ with Israeli CFTs, EW, and weapons. Can reach Iran with CFTs. SPICE precision GPS/EO guided glide bombs.",
                        BaseLat = 30.12, BaseLon = 34.87, BaseLocation = "Ramon AFB", ImageIcon = "✈" },

                    // ── IAF ISR / Special ──
                    new() { Id = "IL-G550", Name = "G550 Shavit (CAEW)", Designation = "G550 CAEW", Country = "Israel", Operator = "IAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.AWACS,
                        Quantity = 4, QuantityActive = 4, Manufacturer = "IAI/Gulfstream", Origin = "Israel",
                        YearIntroduced = 2008, ThreatRating = SeverityLevel.High,
                        Specs = "AESA conformal radar | EL/W-2085 | Range: 6,750 nm | 360° coverage | Can detect stealth aircraft",
                        Description = "Conformal airborne early warning. EL/W-2085 AESA radar arrays installed conformally. Sees deep into Iran/Syria without entering airspace.",
                        BaseLat = 32.60, BaseLon = 35.23, BaseLocation = "Tel Nof AFB", ImageIcon = "📡" },
                    new() { Id = "IL-HERMES900", Name = "Hermes 900 StarLiner", Designation = "Hermes 900", Country = "Israel", Operator = "IAF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.MALE,
                        Quantity = 30, QuantityActive = 28, Manufacturer = "Elbit", Origin = "Israel",
                        YearIntroduced = 2012, ThreatRating = SeverityLevel.High,
                        Specs = "Endurance: 36 hrs | Ceiling: 30,000 ft | Payload: 350 kg | SAR/GMTI, EO/IR, SIGINT | Can be armed",
                        Description = "MALE UAS for persistent ISR and strike. Flies in civilian airspace (StarLiner variant). Used extensively for Gaza/Lebanon surveillance.",
                        BaseLat = 31.29, BaseLon = 34.39, BaseLocation = "Nevatim / Palmachim", ImageIcon = "🛩" },
                    new() { Id = "IL-HAROP", Name = "IAI Harop", Designation = "Harop", Country = "Israel", Operator = "IAF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.LoiteringMunition,
                        Quantity = 100, QuantityActive = 80, Manufacturer = "IAI", Origin = "Israel",
                        YearIntroduced = 2009, ThreatRating = SeverityLevel.Critical,
                        Specs = "Endurance: 6 hrs | Range: 1,000 km | Anti-radiation seeker | Warhead: 23 kg | Autonomous SEAD",
                        Description = "Anti-radiation loitering munition. Autonomously hunts and destroys enemy radar/SAM sites. Used by Israel and exported to India, Azerbaijan.",
                        BaseLat = 31.88, BaseLon = 34.69, BaseLocation = "Palmachim", ImageIcon = "🎯" },

                    // ── Israel Navy ──
                    new() { Id = "IL-DOLPHIN", Name = "Dolphin-class Submarine", Designation = "Dolphin II", Country = "Israel", Operator = "IN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.Submarine,
                        Quantity = 5, QuantityActive = 5, Manufacturer = "ThyssenKrupp", Origin = "Germany",
                        YearIntroduced = 1999, ThreatRating = SeverityLevel.Critical,
                        Specs = "Displacement: 2,400 tons | 10 torpedo tubes (4 x 650mm capable of cruise missiles) | AIP system | Rumored nuclear-capable SLCMs",
                        Description = "Germany-built advanced diesel-electric subs with AIP. 650mm tubes can launch nuclear-capable Popeye Turbo SLCMs. Israel's second-strike deterrent.",
                        BaseLat = 32.81, BaseLon = 34.96, BaseLocation = "Haifa Naval Base", ImageIcon = "🔱" },
                    new() { Id = "IL-SAAR6", Name = "Sa'ar 6 Corvette", Designation = "Sa'ar 6", Country = "Israel", Operator = "IN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.Corvette,
                        Quantity = 4, QuantityActive = 4, Manufacturer = "ThyssenKrupp/German Naval", Origin = "Germany",
                        YearIntroduced = 2020, ThreatRating = SeverityLevel.High,
                        Specs = "Displacement: 1,900 tons | Weapons: 16x Gabriel V/Barak-8 VLS, 76mm, C-Dome (naval Iron Dome), EL/M-2248 radar",
                        Description = "Advanced multi-mission corvette. Integrates C-Dome naval SHORAD, Barak-8 area defense, and Gabriel V AShM. Protects offshore gas platforms.",
                        BaseLat = 32.81, BaseLon = 34.96, BaseLocation = "Haifa", ImageIcon = "🚢" },

                    // ── Israel Ground Forces ──
                    new() { Id = "IL-MERKAVA4", Name = "Merkava Mk.4M Windbreaker", Designation = "Merkava IV", Country = "Israel", Operator = "IDF",
                        Domain = EquipmentDomain.GroundForces, Role = EquipmentRole.MBT,
                        Quantity = 660, QuantityActive = 580, Manufacturer = "IMI / MANTAK", Origin = "Israel",
                        YearIntroduced = 2004, ThreatRating = SeverityLevel.High,
                        Specs = "120mm smoothbore | Trophy APS (hard-kill) | Modular armor | 1,500 hp | FCS: Knight Mark 4 | Infantry compartment",
                        Description = "World-class MBT with Trophy active protection system (hard-kill RPG/ATGM intercept). Urban warfare optimized. Infantry carrying capability unique among MBTs.",
                        BaseLat = 32.80, BaseLon = 35.40, BaseLocation = "Northern Command", ImageIcon = "🪖" },
                    new() { Id = "IL-NAMER", Name = "Namer Heavy IFV", Designation = "Namer", Country = "Israel", Operator = "IDF",
                        Domain = EquipmentDomain.GroundForces, Role = EquipmentRole.IFV,
                        Quantity = 250, QuantityActive = 230, Manufacturer = "IMI / MANTAK", Origin = "Israel",
                        YearIntroduced = 2008, ThreatRating = SeverityLevel.Medium,
                        Specs = "60 tons | Trophy APS | Based on Merkava hull | 30mm RWS or Iron Fist | Carries 9 dismounts",
                        Description = "Heaviest IFV in service worldwide. Merkava-based hull provides unmatched protection. Trophy APS for anti-ATGM defense.",
                        BaseLat = 31.35, BaseLon = 34.30, BaseLocation = "Gaza Division", ImageIcon = "🪖" },
                    new() { Id = "IL-IRONDOME", Name = "Iron Dome Battery", Designation = "Iron Dome", Country = "Israel", Operator = "IDF/IAF",
                        Domain = EquipmentDomain.AirDefense, Role = EquipmentRole.SHORAD,
                        Quantity = 12, QuantityActive = 12, Manufacturer = "Rafael", Origin = "Israel",
                        YearIntroduced = 2011, ThreatRating = SeverityLevel.Critical,
                        Specs = "Range: 4-70 km | Tamir interceptor | 90%+ success rate | Each battery: 3-4 launchers, 20 missiles each | EL/M-2084 radar",
                        Description = "Revolutionary SHORAD system. 90%+ intercept rate against rockets, mortars, artillery. 10 batteries provide overlapping national coverage. 5,000+ intercepts to date.",
                        BaseLat = 32.08, BaseLon = 34.78, BaseLocation = "Nationwide deployment", ImageIcon = "🛡" },
                }
            },

            // ────────────────────────────────────────
            //  UNITED STATES (CENTCOM AOR)
            // ────────────────────────────────────────
            new ForceComposition
            {
                Country = "US CENTCOM", FlagEmoji = "🇺🇸",
                TotalPersonnel = 45_000, ActivePersonnel = 45_000,
                ReservePersonnel = 0, ParamilitaryPersonnel = 0,
                DefenseBudgetBillions = 886.0, GlobalFirepowerRank = 1,
                Notes = "Rotating forward-deployed forces. Carrier Strike Group, Bomber Task Force, and land-based assets across ME bases.",
                Equipment = new()
                {
                    new() { Id = "US-CVN", Name = "Nimitz/Ford-class Carrier", Designation = "CVN", Country = "US CENTCOM", Operator = "USN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.Carrier,
                        Quantity = 2, QuantityActive = 1, Manufacturer = "HII Newport News", Origin = "United States",
                        YearIntroduced = 1975, ThreatRating = SeverityLevel.Critical,
                        Specs = "100,000 tons | 75+ aircraft | Carrier Air Wing: F/A-18E/F, EA-18G, E-2D, MH-60R/S | Nuclear powered | Escorts: 2 CG, 4 DDG, 2 SSN",
                        Description = "Forward-deployed CSG in 5th Fleet AOR. Full carrier air wing: 44 F/A-18E/F, 5 EA-18G, 5 E-2D, helos. 1-2 CSGs typically in CENTCOM.",
                        BaseLat = 26.23, BaseLon = 50.59, BaseLocation = "NSA Bahrain (5th Fleet HQ)", ImageIcon = "⚓" },
                    new() { Id = "US-DDG", Name = "Arleigh Burke-class Destroyer", Designation = "DDG-51", Country = "US CENTCOM", Operator = "USN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.Destroyer,
                        Quantity = 8, QuantityActive = 6, Manufacturer = "HII/BIW", Origin = "United States",
                        YearIntroduced = 1991, ThreatRating = SeverityLevel.Critical,
                        Specs = "96-cell Mk 41 VLS | SM-2/SM-3/SM-6 SAM | Tomahawk LACM | AN/SPY-1D Aegis | 5-inch gun | BMD capable",
                        Description = "Aegis BMD-capable destroyers providing integrated air/missile defense. SM-3 can intercept ballistic missiles exo-atmospherically. Tomahawk land-attack.",
                        BaseLat = 26.23, BaseLon = 50.59, BaseLocation = "5th Fleet / rotating", ImageIcon = "🚢" },
                    new() { Id = "US-SSGN", Name = "Ohio-class SSGN", Designation = "SSGN-726", Country = "US CENTCOM", Operator = "USN",
                        Domain = EquipmentDomain.Navy, Role = EquipmentRole.Submarine,
                        Quantity = 1, QuantityActive = 1, Manufacturer = "General Dynamics EB", Origin = "United States",
                        YearIntroduced = 2002, ThreatRating = SeverityLevel.Critical,
                        Specs = "154 Tomahawk cruise missiles | 66 SOF embarked | 18,750 tons submerged | Nuclear powered",
                        Description = "Converted ballistic missile submarine carrying 154 Tomahawk cruise missiles. Most firepower of any submarine. SOF insertion capability.",
                        BaseLat = 26.23, BaseLon = 50.59, BaseLocation = "5th Fleet AOR", ImageIcon = "🔱" },
                    new() { Id = "US-F22", Name = "F-22A Raptor", Designation = "F-22A", Country = "US CENTCOM", Operator = "USAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.AirSuperiority,
                        Quantity = 24, QuantityActive = 24, Manufacturer = "Lockheed Martin", Origin = "United States",
                        YearIntroduced = 2005, ThreatRating = SeverityLevel.Critical,
                        Specs = "Max speed: Mach 2.25 | Supercruise: Mach 1.82 | AN/APG-77 AESA | Stealth | AIM-120D + AIM-9X | 3D TVC",
                        Description = "World's most capable air superiority fighter. Rotating deployments to Al Dhafra (UAE). Supercruise, stealth, sensor fusion. Has deployed to ME multiple times.",
                        BaseLat = 24.25, BaseLon = 54.55, BaseLocation = "Al Dhafra AB, UAE", ImageIcon = "🔴✈" },
                    new() { Id = "US-F15E", Name = "F-15E Strike Eagle", Designation = "F-15E", Country = "US CENTCOM", Operator = "USAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 48, QuantityActive = 48, Manufacturer = "Boeing", Origin = "United States",
                        YearIntroduced = 1988, ThreatRating = SeverityLevel.High,
                        Specs = "Max speed: Mach 2.5 | Range: 3,900 km | AN/APG-82(V)1 AESA | JDAM, SDB, JSOW, Paveway | Sniper ATP",
                        Description = "Primary CENTCOM deep strike platform. Deployed to Al Dhafra, UAE and various ME bases. AESA upgrade. Full PGM suite including GBU-39 SDB.",
                        BaseLat = 24.25, BaseLon = 54.55, BaseLocation = "Al Dhafra AB, UAE", ImageIcon = "✈" },
                    new() { Id = "US-F35A", Name = "F-35A Lightning II", Designation = "F-35A", Country = "US CENTCOM", Operator = "USAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 24, QuantityActive = 24, Manufacturer = "Lockheed Martin", Origin = "United States",
                        YearIntroduced = 2016, ThreatRating = SeverityLevel.Critical,
                        Specs = "Max speed: Mach 1.6 | AN/APG-81 AESA | DAS (360° IR) | Stealth | MADL datalink | AIM-120D, SDB-II, JSOW",
                        Description = "5th-gen joint strike fighter. Deployed to multiple CENTCOM locations. Sensor fusion provides unmatched situational awareness. First combat use Sept 2018.",
                        BaseLat = 24.25, BaseLon = 54.55, BaseLocation = "Al Dhafra / rotating", ImageIcon = "🔴✈" },
                    new() { Id = "US-B1B", Name = "B-1B Lancer", Designation = "B-1B", Country = "US CENTCOM", Operator = "USAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.Bomber,
                        Quantity = 4, QuantityActive = 4, Manufacturer = "Rockwell/Boeing", Origin = "United States",
                        YearIntroduced = 1986, ThreatRating = SeverityLevel.Critical,
                        Specs = "Max speed: Mach 1.25 | Payload: 34,000 kg (largest conventional payload) | 3 weapons bays | JDAM, JASSM, LRASM",
                        Description = "Supersonic bomber deployed to Diego Garcia/Al Udeid for BTF rotations. Carries largest conventional weapons load of any aircraft. JASSM standoff missile capable.",
                        BaseLat = 25.12, BaseLon = 51.32, BaseLocation = "Al Udeid AB, Qatar", ImageIcon = "✈" },
                    new() { Id = "US-MQ9", Name = "MQ-9 Reaper", Designation = "MQ-9A", Country = "US CENTCOM", Operator = "USAF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.MALE,
                        Quantity = 40, QuantityActive = 30, Manufacturer = "General Atomics", Origin = "United States",
                        YearIntroduced = 2007, ThreatRating = SeverityLevel.High,
                        Specs = "Endurance: 27 hrs | Ceiling: 50,000 ft | Payload: Hellfire, GBU-12/38, JDAM | MTS-B EO/IR/laser | Lynx SAR",
                        Description = "Primary CENTCOM armed ISR platform. Persistent overwatch and precision strike. Killed Soleimani Jan 2020. Orbits over Iraq, Syria, Yemen, Horn of Africa.",
                        BaseLat = 25.12, BaseLon = 51.32, BaseLocation = "Al Udeid / Ali Al Salem", ImageIcon = "🛩" },
                    new() { Id = "US-RQ4", Name = "RQ-4 Global Hawk", Designation = "RQ-4B", Country = "US CENTCOM", Operator = "USAF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.HALE,
                        Quantity = 6, QuantityActive = 4, Manufacturer = "Northrop Grumman", Origin = "United States",
                        YearIntroduced = 2001, ThreatRating = SeverityLevel.High,
                        Specs = "Endurance: 32 hrs | Ceiling: 60,000 ft | EISS (EO/IR) | MP-RTIP AESA radar | SIGINT | Wingspan: 39.9m",
                        Description = "HALE ISR drone. Continuous surveillance of Iran, Gulf, Yemen. Iran shot down RQ-4A (BAMS-D) in 2019. Provides strategic-level imagery and SIGINT.",
                        BaseLat = 24.25, BaseLon = 54.55, BaseLocation = "Al Dhafra AB, UAE", ImageIcon = "📡" },
                    new() { Id = "US-THAAD", Name = "THAAD Battery", Designation = "THAAD", Country = "US CENTCOM", Operator = "US Army",
                        Domain = EquipmentDomain.AirDefense, Role = EquipmentRole.SAM,
                        Quantity = 2, QuantityActive = 2, Manufacturer = "Lockheed Martin", Origin = "United States",
                        YearIntroduced = 2008, ThreatRating = SeverityLevel.Critical,
                        Specs = "Range: 200 km | Altitude: 150 km | Hit-to-kill | AN/TPY-2 radar (1,000 km detection) | 48 interceptors per battery",
                        Description = "Terminal High Altitude Area Defense. Deployed to UAE and Israel. Exo-atmospheric ballistic missile intercept. AN/TPY-2 provides BMD early warning for entire theater.",
                        BaseLat = 24.25, BaseLon = 54.55, BaseLocation = "Al Dhafra / Israel", ImageIcon = "🛡" },
                    new() { Id = "US-PATRIOT", Name = "Patriot PAC-3 MSE", Designation = "MIM-104F", Country = "US CENTCOM", Operator = "US Army",
                        Domain = EquipmentDomain.AirDefense, Role = EquipmentRole.SAM,
                        Quantity = 4, QuantityActive = 4, Manufacturer = "RTX", Origin = "United States",
                        YearIntroduced = 2015, ThreatRating = SeverityLevel.High,
                        Specs = "Range: 160 km | Hit-to-kill ABM | AN/MPQ-65A radar | 16 MSE interceptors per launcher | TBM, cruise missile, aircraft",
                        Description = "Forward-deployed to Kuwait, Saudi Arabia, UAE, Qatar. PAC-3 MSE provides ABM capability. Proven against Houthi ballistic missiles.",
                        BaseLat = 29.38, BaseLon = 47.99, BaseLocation = "Camp Arifjan, Kuwait", ImageIcon = "🛡" },
                }
            },

            // ────────────────────────────────────────
            //  SAUDI ARABIA
            // ────────────────────────────────────────
            new ForceComposition
            {
                Country = "Saudi Arabia", FlagEmoji = "🇸🇦",
                TotalPersonnel = 257_000, ActivePersonnel = 227_000,
                ReservePersonnel = 0, ParamilitaryPersonnel = 100_000,
                DefenseBudgetBillions = 75.8, GlobalFirepowerRank = 22,
                Notes = "Highest per-capita defense spending. Premium US/UK equipment. Leading Operation Decisive Storm (Yemen). National Guard 100K+.",
                Equipment = new()
                {
                    new() { Id = "SA-F15SA", Name = "F-15SA Advanced Eagle", Designation = "F-15SA", Country = "Saudi Arabia", Operator = "RSAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 84, QuantityActive = 78, Manufacturer = "Boeing", Origin = "United States",
                        YearIntroduced = 2016, ThreatRating = SeverityLevel.High,
                        Specs = "Max speed: Mach 2.5 | AN/APG-63(V)3 AESA | JDAM, Paveway IV, Harpoon, SLAM-ER | DWS, DEWS | Most advanced Eagle variant",
                        Description = "Most advanced F-15 variant ever built. Saudi-specific configuration exceeds F-15E in several areas. Primary RSAF strike/air superiority platform.",
                        BaseLat = 24.71, BaseLon = 46.67, BaseLocation = "RSAF bases nationwide", ImageIcon = "✈" },
                    new() { Id = "SA-TYPHOON", Name = "Eurofighter Typhoon", Designation = "Typhoon Tranche 2/3", Country = "Saudi Arabia", Operator = "RSAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 72, QuantityActive = 65, Manufacturer = "Eurofighter", Origin = "UK/Germany/Italy/Spain",
                        YearIntroduced = 2009, ThreatRating = SeverityLevel.High,
                        Specs = "Max speed: Mach 2.0 | CAPTOR-M radar | Meteor BVR | Paveway, Brimstone, Storm Shadow | Extensive A2A and A2G capability",
                        Description = "Twin-engine multirole fighter. Extensively used in Yemen campaign. Storm Shadow cruise missile capability for deep strike.",
                        BaseLat = 24.71, BaseLon = 46.67, BaseLocation = "King Fahd AB", ImageIcon = "✈" },
                    new() { Id = "SA-ABRAMS", Name = "M1A2S Abrams", Designation = "M1A2S", Country = "Saudi Arabia", Operator = "RSLF",
                        Domain = EquipmentDomain.GroundForces, Role = EquipmentRole.MBT,
                        Quantity = 400, QuantityActive = 350, Manufacturer = "GDLS", Origin = "United States",
                        YearIntroduced = 2014, ThreatRating = SeverityLevel.High,
                        Specs = "120mm M256 smoothbore | Chobham composite + DU armor | 1,500 hp AGT-1500 | Depleted uranium sabot rounds | Thermal sights",
                        Description = "Saudi-specific M1A2 with SEP-equivalent upgrades. Used in Yemen border operations. Some losses to Houthi ATGMs. 400 in service.",
                        BaseLat = 17.61, BaseLon = 44.22, BaseLocation = "Yemen border / central", ImageIcon = "🪖" },
                }
            },

            // ────────────────────────────────────────
            //  TURKEY
            // ────────────────────────────────────────
            new ForceComposition
            {
                Country = "Turkey", FlagEmoji = "🇹🇷",
                TotalPersonnel = 775_000, ActivePersonnel = 425_000,
                ReservePersonnel = 200_000, ParamilitaryPersonnel = 150_000,
                DefenseBudgetBillions = 10.6, GlobalFirepowerRank = 8,
                Notes = "NATO's 2nd largest army. Rapidly growing defense industry (Bayraktar TB2, KAAN 5th-gen, Togg). Cross-border ops in Syria/Iraq.",
                Equipment = new()
                {
                    new() { Id = "TR-F16V", Name = "F-16C/D Block 50+ (Viper upgrade)", Designation = "F-16V", Country = "Turkey", Operator = "TurAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 245, QuantityActive = 220, Manufacturer = "TAI/Lockheed Martin", Origin = "United States/Turkey",
                        YearIntroduced = 1987, ThreatRating = SeverityLevel.High,
                        Specs = "Block 50+ with Viper upgrade | AN/APG-83 AESA | AIM-120C-7 AMRAAM | SOM cruise missile | Sniper ATP | Link-16",
                        Description = "245 F-16s form TurAF backbone — largest F-16 fleet outside USAF. 79 receiving Viper upgrade with AESA radar. SOM indigenous cruise missile integration.",
                        BaseLat = 37.09, BaseLon = 37.00, BaseLocation = "Incirlik / Diyarbakir", ImageIcon = "✈" },
                    new() { Id = "TR-KAAN", Name = "TAI KAAN (TF-X)", Designation = "KAAN", Country = "Turkey", Operator = "TurAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.AirSuperiority,
                        Quantity = 2, QuantityActive = 0, Manufacturer = "TAI", Origin = "Turkey",
                        Status = EquipmentStatus.InMaintenance,
                        YearIntroduced = 2028, ThreatRating = SeverityLevel.High,
                        Specs = "5th-gen stealth | Twin-engine | Internal weapons bay | AESA radar | Supercruise planned | First flight Feb 2024",
                        Description = "Turkey's indigenous 5th-gen stealth fighter. First flight Feb 2024. Designed to replace F-16. IOC target 2028-2029. Response to F-35 program ejection.",
                        BaseLat = 39.93, BaseLon = 32.86, BaseLocation = "Ankara (test program)", ImageIcon = "🔴✈" },
                    new() { Id = "TR-TB2", Name = "Bayraktar TB2", Designation = "TB2", Country = "Turkey", Operator = "TurAF/TSK",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.UCAV,
                        Quantity = 250, QuantityActive = 200, Manufacturer = "Baykar", Origin = "Turkey",
                        YearIntroduced = 2015, ThreatRating = SeverityLevel.High,
                        Specs = "Endurance: 27 hrs | Ceiling: 25,000 ft | MAM-L/C smart munitions | ASELSAN CATS EO/IR | Cost: ~$5M",
                        Description = "Game-changing UCAV proven in Libya, Syria, Karabakh, Ukraine. Destroyed Russian Pantsir SAMs. Exported 30+ countries. MAM-L laser-guided munition.",
                        BaseLat = 37.09, BaseLon = 37.00, BaseLocation = "Multiple bases / Syria ops", ImageIcon = "🛩" },
                    new() { Id = "TR-AKINCI", Name = "Bayraktar Akıncı", Designation = "Akıncı", Country = "Turkey", Operator = "TurAF",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.UCAV,
                        Quantity = 18, QuantityActive = 14, Manufacturer = "Baykar", Origin = "Turkey",
                        YearIntroduced = 2021, ThreatRating = SeverityLevel.Critical,
                        Specs = "Endurance: 24 hrs | Ceiling: 40,000 ft | AESA radar | Payload: 1,350 kg | SOM-J cruise missile | AI-assisted targeting",
                        Description = "Heavy UCAV with AESA radar and cruise missile capability. Can carry SOM-J standoff missile. Air-to-air missile integration planned. Most capable Turkish UAS.",
                        BaseLat = 39.93, BaseLon = 32.86, BaseLocation = "Various", ImageIcon = "🛩" },
                    new() { Id = "TR-S400", Name = "S-400 Triumf", Designation = "S-400", Country = "Turkey", Operator = "TSK",
                        Domain = EquipmentDomain.AirDefense, Role = EquipmentRole.SAM,
                        Quantity = 2, QuantityActive = 0, Manufacturer = "Almaz-Antey", Origin = "Russia",
                        Status = EquipmentStatus.Reserve,
                        YearIntroduced = 2019, ThreatRating = SeverityLevel.Critical,
                        Specs = "Range: 400 km | Altitude: 30 km | 4 missile types | 91N6E radar | Controversial NATO purchase — caused F-35 ejection",
                        Description = "Russian-supplied long-range SAM. Controversial — led to Turkey's removal from F-35 program. Reportedly not activated under US pressure. 2 batteries delivered.",
                        BaseLat = 39.93, BaseLon = 32.86, BaseLocation = "Ankara (stored)", ImageIcon = "🛡" },
                }
            },

            // ────────────────────────────────────────
            //  HOUTHI (ANSAR ALLAH)
            // ────────────────────────────────────────
            new ForceComposition
            {
                Country = "Houthis (Yemen)", FlagEmoji = "🇾🇪",
                TotalPersonnel = 150_000, ActivePersonnel = 100_000,
                ReservePersonnel = 50_000, ParamilitaryPersonnel = 0,
                DefenseBudgetBillions = 0.5, GlobalFirepowerRank = 0,
                Notes = "Iranian-backed militia controlling northern Yemen. Significant anti-ship and ballistic missile capability threatening Red Sea shipping.",
                Equipment = new()
                {
                    new() { Id = "HO-TOUFAN", Name = "Toufan Ballistic Missile", Designation = "Toufan (Qiam-variant)", Country = "Houthis (Yemen)", Operator = "Ansar Allah",
                        Domain = EquipmentDomain.Missiles, Role = EquipmentRole.BallisticMissile,
                        Quantity = 50, QuantityActive = 40, Manufacturer = "Iranian-supplied", Origin = "Iran",
                        YearIntroduced = 2017, ThreatRating = SeverityLevel.Critical,
                        Specs = "Range: 800 km | Warhead: 750 kg | Liquid fuel | GPS/INS guidance | Launched at Riyadh, UAE",
                        Description = "Iranian Qiam-1 derivative. Has been fired at Saudi Arabia and UAE. Intercepted by Patriot/THAAD. Demonstrates Iranian missile proliferation.",
                        BaseLat = 16.94, BaseLon = 43.76, BaseLocation = "Sa'ada / mobile", ImageIcon = "🚀" },
                    new() { Id = "HO-ASHM", Name = "Anti-Ship Cruise Missiles", Designation = "C-802/Noor variants", Country = "Houthis (Yemen)", Operator = "Ansar Allah",
                        Domain = EquipmentDomain.Missiles, Role = EquipmentRole.AntiShipMissile,
                        Quantity = 100, QuantityActive = 80, Manufacturer = "Iranian-supplied", Origin = "Iran/China",
                        YearIntroduced = 2016, ThreatRating = SeverityLevel.Critical,
                        Specs = "Range: 120-300 km | Sea-skimming | Active radar seeker | Hit USS Mason (attempted, intercepted) | Struck commercial vessels",
                        Description = "Iranian-supplied anti-ship missiles threatening Red Sea/Bab el-Mandeb shipping. Multiple successful hits on commercial vessels since Oct 2023.",
                        BaseLat = 14.80, BaseLon = 42.95, BaseLocation = "Hodeidah coast", ImageIcon = "🎯" },
                    new() { Id = "HO-DRONE", Name = "Samad/Qasef Attack Drones", Designation = "Various", Country = "Houthis (Yemen)", Operator = "Ansar Allah",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.LoiteringMunition,
                        Quantity = 500, QuantityActive = 400, Manufacturer = "Iranian-supplied", Origin = "Iran",
                        YearIntroduced = 2017, ThreatRating = SeverityLevel.High,
                        Specs = "Samad-3: 1,500 km range | Qasef-2K: 150 km | Shahed-136 variants | Swarm capable",
                        Description = "Large inventory of Iranian-supplied one-way attack drones. Used in Aramco attacks (Sep 2019), Red Sea shipping attacks. GPS-guided, difficult to intercept.",
                        BaseLat = 15.37, BaseLon = 44.19, BaseLocation = "Sanaa / northwestern Yemen", ImageIcon = "🛩" },
                }
            },

            // ────────────────────────────────────────
            //  HEZBOLLAH
            // ────────────────────────────────────────
            new ForceComposition
            {
                Country = "Hezbollah (Lebanon)", FlagEmoji = "🇱🇧",
                TotalPersonnel = 100_000, ActivePersonnel = 30_000,
                ReservePersonnel = 20_000, ParamilitaryPersonnel = 50_000,
                DefenseBudgetBillions = 0.7, GlobalFirepowerRank = 0,
                Notes = "Most heavily armed non-state actor. 130,000+ rockets/missiles pointed at Israel. IRGC-supplied PGMs. Battle-hardened in Syria.",
                Equipment = new()
                {
                    new() { Id = "HZ-ROCKET", Name = "Rocket Arsenal (122-302mm)", Designation = "Various", Country = "Hezbollah (Lebanon)", Operator = "Hezbollah",
                        Domain = EquipmentDomain.Missiles, Role = EquipmentRole.BallisticMissile,
                        Quantity = 130000, QuantityActive = 100000, Manufacturer = "Various", Origin = "Iran/Syria",
                        YearIntroduced = 2000, ThreatRating = SeverityLevel.Critical,
                        Specs = "122mm Grad: 20 km | 220mm Fajr-3: 43 km | 302mm Khaibar-1: 100 km | Zelzal-2: 200 km | Most unguided",
                        Description = "130,000+ rockets of various calibers. Can overwhelm Iron Dome through saturation. Range covers all of northern Israel. Includes Iranian Fajr, Zelzal, Falaq.",
                        BaseLat = 33.27, BaseLon = 35.46, BaseLocation = "Southern Lebanon / Bekaa", ImageIcon = "🚀" },
                    new() { Id = "HZ-PGM", Name = "Precision-Guided Munitions", Designation = "Fateh-110/M-600", Country = "Hezbollah (Lebanon)", Operator = "Hezbollah",
                        Domain = EquipmentDomain.Missiles, Role = EquipmentRole.BallisticMissile,
                        Quantity = 2000, QuantityActive = 1500, Manufacturer = "Iran", Origin = "Iran",
                        YearIntroduced = 2014, ThreatRating = SeverityLevel.Critical,
                        Specs = "Fateh-110: 300 km, ~500 kg warhead, GPS/INS, CEP 10m | M-600: 300 km, 500 kg | Can strike any point in Israel with precision",
                        Description = "Israel's #1 strategic concern. 2,000+ PGMs capable of striking strategic infrastructure (refineries, ports, air bases, Dimona) with high accuracy.",
                        BaseLat = 33.85, BaseLon = 36.10, BaseLocation = "Bekaa Valley (underground)", ImageIcon = "🎯" },
                    new() { Id = "HZ-ATGM", Name = "ATGM Arsenal", Designation = "Kornet/Konkurs/Almas", Country = "Hezbollah (Lebanon)", Operator = "Hezbollah",
                        Domain = EquipmentDomain.GroundForces, Role = EquipmentRole.SpecOps,
                        Quantity = 5000, QuantityActive = 4000, Manufacturer = "Russia/Iran", Origin = "Russia/Iran",
                        YearIntroduced = 2006, ThreatRating = SeverityLevel.High,
                        Specs = "Kornet-E: 5,500m range, tandem HEAT | Konkurs: 4,000m | Almas (Iranian Spike NLOS clone): 8 km | Tharallah: laser-guided",
                        Description = "Massive ATGM inventory. Kornet destroyed multiple Merkava tanks in 2006. Iranian Almas = reverse-engineered Israeli Spike NLOS. Tunnel complexes.",
                        BaseLat = 33.27, BaseLon = 35.46, BaseLocation = "Southern Lebanon", ImageIcon = "🎯" },
                    new() { Id = "HZ-DRONE", Name = "Drone Fleet", Designation = "Ababil/Mirsad/Iranian types", Country = "Hezbollah (Lebanon)", Operator = "Hezbollah",
                        Domain = EquipmentDomain.Drones, Role = EquipmentRole.UCAV,
                        Quantity = 2000, QuantityActive = 1500, Manufacturer = "Iran", Origin = "Iran",
                        YearIntroduced = 2006, ThreatRating = SeverityLevel.High,
                        Specs = "Ababil-T: 150 km | Mirsad: recon | Shahed-101/136 variants | Swarm-capable | EO/IR payloads",
                        Description = "Extensive Iranian-supplied drone fleet. ISR and attack variants. Used to probe Israeli air defenses. Shahed-type loitering munitions for strikes.",
                        BaseLat = 33.89, BaseLon = 35.50, BaseLocation = "Lebanon / Syria border", ImageIcon = "🛩" },
                }
            },

            // ────────────────────────────────────────
            //  UAE
            // ────────────────────────────────────────
            new ForceComposition
            {
                Country = "UAE", FlagEmoji = "🇦🇪",
                TotalPersonnel = 63_000, ActivePersonnel = 63_000,
                ReservePersonnel = 0, ParamilitaryPersonnel = 0,
                DefenseBudgetBillions = 22.8, GlobalFirepowerRank = 45,
                Notes = "Small but extremely well-equipped. F-16E/F Block 60 Desert Falcon. Leclerc MBTs. Active in Yemen, Libya.",
                Equipment = new()
                {
                    new() { Id = "AE-F16E", Name = "F-16E/F Desert Falcon", Designation = "F-16 Block 60", Country = "UAE", Operator = "UAEAF",
                        Domain = EquipmentDomain.AirForce, Role = EquipmentRole.MultiRole,
                        Quantity = 79, QuantityActive = 75, Manufacturer = "Lockheed Martin", Origin = "United States",
                        YearIntroduced = 2004, ThreatRating = SeverityLevel.High,
                        Specs = "AN/APG-80 AESA (first F-16 AESA) | CFTs | FLIR | Range: 3,200+ km | AIM-120C, AIM-9X, JDAM, JSOW, Harpoon",
                        Description = "Most advanced F-16 variant. First to feature AESA radar. Block 60 avionics exceed many F-15s. Extensively used in Yemen operations.",
                        BaseLat = 24.25, BaseLon = 54.55, BaseLocation = "Al Dhafra AB", ImageIcon = "✈" },
                    new() { Id = "AE-LECLERC", Name = "Leclerc MBT", Designation = "Leclerc Tropicalisé", Country = "UAE", Operator = "UAE Army",
                        Domain = EquipmentDomain.GroundForces, Role = EquipmentRole.MBT,
                        Quantity = 388, QuantityActive = 340, Manufacturer = "Nexter/KNDS", Origin = "France",
                        YearIntroduced = 1996, ThreatRating = SeverityLevel.Medium,
                        Specs = "120mm smoothbore | FINDERS FCS | Tropicalized cooling | Autoloader | 1,500 hp | Fastest NATO MBT",
                        Description = "French-built MBT adapted for desert operations. Deployed to Yemen — first Leclerc combat use. Several losses to ATGMs. Autoloader enables 3-man crew.",
                        BaseLat = 24.45, BaseLon = 54.65, BaseLocation = "Abu Dhabi garrisons", ImageIcon = "🪖" },
                }
            },
        };

        // ══════════════════════════════════════════════════════
        //  LIVE EQUIPMENT INTELLIGENCE FEEDS
        // ══════════════════════════════════════════════════════

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
        private readonly List<EquipmentIntelItem> _intelFeed = new();
        private DateTime _lastIntelScan = DateTime.MinValue;

        private static readonly List<(string Name, string Url)> _defenseFeeds = new()
        {
            ("Defense News", "https://www.defensenews.com/arc/outboundfeeds/rss/category/air/?outputType=xml"),
            ("Janes", "https://www.janes.com/feeds/news"),
            ("The War Zone", "https://www.thedrive.com/the-war-zone/feed"),
            ("Defense One", "https://www.defenseone.com/rss/"),
            ("Breaking Defense", "https://breakingdefense.com/feed/"),
            ("Military Times", "https://www.militarytimes.com/arc/outboundfeeds/rss/?outputType=xml"),
            ("Naval News", "https://www.navalnews.com/feed/"),
            ("Air Force Magazine", "https://www.airandspaceforces.com/feed/"),
            ("Army Recognition", "https://www.armyrecognition.com/rss"),
            ("SIPRI News", "https://www.sipri.org/news/rss.xml"),
            ("Defense Blog", "https://defence-blog.com/feed/"),
            ("Global Defense Corp", "https://www.globaldefensecorp.com/feed/"),
        };

        /// <summary>
        /// Scans defense news RSS feeds for equipment-related articles.
        /// Tracks new procurement, deliveries, deployments, and capability upgrades.
        /// </summary>
        public async Task<List<EquipmentIntelItem>> FetchEquipmentIntelAsync()
        {
            var results = new List<EquipmentIntelItem>();
            var keywords = new[] {
                "delivery", "delivered", "procurement", "contract", "order", "purchase",
                "f-35", "f-16", "su-35", "patriot", "thaad", "iron dome", "s-400", "s-300",
                "drone", "uav", "ucav", "shahed", "bayraktar", "switchblade",
                "tank", "abrams", "leopard", "merkava", "t-90",
                "missile", "hypersonic", "ballistic", "cruise",
                "corvette", "frigate", "submarine", "destroyer", "carrier",
                "air defense", "radar", "satellite", "munition",
                "iran", "israel", "saudi", "turkey", "egypt", "uae", "qatar",
                "irgc", "iriaf", "idf", "usaf", "centcom"
            };

            foreach (var (name, url) in _defenseFeeds)
            {
                try
                {
                    var xml = await _http.GetStringAsync(url);
                    var doc = XDocument.Parse(xml);
                    var items = doc.Descendants("item").Take(15);

                    foreach (var item in items)
                    {
                        var title = item.Element("title")?.Value ?? "";
                        var desc = item.Element("description")?.Value ?? "";
                        var link = item.Element("link")?.Value ?? "";
                        var pubDate = item.Element("pubDate")?.Value ?? "";
                        var combined = (title + " " + desc).ToLowerInvariant();

                        // Only include articles matching defense equipment keywords
                        if (keywords.Any(k => combined.Contains(k)))
                        {
                            DateTime.TryParse(pubDate, out var dt);
                            if (dt == default) dt = DateTime.UtcNow;

                            results.Add(new EquipmentIntelItem
                            {
                                Title = title.Length > 120 ? title[..120] + "..." : title,
                                Source = name,
                                Url = link,
                                Summary = desc.Length > 300 ? desc[..300] + "..." : desc,
                                Timestamp = dt,
                                IsNew = dt > _lastIntelScan
                            });
                        }
                    }
                }
                catch { /* Feed unavailable — continue to next */ }
            }

            _lastIntelScan = DateTime.UtcNow;
            _intelFeed.Clear();
            _intelFeed.AddRange(results.OrderByDescending(r => r.Timestamp).Take(100));
            return _intelFeed.ToList();
        }

        public List<EquipmentIntelItem> GetLatestIntel() => _intelFeed.ToList();

        // ══════════════════════════════════════════════════════
        //  PUBLIC ACCESSORS
        // ══════════════════════════════════════════════════════

        public List<ForceComposition> GetAllForces() => _forces.ToList();

        public ForceComposition? GetForceByCountry(string country) =>
            _forces.FirstOrDefault(f => f.Country.Contains(country, StringComparison.OrdinalIgnoreCase));

        public List<MilitaryEquipment> GetAllEquipment() =>
            _forces.SelectMany(f => f.Equipment).ToList();

        public List<MilitaryEquipment> GetEquipmentByCountry(string country) =>
            _forces.Where(f => f.Country.Contains(country, StringComparison.OrdinalIgnoreCase))
                   .SelectMany(f => f.Equipment).ToList();

        public List<MilitaryEquipment> GetEquipmentByDomain(EquipmentDomain domain) =>
            _forces.SelectMany(f => f.Equipment).Where(e => e.Domain == domain).ToList();

        public List<MilitaryEquipment> GetEquipmentByRole(EquipmentRole role) =>
            _forces.SelectMany(f => f.Equipment).Where(e => e.Role == role).ToList();

        public List<MilitaryEquipment> GetCriticalAssets() =>
            _forces.SelectMany(f => f.Equipment).Where(e => e.ThreatRating >= SeverityLevel.High).ToList();

        public List<MilitaryEquipment> GetEquipmentWithLocations() =>
            _forces.SelectMany(f => f.Equipment).Where(e => e.BaseLat.HasValue && e.BaseLon.HasValue).ToList();

        public int GetTotalEquipmentCount() =>
            _forces.SelectMany(f => f.Equipment).Sum(e => e.Quantity);

        public int GetTotalActiveCount() =>
            _forces.SelectMany(f => f.Equipment).Sum(e => e.QuantityActive);

        /// <summary>Builds equipment deployment data for the map JavaScript.</summary>
        public string BuildEquipmentMapData()
        {
            var items = GetEquipmentWithLocations();
            var entries = items.Select(e =>
            {
                var color = e.ThreatRating switch
                {
                    SeverityLevel.Critical => "#EE3333",
                    SeverityLevel.High => "#FF8833",
                    SeverityLevel.Medium => "#DDCC33",
                    _ => "#6A6A80"
                };
                var domainIcon = e.Domain switch
                {
                    EquipmentDomain.AirForce => "✈",
                    EquipmentDomain.Navy => "🚢",
                    EquipmentDomain.GroundForces => "🪖",
                    EquipmentDomain.Drones => "🛩",
                    EquipmentDomain.AirDefense => "🛡",
                    EquipmentDomain.Missiles => "🚀",
                    _ => "⚙"
                };
                var name = (e.Name ?? "").Replace("'", "\\'").Replace("\"", "\\\"");
                var desc = (e.Description ?? "").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ");
                var specs = (e.Specs ?? "").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", " ").Replace("|", "│");
                var country = (e.Country ?? "").Replace("'", "\\'");
                var baseLoc = (e.BaseLocation ?? "").Replace("'", "\\'");
                var designation = (e.Designation ?? "").Replace("'", "\\'");
                var op = (e.Operator ?? "").Replace("'", "\\'");

                return $"{{lat:{e.BaseLat},lon:{e.BaseLon},name:'{name}',designation:'{designation}'," +
                       $"country:'{country}',operator:'{op}',domain:'{e.Domain}',icon:'{domainIcon}'," +
                       $"color:'{color}',qty:{e.Quantity},active:{e.QuantityActive}," +
                       $"base:'{baseLoc}',specs:'{specs}',desc:'{desc}',sev:{(int)e.ThreatRating}}}";
            });
            return string.Join(",", entries);
        }

        /// <summary>Gets summary statistics for the status bar / dashboard.</summary>
        public (int Countries, int TotalTypes, int TotalActive, int Drones, int Aircraft, int Naval, int Missiles) GetStats()
        {
            var all = GetAllEquipment();
            return (
                Countries: _forces.Count,
                TotalTypes: all.Count,
                TotalActive: all.Sum(e => e.QuantityActive),
                Drones: all.Where(e => e.Domain == EquipmentDomain.Drones).Sum(e => e.QuantityActive),
                Aircraft: all.Where(e => e.Domain == EquipmentDomain.AirForce).Sum(e => e.QuantityActive),
                Naval: all.Where(e => e.Domain == EquipmentDomain.Navy).Sum(e => e.QuantityActive),
                Missiles: all.Where(e => e.Domain == EquipmentDomain.Missiles).Sum(e => e.QuantityActive)
            );
        }
    }
}
