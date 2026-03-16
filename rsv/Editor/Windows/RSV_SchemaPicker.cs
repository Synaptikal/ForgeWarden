using System.Linq;
using LiveGameDev.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Popup dialog for selecting a DataSchemaDefinition.
    /// Used by the Playground tab to pick a schema for validation.
    /// </summary>
    public class RSV_SchemaPicker : EditorWindow
    {
        private DataSchemaDefinition _selectedSchema;
        private System.Action<DataSchemaDefinition> _onSchemaSelected;

        public static void Show(System.Action<DataSchemaDefinition> onSelected)
        {
            var window = GetWindow<RSV_SchemaPicker>("Select Schema");
            window._onSchemaSelected = onSelected;
            window.minSize = new Vector2(400, 300);
            window.ShowModal();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;

            // Load styles
            root.styleSheets.Add(
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Packages/com.forgegames.livegamedev.rsv/Editor/UI/USS/RSV_Window.uss"));

            // Header
            var header = new Label("Select a Schema") { name = "header" };
            header.AddToClassList("rsv-panel-header");
            root.Add(header);

            // Schema list
            var schemas = LGD_AssetUtility.FindAllAssetsOfType<DataSchemaDefinition>();
            var listView = new ListView
            {
                itemsSource = schemas,
                makeItem = () => new SchemaPickerItem(),
                bindItem = (el, i) => ((SchemaPickerItem)el).SetData(schemas[i]),
                selectionType = SelectionType.Single
            };

            listView.onItemsChosen += items =>
            {
                foreach (var item in items)
                {
                    if (item is DataSchemaDefinition schema)
                    {
                        _selectedSchema = schema;
                        _onSchemaSelected?.Invoke(schema);
                        Close();
                        break;
                    }
                }
            };

            root.Add(listView);

            // Cancel button
            var cancelBtn = new Button(() => Close()) { text = "Cancel" };
            cancelBtn.style.marginTop = 8;
            root.Add(cancelBtn);
        }

        private class SchemaPickerItem : VisualElement
        {
            private readonly Label _nameLabel;
            private readonly Label _idLabel;

            public SchemaPickerItem()
            {
                AddToClassList("rsv-schema-list-item");

                var container = new VisualElement { name = "container" };
                container.AddToClassList("rsv-schema-item-container");

                _nameLabel = new Label { name = "name" };
                _nameLabel.AddToClassList("rsv-schema-name");

                _idLabel = new Label { name = "id" };
                _idLabel.AddToClassList("rsv-schema-id");

                container.Add(_nameLabel);
                container.Add(_idLabel);

                Add(container);
            }

            public void SetData(DataSchemaDefinition schema)
            {
                _nameLabel.text = schema.DisplayName ?? schema.name;
                _idLabel.text = $"ID: {schema.SchemaId ?? "N/A"}";
            }
        }
    }
}
