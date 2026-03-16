using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Computes economy-wide diagnostic metrics each day from player state snapshots.
    ///
    /// All metrics are written into DayMetrics and appended to the
    /// time-series MetricsHistory so charts can visualize trends.
    /// </summary>
    public class EconomyMetrics
    {
        // Time-series history (one entry per simulated day)
        public List<DayMetrics> History { get; } = new();

        /// <summary>Record one day's metrics from the full player population.</summary>
        public DayMetrics Snapshot(
            int day,
            IReadOnlyList<SimPlayerState> players,
            Dictionary<string, float> prices,
            AuctionHouseModel ah,
            SimConfig config)
        {
            var m = new DayMetrics { Day = day };

            // ── Wealth distribution (Gini coefficient) ────────────
            // Use total portfolio value per player (inventory + currency)
            var wealth = players
                .Select(p => p.TotalInventoryValue(prices))
                .OrderBy(v => v)
                .ToArray();

            m.GiniCoefficient = ComputeGini(wealth);

            // ── Top/bottom 10% wealth gap ─────────────────────────
            if (wealth.Length >= 10)
            {
                int top    = Mathf.Max(1, wealth.Length / 10);
                int bottom = Mathf.Max(1, wealth.Length / 10);
                float topMean    = wealth.Skip(wealth.Length - top).Average();
                float bottomMean = wealth.Take(bottom).Average();
                m.WealthGapRatio = bottomMean > 0f ? topMean / bottomMean : float.MaxValue;
            }

            // ── Total currency supply ─────────────────────────────
            m.TotalCurrencySupply = players.Sum(p => p.Currency);

            // ── Money velocity (Fisher equation: MV = PT) ─────────
            // V = daily transactions / total money supply
            m.MoneyVelocity = ah.MoneyVelocity(m.TotalCurrencySupply);

            // ── Daily currency destroyed (sinks) ──────────────────
            m.CurrencyDestroyedToday = ah.DailyCurrencyDestroyed;
            m.CurrencyTransactedToday = ah.DailyCurrencyTransacted;

            // ── Per-item inflation velocity ───────────────────────
            foreach (var item in config.TrackedItems)
            {
                if (item == null) continue;
                m.InflationVelocity[item.name] = ah.GetInflationVelocity(item.name, windowDays: 7);
                m.ConsecutiveInflationDays[item.name] = ah.GetConsecutiveInflationDays(item.name);
            }

            // ── Item supply metrics ───────────────────────────────
            // Aggregate all player inventories per item
            var totalSupply = new Dictionary<string, float>();
            foreach (var player in players)
                foreach (var kvp in player.Inventory)
                {
                    totalSupply.TryGetValue(kvp.Key, out float s);
                    totalSupply[kvp.Key] = s + kvp.Value;
                }

            foreach (var item in config.TrackedItems)
            {
                if (item == null) continue;
                totalSupply.TryGetValue(item.name, out float supply);
                float expected = item.TargetCirculationPerPlayer * config.PlayerCount;
                m.SupplyRatios[item.name] = expected > 0f ? supply / expected : 0f;
            }

            // ── Active players (played today) ─────────────────────
            m.ActivePlayerCount = players.Count(p => p.DaysSinceActive == 0);

            History.Add(m);
            return m;
        }

        /// <summary>
        /// Gini coefficient from a sorted wealth array.
        /// Lorenz curve area method — O(n) with pre-sorted input.
        /// Returns 0 (perfect equality) to ~1 (one player holds all wealth).
        /// </summary>
        public static float ComputeGini(float[] sortedWealth)
        {
            if (sortedWealth == null || sortedWealth.Length < 2) return 0f;

            double totalWealth = 0;
            double lorenzSum   = 0;
            int    n           = sortedWealth.Length;

            for (int i = 0; i < n; i++)
            {
                totalWealth += sortedWealth[i];
                lorenzSum   += sortedWealth[i] * (i + 1); // weighted rank sum (ascending)
            }

            if (totalWealth <= 0) return 0f;

            // Gini = 1 - 2 * (area under Lorenz curve)
            // = (2 * lorenzSum / (n * totalWealth)) - (n + 1) / n
            // Simplified to avoid precision issues with large n
            double gini = (2.0 * lorenzSum) / (n * totalWealth) - (double)(n + 1) / n;
            return Mathf.Clamp((float)gini, 0f, 1f);
        }
    }

    /// <summary>One day's worth of economy-wide metrics.</summary>
    public class DayMetrics
    {
        public int Day { get; set; }

        /// <summary>Wealth inequality (0=equal, 1=maximally unequal). Based on player portfolio values.</summary>
        public float GiniCoefficient { get; set; }

        /// <summary>Ratio of average wealth: top 10% / bottom 10%. >10 = severe inequality.</summary>
        public float WealthGapRatio { get; set; }

        /// <summary>Sum of all player gold holdings.</summary>
        public float TotalCurrencySupply { get; set; }

        /// <summary>Fisher equation money velocity: transactions / money supply. >2 = overheating.</summary>
        public float MoneyVelocity { get; set; }

        /// <summary>Gold destroyed via AH fees + taxes today. Should approximate gold injected.</summary>
        public float CurrencyDestroyedToday { get; set; }

        /// <summary>Total gold transacted via AH today.</summary>
        public float CurrencyTransactedToday { get; set; }

        /// <summary>Number of players who played today (DaysSinceActive == 0).</summary>
        public int ActivePlayerCount { get; set; }

        /// <summary>Fractional price change per day for each item (7-day least-squares slope).</summary>
        public Dictionary<string, float> InflationVelocity { get; } = new();

        /// <summary>Consecutive days of inflation streak above threshold per item.</summary>
        public Dictionary<string, int> ConsecutiveInflationDays { get; } = new();

        /// <summary>Current supply / expected supply per item (>3 = Overfarming, <0.1 = Collapse).</summary>
        public Dictionary<string, float> SupplyRatios { get; } = new();
    }
}
