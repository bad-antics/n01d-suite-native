using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using N01D.Overwatch.Models;

namespace N01D.Overwatch.Services
{
    /// <summary>
    /// Fetches crude oil prices from free public APIs.
    /// Monitors WTI and Brent crude — key indicators of conflict impact.
    /// </summary>
    public class OilPriceService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        // Free API: exchangerate-host or a similar commodity endpoint
        // We use Yahoo Finance's chart API (public, no key)
        private const string BrentUrl = "https://query1.finance.yahoo.com/v8/finance/chart/BZ=F?range=1d&interval=5m";
        private const string WtiUrl = "https://query1.finance.yahoo.com/v8/finance/chart/CL=F?range=1d&interval=5m";

        public async Task<List<OilPriceData>> FetchPricesAsync()
        {
            var prices = new List<OilPriceData>();
            var tasks = new[]
            {
                FetchSymbolAsync("Brent Crude", BrentUrl),
                FetchSymbolAsync("WTI Crude", WtiUrl)
            };

            var results = await Task.WhenAll(tasks);
            foreach (var r in results)
                if (r != null) prices.Add(r);

            return prices;
        }

        private async Task<OilPriceData?> FetchSymbolAsync(string name, string url)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                var response = await _http.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var result = doc.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0];

                var meta = result.GetProperty("meta");
                var price = meta.GetProperty("regularMarketPrice").GetDouble();
                var prevClose = meta.GetProperty("previousClose").GetDouble();
                var change = price - prevClose;
                var changePct = prevClose > 0 ? (change / prevClose) * 100 : 0;

                return new OilPriceData
                {
                    Name = name,
                    Price = price,
                    Change = change,
                    ChangePercent = changePct,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        public ConflictEvent ToConflictEvent(OilPriceData data)
        {
            var direction = data.Change >= 0 ? "📈" : "📉";
            var severity = Math.Abs(data.ChangePercent) switch
            {
                > 5 => SeverityLevel.Critical,
                > 3 => SeverityLevel.High,
                > 1 => SeverityLevel.Medium,
                _ => SeverityLevel.Low
            };

            return new ConflictEvent
            {
                Title = $"🛢 {data.Name}: ${data.Price:F2} {direction} {data.Change:+0.00;-0.00} ({data.ChangePercent:+0.00;-0.00}%)",
                Summary = $"Market indicator — large swings may signal conflict escalation or supply disruption",
                Source = "Oil Markets",
                DataSource = DataSource.OilPrice,
                Category = EventCategory.Economic,
                Severity = severity,
                Timestamp = data.Timestamp,
                Tags = new List<string> { "OIL", data.Name.Split(' ')[0].ToUpperInvariant() }
            };
        }
    }
}
