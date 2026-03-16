using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// UI panel displaying scene objects with narrative bindings.
    /// </summary>
    /// <remarks>
    /// Right panel of the NLM main window. Shows all NarrativeObjectBinding
    /// components in the scene and which rules are active for the current beat.
    /// </remarks>
    public class NLM_ObjectListPanel : VisualElement
    {
        private readonly Label _header;
        private readonly ScrollView _scroll;

        /// <summary>
        /// Creates a new object list panel.
        /// </summary>
        public NLM_ObjectListPanel()
        {
            style.minWidth = 220;
            style.maxWidth = 300;
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.22f, 0.22f, 0.22f);
            style.paddingLeft = 8;

            _header = new Label("Scene Objects")
            { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
            Add(_header);
            _scroll = new ScrollView(); Add(_scroll);
        }

        /// <summary>
        /// Displays bindings and their active rules for the given state.
        /// </summary>
        /// <param name="bindings">List of bindings to display</param>
        /// <param name="state">Current narrative state to evaluate against</param>
        public void ShowBindings(List<NarrativeObjectBinding> bindings, NarrativeStateDefinition state)
        {
            _scroll.Clear();
            _header.text = $"Scene Objects ({bindings.Count})";
            foreach (var b in bindings)
            {
                if (b == null || b.HideInNLMList) continue;
                var rules = NLM_Evaluator.ResolveBinding(b, state);
                var active = rules.Count > 0;
                var row = MakeRow(
                    active ? $"✔  {b.gameObject.name}" : $"·  {b.gameObject.name}",
                    active ? new Color(0.4f, 0.9f, 0.5f) : new Color(0.55f, 0.55f, 0.55f),
                    b.gameObject);
                if (active && rules[0] != null)
                {
                    var sub = new Label($"   → {rules[0].RuleName}")
                    { style = { fontSize = 10, color = new Color(0.6f, 0.8f, 0.6f) } };
                    _scroll.Add(row);
                    _scroll.Add(sub);
                }
                else _scroll.Add(row);
            }
        }

        /// <summary>
        /// Displays diff results between two beats.
        /// </summary>
        /// <param name="entries">List of diff entries to display</param>
        public void ShowDiff(List<NLM_BeatDiff.DiffEntry> entries)
        {
            _scroll.Clear();
            _header.text = $"Diff ({entries.Count} changes)";
            foreach (var e in entries)
            {
                var row = MakeRow($"▶  {e.Summary}", new Color(1f, 0.85f, 0.3f), e.Target);
                _scroll.Add(row);
            }
        }

        private static VisualElement MakeRow(string label, Color color, GameObject ping)
        {
            var row = new VisualElement
            { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            var lbl = new Label(label)
            { style = { flexGrow = 1, whiteSpace = WhiteSpace.Normal, color = color } };
            var btn = new Button(() => { if (ping) EditorGUIUtility.PingObject(ping); })
            { text = "→", style = { width = 24, height = 18 } };
            row.Add(lbl); row.Add(btn);
            return row;
        }
    }
}
