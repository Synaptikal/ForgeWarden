namespace LiveGameDev.Core.Events
{
    public class SimulationCompleteEvent : LGD_EventBase
    {
        public string SimName      { get; }
        public LGD_ValidationReport Report { get; }
        public SimulationCompleteEvent(string simName, LGD_ValidationReport report)
        {
            SimName = simName;
            Report  = report;
        }
    }
}
