using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Visual node editor for building DataSchemaDefinition trees.
    /// Split into partial classes by concern:
    ///   RSV_SchemaDesigner.cs          — Core: fields, constructor, load, UI state
    ///   RSV_SchemaDesigner.NodeOps.cs  — Node CRUD operations and tree building
    ///   RSV_SchemaDesigner.SchemaOps.cs — Import/export, validate, compile, migration, preview
    /// </summary>
    public partial class RSV_SchemaDesigner : VisualElement
    {
        private DataSchemaDefinition _target;
        private RsvSchemaNode _selectedNode;
        private readonly VisualElement _treeContainer;
        private readonly VisualElement _editorContainer;
        private readonly VisualElement _previewContainer;
        private readonly Label _schemaLabel;
        private readonly TextField _schemaIdField;
        private readonly TextField _versionField;
        private readonly TextField _descriptionField;
        private readonly Button _addRootBtn;
        private readonly Button _importBtn;
        private readonly Button _exportBtn;
        private readonly Button _validateBtn;
        private readonly Button _addMigrationBtn;
        private readonly Label _previewLabel;
        private readonly VisualElement _migrationContainer;
        private readonly ListView _migrationList;

        // Performance optimization components
        private RSV_VirtualizedTreeView _virtualizedTreeView;
        private RSV_NodeEditor _nodeEditor;

        // Debounce field IDs
        private string _schemaIdFieldId;
        private string _versionFieldId;
        private string _descriptionFieldId;

        // Throttling
        private float _lastPreviewUpdate;
        private const float PREVIEW_THROTTLE_MS = 500f;

        // Node cache for incremental updates
        private Dictionary<string, VisualElement> _nodeElementCache;

        public RSV_SchemaDesigner()
        {
            AddToClassList("rsv-panel");
            AddToClassList("rsv-schema-designer");

            _nodeElementCache = new Dictionary<string, VisualElement>();
            _schemaIdFieldId  = $"schemadesigner_schemaid_{Guid.NewGuid():N}";
            _versionFieldId   = $"schemadesigner_version_{Guid.NewGuid():N}";
            _descriptionFieldId = $"schemadesigner_desc_{Guid.NewGuid():N}";

            // Header
            var header = new Label("Schema Designer") { name = "header" };
            header.AddToClassList("rsv-panel-header");
            Add(header);

            // Schema metadata section
            var metaContainer = new VisualElement { name = "meta-container" };
            metaContainer.AddToClassList("rsv-meta-section");

            _schemaLabel = new Label("No schema selected");
            _schemaLabel.AddToClassList("rsv-schema-label");

            var idRow = new VisualElement { name = "id-row" };
            idRow.AddToClassList("rsv-row");
            var idLabel = new Label("Schema ID:");
            _schemaIdField = new TextField();
            _schemaIdField.RegisterValueChangedCallback(evt =>
                RSV_DebouncedField.Debounce(_schemaIdFieldId, 300, OnMetadataChanged));
            idRow.Add(idLabel);
            idRow.Add(_schemaIdField);

            var versionRow = new VisualElement { name = "version-row" };
            versionRow.AddToClassList("rsv-row");
            var versionLabel = new Label("Version:");
            _versionField = new TextField { value = "1.0.0" };
            _versionField.RegisterValueChangedCallback(evt =>
                RSV_DebouncedField.Debounce(_versionFieldId, 300, OnMetadataChanged));
            versionRow.Add(versionLabel);
            versionRow.Add(_versionField);

            var descRow = new VisualElement { name = "desc-row" };
            descRow.AddToClassList("rsv-row");
            var descLabel = new Label("Description:");
            _descriptionField = new TextField { multiline = true };
            _descriptionField.RegisterValueChangedCallback(evt =>
                RSV_DebouncedField.Debounce(_descriptionFieldId, 300, OnMetadataChanged));
            descRow.Add(descLabel);
            descRow.Add(_descriptionField);

            metaContainer.Add(_schemaLabel);
            metaContainer.Add(idRow);
            metaContainer.Add(versionRow);
            metaContainer.Add(descRow);
            Add(metaContainer);

            // Toolbar
            var toolbar = new VisualElement { name = "toolbar" };
            toolbar.AddToClassList("rsv-toolbar");

            _addRootBtn = new Button(AddRootNode)    { text = "+ Add Root Field"  };
            _importBtn  = new Button(ImportSchema)   { text = "Import JSON Schema" };
            _exportBtn  = new Button(ExportSchema)   { text = "Export JSON Schema" };
            _validateBtn = new Button(ValidateSchema) { text = "Validate Schema"   };
            var compileBtn = new Button(CompileSchema) { text = "Compile Schema"   };

            toolbar.Add(_addRootBtn);
            toolbar.Add(_importBtn);
            toolbar.Add(_exportBtn);
            toolbar.Add(_validateBtn);
            toolbar.Add(compileBtn);
            Add(toolbar);

            // Migration section
            _migrationContainer = new VisualElement { name = "migration-container" };
            _migrationContainer.AddToClassList("rsv-migration-section");

            var migrationHeader = new VisualElement { name = "migration-header" };
            migrationHeader.AddToClassList("rsv-row");
            var migrationLabel = new Label("Migration Hints") { name = "migration-label" };
            migrationLabel.AddToClassList("rsv-section-header");
            _addMigrationBtn = new Button(AddMigrationHint) { text = "+ Add Migration Hint" };
            _addMigrationBtn.AddToClassList("rsv-add-migration-btn");
            migrationHeader.Add(migrationLabel);
            migrationHeader.Add(_addMigrationBtn);
            _migrationContainer.Add(migrationHeader);

            _migrationList = new ListView
            {
                makeItem = () => new MigrationHintItem(),
                bindItem = (el, i) => ((MigrationHintItem)el).SetData(_target?.MigrationHints[i], i, this),
                selectionType = SelectionType.Single
            };
            _migrationList.AddToClassList("rsv-migration-list");
            _migrationContainer.Add(_migrationList);
            Add(_migrationContainer);

            // Split view: tree left, editor right
            var splitView = new VisualElement { name = "split-view" };
            splitView.AddToClassList("rsv-split-view");

            var treeSection = new VisualElement { name = "tree-section" };
            treeSection.AddToClassList("rsv-tree-section");
            var treeHeader = new Label("Field Tree") { name = "tree-header" };
            treeHeader.AddToClassList("rsv-section-header");

            _virtualizedTreeView = new RSV_VirtualizedTreeView();
            _virtualizedTreeView.OnNodeSelected  += OnTreeNodeSelected;
            _virtualizedTreeView.OnNodeExpanded  += OnTreeNodeExpanded;
            _virtualizedTreeView.OnNodeCollapsed += OnTreeNodeCollapsed;

            treeSection.Add(treeHeader);
            treeSection.Add(_virtualizedTreeView);

            var editorSection = new VisualElement { name = "editor-section" };
            editorSection.AddToClassList("rsv-editor-section");
            var editorHeader = new Label("Field Editor") { name = "editor-header" };
            editorHeader.AddToClassList("rsv-section-header");
            _editorContainer = new VisualElement { name = "editor-container" };
            _editorContainer.AddToClassList("rsv-editor-container");
            var placeholder = new Label("Select a field to edit its properties.");
            placeholder.AddToClassList("rsv-placeholder");
            _editorContainer.Add(placeholder);
            editorSection.Add(editorHeader);
            editorSection.Add(_editorContainer);

            splitView.Add(treeSection);
            splitView.Add(editorSection);
            Add(splitView);

            // Preview section
            var previewSection = new VisualElement { name = "preview-section" };
            previewSection.AddToClassList("rsv-preview-section");
            var previewHeader = new Label("JSON Example Preview") { name = "preview-header" };
            previewHeader.AddToClassList("rsv-section-header");
            _previewLabel = new Label { name = "preview-label" };
            _previewLabel.AddToClassList("rsv-preview-label");
            _previewContainer = new ScrollView { name = "preview-container" };
            _previewContainer.AddToClassList("rsv-preview-container");
            _previewContainer.Add(_previewLabel);
            previewSection.Add(previewHeader);
            previewSection.Add(_previewContainer);
            Add(previewSection);

            UpdateUIState();
        }

        public void LoadSchema(DataSchemaDefinition schema)
        {
            RSV_DebouncedField.Cancel(_schemaIdFieldId);
            RSV_DebouncedField.Cancel(_versionFieldId);
            RSV_DebouncedField.Cancel(_descriptionFieldId);
            RSV_DebouncedField.Cancel("preview_update");
            _nodeEditor?.CancelPendingUpdates();

            _target = schema;
            if (_target == null)
            {
                _schemaLabel.text = "No schema selected";
                _virtualizedTreeView?.LoadSchema(null);
                UpdateUIState();
                return;
            }

            _schemaLabel.text      = $"Schema: {_target.DisplayName ?? _target.name}";
            _schemaIdField.value   = _target.SchemaId ?? "";
            _versionField.value    = _target.Version ?? "1.0.0";
            _descriptionField.value = _target.Description ?? "";

            _migrationList.itemsSource = _target.MigrationHints ?? new List<RsvMigrationHint>();
            _migrationList.Rebuild();

            _virtualizedTreeView?.LoadSchema(_target);
            UpdatePreview();
            UpdateUIState();
        }

        private void OnTreeNodeSelected(RsvSchemaNode node) => SelectNode(node);

        private void OnTreeNodeExpanded(RsvSchemaNode node)
        {
            _virtualizedTreeView.Refresh();
        }

        private void OnTreeNodeCollapsed(RsvSchemaNode node)
        {
            _virtualizedTreeView.Refresh();
        }

        private void UpdateUIState()
        {
            var hasSchema = _target != null;
            _addRootBtn.SetEnabled(hasSchema);
            _importBtn.SetEnabled(hasSchema);
            _exportBtn.SetEnabled(hasSchema);
            _validateBtn.SetEnabled(hasSchema);
            _schemaIdField.SetEnabled(hasSchema);
            _versionField.SetEnabled(hasSchema);
            _descriptionField.SetEnabled(hasSchema);
        }

        private string GetTypeIcon(RsvFieldType type) => type switch
        {
            RsvFieldType.String  => "📝",
            RsvFieldType.Integer => "🔢",
            RsvFieldType.Number  => "🔢",
            RsvFieldType.Boolean => "✓",
            RsvFieldType.Object  => "📦",
            RsvFieldType.Array   => "📋",
            _                    => "?"
        };

        // ── Nested: Migration hint list item ─────────────────────
        private class MigrationHintItem : VisualElement
        {
            private readonly Label _versionLabel;
            private readonly Label _descriptionLabel;
            private readonly Label _requiredLabel;
            private readonly Button _removeBtn;

            public MigrationHintItem()
            {
                AddToClassList("rsv-migration-item");

                var container = new VisualElement { name = "container" };
                container.AddToClassList("rsv-migration-item-container");

                _versionLabel     = new Label { name = "version" };
                _versionLabel.AddToClassList("rsv-migration-version");

                _descriptionLabel = new Label { name = "description" };
                _descriptionLabel.AddToClassList("rsv-migration-description");

                _requiredLabel = new Label { name = "required" };
                _requiredLabel.AddToClassList("rsv-migration-required");

                _removeBtn = new Button { text = "✕" };
                _removeBtn.AddToClassList("rsv-remove-migration-btn");

                container.Add(_versionLabel);
                container.Add(_descriptionLabel);
                container.Add(_requiredLabel);
                container.Add(_removeBtn);
                Add(container);
            }

            public void SetData(RsvMigrationHint hint, int index, RSV_SchemaDesigner designer)
            {
                _versionLabel.text     = $"v{hint.TargetVersion ?? "?"}";
                _descriptionLabel.text = hint.Description ?? "No description";
                _descriptionLabel.tooltip = hint.Description;
                _requiredLabel.text    = hint.IsRequired ? "Required" : "Optional";
                _requiredLabel.style.color = hint.IsRequired
                    ? new Color(1f, 0.6f, 0f)
                    : new Color(0.3f, 0.8f, 0.3f);

                _removeBtn.clicked += () => designer.RemoveMigrationHint(index);
            }
        }
    }
}
