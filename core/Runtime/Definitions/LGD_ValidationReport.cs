using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LiveGameDev.Core
{
    /// <summary>
    /// Unified validation report produced by RSV, ZDHG, and ESS tools.
    /// </summary>
    [Serializable]
    public class LGD_ValidationReport
    {
        public string ToolId         { get; private set; }
        public DateTime Timestamp    { get; private set; }
        public string ProjectName    { get; private set; }

        private readonly List<LGD_ValidationEntry> _entries = new();
        public IReadOnlyList<LGD_ValidationEntry> Entries => _entries;

        /// <summary>The highest severity level present across all entries.</summary>
        public ValidationStatus OverallStatus
        {
            get
            {
                if (_entries.Count == 0) return ValidationStatus.Pass;
                return _entries.Max(e => e.Status);
            }
        }

        public bool HasErrors    => _entries.Any(e => e.Status == ValidationStatus.Error);
        public bool HasCritical  => _entries.Any(e => e.Status == ValidationStatus.Critical);

        public LGD_ValidationReport(string toolId)
        {
            ToolId      = toolId;
            Timestamp   = DateTime.Now;
            ProjectName = UnityEngine.Application.productName;
        }

        /// <summary>Add a single validation entry to this report.</summary>
        public void AddEntry(LGD_ValidationEntry entry) => _entries.Add(entry);

        /// <summary>Convenience: add an entry without constructing it manually.</summary>
        public void Add(ValidationStatus status, string category, string message,
            string assetPath = null, int line = -1, string suggestedFix = null)
            => _entries.Add(new LGD_ValidationEntry(status, category, message, assetPath, line, suggestedFix));

        /// <summary>Export report as a Markdown-formatted string.</summary>
        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Validation Report — {ToolId}");
            sb.AppendLine($"**Generated:** {Timestamp:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**Project:** {ProjectName}  ");
            sb.AppendLine($"**Overall:** {OverallStatus}  ");
            sb.AppendLine();
            sb.AppendLine("| Status | Category | Message | Asset | Line |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var e in _entries)
            {
                var msg = e.Message?.Replace("|", "\\|") ?? "";
                var cat = e.Category?.Replace("|", "\\|") ?? "";
                sb.AppendLine($"| {e.Status} | {cat} | {msg} | {e.AssetPath ?? "-"} | {(e.Line >= 0 ? e.Line.ToString() : "-")} |");
            }
            return sb.ToString();
        }

        /// <summary>Export report as CSV.</summary>
        public string ToCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Status,Category,Message,AssetPath,Line,SuggestedFix");
            foreach (var e in _entries)
            {
                var msg = e.Message?.Replace("\"", "\"\"") ?? "";
                var fix = e.SuggestedFix?.Replace("\"", "\"\"") ?? "";
                sb.AppendLine($"{e.Status},{e.Category},\"{msg}\",{e.AssetPath ?? ""},{(e.Line >= 0 ? e.Line.ToString() : "")},\"{fix}\"");
            }
            return sb.ToString();
        }

        /// <summary>Export report as JSON string.</summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(new
            {
                toolId = ToolId,
                timestamp = Timestamp,
                projectName = ProjectName,
                overallStatus = OverallStatus.ToString(),
                entries = _entries.Select(e => new
                {
                    status = e.Status.ToString(),
                    category = e.Category,
                    message = e.Message,
                    assetPath = e.AssetPath,
                    line = e.Line,
                    suggestedFix = e.SuggestedFix
                })
            }, Formatting.Indented);
        }
    }
}
