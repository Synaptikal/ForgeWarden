using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Collapsible validation report panel shown at the bottom of the NLM window.
    /// </summary>
    public class NLM_ReportPanel : VisualElement
    {
        private readonly Foldout _foldout;
        private readonly ScrollView _scroll;

        /// <summary>
        /// Creates a new report panel.
        /// </summary>
        public NLM_ReportPanel()
        {
            _foldout = new Foldout { text = "Validation Report", value = false };
            _foldout.style.borderTopWidth = 1;
            _foldout.style.borderTopColor = new Color(0.2f, 0.2f, 0.2f);
            _foldout.style.paddingTop = 4;
            _scroll = new ScrollView { style = { maxHeight = 160 } };
            _foldout.Add(_scroll);
            Add(_foldout);
        }

        /// <summary>
        /// Displays a validation report.
        /// </summary>
        /// <param name="report">The report to display</param>
        public void ShowReport(NLM_ValidationReport report)
        {
            _scroll.Clear();
            if (report == null) return;
            _foldout.text = $"Validation — {report.OverallStatus} ({report.Entries.Count} entries)";
            _foldout.value = report.HasErrors || report.HasWarnings;

            foreach (var e in report.Entries)
            {
                var row = new VisualElement
                { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
                var badge = new Label(StatusBadge(e.Status))
                {
                    style = {
                        width = 80,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = StatusColor(e.Status)
                    }
                };
                var msg = new Label($"[{e.Tag}] {e.Message}")
                { style = { whiteSpace = WhiteSpace.Normal, flexGrow = 1 } };
                row.Add(badge); row.Add(msg);
                if (!string.IsNullOrEmpty(e.SuggestedFix))
                {
                    var fix = new Label($"  ↳ {e.SuggestedFix}")
                    {
                        style = {
                            whiteSpace = WhiteSpace.Normal,
                            color = new Color(0.6f, 0.8f, 0.6f),
                            marginLeft = 82
                        }
                    };
                    _scroll.Add(row);
                    _scroll.Add(fix);
                }
                else _scroll.Add(row);
            }
        }

        /// <summary>
        /// Displays conflict detection results.
        /// </summary>
        /// <param name="conflicts">List of conflicts to display</param>
        public void ShowConflicts(List<NLM_ConflictDetector.ConflictEntry> conflicts)
        {
            _scroll.Clear();
            _foldout.text = $"Conflicts — {conflicts.Count} found";
            _foldout.value = conflicts.Count > 0;
            foreach (var c in conflicts)
            {
                var row = new VisualElement
                { style = { flexDirection = FlexDirection.Row, marginBottom = 3 } };
                var badge = new Label(StatusBadge(c.Severity))
                {
                    style = {
                        width = 80,
                        color = StatusColor(c.Severity),
                        unityFontStyleAndWeight = FontStyle.Bold
                    }
                };
                var msg = new Label($"[{c.Type}] {c.Message}")
                { style = { whiteSpace = WhiteSpace.Normal, flexGrow = 1 } };
                var ping = new Button(() => { if (c.AffectedObject != null) EditorGUIUtility.PingObject(c.AffectedObject); })
                { text = "→", style = { width = 24, height = 18 } };
                row.Add(badge); row.Add(msg); row.Add(ping);
                _scroll.Add(row);
            }
        }

        private static string StatusBadge(NLM_Status s) => s switch
        {
            NLM_Status.Pass => "✅ Pass",
            NLM_Status.Info => "ℹ Info",
            NLM_Status.Warning => "⚠ Warn",
            NLM_Status.Error => "❌ Error",
            _ => s.ToString()
        };

        private static Color StatusColor(NLM_Status s) => s switch
        {
            NLM_Status.Pass => new Color(0.4f, 0.9f, 0.4f),
            NLM_Status.Info => new Color(0.6f, 0.8f, 1.0f),
            NLM_Status.Warning => new Color(1.0f, 0.85f, 0.3f),
            NLM_Status.Error => new Color(1.0f, 0.35f, 0.35f),
            _ => Color.white
        };
    }
}
