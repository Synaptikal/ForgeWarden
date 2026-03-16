using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    public partial class RSV_SchemaDesigner
    {
        // ── Metadata ─────────────────────────────────────────────
        private void OnMetadataChanged()
        {
            if (_target == null) return;

            _target.SchemaId   = _schemaIdField.value;
            _target.Version    = _versionField.value;
            _target.Description = _descriptionField.value;

            EditorUtility.SetDirty(_target);
            UpdatePreview();
        }

        // ── Import / Export ──────────────────────────────────────
        private void ImportSchema()
        {
            if (_target == null) return;

            var path = EditorUtility.OpenFilePanel("Import JSON Schema", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            var imported = RsvJsonSchemaInterop.ImportFromFile(path);
            if (imported == null)
            {
                Debug.LogError("[RSV] Failed to import schema.");
                return;
            }

            _target.SchemaId   = imported.SchemaId;
            _target.Version    = imported.Version;
            _target.Description = imported.Description;
            _target.RootNodes  = imported.RootNodes;

            EditorUtility.SetDirty(_target);
            LoadSchema(_target);
            Debug.Log($"[RSV] Imported schema from: {path}");
        }

        private void ExportSchema()
        {
            if (_target == null) return;

            var path = EditorUtility.SaveFilePanel(
                "Export JSON Schema", "", _target.SchemaId ?? "schema", "json");
            if (string.IsNullOrEmpty(path)) return;

            RsvJsonSchemaInterop.ExportToFile(_target, path);
        }

        // ── Validate / Compile ───────────────────────────────────
        private void ValidateSchema()
        {
            if (_target == null) return;

            var report = new LiveGameDev.Core.LGD_ValidationReport("RSV");
            var status = _target.Validate(report);

            if (status == LiveGameDev.Core.ValidationStatus.Pass)
                Debug.Log($"[RSV] Schema '{_target.SchemaId}' is valid.");
            else
                Debug.LogWarning($"[RSV] Schema '{_target.SchemaId}' has issues:\n{report.ToMarkdown()}");
        }

        private void CompileSchema()
        {
            if (_target == null) return;

            var compiledAsset = RsvSchemaAssetCompiler.CompileAndSaveToDefaultLocation(_target);
            if (compiledAsset != null)
            {
                EditorUtility.DisplayDialog(
                    "Schema Compiled",
                    $"Schema '{_target.SchemaId}' compiled successfully.\n" +
                    $"Asset: {compiledAsset.name}\nLocation: Resources/RSV/CompiledSchemas/",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Compilation Failed",
                    $"Failed to compile '{_target.SchemaId}'. Check the console for errors.",
                    "OK");
            }
        }

        // ── Migration Hints ──────────────────────────────────────
        private void AddMigrationHint()
        {
            if (_target == null) return;

            if (_target.MigrationHints == null)
                _target.MigrationHints = new List<RsvMigrationHint>();

            _target.MigrationHints.Add(new RsvMigrationHint
            {
                TargetVersion = "2.0.0",
                Description   = "Describe the breaking changes and migration steps here.",
                IsRequired    = true
            });

            EditorUtility.SetDirty(_target);
            _migrationList.itemsSource = _target.MigrationHints;
            _migrationList.Rebuild();
        }

        public void RemoveMigrationHint(int index)
        {
            if (_target?.MigrationHints == null) return;
            if (index < 0 || index >= _target.MigrationHints.Count) return;

            _target.MigrationHints.RemoveAt(index);
            EditorUtility.SetDirty(_target);
            _migrationList.itemsSource = _target.MigrationHints;
            _migrationList.Rebuild();
        }

        public void UpdateMigrationHint(int index, RsvMigrationHint hint)
        {
            if (_target?.MigrationHints == null) return;
            if (index < 0 || index >= _target.MigrationHints.Count) return;

            _target.MigrationHints[index] = hint;
            EditorUtility.SetDirty(_target);
        }

        // ── JSON Preview ─────────────────────────────────────────
        private void UpdatePreview()
        {
            var now = UnityEngine.Time.realtimeSinceStartup * 1000;
            if (now - _lastPreviewUpdate < PREVIEW_THROTTLE_MS)
            {
                RSV_DebouncedField.Debounce("preview_update", (int)PREVIEW_THROTTLE_MS, DoUpdatePreview);
                return;
            }
            DoUpdatePreview();
        }

        private void DoUpdatePreview()
        {
            _lastPreviewUpdate = UnityEngine.Time.realtimeSinceStartup * 1000;

            if (_target == null)
            {
                _previewLabel.text = "No schema selected.";
                return;
            }

            try
            {
                _previewLabel.text = _target.GenerateExampleJson();
            }
            catch (System.Exception ex)
            {
                _previewLabel.text = $"Error generating preview: {ex.Message}";
            }
        }
    }
}
