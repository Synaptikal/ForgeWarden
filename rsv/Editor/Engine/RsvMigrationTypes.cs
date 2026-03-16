using System.Collections.Generic;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>Status of a single migration step.</summary>
    public enum MigrationStepStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Skipped
    }

    /// <summary>Result of a single migration step.</summary>
    public class MigrationStepResult
    {
        public string TargetVersion { get; set; }
        public string Description   { get; set; }
        public bool IsRequired      { get; set; }
        public MigrationStepStatus Status { get; set; }
        public string Output        { get; set; }
        public string Error         { get; set; }
    }

    /// <summary>Result of a complete migration operation.</summary>
    public class MigrationResult
    {
        public string FromVersion   { get; set; }
        public string ToVersion     { get; set; }
        public string CurrentJson   { get; set; }
        public string MigratedJson  { get; set; }
        public int StepsExecuted    { get; set; }
        public List<MigrationStepResult> Steps    { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Errors   { get; set; }
        public bool IsValid          { get; set; }

        /// <summary>Returns a one-line summary of the migration outcome.</summary>
        public string GetSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Migration: {FromVersion} → {ToVersion}");
            sb.AppendLine($"Steps Executed: {StepsExecuted}/{Steps?.Count ?? 0}");
            sb.AppendLine($"Status: {(IsValid ? "✅ Valid" : "❌ Invalid")}");

            if (Warnings?.Count > 0) sb.AppendLine($"Warnings: {Warnings.Count}");
            if (Errors?.Count > 0)   sb.AppendLine($"Errors: {Errors.Count}");

            return sb.ToString();
        }
    }
}
