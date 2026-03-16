using System;

namespace LiveGameDev.Core
{
    /// <summary>
    /// Five-level severity used across all Live Game Dev Suite validation results.
    /// Pass  = all checks passed.
    /// Info  = informational note only.
    /// Warning = non-blocking issue; should be reviewed.
    /// Error = blocking issue; must be fixed before shipping.
    /// Critical = data corruption risk; may cause runtime failures.
    /// </summary>
    public enum ValidationStatus
    {
        Pass,
        Info,
        Warning,
        Error,
        Critical
    }
}
