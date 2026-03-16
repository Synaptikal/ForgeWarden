using System;
using System.Collections.Generic;
using System.Text;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Simplified validation result struct for runtime validation.
    /// Uses a struct to enable zero-allocation validation for the common pass case.
    /// </summary>
    public struct RsvValidationResult
    {
        /// <summary>
        /// Overall validation status.
        /// </summary>
        public RsvValidationStatus Status { get; private set; }

        /// <summary>
        /// Number of validation entries (errors, warnings, etc.).
        /// </summary>
        public int EntryCount => entries?.Count ?? 0;

        /// <summary>
        /// Whether validation passed without any issues.
        /// </summary>
        public bool IsPass => Status == RsvValidationStatus.Pass;

        /// <summary>
        /// Whether validation has any errors or critical issues.
        /// </summary>
        public bool HasErrors => Status == RsvValidationStatus.Error || Status == RsvValidationStatus.Critical;

        /// <summary>
        /// Whether validation has any warnings.
        /// </summary>
        public bool HasWarnings => Status == RsvValidationStatus.Warning;

        /// <summary>
        /// Whether validation has any critical issues.
        /// </summary>
        public bool HasCritical => Status == RsvValidationStatus.Critical;

        /// <summary>
        /// Internal list of validation entries. Only allocated when there are issues.
        /// </summary>
        private List<RsvValidationEntry> entries;

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static RsvValidationResult Pass()
        {
            return new RsvValidationResult
            {
                Status = RsvValidationStatus.Pass,
                entries = null
            };
        }

        /// <summary>
        /// Creates a validation result with a specific status.
        /// </summary>
        public static RsvValidationResult Create(RsvValidationStatus status)
        {
            return new RsvValidationResult
            {
                Status = status,
                entries = new List<RsvValidationEntry>()
            };
        }

        /// <summary>
        /// Adds a validation entry to this result.
        /// </summary>
        public void AddEntry(RsvValidationEntry entry)
        {
            if (entries == null)
                entries = new List<RsvValidationEntry>();

            entries.Add(entry);

            // Update overall status based on the new entry
            if (entry.Status > Status)
                Status = entry.Status;
        }

        /// <summary>
        /// Adds a validation entry with the specified parameters.
        /// </summary>
        public void AddEntry(RsvValidationStatus status, string category, string message, string path = null)
        {
            AddEntry(new RsvValidationEntry
            {
                Status = status,
                Category = category,
                Message = message,
                Path = path
            });
        }

        /// <summary>
        /// Gets all validation entries.
        /// </summary>
        public IReadOnlyList<RsvValidationEntry> GetEntries()
        {
            return entries ?? (IReadOnlyList<RsvValidationEntry>)Array.Empty<RsvValidationEntry>();
        }

        /// <summary>
        /// Gets entries filtered by status.
        /// </summary>
        public IReadOnlyList<RsvValidationEntry> GetEntriesByStatus(RsvValidationStatus status)
        {
            if (entries == null)
                return Array.Empty<RsvValidationEntry>();

            var filtered = new List<RsvValidationEntry>();
            foreach (var entry in entries)
            {
                if (entry.Status == status)
                    filtered.Add(entry);
            }
            return filtered;
        }

        /// <summary>
        /// Gets entries filtered by category.
        /// </summary>
        public IReadOnlyList<RsvValidationEntry> GetEntriesByCategory(string category)
        {
            if (entries == null)
                return Array.Empty<RsvValidationEntry>();

            var filtered = new List<RsvValidationEntry>();
            foreach (var entry in entries)
            {
                if (entry.Category == category)
                    filtered.Add(entry);
            }
            return filtered;
        }

        /// <summary>
        /// Returns a string representation of this result.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"RSV Validation Result: {Status}");
            sb.AppendLine($"Entries: {EntryCount}");

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    sb.AppendLine($"  [{entry.Status}] {entry.Category}: {entry.Message}");
                    if (!string.IsNullOrEmpty(entry.Path))
                        sb.AppendLine($"    Path: {entry.Path}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Merges another validation result into this one.
        /// </summary>
        public void Merge(RsvValidationResult other)
        {
            if (other.entries == null || other.entries.Count == 0)
                return;

            if (entries == null)
                entries = new List<RsvValidationEntry>();

            foreach (var entry in other.entries)
            {
                entries.Add(entry);
                if (entry.Status > Status)
                    Status = entry.Status;
            }
        }
    }

    /// <summary>
    /// A single validation entry representing an issue or information.
    /// </summary>
    public struct RsvValidationEntry
    {
        /// <summary>
        /// Status of this entry.
        /// </summary>
        public RsvValidationStatus Status;

        /// <summary>
        /// Category of the issue (e.g., "TypeMismatch", "MissingField", "RangeViolation").
        /// </summary>
        public string Category;

        /// <summary>
        /// Human-readable message describing the issue.
        /// </summary>
        public string Message;

        /// <summary>
        /// JSON path to the field that caused the issue (e.g., "items[0].name").
        /// </summary>
        public string Path;

        /// <summary>
        /// Optional suggested fix for the issue.
        /// </summary>
        public string SuggestedFix;

        /// <summary>
        /// Returns a string representation of this entry.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Status}] {Category}: {Message}");
            if (!string.IsNullOrEmpty(Path))
                sb.Append($" (Path: {Path})");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Validation status levels.
    /// </summary>
    public enum RsvValidationStatus
    {
        /// <summary>
        /// Validation passed without any issues.
        /// </summary>
        Pass = 0,

        /// <summary>
        /// Informational message (not an error).
        /// </summary>
        Info = 1,

        /// <summary>
        /// Warning that should be reviewed but doesn't block execution.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Error that should be addressed.
        /// </summary>
        Error = 3,

        /// <summary>
        /// Critical error that prevents validation from completing.
        /// </summary>
        Critical = 4
    }
}