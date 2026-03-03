using System;
using System.Collections.Generic;
using System.Linq;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Tracks wartime market impacts — how conflict events drive commodity prices,
    /// defense stocks, currency moves, shipping costs, and energy markets.
    /// Data is curated from open-source financial intelligence and historical
    /// conflict-market correlations.
    /// </summary>
    public class MarketImpactService
    {
        private readonly List<MarketImpact> _impacts = new();
        private readonly List<ConflictCommodity> _commodities = new();
        private readonly List<DefenseStock> _defenseStocks = new();
        private readonly List<MarketAlert> _alerts = new();
        private readonly List<SanctionImpact> _sanctions = new();
        private readonly List<TradeFeed> _feeds = new();

        public MarketImpactService()
        {
            InitializeImpacts();
            InitializeCommodities();
            InitializeDefenseStocks();
            InitializeAlerts();
            InitializeSanctions();
            InitializeFeeds();
        }

        // ═══════════════════════════════════════════
        //  CONFLICT-DRIVEN MARKET IMPACTS
        // ═══════════════════════════════════════════

        private void InitializeImpacts()
        {
            _impacts.AddRange(new[]
            {
                // ── Energy / Oil & Gas ──
                new MarketImpact
                {
                    Id = "IMP-001", Sector = MarketSector.Energy,
                    Title = "Strait of Hormuz Disruption Risk",
                    Trigger = "Iran-Israel escalation / IRGC naval operations",
                    Impact = "Crude oil +15-30% spike; Brent > $120/bbl; global shipping insurance surcharges 300%+",
                    Assets = "CL (WTI Crude), BZ (Brent), NG (Natural Gas), XOM, CVX, SLB, HAL, RIG",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Critical,
                    Region = "Persian Gulf",
                    Probability = "High",
                    TimeHorizon = "Immediate — 72hr spike, sustained 2-4 weeks",
                    HistoricalPrecedent = "2019 Abqaiq attack: Brent +15% in hours; 1990 Kuwait invasion: oil doubled in 3 months",
                    Latitude = 26.57, Longitude = 56.25
                },
                new MarketImpact
                {
                    Id = "IMP-002", Sector = MarketSector.Energy,
                    Title = "Saudi Aramco Infrastructure Threat",
                    Trigger = "Houthi drone/missile strikes on Saudi oil infrastructure",
                    Impact = "Crude +5-15% on successful strikes; Saudi Aramco (2222.SR) volatility; global refinery margins surge",
                    Assets = "2222.SR (Aramco), CL, BZ, VDE (Energy ETF), USO, XLE",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.High,
                    Region = "Saudi Arabia / Yemen",
                    Probability = "Medium-High",
                    TimeHorizon = "1-5 days spike, repair-dependent recovery",
                    HistoricalPrecedent = "Sept 2019 Abqaiq/Khurais: 5.7M bpd offline, oil +15% overnight",
                    Latitude = 25.94, Longitude = 49.69
                },
                new MarketImpact
                {
                    Id = "IMP-003", Sector = MarketSector.Energy,
                    Title = "Red Sea / Suez Canal Disruption",
                    Trigger = "Houthi anti-ship attacks in Bab el-Mandeb strait",
                    Impact = "Shipping costs +200-400%; European gas prices +20%; Suez transit revenue collapse; rerouting via Cape of Good Hope adds 10-15 days",
                    Assets = "NG (EU TTF Gas), BZ, ZIM, APMM.AS, HLAG.DE, DSV.CO, shipping futures",
                    Direction = MarketDirection.Bearish,
                    Severity = ImpactSeverity.Critical,
                    Region = "Red Sea / Gulf of Aden",
                    Probability = "Active — ongoing since 2023",
                    TimeHorizon = "Sustained disruption, months-long impact",
                    HistoricalPrecedent = "2024 Houthi campaign: 90% drop in Suez container traffic; shipping rates +300%",
                    Latitude = 12.58, Longitude = 43.33
                },
                new MarketImpact
                {
                    Id = "IMP-004", Sector = MarketSector.Energy,
                    Title = "Iraqi Kurdistan Pipeline Shutdown",
                    Trigger = "Turkey-PKK conflict / Iraqi federal-Kurdish disputes",
                    Impact = "Crude +3-5%; Kurdish oil stocks collapse; Turkey energy costs +10%",
                    Assets = "CL, DNO.OL, GKPRF, TUR (Turkey ETF)",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Medium,
                    Region = "Iraq / Turkey",
                    Probability = "Medium",
                    TimeHorizon = "2-8 weeks depending on political resolution",
                    HistoricalPrecedent = "2023 KRG pipeline shutdown: 450K bpd offline for months",
                    Latitude = 36.19, Longitude = 44.01
                },
                new MarketImpact
                {
                    Id = "IMP-005", Sector = MarketSector.Energy,
                    Title = "Iran Oil Sanctions Enforcement",
                    Trigger = "US tightened sanctions / secondary sanctions on Chinese refiners",
                    Impact = "Iran oil exports -500K to -1.5M bpd; global crude +5-10%; China pivot to alternative suppliers",
                    Assets = "CL, BZ, PetroChina (PTR), Sinopec (SHI), CNOOC (CEO)",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.High,
                    Region = "Iran / Global",
                    Probability = "Medium",
                    TimeHorizon = "Weeks to months for full enforcement impact",
                    HistoricalPrecedent = "2018 sanctions: Iran exports dropped from 2.5M to 300K bpd",
                    Latitude = 32.43, Longitude = 53.69
                },

                // ── Defense & Aerospace ──
                new MarketImpact
                {
                    Id = "IMP-010", Sector = MarketSector.Defense,
                    Title = "Iron Dome / Air Defense Demand Surge",
                    Trigger = "Multi-front rocket/drone attacks on Israel",
                    Impact = "Defense stocks +10-25%; Rafael, Elbit Systems, RTX, LMT surge; US supplemental defense spending",
                    Assets = "ESLT (Elbit), LMT, RTX, NOC, GD, BA, ITA (Aerospace ETF)",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.High,
                    Region = "Israel / USA",
                    Probability = "High",
                    TimeHorizon = "Immediate spike + sustained procurement cycle (12-24 months)",
                    HistoricalPrecedent = "Oct 2023: Elbit +40%, RTX +15% in 3 months post-attack",
                    Latitude = 32.08, Longitude = 34.78
                },
                new MarketImpact
                {
                    Id = "IMP-011", Sector = MarketSector.Defense,
                    Title = "Drone Warfare Proliferation",
                    Trigger = "Iranian Shahed-series drone use / Turkish Bayraktar expansion",
                    Impact = "Counter-UAS stocks surge; drone defense companies +20-40%; new procurement contracts",
                    Assets = "AVAV (AeroVironment), KTOS (Kratos), LHX, RCAT, JOBY, drone ETFs",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.High,
                    Region = "Global",
                    Probability = "Active — ongoing trend",
                    TimeHorizon = "Multi-year growth cycle",
                    HistoricalPrecedent = "Ukraine conflict: counter-drone spending increased 500% globally since 2022",
                    Latitude = 33.69, Longitude = 51.42
                },
                new MarketImpact
                {
                    Id = "IMP-012", Sector = MarketSector.Defense,
                    Title = "Naval Escalation — Carrier Strike Group Deployments",
                    Trigger = "US CSG deployment to Eastern Mediterranean / Persian Gulf",
                    Impact = "Defense shipbuilders +5-10%; fuel/logistics contractors surge; regional deterrence premium",
                    Assets = "HII, GD (NASSCO), BWX, SAIC, LDOS, CACI",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Medium,
                    Region = "Eastern Mediterranean",
                    Probability = "Active",
                    TimeHorizon = "Duration of deployment + follow-on contracts",
                    HistoricalPrecedent = "Every ME crisis since 1990: naval presence = defense spending increase",
                    Latitude = 34.00, Longitude = 33.00
                },

                // ── Commodities ──
                new MarketImpact
                {
                    Id = "IMP-020", Sector = MarketSector.Commodities,
                    Title = "Gold Safe Haven Rally",
                    Trigger = "Any major ME escalation / nuclear threat / multi-front war",
                    Impact = "Gold +5-15%; silver +8-20%; mining stocks surge; USD strengthens initially then reverses",
                    Assets = "GC (Gold), SI (Silver), GLD, IAU, SLV, GDX, NEM, GOLD, AEM",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.High,
                    Region = "Global",
                    Probability = "High — standard conflict response",
                    TimeHorizon = "Immediate spike, sustained during uncertainty",
                    HistoricalPrecedent = "Every ME war: gold rallies. Oct 2023: gold +8% in 6 weeks. 2020 Iran crisis: gold hit $1,600+",
                    Latitude = 0, Longitude = 0
                },
                new MarketImpact
                {
                    Id = "IMP-021", Sector = MarketSector.Commodities,
                    Title = "Wheat & Food Security Crisis",
                    Trigger = "Supply chain disruption through Suez / Black Sea",
                    Impact = "Wheat +20-40%; Egypt/Lebanon food price crisis; fertilizer costs surge",
                    Assets = "ZW (Wheat), ZC (Corn), ZS (Soybeans), WEAT, MOS, NTR, CF",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Critical,
                    Region = "Middle East / North Africa",
                    Probability = "Medium",
                    TimeHorizon = "Weeks to months; supply chain dependent",
                    HistoricalPrecedent = "2022 Ukraine: wheat +60%; Egypt bread crisis; Arab Spring 2011 triggered by food prices",
                    Latitude = 30.04, Longitude = 31.24
                },
                new MarketImpact
                {
                    Id = "IMP-022", Sector = MarketSector.Commodities,
                    Title = "Uranium / Nuclear Escalation Premium",
                    Trigger = "Iran nuclear breakout / enrichment to 90%+ / IAEA expulsion",
                    Impact = "Uranium +15-30%; nuclear energy stocks surge; SPR releases; oil embargo risk",
                    Assets = "URA (Uranium ETF), CCJ (Cameco), NXE, UEC, DNN, UUUU, SRUUF",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Critical,
                    Region = "Iran / Global",
                    Probability = "Low-Medium",
                    TimeHorizon = "Immediate spike + months of uncertainty",
                    HistoricalPrecedent = "2006 Iran nuclear crisis: uranium prices tripled; 2022 JCPOA collapse: spot uranium +80%",
                    Latitude = 32.62, Longitude = 51.68
                },

                // ── Currencies ──
                new MarketImpact
                {
                    Id = "IMP-030", Sector = MarketSector.Currencies,
                    Title = "Israeli Shekel Depreciation",
                    Trigger = "Major conflict escalation / multi-front war / reserve call-up",
                    Impact = "ILS -10-20% vs USD; Bank of Israel intervention; Israeli tech sector exit flows",
                    Assets = "USD/ILS, ISRA (Israel ETF), TEVA, CYBR, CHKP, WIX, MNDY",
                    Direction = MarketDirection.Bearish,
                    Severity = ImpactSeverity.High,
                    Region = "Israel",
                    Probability = "Active in escalation scenarios",
                    TimeHorizon = "Immediate 1-2 weeks; BOI intervention stabilizes",
                    HistoricalPrecedent = "Oct 2023: shekel fell 7% in 2 weeks; BOI sold $8.2B in reserves",
                    Latitude = 31.77, Longitude = 35.22
                },
                new MarketImpact
                {
                    Id = "IMP-031", Sector = MarketSector.Currencies,
                    Title = "Turkish Lira Pressure",
                    Trigger = "Syria/Iraq military operations / sanctions / NATO tensions",
                    Impact = "TRY -5-15%; Turkish equities sell-off; CBRT rate hikes; capital flight",
                    Assets = "USD/TRY, TUR (Turkey ETF), BIST100, Turkish sovereign CDS",
                    Direction = MarketDirection.Bearish,
                    Severity = ImpactSeverity.Medium,
                    Region = "Turkey",
                    Probability = "Medium",
                    TimeHorizon = "Weeks; structural weakness compounds",
                    HistoricalPrecedent = "2019 Syria incursion: TRY -5% in days; 2018 sanctions crisis: TRY -40%",
                    Latitude = 39.93, Longitude = 32.86
                },
                new MarketImpact
                {
                    Id = "IMP-032", Sector = MarketSector.Currencies,
                    Title = "Iranian Rial Collapse Deepens",
                    Trigger = "New sanctions rounds / military strikes on Iran / regime instability",
                    Impact = "IRR black market -20-50%; Iranian equities crash; capital flight to crypto/gold",
                    Assets = "USD/IRR (black market), Tehran Stock Exchange TEDPIX, Bitcoin (BTC)",
                    Direction = MarketDirection.Bearish,
                    Severity = ImpactSeverity.Critical,
                    Region = "Iran",
                    Probability = "High in strike scenario",
                    TimeHorizon = "Immediate and sustained",
                    HistoricalPrecedent = "2018 sanctions: IRR lost 60% value; 2022 protests: TEDPIX -30%",
                    Latitude = 35.69, Longitude = 51.39
                },

                // ── Shipping & Logistics ──
                new MarketImpact
                {
                    Id = "IMP-040", Sector = MarketSector.Shipping,
                    Title = "War Risk Insurance Premium Surge",
                    Trigger = "Attacks on commercial shipping in Persian Gulf / Red Sea",
                    Impact = "War risk premiums +500-1000%; tanker rates +200%; container shipping costs pass through to consumers",
                    Assets = "FRO, STNG, EURN, TNK, ZIM, APMM.AS, DSV.CO, BDRY (dry bulk ETF)",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Critical,
                    Region = "Persian Gulf / Red Sea",
                    Probability = "Active",
                    TimeHorizon = "Duration of conflict + 3-6 month tail",
                    HistoricalPrecedent = "2024 Red Sea crisis: freight rates +400%; war risk premiums hit $150K+ per transit",
                    Latitude = 15.50, Longitude = 42.00
                },
                new MarketImpact
                {
                    Id = "IMP-041", Sector = MarketSector.Shipping,
                    Title = "LNG Rerouting Premium — Europe",
                    Trigger = "Qatar LNG transit disruption through Strait of Hormuz",
                    Impact = "EU gas prices +30-60%; LNG spot premiums surge; Qatar/Australia LNG rerouting; European industry curtailment",
                    Assets = "NG (US), TTF (EU Gas), LNG (Cheniere), AR, EQT, SWN",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Critical,
                    Region = "Europe / Middle East",
                    Probability = "Medium in full escalation",
                    TimeHorizon = "Immediate spike + seasonal demand overlay",
                    HistoricalPrecedent = "2022 Russia gas cutoff: EU TTF hit €340/MWh; industrial recession",
                    Latitude = 25.29, Longitude = 51.53
                },

                // ── Tech & Cyber ──
                new MarketImpact
                {
                    Id = "IMP-050", Sector = MarketSector.Cybersecurity,
                    Title = "Cyber Warfare Escalation",
                    Trigger = "Iranian/proxy cyber attacks on critical infrastructure / financial systems",
                    Impact = "Cybersecurity stocks +10-20%; Israeli cyber firms premium; government cyber spending surge",
                    Assets = "CRWD, PANW, FTNT, ZS, CYBR, NET, S (SentinelOne), CIBR (Cyber ETF)",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.High,
                    Region = "Global",
                    Probability = "High — active threat",
                    TimeHorizon = "Sustained growth trend + event spikes",
                    HistoricalPrecedent = "Every major conflict escalation drives 15-25% cybersecurity premium",
                    Latitude = 0, Longitude = 0
                },

                // ── Reconstruction & Humanitarian ──
                new MarketImpact
                {
                    Id = "IMP-060", Sector = MarketSector.Reconstruction,
                    Title = "Post-Conflict Reconstruction Boom",
                    Trigger = "Ceasefire / peace deal / post-war rebuilding phase",
                    Impact = "Construction materials +15-30%; cement, steel, heavy equipment surge; aid contracts",
                    Assets = "CAT, DE, VMC, MLM, X (US Steel), STLD, CLF, CX (Cemex)",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Medium,
                    Region = "Conflict zones",
                    Probability = "Medium-Long term",
                    TimeHorizon = "Multi-year reconstruction cycle",
                    HistoricalPrecedent = "Iraq reconstruction: $60B+ in contracts; Syria rebuild estimate: $400B+",
                    Latitude = 33.31, Longitude = 44.37
                },

                // ── Insurance / Reinsurance ──
                new MarketImpact
                {
                    Id = "IMP-070", Sector = MarketSector.Insurance,
                    Title = "Reinsurance Catastrophe Premium",
                    Trigger = "Major infrastructure destruction / missile strikes on urban centers",
                    Impact = "Reinsurance rates +15-40%; ILS (Insurance-Linked Securities) repricing; cat bond spreads widen",
                    Assets = "BRK.B, MKL, RNR, ACGL, ESGR, RE, Swiss Re (SREN.SW)",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Medium,
                    Region = "Global",
                    Probability = "Medium",
                    TimeHorizon = "Annual renewal cycles; immediate repricing on major events",
                    HistoricalPrecedent = "9/11: reinsurance rates doubled; 2023 Israel: property insurance exclusions enacted",
                    Latitude = 0, Longitude = 0
                },

                // ── Crypto / Alternative Assets ──
                new MarketImpact
                {
                    Id = "IMP-080", Sector = MarketSector.Crypto,
                    Title = "Capital Flight to Crypto",
                    Trigger = "Currency controls / sanctions evasion / regime instability",
                    Impact = "BTC +5-15% on conflict spikes; stablecoin premiums in sanctioned regions; P2P volume surge in Iran/Turkey",
                    Assets = "BTC, ETH, USDT (premium), IBIT, ETHA, COIN, MSTR",
                    Direction = MarketDirection.Bullish,
                    Severity = ImpactSeverity.Medium,
                    Region = "Middle East / Global",
                    Probability = "Active — ongoing trend",
                    TimeHorizon = "Event-driven spikes + structural adoption",
                    HistoricalPrecedent = "Iran 2022: BTC P2P premium 30%+; Turkey 2021: crypto adoption surged with lira crash",
                    Latitude = 0, Longitude = 0
                }
            });
        }

        // ═══════════════════════════════════════════
        //  CONFLICT COMMODITIES (Live Price Tracking)
        // ═══════════════════════════════════════════

        private void InitializeCommodities()
        {
            _commodities.AddRange(new[]
            {
                new ConflictCommodity { Symbol = "CL", Name = "WTI Crude Oil", Unit = "$/bbl", ConflictSensitivity = "CRITICAL", WarPremium = "+$15-30", LiveUrl = "https://www.tradingview.com/symbols/USOIL/", WarDriver = "Hormuz chokepoint, Saudi infrastructure, Iraq pipelines" },
                new ConflictCommodity { Symbol = "BZ", Name = "Brent Crude", Unit = "$/bbl", ConflictSensitivity = "CRITICAL", WarPremium = "+$15-30", LiveUrl = "https://www.tradingview.com/symbols/UKOIL/", WarDriver = "Global benchmark — all ME conflict events" },
                new ConflictCommodity { Symbol = "NG", Name = "Natural Gas (US)", Unit = "$/MMBtu", ConflictSensitivity = "HIGH", WarPremium = "+20-40%", LiveUrl = "https://www.tradingview.com/symbols/NATURALGAS/", WarDriver = "LNG rerouting, Qatar supply risk" },
                new ConflictCommodity { Symbol = "TTF", Name = "EU Natural Gas", Unit = "€/MWh", ConflictSensitivity = "CRITICAL", WarPremium = "+30-60%", LiveUrl = "https://www.tradingview.com/symbols/ICEUS-TFM1!/", WarDriver = "Suez/Red Sea disruption, LNG transit risk" },
                new ConflictCommodity { Symbol = "GC", Name = "Gold", Unit = "$/oz", ConflictSensitivity = "HIGH", WarPremium = "+$100-300", LiveUrl = "https://www.tradingview.com/symbols/GOLD/", WarDriver = "Universal safe haven — all escalation events" },
                new ConflictCommodity { Symbol = "SI", Name = "Silver", Unit = "$/oz", ConflictSensitivity = "MEDIUM", WarPremium = "+8-20%", LiveUrl = "https://www.tradingview.com/symbols/SILVER/", WarDriver = "Safe haven + industrial demand from defense" },
                new ConflictCommodity { Symbol = "ZW", Name = "Wheat", Unit = "¢/bu", ConflictSensitivity = "HIGH", WarPremium = "+20-40%", LiveUrl = "https://www.tradingview.com/symbols/WHEAT/", WarDriver = "Suez blockade, MENA food imports, Egypt dependency" },
                new ConflictCommodity { Symbol = "UX", Name = "Uranium", Unit = "$/lb", ConflictSensitivity = "HIGH", WarPremium = "+15-30%", LiveUrl = "https://www.tradingview.com/symbols/AMEX-URA/", WarDriver = "Iran nuclear escalation, energy security pivot" },
                new ConflictCommodity { Symbol = "PA", Name = "Palladium", Unit = "$/oz", ConflictSensitivity = "MEDIUM", WarPremium = "+10-20%", LiveUrl = "https://www.tradingview.com/symbols/PALLADIUM/", WarDriver = "Defense electronics, catalyst supply chains" },
                new ConflictCommodity { Symbol = "HG", Name = "Copper", Unit = "$/lb", ConflictSensitivity = "MEDIUM", WarPremium = "+5-10%", LiveUrl = "https://www.tradingview.com/symbols/COPPER/", WarDriver = "Munitions production, reconstruction demand" },
            });
        }

        // ═══════════════════════════════════════════
        //  DEFENSE SECTOR STOCKS
        // ═══════════════════════════════════════════

        private void InitializeDefenseStocks()
        {
            _defenseStocks.AddRange(new[]
            {
                new DefenseStock { Ticker = "LMT", Name = "Lockheed Martin", Subsector = "Missiles / F-35 / THAAD", ConflictExposure = "Direct", MarketCap = "$120B", LiveUrl = "https://www.tradingview.com/symbols/NYSE-LMT/" },
                new DefenseStock { Ticker = "RTX", Name = "RTX Corp (Raytheon)", Subsector = "Patriot / NASAMS / Tomahawk", ConflictExposure = "Direct", MarketCap = "$155B", LiveUrl = "https://www.tradingview.com/symbols/NYSE-RTX/" },
                new DefenseStock { Ticker = "NOC", Name = "Northrop Grumman", Subsector = "B-21 / Global Hawk / C4ISR", ConflictExposure = "Direct", MarketCap = "$70B", LiveUrl = "https://www.tradingview.com/symbols/NYSE-NOC/" },
                new DefenseStock { Ticker = "GD", Name = "General Dynamics", Subsector = "Abrams / submarines / munitions", ConflictExposure = "Direct", MarketCap = "$80B", LiveUrl = "https://www.tradingview.com/symbols/NYSE-GD/" },
                new DefenseStock { Ticker = "BA", Name = "Boeing", Subsector = "F-15 / KC-46 / JDAM / SDB", ConflictExposure = "Direct", MarketCap = "$130B", LiveUrl = "https://www.tradingview.com/symbols/NYSE-BA/" },
                new DefenseStock { Ticker = "ESLT", Name = "Elbit Systems", Subsector = "Iron Dome / drones / EW / Hermes", ConflictExposure = "Critical — Israel", MarketCap = "$12B", LiveUrl = "https://www.tradingview.com/symbols/NASDAQ-ESLT/" },
                new DefenseStock { Ticker = "HII", Name = "Huntington Ingalls", Subsector = "Aircraft carriers / submarines", ConflictExposure = "Direct", MarketCap = "$12B", LiveUrl = "https://www.tradingview.com/symbols/NYSE-HII/" },
                new DefenseStock { Ticker = "LHX", Name = "L3Harris Technologies", Subsector = "Comms / EW / ISR / night vision", ConflictExposure = "Direct", MarketCap = "$45B", LiveUrl = "https://www.tradingview.com/symbols/NYSE-LHX/" },
                new DefenseStock { Ticker = "AVAV", Name = "AeroVironment", Subsector = "Switchblade / Puma drones", ConflictExposure = "High — drone warfare", MarketCap = "$6B", LiveUrl = "https://www.tradingview.com/symbols/NASDAQ-AVAV/" },
                new DefenseStock { Ticker = "KTOS", Name = "Kratos Defense", Subsector = "Drone swarms / counter-UAS / targets", ConflictExposure = "High — drone defense", MarketCap = "$3B", LiveUrl = "https://www.tradingview.com/symbols/NASDAQ-KTOS/" },
                new DefenseStock { Ticker = "CRWD", Name = "CrowdStrike", Subsector = "Endpoint security / cyber defense", ConflictExposure = "High — cyber warfare", MarketCap = "$72B", LiveUrl = "https://www.tradingview.com/symbols/NASDAQ-CRWD/" },
                new DefenseStock { Ticker = "PANW", Name = "Palo Alto Networks", Subsector = "Network security / cloud / OT", ConflictExposure = "High — critical infra", MarketCap = "$110B", LiveUrl = "https://www.tradingview.com/symbols/NASDAQ-PANW/" },
            });
        }

        // ═══════════════════════════════════════════
        //  LIVE MARKET ALERTS
        // ═══════════════════════════════════════════

        private void InitializeAlerts()
        {
            _alerts.AddRange(new[]
            {
                new MarketAlert
                {
                    Title = "OIL SPIKE — Hormuz Transit Risk Elevated",
                    Category = "Energy", Severity = "CRITICAL",
                    Message = "IRGC naval exercises + US carrier strike group convergence. Brent risk premium expanding. Long CL/BZ, hedge with puts.",
                    Tickers = "CL, BZ, XOM, CVX, SLB",
                    Timestamp = DateTime.UtcNow.AddHours(-4)
                },
                new MarketAlert
                {
                    Title = "DEFENSE RALLY — Multi-front escalation emerging",
                    Category = "Defense", Severity = "HIGH",
                    Message = "Simultaneous rocket/drone attacks across multiple theaters. Defense procurement acceleration likely. Watch supplemental spending bills.",
                    Tickers = "LMT, RTX, NOC, ESLT, AVAV, KTOS",
                    Timestamp = DateTime.UtcNow.AddHours(-8)
                },
                new MarketAlert
                {
                    Title = "SHIPPING COSTS — Red Sea rerouting continues",
                    Category = "Logistics", Severity = "HIGH",
                    Message = "Houthi attacks sustaining; container rates elevated 300%+ vs pre-crisis. European import costs rising. Watch inflation data.",
                    Tickers = "ZIM, FRO, STNG, APMM.AS",
                    Timestamp = DateTime.UtcNow.AddHours(-12)
                },
                new MarketAlert
                {
                    Title = "GOLD BID — Safe haven flows accelerating",
                    Category = "Commodities", Severity = "MEDIUM",
                    Message = "Central bank buying + retail safe haven demand. Gold clearing $2400 resistance. Silver following with industrial overlay.",
                    Tickers = "GC, GLD, GDX, SLV, NEM",
                    Timestamp = DateTime.UtcNow.AddHours(-18)
                },
                new MarketAlert
                {
                    Title = "CYBER THREAT — IRGC-linked APT activity detected",
                    Category = "Cybersecurity", Severity = "HIGH",
                    Message = "CISA advisory on Iranian APT targeting water/energy infrastructure. Cybersecurity spending likely to accelerate.",
                    Tickers = "CRWD, PANW, FTNT, ZS, CYBR",
                    Timestamp = DateTime.UtcNow.AddHours(-24)
                },
                new MarketAlert
                {
                    Title = "FOOD SECURITY — Suez disruption hitting grain flows",
                    Category = "Commodities", Severity = "MEDIUM",
                    Message = "Egyptian wheat imports delayed 10-15 days. MENA food price indices rising. Watch social stability indicators.",
                    Tickers = "ZW, WEAT, MOS, NTR",
                    Timestamp = DateTime.UtcNow.AddHours(-36)
                },
                new MarketAlert
                {
                    Title = "NUCLEAR PREMIUM — Iran enrichment milestone",
                    Category = "Energy/Nuclear", Severity = "CRITICAL",
                    Message = "IAEA reports near-weapons-grade enrichment levels. Uranium spot prices spiking. Strike scenario risk rising.",
                    Tickers = "URA, CCJ, NXE, UEC, CL, GC",
                    Timestamp = DateTime.UtcNow.AddHours(-48)
                },
            });
        }

        // ═══════════════════════════════════════════
        //  SANCTION IMPACTS
        // ═══════════════════════════════════════════

        private void InitializeSanctions()
        {
            _sanctions.AddRange(new[]
            {
                new SanctionImpact { Target = "Iran Oil Exports", Sanctioner = "US (OFAC)", MarketImpact = "Global crude +5-10%, China redirect costs", Status = "Active — enforcement tightening", AffectedAssets = "CL, BZ, PTR, SHI" },
                new SanctionImpact { Target = "Russian Energy (secondary)", Sanctioner = "US/EU", MarketImpact = "LNG/oil price floor, shipping reroutes", Status = "Active — expanding", AffectedAssets = "NG, TTF, tanker stocks" },
                new SanctionImpact { Target = "Hezbollah Financial Networks", Sanctioner = "US Treasury", MarketImpact = "Lebanese banking fragility, capital controls", Status = "Active", AffectedAssets = "Lebanese bonds, BDL reserves" },
                new SanctionImpact { Target = "Houthi Revenue Sources", Sanctioner = "US/UK", MarketImpact = "Red Sea insurance costs, shipping reroutes", Status = "Active — FTO re-designation", AffectedAssets = "ZIM, FRO, STNG, shipping futures" },
                new SanctionImpact { Target = "IRGC Drone Program", Sanctioner = "US/EU", MarketImpact = "Counter-drone procurement surge", Status = "Active", AffectedAssets = "AVAV, KTOS, RCAT, LHX" },
                new SanctionImpact { Target = "Syrian Regime (Caesar Act)", Sanctioner = "US", MarketImpact = "Reconstruction blocked, humanitarian exemption complexity", Status = "Active", AffectedAssets = "Regional construction, cement stocks" },
            });
        }

        // ═══════════════════════════════════════════
        //  LIVE TRADE / MARKET FEEDS
        // ═══════════════════════════════════════════

        private void InitializeFeeds()
        {
            _feeds.AddRange(new[]
            {
                new TradeFeed { Name = "TradingView — Oil & Energy", Url = "https://www.tradingview.com/markets/futures/quotes-energy/", Category = "Energy", Description = "Live oil, gas, energy futures charts and data" },
                new TradeFeed { Name = "TradingView — Metals & Gold", Url = "https://www.tradingview.com/markets/futures/quotes-metals/", Category = "Commodities", Description = "Gold, silver, uranium, palladium live charts" },
                new TradeFeed { Name = "TradingView — Defense ETFs", Url = "https://www.tradingview.com/symbols/AMEX-ITA/", Category = "Defense", Description = "iShares US Aerospace & Defense ETF" },
                new TradeFeed { Name = "Finviz — Defense Sector", Url = "https://finviz.com/screener.ashx?v=111&f=ind_aerospacedefense", Category = "Defense", Description = "Defense stock screener with heat maps" },
                new TradeFeed { Name = "OilPrice.com — Live", Url = "https://oilprice.com/", Category = "Energy", Description = "Live crude oil prices and geopolitical analysis" },
                new TradeFeed { Name = "Seeking Alpha — Defense", Url = "https://seekingalpha.com/sector/industrials", Category = "Defense", Description = "Defense sector analysis and earnings" },
                new TradeFeed { Name = "Reuters — Commodities", Url = "https://www.reuters.com/markets/commodities/", Category = "Commodities", Description = "Reuters live commodity news and data" },
                new TradeFeed { Name = "Bloomberg — ME Markets", Url = "https://www.bloomberg.com/markets", Category = "Markets", Description = "Bloomberg global markets overview" },
                new TradeFeed { Name = "ZeroHedge — Geopolitics", Url = "https://www.zerohedge.com/", Category = "Analysis", Description = "Alternative financial analysis, geopolitical risk" },
                new TradeFeed { Name = "War Economy Tracker", Url = "https://www.sipri.org/databases/milex", Category = "Research", Description = "SIPRI military expenditure database" },
                new TradeFeed { Name = "Shipping Rates — Freightos", Url = "https://www.freightos.com/freight-resources/freightos-baltic-index-global-container-freight-index/", Category = "Shipping", Description = "Global container freight index" },
                new TradeFeed { Name = "CSIS — Defense Budget", Url = "https://www.csis.org/programs/defense-budget-analysis", Category = "Research", Description = "US defense budget analysis and procurement forecasts" },
            });
        }

        // ═══════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════

        public List<MarketImpact> GetAllImpacts() => _impacts.OrderByDescending(i => i.Severity).ToList();
        public List<MarketImpact> GetImpactsBySector(MarketSector sector) => _impacts.Where(i => i.Sector == sector).ToList();
        public List<MarketImpact> GetCriticalImpacts() => _impacts.Where(i => i.Severity >= ImpactSeverity.High).ToList();
        public List<ConflictCommodity> GetAllCommodities() => _commodities;
        public List<DefenseStock> GetAllDefenseStocks() => _defenseStocks;
        public List<MarketAlert> GetAllAlerts() => _alerts.OrderByDescending(a => a.Timestamp).ToList();
        public List<SanctionImpact> GetAllSanctions() => _sanctions;
        public List<TradeFeed> GetAllFeeds() => _feeds;

        public MarketStats GetStats()
        {
            return new MarketStats
            {
                TotalImpacts = _impacts.Count,
                CriticalImpacts = _impacts.Count(i => i.Severity == ImpactSeverity.Critical),
                HighImpacts = _impacts.Count(i => i.Severity == ImpactSeverity.High),
                Sectors = _impacts.Select(i => i.Sector).Distinct().Count(),
                Commodities = _commodities.Count,
                DefenseStocks = _defenseStocks.Count,
                ActiveAlerts = _alerts.Count,
                Sanctions = _sanctions.Count,
                LiveFeeds = _feeds.Count
            };
        }

        /// <summary>Builds JS-compatible map data for impact markers.</summary>
        public string BuildImpactMapData()
        {
            var items = _impacts
                .Where(i => i.Latitude != 0 || i.Longitude != 0)
                .Select(i =>
                {
                    var name = EscapeJs(i.Title);
                    var trigger = EscapeJs(i.Trigger);
                    var assets = EscapeJs(i.Assets);
                    var sev = (int)i.Severity;
                    var dir = i.Direction == MarketDirection.Bullish ? "📈" : "📉";
                    return $"{{lat:{i.Latitude},lon:{i.Longitude},name:'{name}',trigger:'{trigger}',assets:'{assets}',sev:{sev},dir:'{dir}',sector:'{i.Sector}'}}";
                });
            return string.Join(",", items);
        }

        private static string EscapeJs(string s) =>
            s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "");
    }

    // ═══════════════════════════════════════════
    //  MARKET MODELS
    // ═══════════════════════════════════════════

    public enum MarketSector
    {
        Energy,
        Defense,
        Commodities,
        Currencies,
        Shipping,
        Cybersecurity,
        Reconstruction,
        Insurance,
        Crypto
    }

    public enum MarketDirection
    {
        Bullish,
        Bearish,
        Neutral
    }

    public enum ImpactSeverity
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    public class MarketImpact
    {
        public string Id { get; set; } = "";
        public MarketSector Sector { get; set; }
        public string Title { get; set; } = "";
        public string Trigger { get; set; } = "";
        public string Impact { get; set; } = "";
        public string Assets { get; set; } = "";
        public MarketDirection Direction { get; set; }
        public ImpactSeverity Severity { get; set; }
        public string Region { get; set; } = "";
        public string Probability { get; set; } = "";
        public string TimeHorizon { get; set; } = "";
        public string HistoricalPrecedent { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string SeverityDisplay => Severity switch
        {
            ImpactSeverity.Critical => "🔴 CRITICAL",
            ImpactSeverity.High => "🟠 HIGH",
            ImpactSeverity.Medium => "🔵 MEDIUM",
            _ => "⚪ LOW"
        };

        public string DirectionDisplay => Direction switch
        {
            MarketDirection.Bullish => "📈 BULLISH",
            MarketDirection.Bearish => "📉 BEARISH",
            _ => "➡ NEUTRAL"
        };

        public string SectorDisplay => Sector switch
        {
            MarketSector.Energy => "⛽ Energy",
            MarketSector.Defense => "🛡 Defense",
            MarketSector.Commodities => "📦 Commodities",
            MarketSector.Currencies => "💱 Currencies",
            MarketSector.Shipping => "🚢 Shipping",
            MarketSector.Cybersecurity => "💻 Cyber",
            MarketSector.Reconstruction => "🏗 Reconstruction",
            MarketSector.Insurance => "🏦 Insurance",
            MarketSector.Crypto => "₿ Crypto",
            _ => "📊 Other"
        };
    }

    public class ConflictCommodity
    {
        public string Symbol { get; set; } = "";
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public string ConflictSensitivity { get; set; } = "";
        public string WarPremium { get; set; } = "";
        public string LiveUrl { get; set; } = "";
        public string WarDriver { get; set; } = "";

        public string SensitivityDisplay => ConflictSensitivity switch
        {
            "CRITICAL" => "🔴 CRITICAL",
            "HIGH" => "🟠 HIGH",
            "MEDIUM" => "🔵 MEDIUM",
            _ => "⚪ LOW"
        };
    }

    public class DefenseStock
    {
        public string Ticker { get; set; } = "";
        public string Name { get; set; } = "";
        public string Subsector { get; set; } = "";
        public string ConflictExposure { get; set; } = "";
        public string MarketCap { get; set; } = "";
        public string LiveUrl { get; set; } = "";
    }

    public class MarketAlert
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Message { get; set; } = "";
        public string Tickers { get; set; } = "";
        public DateTime Timestamp { get; set; }

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
    }

    public class SanctionImpact
    {
        public string Target { get; set; } = "";
        public string Sanctioner { get; set; } = "";
        public string MarketImpact { get; set; } = "";
        public string Status { get; set; } = "";
        public string AffectedAssets { get; set; } = "";
    }

    public class TradeFeed
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class MarketStats
    {
        public int TotalImpacts { get; set; }
        public int CriticalImpacts { get; set; }
        public int HighImpacts { get; set; }
        public int Sectors { get; set; }
        public int Commodities { get; set; }
        public int DefenseStocks { get; set; }
        public int ActiveAlerts { get; set; }
        public int Sanctions { get; set; }
        public int LiveFeeds { get; set; }
    }
}
