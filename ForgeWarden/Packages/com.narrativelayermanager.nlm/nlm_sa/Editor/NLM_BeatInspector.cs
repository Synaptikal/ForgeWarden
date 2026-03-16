using System.Text;
using UnityEngine.UIElements;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// UI panel displaying detailed information about the selected narrative beat.
    /// </summary>
    /// <remarks>
    /// Center-bottom panel of the NLM main window. Shows beat name, index,
    /// designer notes, and all variable values for the beat's state.
    /// </remarks>
    public class NLM_BeatInspector : VisualElement
    {
        private readonly Label _title;
        private readonly Label _notes;
        private readonly Label _vars;
        private readonly Label _index;

        /// <summary>
        /// Creates a new beat inspector panel.
        /// </summary>
        public NLM_BeatInspector()
        {
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 8;
            style.paddingBottom = 8;

            Add(_title = new Label { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold, marginBottom = 4 } });
            Add(_index = new Label { style = { color = new UnityEngine.Color(0.6f, 0.6f, 0.6f) } });
            Add(_notes = new Label
            {
                style = {
                    whiteSpace = WhiteSpace.Normal,
                    marginTop = 4,
                    color = new UnityEngine.Color(0.72f, 0.72f, 0.72f)
                }
            });
            Add(new Label("Variables:") { style = { marginTop = 6, unityFontStyleAndWeight = UnityEngine.FontStyle.Bold } });
            Add(_vars = new Label { style = { whiteSpace = WhiteSpace.Normal } });
        }

        /// <summary>
        /// Displays information for the specified beat.
        /// </summary>
        /// <param name="beat">The beat to display, or null to show empty state</param>
        public void Show(NarrativeBeat beat)
        {
            if (beat == null)
            {
                _title.text = "(no beat selected)";
                _index.text = _notes.text = _vars.text = "";
                return;
            }
            _title.text = beat.BeatName;
            _index.text = $"Beat index: {beat.Index}";
            _notes.text = beat.DesignerNotes ?? "";

            if (beat.State == null) { _vars.text = "(no state assigned)"; return; }

            var sb = new StringBuilder();
            foreach (var v in beat.State.Variables)
                sb.AppendLine($"  {v.Name}  =  {v.GetValue()}  ({v.Type})");
            _vars.text = sb.ToString();
        }
    }
}
