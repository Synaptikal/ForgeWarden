using System.Collections.Generic;
using LiveGameDev.Core;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Base result class for economy simulation.
    /// </summary>
    public class SimulationResult
    {
        public SimConfig Config { get; }
        public List<SimState> History { get; }
        public List<EssAlert> Alerts { get; }
        public LGD_ValidationReport Report { get; }

        public SimulationResult(
            SimConfig config,
            List<SimState> history,
            List<EssAlert> alerts,
            LGD_ValidationReport report)
        {
            Config = config;
            History = history;
            Alerts = alerts;
            Report = report;
        }
    }
}