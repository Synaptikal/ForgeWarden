using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// Status levels for validation entries.
    /// </summary>
    public enum NLM_Status { Pass, Info, Warning, Error }

    /// <summary>
    /// A single entry in a validation report.
    /// </summary>
    [Serializable]
    public class NLM_ValidationEntry
    {
        /// <summary>The severity/status of this entry</summary>
        public NLM_Status Status;

        /// <summary>Category or tag for grouping entries</summary>
        public string Tag;

        /// <summary>The validation message</summary>
        public string Message;

        /// <summary>Path to the asset if applicable</summary>
        public string AssetPath;

        /// <summary>Suggested fix for the issue</summary>
        public string SuggestedFix;

        /// <summary>
        /// Creates a new validation entry.
        /// </summary>
        public NLM_ValidationEntry(NLM_Status status, string tag, string message,
            string assetPath = "", string suggestedFix = "")
        {
            Status = status;
            Tag = tag;
            Message = message;
            AssetPath = assetPath;
            SuggestedFix = suggestedFix;
        }
    }

    /// <summary>
    /// Self-contained validation report for the Narrative Layer Manager.
    /// </summary>
    /// <remarks>
    /// No external dependencies — works without any other packages.
    /// Can export to Markdown, CSV, or JSON formats.
    /// </remarks>
    public class NLM_ValidationReport
    {
        /// <summary>The context or source of this validation report</summary>
        public string Context { get; }

        /// <summary>All entries in this report</summary>
        public List<NLM_ValidationEntry> Entries { get; } = new();

        /// <summary>Returns true if any entry has Error status</summary>
        public bool HasErrors => Entries.Exists(e => e.Status == NLM_Status.Error);

        /// <summary>Returns true if any entry has Warning status</summary>
        public bool HasWarnings => Entries.Exists(e => e.Status == NLM_Status.Warning);

        /// <summary>
        /// The overall status of this report based on all entries.
        /// </summary>
        public NLM_Status OverallStatus
        {
            get
            {
                if (HasErrors) return NLM_Status.Error;
                if (HasWarnings) return NLM_Status.Warning;
                if (Entries.Exists(e => e.Status == NLM_Status.Info)) return NLM_Status.Info;
                return NLM_Status.Pass;
            }
        }

        /// <summary>
        /// Creates a new validation report.
        /// </summary>
        /// <param name="context">Context or source of the report (e.g., "Layer", "State")</param>
        public NLM_ValidationReport(string context = "NLM") => Context = context;

        /// <summary>
        /// Adds a new entry to this report.
        /// </summary>
        public void Add(NLM_Status status, string tag, string message,
            string assetPath = "", string suggestedFix = "")
            => Entries.Add(new NLM_ValidationEntry(status, tag, message, assetPath, suggestedFix));

        /// <summary>Clears all entries from this report</summary>
        public void Clear() => Entries.Clear();

        #region Export Formats

        /// <summary>
        /// Exports this report as a Markdown table.
        /// </summary>
        /// <returns>Markdown formatted string</returns>
        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Narrative Layer Manager — Validation Report");
            sb.AppendLine($"**Context:** {Context}  ");
            sb.AppendLine($"**Status:** {OverallStatus}  ");
            sb.AppendLine($"**Entries:** {Entries.Count}  ");
            sb.AppendLine();
            sb.AppendLine("| Status | Tag | Message | Fix |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var e in Entries)
                sb.AppendLine($"| {Badge(e.Status)} | {e.Tag} | {e.Message} | {e.SuggestedFix} |");
            return sb.ToString();
        }

        /// <summary>
        /// Exports this report as CSV.
        /// </summary>
        /// <returns>CSV formatted string</returns>
        public string ToCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Status,Tag,Message,AssetPath,SuggestedFix");
            foreach (var e in Entries)
                sb.AppendLine($"{e.Status},{Csv(e.Tag)},{Csv(e.Message)},{Csv(e.AssetPath)},{Csv(e.SuggestedFix)}");
            return sb.ToString();
        }

        /// <summary>
        /// Exports this report as JSON.
        /// </summary>
        /// <returns>JSON formatted string</returns>
        public string ToJson()
            => JsonUtility.ToJson(new ReportJson { context = Context, entries = Entries }, prettyPrint: true);

        #endregion

        #region Private Helpers

        private static string Badge(NLM_Status s) => s switch
        {
            NLM_Status.Pass => "✅ Pass",
            NLM_Status.Info => "ℹ Info",
            NLM_Status.Warning => "⚠ Warning",
            NLM_Status.Error => "❌ Error",
            _ => s.ToString()
        };

        private static string Csv(string s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";

        [Serializable]
        private class ReportJson
        {
            public string context;
            public List<NLM_ValidationEntry> entries;
        }

        #endregion
    }
}
