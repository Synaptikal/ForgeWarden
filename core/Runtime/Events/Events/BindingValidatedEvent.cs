namespace LiveGameDev.Core.Events
{
    public class BindingValidatedEvent : LGD_EventBase
    {
        public string BindingId    { get; }
        public LGD_ValidationReport Report { get; }
        public BindingValidatedEvent(string bindingId, LGD_ValidationReport report)
        {
            BindingId = bindingId;
            Report    = report;
        }
    }
}
