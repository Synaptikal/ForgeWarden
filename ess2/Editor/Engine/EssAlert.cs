using LiveGameDev.Core;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Represents an economy balance alert detected during simulation.
    /// </summary>
    public class EssAlert
    {
        public string ItemName { get; }
        public string AlertType { get; }
        public ValidationStatus Severity { get; }
        public string Message { get; }
        public int Day { get; }

        public EssAlert(
            string itemName,
            string alertType,
            ValidationStatus severity,
            string message,
            int day)
        {
            ItemName = itemName;
            AlertType = alertType;
            Severity = severity;
            Message = message;
            Day = day;
        }
    }
}