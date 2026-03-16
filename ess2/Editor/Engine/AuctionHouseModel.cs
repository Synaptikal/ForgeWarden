using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.ESS;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Simulates a player-driven auction house for a single day.
    ///
    /// Model overview:
    ///
    ///   1. LISTING  – Sellers post items at Ask = currentPrice * (1 + spread * priceNoise)
    ///   2. CLEARING – Buyers purchase if Ask ≤ their reservation price (willingness to pay)
    ///   3. PRICE    – New market price = volume-weighted average of cleared transactions
    ///   4. VELOCITY – Tracks currency changing hands (used in inflation spiral detection)
    ///
    /// Listing fee: listingFeePct of Ask is destroyed on listing (gold sink).
    /// Transaction tax: taxPct of sale proceeds is destroyed on sale (gold sink).
    ///
    /// The AH model does NOT simulate individual player AI — that is ESS_PlayerAgent.
    /// It receives aggregated supply (units listed) and demand (units wanted) per item
    /// and resolves prices. This keeps the sim O(items × archetypes), not O(players²).
    /// </summary>
    public class AuctionHouseModel
    {
        private readonly float _listingFeePct;    // fraction of ask destroyed on listing
        private readonly float _taxPct;           // fraction of revenue destroyed on sale
        private readonly float _priceInertia;     // 0=instant, 1=frozen — momentum of price change
        private readonly float _maxPriceMovePct;  // max % price can move in a single day

        // ── Historical data ───────────────────────────────────────
        // item name → last N days of closing prices (ring buffer, N=30)
        private readonly Dictionary<string, Queue<float>> _priceHistory = new();
        private const int HistoryDepth = 30;

        // ── Daily totals ──────────────────────────────────────────
        public float DailyCurrencyDestroyed { get; private set; }
        public float DailyCurrencyTransacted { get; private set; }
        public int   DailyTransactionCount  { get; private set; }

        public AuctionHouseModel(
            float listingFeePct = 0.02f,
            float taxPct        = 0.05f,
            float priceInertia  = 0.3f,
            float maxPriceMovePct = 0.20f)
        {
            _listingFeePct    = listingFeePct;
            _taxPct           = taxPct;
            _priceInertia     = priceInertia;
            _maxPriceMovePct  = maxPriceMovePct;
        }

        /// <summary>
        /// Resolve one day's auction activity for all items.
        ///
        /// supplyUnits  – total units listed this day per item (from player agents)
        /// demandUnits  – total units sought this day per item (from player agents)
        /// currentPrices – prices at start of day; mutated in place
        /// currencyPool  – total currency in economy; mutated in place (fees/taxes destroyed)
        ///
        /// Returns volume-weighted closing prices per item.
        /// </summary>
        public Dictionary<string, float> Resolve(
            Dictionary<string, float> supplyUnits,
            Dictionary<string, float> demandUnits,
            Dictionary<string, float> currentPrices,
            ref float currencyPool,
            System.Random rng)
        {
            DailyCurrencyDestroyed   = 0f;
            DailyCurrencyTransacted  = 0f;
            DailyTransactionCount    = 0;

            var closingPrices = new Dictionary<string, float>(currentPrices);

            // Process each item independently
            var allItems = new HashSet<string>(supplyUnits.Keys);
            foreach (var k in demandUnits.Keys) allItems.Add(k);

            foreach (var itemName in allItems)
            {
                supplyUnits.TryGetValue(itemName,  out float supply);
                demandUnits.TryGetValue(itemName,  out float demand);
                currentPrices.TryGetValue(itemName, out float price);
                if (price <= 0f) price = 1f;

                // ── Listing: destroy listing fees ─────────────────
                float listingFee = supply * price * _listingFeePct;
                DailyCurrencyDestroyed += listingFee;
                currencyPool           = Mathf.Max(0f, currencyPool - listingFee);

                // ── Clearing: calculate units transacted ──────────
                // Price noise: sellers ask slightly above, buyers bid slightly below
                float askNoise = 1f + (float)(rng.NextDouble() - 0.5f) * 0.04f; // ±2% spread
                float ask      = price * askNoise;

                // Buyer reservation price is demand-pressure adjusted:
                // when demand >> supply buyers pay more, demand << supply buyers pay less
                float pressureRatio = supply > 0f ? demand / supply : 2f;
                float buyerReserve  = price * Mathf.Lerp(0.92f, 1.12f, Mathf.Clamp01(pressureRatio - 0.5f));

                float clearedUnits = 0f;
                float clearPrice   = ask;

                if (ask <= buyerReserve && supply > 0f && demand > 0f)
                {
                    clearedUnits = Mathf.Min(supply, demand);
                    // Volume-weighted clearing price between ask and buyerReserve
                    clearPrice   = (ask + buyerReserve) * 0.5f;

                    float revenue  = clearedUnits * clearPrice;
                    float tax      = revenue * _taxPct;
                    DailyCurrencyDestroyed  += tax;
                    DailyCurrencyTransacted += revenue;
                    DailyTransactionCount++;
                    currencyPool = Mathf.Max(0f, currencyPool - tax);
                }

                // ── Price discovery: supply/demand imbalance pressure ─
                // supplyRatio > 1 → oversupply → price falls
                // supplyRatio < 1 → scarcity  → price rises
                float supplyRatio    = (supply + 0.01f) / (demand + 0.01f);
                float rawTargetPrice = price / Mathf.Max(supplyRatio, 0.1f);

                // Clamp daily price movement
                float maxMove = price * _maxPriceMovePct;
                float candidate = Mathf.Clamp(
                    rawTargetPrice,
                    price - maxMove,
                    price + maxMove);

                // Apply price inertia (momentum)
                float newPrice = Mathf.Lerp(candidate, price, _priceInertia);
                newPrice       = Mathf.Max(0.01f, newPrice);

                closingPrices[itemName] = newPrice;

                // ── History ───────────────────────────────────────
                if (!_priceHistory.TryGetValue(itemName, out var hist))
                    hist = _priceHistory[itemName] = new Queue<float>(HistoryDepth);
                if (hist.Count >= HistoryDepth) hist.Dequeue();
                hist.Enqueue(newPrice);
            }

            return closingPrices;
        }

        // ── Analytics ─────────────────────────────────────────────

        /// <summary>
        /// Inflation velocity for an item over the last N days.
        /// Positive = inflation, negative = deflation.
        /// Computed as least-squares slope of price history (more robust than day-over-day).
        /// </summary>
        public float GetInflationVelocity(string itemName, int windowDays = 7)
        {
            if (!_priceHistory.TryGetValue(itemName, out var hist) || hist.Count < 2)
                return 0f;

            var prices = hist.ToArray();
            int n      = Mathf.Min(prices.Length, windowDays);
            if (n < 2) return 0f;

            // Least-squares slope over last N points
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;
            for (int i = 0; i < n; i++)
            {
                int   dataIdx = prices.Length - n + i;
                float x = i;
                float y = prices[dataIdx];
                sumX  += x;
                sumY  += y;
                sumXY += x * y;
                sumX2 += x * x;
            }
            float denom = n * sumX2 - sumX * sumX;
            if (Mathf.Abs(denom) < 0.0001f) return 0f;

            float slope    = (n * sumXY - sumX * sumY) / denom;
            float meanPrice = sumY / n;
            return meanPrice > 0f ? slope / meanPrice : 0f; // normalized (fractional per day)
        }

        /// <summary>
        /// Consecutive days this item has been inflating above threshold.
        /// Used for InflationSpiral alert.
        /// </summary>
        public int GetConsecutiveInflationDays(string itemName, float threshold = 0.03f)
        {
            if (!_priceHistory.TryGetValue(itemName, out var hist)) return 0;
            var prices = hist.ToArray();
            int streak = 0;
            for (int i = prices.Length - 1; i > 0; i--)
            {
                float daily = prices[i - 1] > 0f ? (prices[i] - prices[i - 1]) / prices[i - 1] : 0f;
                if (daily >= threshold) streak++;
                else break;
            }
            return streak;
        }

        /// <summary>
        /// Money velocity (Fisher equation proxy): daily transactions / total supply.
        /// V > 1 means currency is circulating fast (overheating).
        /// V close to 0 means currency is hoarded (deflationary pressure).
        /// </summary>
        public float MoneyVelocity(float totalCurrencySupply)
            => totalCurrencySupply > 0f ? DailyCurrencyTransacted / totalCurrencySupply : 0f;

        /// <summary>Raw 30-day price history for chart rendering.</summary>
        public float[] GetPriceHistory(string itemName)
        {
            if (!_priceHistory.TryGetValue(itemName, out var h)) return Array.Empty<float>();
            return h.ToArray();
        }
    }
}
