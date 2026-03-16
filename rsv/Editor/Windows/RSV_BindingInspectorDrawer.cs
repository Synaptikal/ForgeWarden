using LiveGameDev.Core;
using LiveGameDev.Core.Editor.UI;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Custom Inspector for JsonSourceBinding.
    /// Shows validation status badge and quick actions.
    /// </summary>
    [CustomEditor(typeof(JsonSourceBinding))]
    public class RSV_BindingInspectorDrawer : UnityEditor.Editor
    {
        private JsonSourceBinding _binding;
        private LGD_ValidationReport _lastReport;
        private bool _showReport;

        private void OnEnable()
        {
            _binding = target as JsonSourceBinding;
        }

        public override void OnInspectorGUI()
        {
            if (_binding == null) return;

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Validation section
            EditorGUILayout.LabelField("RSV Validation", EditorStyles.boldLabel);

            // Status badge
            var status = _lastReport?.OverallStatus ?? ValidationStatus.Pass;
            var statusText = status switch
            {
                ValidationStatus.Pass     => "✅ Pass",
                ValidationStatus.Info     => "ℹ️ Info",
                ValidationStatus.Warning  => "⚠️ Warning",
                ValidationStatus.Error    => "❌ Error",
                ValidationStatus.Critical => "🔴 Critical",
                _                         => status.ToString()
            };

            var statusColor = status switch
            {
                ValidationStatus.Pass     => new Color(0.3f, 0.8f, 0.3f),
                ValidationStatus.Info     => new Color(0.3f, 0.6f, 0.9f),
                ValidationStatus.Warning  => new Color(1.0f, 0.6f, 0.0f),
                ValidationStatus.Error    => new Color(0.9f, 0.3f, 0.3f),
                ValidationStatus.Critical => new Color(0.8f, 0.2f, 0.2f),
                _                         => Color.white
            };

            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Status: {statusText}", EditorStyles.boldLabel);
            GUI.color = oldColor;

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Now", GUILayout.Height(30)))
            {
                ValidateBinding();
            }

            if (GUILayout.Button("Open in RSV Window", GUILayout.Height(30)))
            {
                OpenInRSVWindow();
            }
            EditorGUILayout.EndHorizontal();

            // Show report if available
            if (_lastReport != null && _lastReport.Entries.Count > 0)
            {
                _showReport = EditorGUILayout.Foldout(_showReport, $"Validation Report ({_lastReport.Entries.Count} entries)");

                if (_showReport)
                {
                    EditorGUI.indentLevel++;
                    foreach (var entry in _lastReport.Entries)
                    {
                        var entryColor = entry.Status switch
                        {
                            ValidationStatus.Pass     => new Color(0.3f, 0.8f, 0.3f),
                            ValidationStatus.Info     => new Color(0.3f, 0.6f, 0.9f),
                            ValidationStatus.Warning  => new Color(1.0f, 0.6f, 0.0f),
                            ValidationStatus.Error    => new Color(0.9f, 0.3f, 0.3f),
                            ValidationStatus.Critical => new Color(0.8f, 0.2f, 0.2f),
                            _                         => Color.white
                        };

                        GUI.color = entryColor;
                        EditorGUILayout.LabelField($"[{entry.Status}] {entry.Category}: {entry.Message}");
                        if (!string.IsNullOrEmpty(entry.SuggestedFix))
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.LabelField($"  💡 {entry.SuggestedFix}", EditorStyles.miniLabel);
                            EditorGUI.indentLevel--;
                        }
                        GUI.color = oldColor;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            // Show binding info
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Binding Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Binding ID: {_binding.BindingId}");
            EditorGUILayout.LabelField($"Source Type: {_binding.SourceType}");
            EditorGUILayout.LabelField($"Source Path: {_binding.SourcePathOrUrl ?? "Not set"}");
            EditorGUILayout.LabelField($"Validate on Play: {_binding.ValidateOnPlay}");
            EditorGUILayout.LabelField($"Validate on Build: {_binding.ValidateOnBuild}");
        }

        private void ValidateBinding()
        {
            if (_binding == null) return;

            _lastReport = RsvValidator.ValidateBinding(_binding);

            if (_lastReport.OverallStatus == ValidationStatus.Pass)
            {
                Debug.Log($"[RSV] Binding '{_binding.name}' validation passed.");
            }
            else
            {
                Debug.LogWarning($"[RSV] Binding '{_binding.name}' validation found issues:\n{_lastReport.ToMarkdown()}");
            }

            Repaint();
        }

        private void OpenInRSVWindow()
        {
            var window = EditorWindow.GetWindow<RSV_MainWindow>();
            if (window != null)
            {
                window.Show();
                // Switch to Binding Inspector tab
                // Note: This would require exposing a method in RSV_MainWindow
            }
        }
    }
}
