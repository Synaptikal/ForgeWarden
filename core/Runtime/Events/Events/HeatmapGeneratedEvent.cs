namespace LiveGameDev.Core.Events
{
    public class HeatmapGeneratedEvent : LGD_EventBase
    {
        public string ZoneGuid     { get; }
        public LGD_ValidationReport Report { get; }
        public HeatmapGeneratedEvent(string zoneGuid, LGD_ValidationReport report)
        {
            ZoneGuid = zoneGuid;
            Report   = report;
        }
    }
}
