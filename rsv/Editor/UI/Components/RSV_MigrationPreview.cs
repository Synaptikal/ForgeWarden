using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// UI component for previewing schema migrations with side-by-side diff view.
    /// Shows source JSON, transformed JSON, and highlights changes.
    /// </summary>
    public class RSV_MigrationPreview : VisualElement
    {
        private TextField _sourceJsonField;
        private TextField _migratedJsonField;
        private VisualElement _diffContainer;
        private ListView _stepsList;
        private Label _statusLabel;
        private Label _summaryLabel;
        private Button _applyButton;
        private Button _cancelButton;
        private Button _validateButton;

        private MigrationResult _currentResult;
        private DataSchemaDefinition _targetSchema;

        // Events
        public event Action<MigrationResult> OnApplyMigration;
        public event Action OnCancelMigration;
        public event Action<string> OnValidateMigratedJson;

        public RSV_MigrationPreview()
        {
            AddToClassList("rsv-migration-preview");

            BuildUI();
        }

        private void BuildUI()
        {
            // Header
            var header = new Label("Migration Preview") { name = "header" };
            header.AddToClassList("rsv-panel-header");
            Add(header);

            // Status section
            var statusSection = new VisualElement { name = "status-section" };
            statusSection.AddToClassList("rsv-status-section");
            
            _statusLabel = new Label("Ready to migrate") { name = "status-label" };
            _statusLabel.AddToClassList("rsv-status-label");
            
            _summaryLabel = new Label("") { name = "summary-label" };
            _summaryLabel.AddToClassList("rsv-summary-label");
            
            statusSection.Add(_statusLabel);
            statusSection.Add(_summaryLabel);
            Add(statusSection);

            // Migration steps
            var stepsSection = new VisualElement { name = "steps-section" };
            stepsSection.AddToClassList("rsv-steps-section");
            
            var stepsHeader = new Label("Migration Steps") { name = "steps-header" };
            stepsHeader.AddToClassList("rsv-section-header");
            stepsSection.Add(stepsHeader);

            _stepsList = new ListView
            {
                makeItem = MakeStepItem,
                bindItem = BindStepItem,
                selectionType = SelectionType.Single,
                fixedItemHeight = 40
            };
            _stepsList.AddToClassList("rsv-steps-list");
            stepsSection.Add(_stepsList);
            Add(stepsSection);

            // Diff view (split)
            var diffSection = new VisualElement { name = "diff-section" };
            diffSection.AddToClassList("rsv-diff-section");
            
            var diffHeader = new Label("JSON Comparison") { name = "diff-header" };
            diffHeader.AddToClassList("rsv-section-header");
            diffSection.Add(diffHeader);

            var splitContainer = new VisualElement { name = "split-container" };
            splitContainer.AddToClassList("rsv-split-container");

            // Source JSON
            var sourceContainer = new VisualElement { name = "source-container" };
            sourceContainer.AddToClassList("rsv-source-container");
            var sourceLabel = new Label("Source JSON") { name = "source-label" };
            sourceLabel.AddToClassList("rsv-diff-label");
            _sourceJsonField = new TextField { multiline = true, name = "source-json" };
            _sourceJsonField.AddToClassList("rsv-json-field");
            _sourceJsonField.SetEnabled(false);
            sourceContainer.Add(sourceLabel);
            sourceContainer.Add(_sourceJsonField);

            // Migrated JSON
            var migratedContainer = new VisualElement { name = "migrated-container" };
            migratedContainer.AddToClassList("rsv-migrated-container");
            var migratedLabel = new Label("Migrated JSON") { name = "migrated-label" };
            migratedLabel.AddToClassList("rsv-diff-label");
            _migratedJsonField = new TextField { multiline = true, name = "migrated-json" };
            _migratedJsonField.AddToClassList("rsv-json-field");
            _migratedJsonField.SetEnabled(false);
            migratedContainer.Add(migratedLabel);
            migratedContainer.Add(_migratedJsonField);

            splitContainer.Add(sourceContainer);
            splitContainer.Add(migratedContainer);
            diffSection.Add(splitContainer);

            // Diff visualization
            _diffContainer = new VisualElement { name = "diff-container" };
            _diffContainer.AddToClassList("rsv-diff-container");
            var diffVisLabel = new Label("Changes") { name = "diff-vis-label" };
            diffVisLabel.AddToClassList("rsv-section-header");
            _diffContainer.Add(diffVisLabel);
            diffSection.Add(_diffContainer);

            Add(diffSection);

            // Action buttons
            var buttonRow = new VisualElement { name = "button-row" };
            buttonRow.AddToClassList("rsv-button-row");

            _applyButton = new Button(() => OnApplyMigration?.Invoke(_currentResult)) { text = "✓ Apply Migration" };
            _applyButton.AddToClassList("rsv-apply-btn");
            _applyButton.SetEnabled(false);

            _validateButton = new Button(ValidateMigratedJson) { text = "Validate" };
            _validateButton.AddToClassList("rsv-validate-btn");
            _validateButton.SetEnabled(false);

            _cancelButton = new Button(() => OnCancelMigration?.Invoke()) { text = "Cancel" };
            _cancelButton.AddToClassList("rsv-cancel-btn");

            buttonRow.Add(_applyButton);
            buttonRow.Add(_validateButton);
            buttonRow.Add(_cancelButton);
            Add(buttonRow);
        }

        /// <summary>
        /// Displays a migration result in the preview.
        /// </summary>
        public void ShowMigrationResult(MigrationResult result, DataSchemaDefinition targetSchema)
        {
            _currentResult = result;
            _targetSchema = targetSchema;

            if (result == null)
            {
                ClearPreview();
                return;
            }

            // Update status
            _statusLabel.text = result.IsValid 
                ? $"✅ Migration Successful: {result.FromVersion} → {result.ToVersion}"
                : $"❌ Migration Failed: {result.FromVersion} → {result.ToVersion}";
            
            _summaryLabel.text = result.GetSummary();

            // Update JSON fields
            _sourceJsonField.value = result.CurrentJson ?? "";
            _migratedJsonField.value = result.MigratedJson ?? "";

            // Update steps list
            _stepsList.itemsSource = result.Steps ?? new List<MigrationStepResult>();
            _stepsList.Rebuild();

            // Generate diff visualization
            GenerateDiffView(result.CurrentJson, result.MigratedJson);

            // Update button states
            _applyButton.SetEnabled(result.IsValid && result.Errors.Count == 0);
            _validateButton.SetEnabled(!string.IsNullOrEmpty(result.MigratedJson));
        }

        /// <summary>
        /// Clears the preview.
        /// </summary>
        public void ClearPreview()
        {
            _currentResult = null;
            _targetSchema = null;
            
            _statusLabel.text = "Ready to migrate";
            _summaryLabel.text = "";
            _sourceJsonField.value = "";
            _migratedJsonField.value = "";
            _stepsList.itemsSource = new List<MigrationStepResult>();
            _stepsList.Rebuild();
            _diffContainer.Clear();
            
            _applyButton.SetEnabled(false);
            _validateButton.SetEnabled(false);
        }

        /// <summary>
        /// Creates a visual element for a migration step.
        /// </summary>
        private VisualElement MakeStepItem()
        {
            var element = new VisualElement { name = "step-item" };
            element.AddToClassList("rsv-step-item");

            var headerRow = new VisualElement { name = "step-header" };
            headerRow.AddToClassList("rsv-step-header");

            var versionLabel = new Label { name = "step-version" };
            versionLabel.AddToClassList("rsv-step-version");
            headerRow.Add(versionLabel);

            var statusLabel = new Label { name = "step-status" };
            statusLabel.AddToClassList("rsv-step-status");
            headerRow.Add(statusLabel);

            var requiredLabel = new Label { name = "step-required" };
            requiredLabel.AddToClassList("rsv-step-required");
            headerRow.Add(requiredLabel);

            element.Add(headerRow);

            var descLabel = new Label { name = "step-desc" };
            descLabel.AddToClassList("rsv-step-desc");
            element.Add(descLabel);

            var errorLabel = new Label { name = "step-error" };
            errorLabel.AddToClassList("rsv-step-error");
            element.Add(errorLabel);

            return element;
        }

        /// <summary>
        /// Binds data to a step item.
        /// </summary>
        private void BindStepItem(VisualElement element, int index)
        {
            if (_currentResult?.Steps == null || index >= _currentResult.Steps.Count) return;

            var step = _currentResult.Steps[index];

            var versionLabel = element.Q<Label>("step-version");
            versionLabel.text = $"v{step.TargetVersion}";

            var statusLabel = element.Q<Label>("step-status");
            statusLabel.text = GetStatusIcon(step.Status);
            statusLabel.style.color = GetStatusColor(step.Status);

            var requiredLabel = element.Q<Label>("step-required");
            requiredLabel.text = step.IsRequired ? "Required" : "Optional";
            requiredLabel.style.color = step.IsRequired ? new Color(1f, 0.6f, 0f) : new Color(0.3f, 0.8f, 0.3f);

            var descLabel = element.Q<Label>("step-desc");
            descLabel.text = step.Description ?? "No description";
            descLabel.tooltip = step.Output ?? "";

            var errorLabel = element.Q<Label>("step-error");
            if (!string.IsNullOrEmpty(step.Error))
            {
                errorLabel.text = $"Error: {step.Error}";
                errorLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                errorLabel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Generates a visual diff view between source and migrated JSON.
        /// </summary>
        private void GenerateDiffView(string sourceJson, string migratedJson)
        {
            _diffContainer.Clear();

            if (string.IsNullOrEmpty(sourceJson) || string.IsNullOrEmpty(migratedJson))
            {
                var placeholder = new Label("No diff available");
                placeholder.AddToClassList("rsv-placeholder");
                _diffContainer.Add(placeholder);
                return;
            }

            try
            {
                var sourceLines = sourceJson.Split('\n');
                var migratedLines = migratedJson.Split('\n');

                // Simple line-by-line diff
                var diffView = new VisualElement { name = "diff-view" };
                diffView.AddToClassList("rsv-diff-view");

                int maxLines = Math.Max(sourceLines.Length, migratedLines.Length);
                
                for (int i = 0; i < maxLines; i++)
                {
                    var sourceLine = i < sourceLines.Length ? sourceLines[i] : "";
                    var migratedLine = i < migratedLines.Length ? migratedLines[i] : "";

                    var diffLine = new VisualElement { name = $"diff-line-{i}" };
                    diffLine.AddToClassList("rsv-diff-line");

                    if (sourceLine != migratedLine)
                    {
                        // Line changed
                        diffLine.AddToClassList("rsv-diff-changed");
                        
                        var lineNum = new Label($"{i + 1}") { name = "line-num" };
                        lineNum.AddToClassList("rsv-diff-line-num");
                        
                        var oldText = new Label($"- {sourceLine}") { name = "old-text" };
                        oldText.AddToClassList("rsv-diff-removed");
                        
                        var newText = new Label($"+ {migratedLine}") { name = "new-text" };
                        newText.AddToClassList("rsv-diff-added");

                        diffLine.Add(lineNum);
                        diffLine.Add(oldText);
                        diffLine.Add(newText);
                    }
                    else
                    {
                        // Line unchanged
                        var lineNum = new Label($"{i + 1}") { name = "line-num" };
                        lineNum.AddToClassList("rsv-diff-line-num");
                        
                        var text = new Label($"  {sourceLine}") { name = "text" };
                        text.AddToClassList("rsv-diff-unchanged");

                        diffLine.Add(lineNum);
                        diffLine.Add(text);
                    }

                    diffView.Add(diffLine);
                }

                _diffContainer.Add(diffView);
            }
            catch (Exception ex)
            {
                var errorLabel = new Label($"Error generating diff: {ex.Message}");
                errorLabel.AddToClassList("rsv-error");
                _diffContainer.Add(errorLabel);
            }
        }

        /// <summary>
        /// Validates the migrated JSON.
        /// </summary>
        private void ValidateMigratedJson()
        {
            if (_currentResult?.MigratedJson != null)
            {
                OnValidateMigratedJson?.Invoke(_currentResult.MigratedJson);
            }
        }

        /// <summary>
        /// Gets an icon for a migration step status.
        /// </summary>
        private string GetStatusIcon(MigrationStepStatus status) => status switch
        {
            MigrationStepStatus.Pending => "⏳",
            MigrationStepStatus.InProgress => "🔄",
            MigrationStepStatus.Completed => "✅",
            MigrationStepStatus.Failed => "❌",
            MigrationStepStatus.Skipped => "⏭️",
            _ => "❓"
        };

        /// <summary>
        /// Gets a color for a migration step status.
        /// </summary>
        private Color GetStatusColor(MigrationStepStatus status) => status switch
        {
            MigrationStepStatus.Pending => new Color(0.8f, 0.8f, 0.8f),
            MigrationStepStatus.InProgress => new Color(0.3f, 0.6f, 1f),
            MigrationStepStatus.Completed => new Color(0.3f, 0.8f, 0.3f),
            MigrationStepStatus.Failed => new Color(1f, 0.3f, 0.3f),
            MigrationStepStatus.Skipped => new Color(0.8f, 0.8f, 0.3f),
            _ => Color.white
        };
    }
}
