using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using LiveGameDev.Core.Events;
using LiveGameDev.ESS;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// ESS v2 primary simulation engine.
    ///
    /// Day tick pipeline:
    ///   1. Each player executes a full behavior day (farm → craft → list → buy)
    ///   2. Auction house resolves supply/demand → new prices
    ///   3. Economy metrics snapshot (Gini, velocity, supply ratios)
    ///   4. Alert evaluator produces today's alerts
    ///   5. SimState aggregated for UI / export
    ///
    /// Population scaling:
    ///   For PlayerCount > MaxFullSimPlayers the sim runs a sample of
    ///   MaxFullSimPlayers representative players and scales results
    ///   linearly to the full population. This keeps runtime under 60s
    ///   for 10,000 players / 90 days.
    /// </summary>
    public static class ESS_SimulatorV2
    {
        /// <summary>
        /// Maximum per-archetype representative sample count.
        /// Real player populations scale from this.
        /// </summary>
        public const int MaxSamplePlayersPerArchetype = 50;

        // ── Public async API ──────────────────────────────────────
        public static async Task<SimulationResultV2> RunAsync(
            SimConfig config,
            List<CraftingRecipeDefinition> recipes,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            var report  = new LGD_ValidationReport("ESS-v2");
            var metrics = new EconomyMetrics();
            var history = new List<SimState>();
            var allAlerts = new List<EssAlert>();

            // ── Validate and build crafting graph ─────────────────
            ValidateConfig(config, report);
            if (report.HasErrors)
                return new SimulationResultV2(config, history, allAlerts, report, metrics);

            var graph = new CraftingGraph(recipes);
            if (graph.Build(report) == ValidationStatus.Critical)
                return new SimulationResultV2(config, history, allAlerts, report, metrics);

            // ── Build player population ───────────────────────────
            var players   = BuildPopulation(config);
            var prices    = InitialPrices(config);
            float currencyPool = players.Sum(p => p.Currency);

            var ah = new AuctionHouseModel(
                listingFeePct: 0.02f,
                taxPct:        0.05f,
                priceInertia:  0.3f,
                maxPriceMovePct: 0.20f);

            var rng = new System.Random(config.Seed);

            // ── Main simulation loop ──────────────────────────────
            for (int day = 0; day < config.SimulationDays; day++)
            {
                ct.ThrowIfCancellationRequested();

                // ── 1. Player behavior day ────────────────────────
                var supplyListings    = new Dictionary<string, float>();
                var demandWanted      = new Dictionary<string, float>();
                var currencyInjected  = new Dictionary<string, float>();

                foreach (var player in players)
                {
                    ESS_PlayerAgentV2.ExecuteDay(
                        player, GetProfile(config, player.ArcheType),
                        config, graph, prices,
                        supplyListings, demandWanted, rng);
                }

                // Scale sample → full population
                float scale = (float)config.PlayerCount / Mathf.Max(players.Count, 1);
                var scaledSupply = Scale(supplyListings, scale);
                var scaledDemand = Scale(demandWanted,   scale);

                // ── 2. Auction house resolution ───────────────────
                var closingPrices = ah.Resolve(
                    scaledSupply, scaledDemand, prices, ref currencyPool, rng);
                foreach (var kvp in closingPrices) prices[kvp.Key] = kvp.Value;

                // ── 3. Economy metrics snapshot ───────────────────
                var dayMetrics = metrics.Snapshot(day + 1, players, prices, ah, config);

                // ── 4. Alerts ─────────────────────────────────────
                var dayAlerts = ESS_AlertEvaluatorV2.EvaluateDayMetrics(
                    dayMetrics, metrics, config, currencyInjected);
                allAlerts.AddRange(dayAlerts);

                // ── 5. SimState (aggregated for backward compat) ──
                history.Add(BuildSimState(day + 1, players, prices, currencyPool, dayMetrics));

                // ── 6. Reset daily AH listings ────────────────────
                foreach (var player in players) player.AhListings.Clear();

                progress?.Report((float)(day + 1) / config.SimulationDays);
                await Task.Yield();
            }

            BuildFinalReport(report, allAlerts, history, metrics);
            LGD_EventBus.Publish(new SimulationCompleteEvent(
                $"ESS2_Seed{config.Seed}_D{config.SimulationDays}", report));

            return new SimulationResultV2(config, history, allAlerts, report, metrics);
        }

        // ── Private helpers ───────────────────────────────────────

        private static List<SimPlayerState> BuildPopulation(SimConfig config)
        {
            var players = new List<SimPlayerState>();
            int id = 0;

            foreach (var (profile, pct) in config.PlayerMix)
            {
                if (profile == null) continue;
                // Sample capped at MaxSamplePlayersPerArchetype; scale up in aggregation
                int sampleCount = Mathf.Min(
                    Mathf.RoundToInt(config.PlayerCount * pct),
                    MaxSamplePlayersPerArchetype);

                float startGold = 50f * profile.EfficiencyMultiplier;
                for (int i = 0; i < sampleCount; i++)
                {
                    var p = new SimPlayerState(id++, profile.name, startGold);
                    // Spread starting levels (new & veteran mix)
                    p.NormalizedLevel = (float)i / sampleCount;
                    players.Add(p);
                }
            }

            // Fallback: if no player mix defined, build 50 default players
            if (players.Count == 0)
                for (int i = 0; i < 50; i++)
                    players.Add(new SimPlayerState(i, "Default", 50f));

            return players;
        }

        private static Dictionary<string, float> InitialPrices(SimConfig config)
        {
            var d = new Dictionary<string, float>();
            foreach (var item in config.TrackedItems)
                if (item != null) d[item.name] = item.BaseValue;
            return d;
        }

        private static PlayerProfileDefinition GetProfile(SimConfig config, string archType)
        {
            foreach (var (profile, _) in config.PlayerMix)
                if (profile?.name == archType) return profile;
            return null;
        }

        private static Dictionary<string, float> Scale(
            Dictionary<string, float> source, float factor)
        {
            var d = new Dictionary<string, float>(source.Count);
            foreach (var kvp in source) d[kvp.Key] = kvp.Value * factor;
            return d;
        }

        private static SimState BuildSimState(
            int day,
            IReadOnlyList<SimPlayerState> players,
            Dictionary<string, float> prices,
            float currencyPool,
            DayMetrics dayMetrics)
        {
            var state = new SimState
            {
                Day            = day,
                TotalCurrency  = currencyPool,
                GiniCoefficient = dayMetrics.GiniCoefficient
            };
            foreach (var kvp in prices)
            {
                state.ItemPrices[kvp.Key] = kvp.Value;
                float totalSupply = players.Sum(p => p.GetQuantity(kvp.Key));
                state.ItemSupply[kvp.Key] = totalSupply;
            }
            return state;
        }

        private static void ValidateConfig(SimConfig config, LGD_ValidationReport report)
        {
            if (config == null) { report.Add(ValidationStatus.Error, "Config", "Config is null."); return; }
            if (config.PlayerCount <= 0)
                report.Add(ValidationStatus.Error, "Config", "PlayerCount must be > 0.");
            if (config.SimulationDays <= 0)
                report.Add(ValidationStatus.Error, "Config", "SimulationDays must be > 0.");
            if (config.TrackedItems == null || config.TrackedItems.Length == 0)
                report.Add(ValidationStatus.Warning, "Config",
                    "No tracked items. Simulation will run but produce no price/supply data.");
            if (config.Sources == null || config.Sources.Length == 0)
                report.Add(ValidationStatus.Warning, "Config",
                    "No sources — economy will be static. Add SourceDefinitions.");
            if (config.PlayerMix == null || config.PlayerMix.Length == 0)
                report.Add(ValidationStatus.Warning, "Config",
                    "No player archetypes defined. Using default behavior.");
        }

        private static void BuildFinalReport(
            LGD_ValidationReport report,
            List<EssAlert> alerts,
            List<SimState> history,
            EconomyMetrics metrics)
        {
            // Deduplicate alerts by type+item (only report worst day)
            var seen = new HashSet<string>();
            foreach (var a in alerts)
            {
                string key = $"{a.AlertType}:{a.ItemName}";
                if (!seen.Add(key)) continue;
                report.Add(a.Severity, a.AlertType, a.Message,
                    suggestedFix: GetFix(a.AlertType));
            }

            // Economy summary
            if (history.Count > 0 && metrics.History.Count > 0)
            {
                var final = metrics.History[^1];
                report.Add(ValidationStatus.Info, "Economy",
                    $"Final Gini: {final.GiniCoefficient:F3} | " +
                    $"Money velocity: {final.MoneyVelocity:F2} | " +
                    $"Currency supply: {final.TotalCurrencySupply:N0}g | " +
                    $"Wealth gap: {final.WealthGapRatio:F0}x");
            }

            if (!report.HasErrors && !report.HasCritical && alerts.Count == 0)
                report.Add(ValidationStatus.Pass, "Economy",
                    "No balance issues detected. Economy appears stable.");
        }

        private static string GetFix(string alertType) => alertType switch
        {
            "InflationSpiral"      => "Add an item sink (crafting recipe, vendor, or event) that consumes this item.",
            "DeflationCrash"      => "Reduce source drop rate or add a recipe that requires this item as input.",
            "GoldFlooding"         => "Increase AH tax/listing fees or add a gold-based crafting cost.",
            "WealthInequalityHigh" => "Add catch-up mechanics: login bonuses, leveling rewards, or progressive tax sinks.",
            "OverfarmingPressure"  => "Reduce source rate or add crafting recipes that consume this item.",
            "SupplyCollapse"       => "Increase source rate or reduce the quantity required by sinks.",
            "MoneyVelocityHigh"    => "Increase AH listing fees or add a cooldown to selling.",
            "AHFeesTooLow"         => "Raise listing fee (2%+) or transaction tax (5%+).",
            _                      => "Review source/sink balance for this item."
        };
    }

    /// <summary>Extended SimulationResult including full metrics history.</summary>
    public class SimulationResultV2 : SimulationResult
    {
        public EconomyMetrics Metrics { get; }

        public SimulationResultV2(
            SimConfig config,
            List<SimState> history,
            List<EssAlert> alerts,
            LGD_ValidationReport report,
            EconomyMetrics metrics)
            : base(config, history, alerts, report)
        {
            Metrics = metrics;
        }
    }
}
