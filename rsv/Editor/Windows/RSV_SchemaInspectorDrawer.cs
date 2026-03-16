using LiveGameDev.Core;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Custom Inspector for DataSchemaDefinition.
    /// Shows schema metadata and quick actions.
    /// </summary>
    [CustomEditor(typeof(DataSchemaDefinition))]
    public class RSV_SchemaInspectorDrawer : UnityEditor.Editor
    {
        private DataSchemaDefinition _schema;

        private void OnEnable()
        {
            _schema = target as DataSchemaDefinition;
        }

        public override void OnInspectorGUI()
        {
            if (_schema == null) return;

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Schema info section
            EditorGUILayout.LabelField("Schema Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Schema ID: {_schema.SchemaId ?? "Not set"}");
            EditorGUILayout.LabelField($"Version: {_schema.Version ?? "1.0.0"}");
            EditorGUILayout.LabelField($"Root Nodes: {_schema.RootNodes?.Count ?? 0}");

            // Tags display
            if (_schema.Tags != null && _schema.Tags.Length > 0)
            {
                EditorGUILayout.LabelField($"Tags: {string.Join(", ", _schema.Tags)}");
            }

            EditorGUILayout.Space(10);

            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Schema", GUILayout.Height(30)))
            {
                ValidateSchema();
            }

            if (GUILayout.Button("Generate Example JSON", GUILayout.Height(30)))
            {
                GenerateExampleJson();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import JSON Schema", GUILayout.Height(30)))
            {
                ImportJsonSchema();
            }

            if (GUILayout.Button("Export JSON Schema", GUILayout.Height(30)))
            {
                ExportJsonSchema();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Open in Schema Designer", GUILayout.Height(30)))
            {
                OpenInSchemaDesigner();
            }

            // Show example JSON preview
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Show Example JSON Preview"))
            {
                ShowExampleJsonPreview();
            }
        }

        private void ValidateSchema()
        {
            if (_schema == null) return;

            var report = new LGD_ValidationReport("RSV");
            var status = _schema.Validate(report);

            if (status == ValidationStatus.Pass)
            {
                Debug.Log($"[RSV] Schema '{_schema.SchemaId}' is valid.");
                EditorUtility.DisplayDialog("Schema Validation", "Schema is valid!", "OK");
            }
            else
            {
                var message = $"Schema has {report.Entries.Count} issue(s):\n\n{report.ToMarkdown()}";
                Debug.LogWarning($"[RSV] {message}");
                EditorUtility.DisplayDialog("Schema Validation", message, "OK");
            }
        }

        private void GenerateExampleJson()
        {
            if (_schema == null) return;

            try
            {
                var json = _schema.GenerateExampleJson();
                Debug.Log($"[RSV] Example JSON for '{_schema.SchemaId}':\n{json}");

                // Copy to clipboard
                GUIUtility.systemCopyBuffer = json;
                EditorUtility.DisplayDialog("Example JSON", "Example JSON has been generated and copied to clipboard.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RSV] Failed to generate example JSON: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate example JSON:\n{ex.Message}", "OK");
            }
        }

        private void ImportJsonSchema()
        {
            if (_schema == null) return;

            var path = EditorUtility.OpenFilePanel("Import JSON Schema", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            var imported = RsvJsonSchemaInterop.ImportFromFile(path);
            if (imported == null)
            {
                Debug.LogError("[RSV] Failed to import schema.");
                EditorUtility.DisplayDialog("Import Failed", "Failed to import JSON Schema.", "OK");
                return;
            }

            _schema.SchemaId = imported.SchemaId;
            _schema.Version = imported.Version;
            _schema.Description = imported.Description;
            _schema.RootNodes = imported.RootNodes;

            EditorUtility.SetDirty(_schema);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RSV] Imported schema from: {path}");
            EditorUtility.DisplayDialog("Import Success", "Schema imported successfully!", "OK");
        }

        private void ExportJsonSchema()
        {
            if (_schema == null) return;

            var path = EditorUtility.SaveFilePanel("Export JSON Schema", "", _schema.SchemaId ?? "schema", "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                RsvJsonSchemaInterop.ExportToFile(_schema, path);
                EditorUtility.DisplayDialog("Export Success", $"Schema exported to:\n{path}", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RSV] Failed to export schema: {ex.Message}");
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export schema:\n{ex.Message}", "OK");
            }
        }

        private void OpenInSchemaDesigner()
        {
            var window = EditorWindow.GetWindow<RSV_MainWindow>();
            if (window != null)
            {
                window.Show();
                // Select the schema to trigger the designer
                Selection.activeObject = _schema;
            }
        }

        private void ShowExampleJsonPreview()
        {
            if (_schema == null) return;

            try
            {
                var json = _schema.GenerateExampleJson();

                // Show in a dialog
                EditorUtility.DisplayDialog(
                    "Example JSON Preview",
                    json,
                    "Copy to Clipboard",
                    "Close");

                // Copy to clipboard
                GUIUtility.systemCopyBuffer = json;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RSV] Failed to generate example JSON: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate example JSON:\n{ex.Message}", "OK");
            }
        }
    }
}
