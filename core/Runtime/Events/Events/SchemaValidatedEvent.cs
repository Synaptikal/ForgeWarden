namespace LiveGameDev.Core.Events
{
    public class SchemaValidatedEvent : LGD_EventBase
    {
        public string SchemaGuid   { get; }
        public LGD_ValidationReport Report { get; }
        public SchemaValidatedEvent(string schemaGuid, LGD_ValidationReport report)
        {
            SchemaGuid = schemaGuid;
            Report     = report;
        }
    }
}
