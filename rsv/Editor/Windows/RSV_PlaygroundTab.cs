using LiveGameDev.Core.Editor.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Paste-and-validate playground.
    /// Designer pastes raw JSON, picks a schema, and sees live validation results.
    /// </summary>
    public class RSV_PlaygroundTab : VisualElement
    {
        private DataSchemaDefinition _schema;
        private readonly TextField _jsonField;
        private readonly LGD_ReportPanel _reportPanel;
        private readonly LGD_StatusBadge _badge;
        private readonly Label _schemaLabel;

        public RSV_PlaygroundTab()
        {
            AddToClassList("rsv-panel");
            AddToClassList("rsv-playground-tab");

            var header = new Label("Validation Playground") { name = "header" };
            header.AddToClassList("rsv-panel-header");
            Add(header);

            // Schema selection row
            var schemaRow = new VisualElement { name = "schema-row" };
            schemaRow.AddToClassList("rsv-row");

            var schemaLabel = new Label("Schema:");
            _schemaLabel = new Label("None selected") { name = "selected-schema" };
            _schemaLabel.AddToClassList("rsv-selected-schema-label");

            var pickBtn = new Button(PickSchema) { text = "Select Schema…" };
            pickBtn.AddToClassList("rsv-pick-schema-btn");

            var clearBtn = new Button(ClearSchema) { text = "Clear" };
            clearBtn.AddToClassList("rsv-clear-schema-btn");

            schemaRow.Add(schemaLabel);
            schemaRow.Add(_schemaLabel);
            schemaRow.Add(pickBtn);
            schemaRow.Add(clearBtn);
            Add(schemaRow);

            // JSON input field
            _jsonField = new TextField("JSON Input")
            {
                multiline = true,
                value = "{\n  \n}"
            };
            _jsonField.AddToClassList("rsv-playground-input");
            Add(_jsonField);

            // Validation controls
            var controlRow = new VisualElement { name = "control-row" };
            controlRow.AddToClassList("rsv-row");

            var validateBtn = new Button(RunValidation) { text = "▶ Validate" };
            validateBtn.AddToClassList("rsv-validate-btn");

            var clearJsonBtn = new Button(() => _jsonField.value = "{\n  \n}") { text = "Clear JSON" };

            controlRow.Add(validateBtn);
            controlRow.Add(clearJsonBtn);
            Add(controlRow);

            // Status badge
            _badge = new LGD_StatusBadge();
            _badge.style.marginTop = 8;
            Add(_badge);

            // Report panel
            _reportPanel = new LGD_ReportPanel();
            _reportPanel.AddToClassList("rsv-playground-report");
            Add(_reportPanel);
        }

        private void PickSchema()
        {
            RSV_SchemaPicker.Show(schema =>
            {
                _schema = schema;
                _schemaLabel.text = $"{schema.DisplayName ?? schema.name} (v{schema.Version ?? "1.0.0"})";
                _schemaLabel.style.color = new Color(0.3f, 0.8f, 0.3f);
            });
        }

        private void ClearSchema()
        {
            _schema = null;
            _schemaLabel.text = "None selected";
            _schemaLabel.style.color = Color.white;
            _reportPanel.Clear();
            _badge.SetStatus(LiveGameDev.Core.ValidationStatus.Pass);
        }

        private void RunValidation()
        {
            if (_schema == null)
            {
                Debug.LogWarning("[RSV] Please select a schema first.");
                return;
            }

            var json = _jsonField.value;
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[RSV] Please enter JSON to validate.");
                return;
            }

            var report = RsvValidator.Validate(_schema, json);
            _badge.SetStatus(report.OverallStatus);
            _reportPanel.Populate(report);

            if (report.OverallStatus == LiveGameDev.Core.ValidationStatus.Pass)
            {
                Debug.Log($"[RSV] Validation passed for schema '{_schema.SchemaId}'.");
            }
            else
            {
                Debug.LogWarning($"[RSV] Validation found {report.Entries.Count} issue(s) for schema '{_schema.SchemaId}'.");
            }
        }
    }
}
