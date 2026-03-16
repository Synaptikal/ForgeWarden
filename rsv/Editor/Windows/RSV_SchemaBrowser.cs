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
    /// Displays all DataSchemaDefinition assets in the project.
    /// Allows selection to open in the Schema Designer tab.
    /// Enhanced with search, tag filtering, and double-click to edit.
    /// </summary>
    public class RSV_SchemaBrowser : VisualElement
    {
        private readonly ListView _list;
        private readonly TextField _searchField;
        private readonly VisualElement _tagFilterContainer;
        private DataSchemaDefinition[] _allSchemas;
        private DataSchemaDefinition[] _filteredSchemas;
        private string[] _selectedTags;

        public RSV_SchemaBrowser()
        {
            AddToClassList("rsv-panel");
            AddToClassList("rsv-schema-browser");

            var header = new Label("All Schemas") { name = "header" };
            header.AddToClassList("rsv-panel-header");
            Add(header);

            // Search field
            var searchRow = new VisualElement { name = "search-row" };
            searchRow.AddToClassList("rsv-row");
            var searchLabel = new Label("Search:");
            _searchField = new TextField();
            _searchField.textEdition.placeholder = "Search by name, ID, or description...";
            _searchField.RegisterValueChangedCallback(evt => OnSearchChanged());
            searchRow.Add(searchLabel);
            searchRow.Add(_searchField);
            Add(searchRow);

            // Tag filter
            _tagFilterContainer = new VisualElement { name = "tag-filter-container" };
            _tagFilterContainer.AddToClassList("rsv-tag-filter-section");
            var tagFilterLabel = new Label("Filter by Tags:");
            tagFilterLabel.AddToClassList("rsv-section-header");
            _tagFilterContainer.Add(tagFilterLabel);
            Add(_tagFilterContainer);

            // Toolbar
            var toolbar = new VisualElement { name = "toolbar" };
            toolbar.AddToClassList("rsv-toolbar");
            var refreshBtn = new Button(Refresh) { text = "↺ Refresh" };
            var createBtn = new Button(CreateNewSchema) { text = "+ New Schema" };
            toolbar.Add(refreshBtn);
            toolbar.Add(createBtn);
            Add(toolbar);

            // Schema list
            _list = new ListView { selectionType = SelectionType.Single };
            _list.makeItem = () => new SchemaListItem();
            _list.bindItem = (el, i) =>
            {
                if (i < _filteredSchemas.Length)
                {
                    ((SchemaListItem)el).SetData(_filteredSchemas[i]);
                }
            };
            _list.onItemsChosen += OnSchemaSelected;
            _list.onSelectionChange += OnSelectionChanged;
            Add(_list);

            Refresh();
        }

        private void Refresh()
        {
            _allSchemas = LGD_AssetUtility.FindAllAssetsOfType<DataSchemaDefinition>();
            BuildTagFilter();
            ApplyFilters();
        }

        private void BuildTagFilter()
        {
            _tagFilterContainer.Clear();

            // Collect all unique tags
            var allTags = new System.Collections.Generic.HashSet<string>();
            foreach (var schema in _allSchemas)
            {
                if (schema.Tags != null)
                {
                    foreach (var tag in schema.Tags)
                    {
                        if (!string.IsNullOrEmpty(tag))
                            allTags.Add(tag);
                    }
                }
            }

            if (allTags.Count == 0)
            {
                var noTagsLabel = new Label("No tags found in schemas.");
                noTagsLabel.AddToClassList("rsv-placeholder");
                _tagFilterContainer.Add(noTagsLabel);
                return;
            }

            var tagRow = new VisualElement { name = "tag-row" };
            tagRow.AddToClassList("rsv-row");

            foreach (var tag in allTags.OrderBy(t => t))
            {
                var tagToggle = new Toggle(tag) { value = false };
                tagToggle.AddToClassList("rsv-tag-toggle");
                tagToggle.RegisterValueChangedCallback(evt => OnTagFilterChanged());
                tagRow.Add(tagToggle);
            }

            _tagFilterContainer.Add(tagRow);
        }

        private void OnSearchChanged()
        {
            ApplyFilters();
        }

        private void OnTagFilterChanged()
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var searchText = _searchField.value.ToLower();

            // Get selected tags
            var selectedTags = new System.Collections.Generic.HashSet<string>();
            foreach (var toggle in _tagFilterContainer.Query<Toggle>().ToList())
            {
                if (toggle.value)
                {
                    selectedTags.Add(toggle.text);
                }
            }

            _filteredSchemas = _allSchemas
                .Where(schema =>
                {
                    // Search filter
                    var matchesSearch = string.IsNullOrEmpty(searchText) ||
                        (schema.DisplayName?.ToLower().Contains(searchText) ?? false) ||
                        (schema.SchemaId?.ToLower().Contains(searchText) ?? false) ||
                        (schema.Description?.ToLower().Contains(searchText) ?? false) ||
                        schema.name.ToLower().Contains(searchText);

                    // Tag filter
                    var matchesTags = selectedTags.Count == 0 ||
                        (schema.Tags != null && schema.Tags.Any(tag => selectedTags.Contains(tag)));

                    return matchesSearch && matchesTags;
                })
                .ToArray();

            _list.itemsSource = _filteredSchemas;
            _list.Rebuild();
        }

        private void OnSchemaSelected(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                if (item is DataSchemaDefinition schema)
                {
                    OpenInDesigner(schema);
                    break;
                }
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            // Handle selection change if needed
        }

        private void OpenInDesigner(DataSchemaDefinition schema)
        {
            // Find the main window and switch to designer tab
            var window = EditorWindow.GetWindow<RSV_MainWindow>();
            if (window != null)
            {
                // Access the schema designer via reflection or public method
                // For now, we'll select the asset which will trigger the selection change handler
                Selection.activeObject = schema;
            }
        }

        private void CreateNewSchema()
        {
            var path = EditorUtility.SaveFilePanel(
                "Create New Schema",
                "Assets/LiveGameDevSuite/Schemas",
                "NewDataSchema",
                "asset");

            if (string.IsNullOrEmpty(path)) return;

            // Convert to project-relative path
            var projectPath = LGD_AssetUtility.GetRelativePath(path);

            var schema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            schema.SchemaId = "new-schema-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            schema.Version = "1.0.0";
            schema.DisplayName = "New Schema";

            AssetDatabase.CreateAsset(schema, projectPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = schema;
            Refresh();

            Debug.Log($"[RSV] Created new schema at: {projectPath}");
        }

        /// <summary>
        /// Custom list item for displaying schema information.
        /// </summary>
        private class SchemaListItem : VisualElement
        {
            private readonly Label _nameLabel;
            private readonly Label _idLabel;
            private readonly Label _versionLabel;
            private readonly Label _tagsLabel;

            public SchemaListItem()
            {
                AddToClassList("rsv-schema-list-item");

                var container = new VisualElement { name = "container" };
                container.AddToClassList("rsv-schema-item-container");

                _nameLabel = new Label { name = "name" };
                _nameLabel.AddToClassList("rsv-schema-name");

                _idLabel = new Label { name = "id" };
                _idLabel.AddToClassList("rsv-schema-id");

                _versionLabel = new Label { name = "version" };
                _versionLabel.AddToClassList("rsv-schema-version");

                _tagsLabel = new Label { name = "tags" };
                _tagsLabel.AddToClassList("rsv-schema-tags");

                container.Add(_nameLabel);
                container.Add(_idLabel);
                container.Add(_versionLabel);
                container.Add(_tagsLabel);

                Add(container);
            }

            public void SetData(DataSchemaDefinition schema)
            {
                _nameLabel.text = schema.DisplayName ?? schema.name;
                _idLabel.text = $"ID: {schema.SchemaId ?? "N/A"}";
                _versionLabel.text = $"v{schema.Version ?? "1.0.0"}";

                if (schema.Tags != null && schema.Tags.Length > 0)
                {
                    _tagsLabel.text = $"[{string.Join(", ", schema.Tags)}]";
                }
                else
                {
                    _tagsLabel.text = "";
                }
            }
        }
    }
}
