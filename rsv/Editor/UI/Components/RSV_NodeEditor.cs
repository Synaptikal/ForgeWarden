using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// UI component for editing a single RsvSchemaNode and its constraints.
    /// Used within the Schema Designer tree view.
    /// Features debounced field changes for improved performance.
    /// </summary>
    public class RSV_NodeEditor : VisualElement
    {
        private RsvSchemaNode _node;
        private readonly TextField _nameField;
        private readonly EnumField _typeField;
        private readonly Toggle _requiredToggle;
        private readonly Toggle _hasMinMaxToggle;
        private readonly DoubleField _minField;
        private readonly DoubleField _maxField;
        private readonly TextField _enumField;
        private readonly TextField _descriptionField;
        private readonly TextField _defaultField;
        private readonly Button _deleteBtn;
        private readonly Button _addChildBtn;

        // Debounce delay in milliseconds
        private const int DEBOUNCE_DELAY_MS = 300;

        // Field IDs for debouncing
        private string _nameFieldId;
        private string _enumFieldId;
        private string _descriptionFieldId;
        private string _defaultFieldId;
        private string _minFieldId;
        private string _maxFieldId;

        public event System.Action OnChanged;
        public event System.Action OnDeleteRequested;
        public event System.Action OnAddChildRequested;

        public RSV_NodeEditor()
        {
            AddToClassList("rsv-node-editor");

            // Generate unique field IDs
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            _nameFieldId = $"nodeeditor_name_{guid}";
            _enumFieldId = $"nodeeditor_enum_{guid}";
            _descriptionFieldId = $"nodeeditor_desc_{guid}";
            _defaultFieldId = $"nodeeditor_default_{guid}";
            _minFieldId = $"nodeeditor_min_{guid}";
            _maxFieldId = $"nodeeditor_max_{guid}";

            // Name field
            var nameRow = new VisualElement { name = "name-row" };
            nameRow.AddToClassList("rsv-row");
            var nameLabel = new Label("Name:");
            _nameField = new TextField();
            _nameField.RegisterValueChangedCallback(evt => 
            {
                RSV_DebouncedField.Debounce(_nameFieldId, DEBOUNCE_DELAY_MS, OnFieldChanged);
            });
            nameRow.Add(nameLabel);
            nameRow.Add(_nameField);
            Add(nameRow);

            // Type field - no debounce for enum, immediate feedback
            var typeRow = new VisualElement { name = "type-row" };
            typeRow.AddToClassList("rsv-row");
            var typeLabel = new Label("Type:");
            _typeField = new EnumField(RsvFieldType.String);
            _typeField.RegisterValueChangedCallback(evt => OnTypeChanged());
            typeRow.Add(typeLabel);
            typeRow.Add(_typeField);
            Add(typeRow);

            // Required toggle - no debounce, immediate feedback
            _requiredToggle = new Toggle("Required") { value = true };
            _requiredToggle.RegisterValueChangedCallback(evt => OnFieldChanged());
            Add(_requiredToggle);

            // Min/Max section
            var minMaxContainer = new VisualElement { name = "minmax-container" };
            minMaxContainer.AddToClassList("rsv-minmax-section");
            _hasMinMaxToggle = new Toggle("Enable Range Constraints");
            _hasMinMaxToggle.RegisterValueChangedCallback(evt => OnMinMaxToggled());
            minMaxContainer.Add(_hasMinMaxToggle);

            var minMaxRow = new VisualElement { name = "minmax-row" };
            minMaxRow.AddToClassList("rsv-row");
            var minLabel = new Label("Min:");
            var maxLabel = new Label("Max:");
            _minField = new DoubleField { value = 0 };
            _maxField = new DoubleField { value = double.MaxValue };
            _minField.RegisterValueChangedCallback(evt =>
            {
                RSV_DebouncedField.Debounce(_minFieldId, DEBOUNCE_DELAY_MS, OnFieldChanged);
            });
            _maxField.RegisterValueChangedCallback(evt =>
            {
                RSV_DebouncedField.Debounce(_maxFieldId, DEBOUNCE_DELAY_MS, OnFieldChanged);
            });
            minMaxRow.Add(minLabel);
            minMaxRow.Add(_minField);
            minMaxRow.Add(maxLabel);
            minMaxRow.Add(_maxField);
            minMaxContainer.Add(minMaxRow);
            Add(minMaxContainer);

            // Enum field
            var enumRow = new VisualElement { name = "enum-row" };
            enumRow.AddToClassList("rsv-row");
            var enumLabel = new Label("Enum (comma-separated):");
            _enumField = new TextField();
            _enumField.textEdition.placeholder = "e.g. Warrior,Mage,Rogue";
            _enumField.RegisterValueChangedCallback(evt =>
            {
                RSV_DebouncedField.Debounce(_enumFieldId, DEBOUNCE_DELAY_MS, OnFieldChanged);
            });
            enumRow.Add(enumLabel);
            enumRow.Add(_enumField);
            Add(enumRow);

            // Description field
            var descRow = new VisualElement { name = "desc-row" };
            descRow.AddToClassList("rsv-row");
            var descLabel = new Label("Description:");
            _descriptionField = new TextField { multiline = true };
            _descriptionField.RegisterValueChangedCallback(evt =>
            {
                RSV_DebouncedField.Debounce(_descriptionFieldId, DEBOUNCE_DELAY_MS, OnFieldChanged);
            });
            descRow.Add(descLabel);
            descRow.Add(_descriptionField);
            Add(descRow);

            // Default value field
            var defaultRow = new VisualElement { name = "default-row" };
            defaultRow.AddToClassList("rsv-row");
            var defaultLabel = new Label("Default (example only):");
            _defaultField = new TextField();
            _defaultField.RegisterValueChangedCallback(evt =>
            {
                RSV_DebouncedField.Debounce(_defaultFieldId, DEBOUNCE_DELAY_MS, OnFieldChanged);
            });
            defaultRow.Add(defaultLabel);
            defaultRow.Add(_defaultField);
            Add(defaultRow);

            // Action buttons
            var buttonRow = new VisualElement { name = "button-row" };
            buttonRow.AddToClassList("rsv-row");
            _addChildBtn = new Button(() => OnAddChildRequested?.Invoke()) { text = "+ Add Child" };
            _deleteBtn = new Button(() => OnDeleteRequested?.Invoke()) { text = "Delete Node" };
            _deleteBtn.AddToClassList("rsv-delete-btn");
            buttonRow.Add(_addChildBtn);
            buttonRow.Add(_deleteBtn);
            Add(buttonRow);

            UpdateUIState();
        }

        /// <summary>
        /// Cancels any pending debounced updates for this editor.
        /// Call when unloading or switching nodes.
        /// </summary>
        public void CancelPendingUpdates()
        {
            RSV_DebouncedField.Cancel(_nameFieldId);
            RSV_DebouncedField.Cancel(_enumFieldId);
            RSV_DebouncedField.Cancel(_descriptionFieldId);
            RSV_DebouncedField.Cancel(_defaultFieldId);
            RSV_DebouncedField.Cancel(_minFieldId);
            RSV_DebouncedField.Cancel(_maxFieldId);
        }

        public void LoadNode(RsvSchemaNode node)
        {
            _node = node;
            if (_node == null) return;

            _nameField.value = _node.Name;
            _typeField.value = _node.Constraint.FieldType;
            _requiredToggle.value = _node.Constraint.IsRequired;
            _hasMinMaxToggle.value = _node.Constraint.HasMinMax;
            _minField.value = _node.Constraint.Min;
            _maxField.value = _node.Constraint.Max;
            _enumField.value = _node.Constraint.EnumValues != null
                ? string.Join(", ", _node.Constraint.EnumValues)
                : "";
            _descriptionField.value = _node.Constraint.Description ?? "";
            _defaultField.value = _node.Constraint.DefaultValue ?? "";

            UpdateUIState();
        }

        private void OnFieldChanged()
        {
            if (_node == null) return;

            _node.Name = _nameField.value;
            _node.Constraint.FieldType = (RsvFieldType)_typeField.value;
            _node.Constraint.IsRequired = _requiredToggle.value;
            _node.Constraint.HasMinMax = _hasMinMaxToggle.value;
            _node.Constraint.Min = _minField.value;
            _node.Constraint.Max = _maxField.value;
            _node.Constraint.EnumValues = ParseEnumValues(_enumField.value);
            _node.Constraint.Description = _descriptionField.value;
            _node.Constraint.DefaultValue = _defaultField.value;

            OnChanged?.Invoke();
        }

        private void OnTypeChanged()
        {
            OnFieldChanged();
            UpdateUIState();
        }

        private void OnMinMaxToggled()
        {
            OnFieldChanged();
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            var type = (RsvFieldType)_typeField.value;
            var isNumeric = type == RsvFieldType.Integer || type == RsvFieldType.Number;
            var hasMinMax = _hasMinMaxToggle.value;

            // Show/hide min/max based on type and toggle
            var minMaxContainer = this.Q("minmax-container");
            if (minMaxContainer != null)
            {
                minMaxContainer.style.display = isNumeric && hasMinMax
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            // Show/hide add child button based on type
            var canHaveChildren = type == RsvFieldType.Object || type == RsvFieldType.Array;
            _addChildBtn.style.display = canHaveChildren ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private string[] ParseEnumValues(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return System.Array.Empty<string>();
            return input.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }
    }
}
