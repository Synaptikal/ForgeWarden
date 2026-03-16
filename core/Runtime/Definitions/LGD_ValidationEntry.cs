using System;

namespace LiveGameDev.Core
{
    /// <summary>
    /// A single validation result entry within an LGD_ValidationReport.
    /// </summary>
    [Serializable]
    public class LGD_ValidationEntry
    {
        /// <summary>Severity of this entry.</summary>
        public ValidationStatus Status { get; set; }

        /// <summary>Category label (e.g. "TypeMismatch", "MissingField", "RangeViolation").</summary>
        public string Category { get; set; }

        /// <summary>Human-readable description of the issue.</summary>
        public string Message { get; set; }

        /// <summary>Project-relative asset path of the offending asset, if applicable.</summary>
        public string AssetPath { get; set; }

        /// <summary>Line number within a JSON file, if applicable. -1 if not applicable.</summary>
        public int Line { get; set; } = -1;

        /// <summary>Optional suggested fix text shown to the user.</summary>
        public string SuggestedFix { get; set; }

        public LGD_ValidationEntry() { }

        public LGD_ValidationEntry(
            ValidationStatus status,
            string category,
            string message,
            string assetPath = null,
            int line = -1,
            string suggestedFix = null)
        {
            Status       = status;
            Category     = category;
            Message      = message;
            AssetPath    = assetPath;
            Line         = line;
            SuggestedFix = suggestedFix;
        }
    }
}
