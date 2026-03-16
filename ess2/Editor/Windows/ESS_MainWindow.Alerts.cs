using System.Linq;
using LiveGameDev.Core;
using LiveGameDev.ESS;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    public partial class ESS_MainWindow
    {
        // ── Alerts Panel ─────────────────────────────────────────
        private void DrawAlertsPanel()
        {
            if (_lastResult == null)
            {
                EditorGUILayout.HelpBox(
                    "No simulation results available. Run a simulation first.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Economy Alerts", _headerStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(50));
            _alertFilter = (ValidationStatus)EditorGUILayout.EnumPopup(_alertFilter);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            _alertsScroll = EditorGUILayout.BeginScrollView(_alertsScroll);

            var filteredAlerts = _lastResult.Alerts
                .Where(a => a.Severity >= _alertFilter)
                .OrderBy(a => a.Day)
                .ThenBy(a => a.Severity);

            foreach (var alert in filteredAlerts)
                DrawAlertCard(alert);

            if (!filteredAlerts.Any())
            {
                EditorGUILayout.HelpBox(
                    "No alerts match the current filter.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAlertCard(EssAlert alert)
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Day {alert.Day}", GUILayout.Width(60));
            EditorGUILayout.LabelField(alert.AlertType, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(alert.Severity.ToString(), GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Item: {alert.ItemName}");
            EditorGUILayout.LabelField(alert.Message, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}
