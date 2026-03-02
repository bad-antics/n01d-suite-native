using System;
using System.Collections.Generic;
using System.Linq;
using N01D.Overwatch.Models;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Provides solar and lunar eclipse data, visibility predictions, and countdown timers
    /// for eclipses relevant to the Middle East theater of operations.
    /// Eclipse paths can create optical/sensor disruption windows — militarily significant.
    /// </summary>
    public class EclipseService
    {
        /// <summary>
        /// Known upcoming eclipse events 2024–2028 with Middle East relevance.
        /// Data sourced from NASA Eclipse Predictions (Fred Espenak).
        /// </summary>
        private static readonly List<EclipseEvent> _eclipseDatabase = new()
        {
            // ══════════════════════════════════════
            //  SOLAR ECLIPSES
            // ══════════════════════════════════════
            new EclipseEvent
            {
                Id = "SE-2025-03-29",
                Name = "Partial Solar Eclipse",
                Type = EclipseType.SolarPartial,
                Date = new DateTime(2025, 3, 29, 10, 48, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2025, 3, 29, 10, 48, 0, DateTimeKind.Utc),
                DurationMinutes = 180,
                MaxMagnitude = 0.938,
                Description = "Partial solar eclipse visible across Europe, North Africa, and the Middle East. Peak coverage ~93.8% in northwest Iran.",
                MilitarySignificance = "Reduced solar illumination may impact satellite imagery quality and solar-powered sensor systems. Brief window of degraded optical ISR coverage.",
                VisibilityRegion = "Europe, N. Africa, Middle East, W. Asia",
                IsVisibleFromME = true,
                MECoveragePercent = 65,
                PathCoordinates = new() {
                    (35.0, 30.0), (36.0, 35.0), (37.0, 40.0), (38.0, 45.0), (39.0, 50.0), (40.0, 55.0)
                }
            },
            new EclipseEvent
            {
                Id = "SE-2025-09-21",
                Name = "Partial Solar Eclipse",
                Type = EclipseType.SolarPartial,
                Date = new DateTime(2025, 9, 21, 19, 42, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2025, 9, 21, 19, 42, 0, DateTimeKind.Utc),
                DurationMinutes = 210,
                MaxMagnitude = 0.855,
                Description = "Partial solar eclipse visible from southern hemisphere. Minimal Middle East impact — low on horizon at sunset.",
                MilitarySignificance = "Negligible operational impact for ME theater. Monitor for sun-glare complications at low solar angles.",
                VisibilityRegion = "S. Pacific, New Zealand, Antarctica",
                IsVisibleFromME = false,
                MECoveragePercent = 0,
                PathCoordinates = new() {
                    (-40.0, 160.0), (-45.0, 170.0), (-50.0, 180.0)
                }
            },
            new EclipseEvent
            {
                Id = "SE-2026-02-17",
                Name = "Annular Solar Eclipse",
                Type = EclipseType.SolarAnnular,
                Date = new DateTime(2026, 2, 17, 12, 13, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2026, 2, 17, 12, 13, 0, DateTimeKind.Utc),
                DurationMinutes = 260,
                MaxMagnitude = 0.963,
                Description = "Annular 'Ring of Fire' eclipse crossing Antarctica and South America. Not visible from Middle East.",
                MilitarySignificance = "No ME operational impact.",
                VisibilityRegion = "Antarctica, S. America, W. Africa",
                IsVisibleFromME = false,
                MECoveragePercent = 0,
                PathCoordinates = new() {
                    (-70.0, -30.0), (-60.0, -40.0), (-50.0, -50.0)
                }
            },
            new EclipseEvent
            {
                Id = "SE-2026-08-12",
                Name = "Total Solar Eclipse",
                Type = EclipseType.SolarTotal,
                Date = new DateTime(2026, 8, 12, 17, 46, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2026, 8, 12, 17, 46, 0, DateTimeKind.Utc),
                DurationMinutes = 300,
                MaxMagnitude = 1.039,
                Description = "Total solar eclipse crossing Arctic, Greenland, Iceland, Spain, and North Africa. Partial visibility across the entire Middle East.",
                MilitarySignificance = "SIGNIFICANT — Partial totality (~40-60%) over eastern Mediterranean and Levant. Degraded satellite optical windows. Night-vision equipment may auto-activate. Potential for cover/concealment operations during darkened period.",
                VisibilityRegion = "Arctic, Greenland, Iceland, W. Europe, N. Africa, Middle East",
                IsVisibleFromME = true,
                MECoveragePercent = 55,
                PathCoordinates = new() {
                    (72.0, -40.0), (65.0, -20.0), (43.0, -4.0), (37.0, 2.0), (33.0, 8.0), (30.0, 15.0), (28.0, 25.0)
                }
            },
            new EclipseEvent
            {
                Id = "SE-2027-08-02",
                Name = "Total Solar Eclipse",
                Type = EclipseType.SolarTotal,
                Date = new DateTime(2027, 8, 2, 10, 7, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2027, 8, 2, 10, 7, 0, DateTimeKind.Utc),
                DurationMinutes = 300,
                MaxMagnitude = 1.079,
                Description = "MAJOR — Total solar eclipse crossing North Africa directly through Egypt, Saudi Arabia, Yemen, and Somalia. Totality path passes through active conflict zones.",
                MilitarySignificance = "CRITICAL — Totality crosses Strait of Bab el-Mandeb, Red Sea, and Arabian Peninsula. Complete optical darkness for 6min+ along path. Major ISR blackout window. GPS scintillation possible. Potential cover for naval/air operations. All optical surveillance degraded.",
                VisibilityRegion = "Morocco, Algeria, Libya, Egypt, Saudi Arabia, Yemen, Somalia, Indian Ocean",
                IsVisibleFromME = true,
                MECoveragePercent = 90,
                PathCoordinates = new() {
                    (36.0, -9.0), (34.0, 0.0), (32.0, 8.0), (30.5, 15.0), (29.0, 25.0),
                    (27.5, 30.0), (25.0, 35.0), (22.0, 40.0), (18.0, 43.0),
                    (14.0, 46.0), (10.0, 50.0), (5.0, 55.0)
                }
            },

            // ══════════════════════════════════════
            //  LUNAR ECLIPSES
            // ══════════════════════════════════════
            new EclipseEvent
            {
                Id = "LE-2025-03-14",
                Name = "Total Lunar Eclipse",
                Type = EclipseType.LunarTotal,
                Date = new DateTime(2025, 3, 14, 6, 58, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2025, 3, 14, 6, 58, 0, DateTimeKind.Utc),
                DurationMinutes = 340,
                MaxMagnitude = 1.178,
                Description = "Total lunar eclipse ('Blood Moon'). Visible at moonset from Middle East. Best viewed from Americas.",
                MilitarySignificance = "Reduced lunar illumination during nighttime hours. Enhanced darkness benefits covert operations. Night vision advantage shifts.",
                VisibilityRegion = "Americas, W. Europe, W. Africa",
                IsVisibleFromME = true,
                MECoveragePercent = 25,
                PathCoordinates = new()
            },
            new EclipseEvent
            {
                Id = "LE-2025-09-07",
                Name = "Total Lunar Eclipse",
                Type = EclipseType.LunarTotal,
                Date = new DateTime(2025, 9, 7, 18, 11, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2025, 9, 7, 18, 11, 0, DateTimeKind.Utc),
                DurationMinutes = 320,
                MaxMagnitude = 1.367,
                Description = "Total lunar eclipse visible across Europe, Africa, and Middle East. Deep totality — very dark Blood Moon.",
                MilitarySignificance = "SIGNIFICANT — Full Blood Moon over ME theater. ~2+ hours of reduced lunar illumination. Enhanced concealment for ground/naval operations. Night vision advantage. Monitor for correlated SIGINT changes.",
                VisibilityRegion = "Europe, Africa, Middle East, Asia",
                IsVisibleFromME = true,
                MECoveragePercent = 95,
                PathCoordinates = new()
            },
            new EclipseEvent
            {
                Id = "LE-2026-03-03",
                Name = "Total Lunar Eclipse",
                Type = EclipseType.LunarTotal,
                Date = new DateTime(2026, 3, 3, 11, 33, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2026, 3, 3, 11, 33, 0, DateTimeKind.Utc),
                DurationMinutes = 330,
                MaxMagnitude = 1.151,
                Description = "Total lunar eclipse. Daytime in Middle East — not visible locally. Visible from East Asia, Pacific, Americas.",
                MilitarySignificance = "No ME operational impact — occurs during local daytime.",
                VisibilityRegion = "E. Asia, Pacific, Americas",
                IsVisibleFromME = false,
                MECoveragePercent = 0,
                PathCoordinates = new()
            },
            new EclipseEvent
            {
                Id = "LE-2026-08-28",
                Name = "Partial Lunar Eclipse",
                Type = EclipseType.LunarPartial,
                Date = new DateTime(2026, 8, 28, 4, 13, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2026, 8, 28, 4, 13, 0, DateTimeKind.Utc),
                DurationMinutes = 210,
                MaxMagnitude = 0.930,
                Description = "Deep partial lunar eclipse visible across Middle East at dawn. Nearly total coverage of the Moon's disk.",
                MilitarySignificance = "Moderate — reduced ambient nighttime illumination in final hours before dawn. Tactical advantage for pre-dawn operations.",
                VisibilityRegion = "E. Africa, Middle East, Asia, Pacific",
                IsVisibleFromME = true,
                MECoveragePercent = 70,
                PathCoordinates = new()
            },
            new EclipseEvent
            {
                Id = "LE-2028-01-12",
                Name = "Total Lunar Eclipse",
                Type = EclipseType.LunarTotal,
                Date = new DateTime(2028, 1, 12, 4, 13, 0, DateTimeKind.Utc),
                PeakTime = new DateTime(2028, 1, 12, 4, 13, 0, DateTimeKind.Utc),
                DurationMinutes = 340,
                MaxMagnitude = 1.241,
                Description = "Total lunar eclipse visible from Middle East in pre-dawn hours. Deep totality with dark Blood Moon.",
                MilitarySignificance = "Moderate — reduced nighttime illumination. Enhanced concealment window for pre-dawn military operations.",
                VisibilityRegion = "Americas, Europe, Africa, Middle East",
                IsVisibleFromME = true,
                MECoveragePercent = 80,
                PathCoordinates = new()
            }
        };

        /// <summary>Gets all known eclipse events.</summary>
        public List<EclipseEvent> GetAllEclipses() => _eclipseDatabase
            .OrderBy(e => e.Date)
            .ToList();

        /// <summary>Gets eclipses visible from the Middle East.</summary>
        public List<EclipseEvent> GetMEVisibleEclipses() => _eclipseDatabase
            .Where(e => e.IsVisibleFromME)
            .OrderBy(e => e.Date)
            .ToList();

        /// <summary>Gets upcoming eclipses (future only).</summary>
        public List<EclipseEvent> GetUpcomingEclipses() => _eclipseDatabase
            .Where(e => e.Date > DateTime.UtcNow)
            .OrderBy(e => e.Date)
            .ToList();

        /// <summary>Gets the next upcoming eclipse visible from the Middle East.</summary>
        public EclipseEvent? GetNextMEEclipse() => _eclipseDatabase
            .Where(e => e.Date > DateTime.UtcNow && e.IsVisibleFromME)
            .OrderBy(e => e.Date)
            .FirstOrDefault();

        /// <summary>Gets the countdown to the next ME-visible eclipse.</summary>
        public (EclipseEvent? Eclipse, TimeSpan Countdown) GetNextEclipseCountdown()
        {
            var next = GetNextMEEclipse();
            if (next == null) return (null, TimeSpan.Zero);
            return (next, next.Date - DateTime.UtcNow);
        }

        /// <summary>Checks if an eclipse is currently in progress.</summary>
        public EclipseEvent? GetActiveEclipse()
        {
            var now = DateTime.UtcNow;
            return _eclipseDatabase.FirstOrDefault(e =>
                now >= e.Date.AddMinutes(-e.DurationMinutes / 2.0) &&
                now <= e.Date.AddMinutes(e.DurationMinutes / 2.0));
        }

        /// <summary>Converts an eclipse event to a ConflictEvent for the timeline.</summary>
        public ConflictEvent ToConflictEvent(EclipseEvent eclipse)
        {
            var isActive = GetActiveEclipse()?.Id == eclipse.Id;
            var countdown = eclipse.Date - DateTime.UtcNow;
            var status = isActive ? "⚡ IN PROGRESS"
                : countdown.TotalDays > 0 ? $"T-{countdown.Days}d {countdown.Hours}h"
                : "COMPLETED";

            var severity = eclipse.Type switch
            {
                EclipseType.SolarTotal when eclipse.MECoveragePercent > 50 => SeverityLevel.Critical,
                EclipseType.SolarTotal => SeverityLevel.High,
                EclipseType.SolarAnnular when eclipse.IsVisibleFromME => SeverityLevel.High,
                EclipseType.SolarPartial when eclipse.MECoveragePercent > 40 => SeverityLevel.Medium,
                EclipseType.LunarTotal when eclipse.IsVisibleFromME => SeverityLevel.Medium,
                _ => SeverityLevel.Low
            };

            if (isActive) severity = SeverityLevel.Critical;

            var icon = eclipse.Type switch
            {
                EclipseType.SolarTotal => "🌑",
                EclipseType.SolarAnnular => "🌗",
                EclipseType.SolarPartial => "🌘",
                EclipseType.LunarTotal => "🌕",
                EclipseType.LunarPartial => "🌔",
                _ => "🌙"
            };

            // Center of the ME as approximate location
            double? lat = eclipse.PathCoordinates.Count > 0 ? eclipse.PathCoordinates[eclipse.PathCoordinates.Count / 2].Lat : 28.0;
            double? lon = eclipse.PathCoordinates.Count > 0 ? eclipse.PathCoordinates[eclipse.PathCoordinates.Count / 2].Lon : 50.0;

            return new ConflictEvent
            {
                Id = eclipse.Id,
                Title = $"{icon} {eclipse.Name} — {status}",
                Summary = $"{eclipse.Description}\n{eclipse.MilitarySignificance}",
                Source = "NASA Eclipse Predictions",
                DataSource = DataSource.OSINT,
                Category = EventCategory.Intelligence,
                Severity = severity,
                Latitude = lat,
                Longitude = lon,
                Location = eclipse.VisibilityRegion,
                Timestamp = eclipse.Date,
                Tags = new List<string>
                {
                    "ECLIPSE",
                    eclipse.Type.ToString().ToUpperInvariant(),
                    eclipse.IsVisibleFromME ? "ME-VISIBLE" : "NOT-ME",
                    $"MAG:{eclipse.MaxMagnitude:F3}"
                }
            };
        }

        /// <summary>Gets eclipses with military significance for ME operations.</summary>
        public List<EclipseEvent> GetMilitarilySignificantEclipses() => _eclipseDatabase
            .Where(e => e.IsVisibleFromME && e.MECoveragePercent >= 40)
            .OrderBy(e => e.Date)
            .ToList();
    }
}
