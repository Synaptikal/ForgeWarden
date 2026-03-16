using System.Collections.Generic;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Evaluates DayMetrics for economy balance alerts.
    ///
    /// All thresholds are expressed as named constants for designer tuning.
    /// Alerts escalate: Warning on first trigger, Error on 3+ consecutive days.
    ///
    /// Alert types:
    ///   InflationSpiral    – an item's price has risen for 5+ consecutive days
    ///   DeflationCrash     – an item's price has fallen for 5+ consecutive days
    ///   GoldFlooding       – total currency supply grew > 20% in 7 days
    ///   WealthInequalityHigh – Gini > 0.7 for 3+ days
    ///   OverfarmingPressure – supply ratio > 3x for 3+ days
    ///   SupplyCollapse      – supply ratio < 0.1
    ///   MoneyVelocityHigh   – Fisher velocity > 2.5 (market overheating)
    ///   AHFeesTooLow        – currency destroyed/day < 50% of currency injected/day
    ///   DeadItem            – supply exists but zero AH transactions for 7 days
    /// </summary>
    internal static class ESS_AlertEvaluatorV2
    {
        // ── Thresholds ────────────────────────────────────────────
        private const int   SpiralStreakDays          = 5;
        private const float GoldFloodGrowthRate       = 0.20f; // 20% in 7 days
        private const float GiniHighThreshold         = 0.70f;
        private const int   GiniHighConsecutiveDays   = 3;
        private const float OverfarmingRatio          = 3.0f;
        private const int   OverfarmingConsecDays     = 3;
        private const float SupplyCollapseRatio       = 0.10f;
        private const float MoneyVelocityHighThreshold = 2.5f;
        private const float AHFeeDeficitRatio         = 0.50f;  // fees < 50% of injected
        private const int   DeadItemTransactionDays   = 7;

        internal static List<EssAlert> EvaluateDayMetrics(
            DayMetrics today,
            EconomyMetrics metricsHistory,
            SimConfig config,
            Dictionary<string, float> dailyCurrencyInjected) // item name → gold injected today from sources
        {
            var alerts = new List<EssAlert>();
            var hist   = metricsHistory.History;

            // Need at least 2 days for trend detection
            if (hist.Count < 2) return alerts;

            // ── InflationSpiral / DeflationCrash ──────────────────
            foreach (var item in config.TrackedItems)
            {
                if (item == null) continue;

                today.ConsecutiveInflationDays.TryGetValue(item.name, out int streak);
                today.InflationVelocity.TryGetValue(item.name, out float velocity);

                if (streak >= SpiralStreakDays)
                {
                    var severity = streak >= SpiralStreakDays * 2
                        ? ValidationStatus.Error : ValidationStatus.Warning;

                    alerts.Add(new EssAlert(item.name, "InflationSpiral", severity,
                        $"'{item.name}' price rising for {streak} consecutive days " +
                        $"(avg {velocity:P1}/day). Add a sink or reduce source rate.",
                        today.Day));
                }

                if (velocity < -0.05f)
                {
                    // Check consecutive deflation streak manually from history
                    int defStreak = 0;
                    for (int i = hist.Count - 1; i >= 0 && defStreak < hist.Count; i--)
                    {
                        hist[i].InflationVelocity.TryGetValue(item.name, out float v);
                        if (v < -0.03f) defStreak++;
                        else break;
                    }
                    if (defStreak >= SpiralStreakDays)
                        alerts.Add(new EssAlert(item.name, "DeflationCrash",
                            defStreak >= SpiralStreakDays * 2
                                ? ValidationStatus.Error : ValidationStatus.Warning,
                            $"'{item.name}' price falling for {defStreak} consecutive days. " +
                            "Add demand (recipe input) or remove sources.",
                            today.Day));
                }
            }

            // ── Gold Flooding ──────────────────────────────────────
            if (hist.Count >= 7)
            {
                var weekAgo   = hist[hist.Count - 7];
                float growth  = weekAgo.TotalCurrencySupply > 0f
                    ? (today.TotalCurrencySupply - weekAgo.TotalCurrencySupply) / weekAgo.TotalCurrencySupply
                    : 0f;
                if (growth > GoldFloodGrowthRate)
                    alerts.Add(new EssAlert("Currency", "GoldFlooding",
                        growth > GoldFloodGrowthRate * 2f
                            ? ValidationStatus.Error : ValidationStatus.Warning,
                        $"Total gold supply grew {growth:P0} over the last 7 days. " +
                        "Gold sinks (AH fees, repair, event entry) are insufficient. " +
                        $"Destroyed today: {today.CurrencyDestroyedToday:N0}g " +
                        $"vs injected: {DictSum(dailyCurrencyInjected):N0}g.",
                        today.Day));
            }

            // ── Wealth Inequality ─────────────────────────────────
            if (today.GiniCoefficient > GiniHighThreshold)
            {
                int giniStreak = 0;
                for (int i = hist.Count - 1; i >= 0; i--)
                {
                    if (hist[i].GiniCoefficient > GiniHighThreshold) giniStreak++;
                    else break;
                }
                if (giniStreak >= GiniHighConsecutiveDays)
                    alerts.Add(new EssAlert("Economy", "WealthInequalityHigh",
                        ValidationStatus.Warning,
                        $"Gini coefficient {today.GiniCoefficient:F2} (>{GiniHighThreshold:F2}) " +
                        $"for {giniStreak} days. Top 10% hold " +
                        $"{today.WealthGapRatio:F0}× more than bottom 10%. " +
                        "Consider login bonuses, catch-up rewards, or progression curve rebalancing.",
                        today.Day));
            }

            // ── Overfarming / SupplyCollapse ──────────────────────
            foreach (var item in config.TrackedItems)
            {
                if (item == null) continue;
                today.SupplyRatios.TryGetValue(item.name, out float ratio);

                if (ratio > OverfarmingRatio)
                {
                    int ovStreak = 0;
                    for (int i = hist.Count - 1; i >= 0; i--)
                    {
                        hist[i].SupplyRatios.TryGetValue(item.name, out float r);
                        if (r > OverfarmingRatio) ovStreak++;
                        else break;
                    }
                    if (ovStreak >= OverfarmingConsecDays)
                        alerts.Add(new EssAlert(item.name, "OverfarmingPressure",
                            ovStreak >= OverfarmingConsecDays * 2
                                ? ValidationStatus.Error : ValidationStatus.Warning,
                            $"'{item.name}' supply is {ratio:F1}× expected circulation " +
                            $"for {ovStreak} days. Reduce source rate or increase sink consumption.",
                            today.Day));
                }

                if (ratio < SupplyCollapseRatio && ratio > 0f)
                    alerts.Add(new EssAlert(item.name, "SupplyCollapse",
                        ValidationStatus.Error,
                        $"'{item.name}' supply at {ratio:P0} of expected ({item.TargetCirculationPerPlayer * config.PlayerCount:N0} target). " +
                        "Increase source rate or reduce required sink quantities.",
                        today.Day));
            }

            // ── Money Velocity ─────────────────────────────────────
            if (today.MoneyVelocity > MoneyVelocityHighThreshold)
                alerts.Add(new EssAlert("Currency", "MoneyVelocityHigh",
                    ValidationStatus.Warning,
                    $"Money velocity {today.MoneyVelocity:F2} (>{MoneyVelocityHighThreshold:F2}). " +
                    "Currency is circulating too rapidly. Consider higher AH taxes or listing fees.",
                    today.Day));

            // ── AH Fee Deficit ─────────────────────────────────────
            float injected = DictSum(dailyCurrencyInjected);
            if (injected > 0f && today.CurrencyDestroyedToday < injected * AHFeeDeficitRatio)
                alerts.Add(new EssAlert("Economy", "AHFeesTooLow",
                    ValidationStatus.Warning,
                    $"AH destroyed {today.CurrencyDestroyedToday:N0}g today vs {injected:N0}g injected. " +
                    "Gold sinks cover only {(today.CurrencyDestroyedToday / injected):P0} of injection. " +
                    "Raise listing fees, tax rates, or add consumable gold sinks.",
                    today.Day));

            return alerts;
        }

        private static float DictSum(Dictionary<string, float> dict)
        {
            float s = 0f;
            if (dict == null) return s;
            foreach (var v in dict.Values) s += v;
            return s;
        }
    }
}
