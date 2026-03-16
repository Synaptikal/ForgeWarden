using LiveGameDev.Core;
using LiveGameDev.ESS;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Configuration for an economy simulation run.
    /// </summary>
    public class SimConfig
    {
        [Header("Simulation")]
        public int SimulationDays = 90;

        public int PlayerCount = 1000;

        public int Seed = 42;

        [Header("Economy")]
        public ItemDefinition[] TrackedItems;

        public SourceDefinition[] Sources;

        public SinkDefinition[] Sinks;

        [Header("Players")]
        public (PlayerProfileDefinition profile, float percentage)[] PlayerMix;

        /// <summary>
        /// Validate this configuration.
        /// </summary>
        public LGD_ValidationReport Validate()
        {
            var report = new LGD_ValidationReport("SimConfig");

            if (SimulationDays <= 0)
                report.Add(ValidationStatus.Error, "Config", "SimulationDays must be > 0.");

            if (PlayerCount <= 0)
                report.Add(ValidationStatus.Error, "Config", "PlayerCount must be > 0.");

            if (TrackedItems == null || TrackedItems.Length == 0)
                report.Add(ValidationStatus.Warning, "Config",
                    "No tracked items. Simulation will run but produce no price/supply data.");

            if (Sources == null || Sources.Length == 0)
                report.Add(ValidationStatus.Warning, "Config",
                    "No sources — economy will be static. Add SourceDefinitions.");

            if (Sinks == null || Sinks.Length == 0)
                report.Add(ValidationStatus.Warning, "Config",
                    "No sinks — inflation risk is high. Add SinkDefinitions.");

            if (PlayerMix == null || PlayerMix.Length == 0)
                report.Add(ValidationStatus.Warning, "Config",
                    "No player archetypes defined. Using default behavior.");

            float totalPercentage = 0f;
            if (PlayerMix != null)
            {
                foreach (var (profile, pct) in PlayerMix)
                {
                    if (profile == null)
                        report.Add(ValidationStatus.Error, "Config", "PlayerMix contains null profile.");
                    totalPercentage += pct;
                }
            }

            if (Mathf.Abs(totalPercentage - 1f) > 0.01f && PlayerMix != null && PlayerMix.Length > 0)
                report.Add(ValidationStatus.Warning, "Config",
                    $"PlayerMix percentages sum to {totalPercentage:P0} (should be 100%).");

            return report;
        }
    }
}