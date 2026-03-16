using LiveGameDev.Core.Editor.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Shows details and validation results for the selected JsonSourceBinding asset.
    /// Updates automatically when Project window selection changes.
    /// </summary>
    public class RSV_BindingInspector : VisualElement
    {
        private JsonSourceBinding   _binding;
        private readonly LGD_ReportPanel _reportPanel;
        private readonly LGD_StatusBadge _badge;
        private readonly Label           _bindingLabel;

        public RSV_BindingInspector()
        {
            AddToClassList("rsv-panel");

            var header = new Label("Binding Inspector") { name = "header" };
            header.AddToClassList("rsv-panel-header");

            _bindingLabel = new Label("No binding selected.");
            _badge        = new LGD_StatusBadge(LiveGameDev.Core.ValidationStatus.Pass);
            var validateBtn = new Button(ValidateSelected) { text = "Validate This Binding" };
            _reportPanel  = new LGD_ReportPanel();

            Add(header);
            Add(_bindingLabel);
            Add(_badge);
            Add(validateBtn);
            Add(_reportPanel);
        }

        public void OnSelectionChanged(Object selected)
        {
            _binding = selected as JsonSourceBinding;
            _bindingLabel.text = _binding != null
                ? $"Binding: {_binding.name}"
                : "No binding selected.";
            _reportPanel.Clear();
        }

        private void ValidateSelected()
        {
            if (_binding == null) return;
            var report = RsvValidator.ValidateBinding(_binding);
            _badge.SetStatus(report.OverallStatus);
            _reportPanel.Populate(report);
        }
    }
}
