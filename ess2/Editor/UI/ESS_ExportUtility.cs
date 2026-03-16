using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Utility class for exporting simulation results to CSV and JSON formats.
    /// </summary>
    public static class ESS_ExportUtility
    {
        /// <summary>
        /// Export simulation results to CSV format.
        /// </summary>
        public static void ExportToCsv(SimulationResult result, string filePath)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var sb = new StringBuilder();

            // Header with metadata
            sb.AppendLine("# Economy Simulation Results - CSV Export");
            sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# Simulation Days: {result.Config.SimulationDays}");
            sb.AppendLine($"# Player Count: {result.Config.PlayerCount}");
            sb.AppendLine($"# Seed: {result.Config.Seed}");
            sb.AppendLine();

            // Time-series data
            sb.AppendLine("=== TIME SERIES DATA ===");
            sb.AppendLine("Day,TotalCurrency,GiniCoefficient");

            foreach (var state in result.History)
            {
                sb.AppendLine($"{state.Day},{state.TotalCurrency:F2},{state.GiniCoefficient:F4}");
            }

            sb.AppendLine();

            // Item prices
            if (result.History.Count > 0 && result.History[0].ItemPrices.Count > 0)
            {
                sb.AppendLine("=== ITEM PRICES ===");
                var itemNames = result.History[0].ItemPrices.Keys.ToList();
                sb.AppendLine("Day," + string.Join(",", itemNames));

                foreach (var state in result.History)
                {
                    var values = itemNames.Select(name => 
                        state.ItemPrices.TryGetValue(name, out float price) ? price.ToString("F2") : "0.00");
                    sb.AppendLine($"{state.Day},{string.Join(",", values)}");
                }

                sb.AppendLine();
            }

            // Item supply
            if (result.History.Count > 0 && result.History[0].ItemSupply.Count > 0)
            {
                sb.AppendLine("=== ITEM SUPPLY ===");
                var itemNames = result.History[0].ItemSupply.Keys.ToList();
                sb.AppendLine("Day," + string.Join(",", itemNames));

                foreach (var state in result.History)
                {
                    var values = itemNames.Select(name => 
                        state.ItemSupply.TryGetValue(name, out float supply) ? supply.ToString("F2") : "0.00");
                    sb.AppendLine($"{state.Day},{string.Join(",", values)}");
                }

                sb.AppendLine();
            }

            // Alerts
            if (result.Alerts.Count > 0)
            {
                sb.AppendLine("=== ALERTS ===");
                sb.AppendLine("Day,ItemName,AlertType,Severity,Message");

                foreach (var alert in result.Alerts)
                {
                    string severity = alert.Severity.ToString();
                    string message = alert.Message.Replace("\"", "\"\"");
                    sb.AppendLine($"{alert.Day},{alert.ItemName},{alert.AlertType},{severity},\"{message}\"");
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[ESS] Exported results to CSV: {filePath}");
        }

        /// <summary>
        /// Export simulation results to JSON format.
        /// </summary>
        public static void ExportToJson(SimulationResult result, string filePath)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"metadata\": {");
            sb.AppendLine($"    \"generated\": \"{DateTime.Now:O}\",");
            sb.AppendLine($"    \"simulationDays\": {result.Config.SimulationDays},");
            sb.AppendLine($"    \"playerCount\": {result.Config.PlayerCount},");
            sb.AppendLine($"    \"seed\": {result.Config.Seed}");
            sb.AppendLine("  },");
            sb.AppendLine("  \"config\": {");
            sb.AppendLine($"    \"simulationDays\": {result.Config.SimulationDays},");
            sb.AppendLine($"    \"playerCount\": {result.Config.PlayerCount},");
            sb.AppendLine($"    \"seed\": {result.Config.Seed}");
            sb.AppendLine("  },");
            sb.AppendLine("  \"history\": [");

            for (int i = 0; i < result.History.Count; i++)
            {
                var state = result.History[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"day\": {state.Day},");
                sb.AppendLine($"      \"totalCurrency\": {state.TotalCurrency:F4},");
                sb.AppendLine($"      \"giniCoefficient\": {state.GiniCoefficient:F4},");

                // Item prices
                sb.AppendLine("      \"itemPrices\": {");
                var priceEntries = state.ItemPrices.ToList();
                for (int j = 0; j < priceEntries.Count; j++)
                {
                    var kvp = priceEntries[j];
                    string comma = j < priceEntries.Count - 1 ? "," : "";
                    sb.AppendLine($"        \"{kvp.Key}\": {kvp.Value:F4}{comma}");
                }
                sb.AppendLine("      },");

                // Item supply
                sb.AppendLine("      \"itemSupply\": {");
                var supplyEntries = state.ItemSupply.ToList();
                for (int j = 0; j < supplyEntries.Count; j++)
                {
                    var kvp = supplyEntries[j];
                    string comma = j < supplyEntries.Count - 1 ? "," : "";
                    sb.AppendLine($"        \"{kvp.Key}\": {kvp.Value:F4}{comma}");
                }
                sb.AppendLine("      }");

                string stateComma = i < result.History.Count - 1 ? "," : "";
                sb.AppendLine($"    }}{stateComma}");
            }

            sb.AppendLine("  ],");
            sb.AppendLine("  \"alerts\": [");

            for (int i = 0; i < result.Alerts.Count; i++)
            {
                var alert = result.Alerts[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"day\": {alert.Day},");
                sb.AppendLine($"      \"itemName\": \"{EscapeJson(alert.ItemName)}\",");
                sb.AppendLine($"      \"alertType\": \"{EscapeJson(alert.AlertType)}\",");
                sb.AppendLine($"      \"severity\": \"{alert.Severity}\",");
                sb.AppendLine($"      \"message\": \"{EscapeJson(alert.Message)}\"");

                string alertComma = i < result.Alerts.Count - 1 ? "," : "";
                sb.AppendLine($"    }}{alertComma}");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[ESS] Exported results to JSON: {filePath}");
        }

        /// <summary>
        /// Export alerts to CSV format.
        /// </summary>
        public static void ExportAlertsToCsv(List<EssAlert> alerts, string filePath)
        {
            if (alerts == null)
                throw new ArgumentNullException(nameof(alerts));

            var sb = new StringBuilder();
            sb.AppendLine("Day,ItemName,AlertType,Severity,Message");

            foreach (var alert in alerts)
            {
                string severity = alert.Severity.ToString();
                string message = alert.Message.Replace("\"", "\"\"");
                sb.AppendLine($"{alert.Day},{alert.ItemName},{alert.AlertType},{severity},\"{message}\"");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[ESS] Exported alerts to CSV: {filePath}");
        }

        /// <summary>
        /// Export alerts to JSON format.
        /// </summary>
        public static void ExportAlertsToJson(List<EssAlert> alerts, string filePath)
        {
            if (alerts == null)
                throw new ArgumentNullException(nameof(alerts));

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"generated\": \"" + DateTime.Now.ToString("O") + "\",");
            sb.AppendLine("  \"alerts\": [");

            for (int i = 0; i < alerts.Count; i++)
            {
                var alert = alerts[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"day\": {alert.Day},");
                sb.AppendLine($"      \"itemName\": \"{EscapeJson(alert.ItemName)}\",");
                sb.AppendLine($"      \"alertType\": \"{EscapeJson(alert.AlertType)}\",");
                sb.AppendLine($"      \"severity\": \"{alert.Severity}\",");
                sb.AppendLine($"      \"message\": \"{EscapeJson(alert.Message)}\"");

                string comma = i < alerts.Count - 1 ? "," : "";
                sb.AppendLine($"    }}{comma}");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[ESS] Exported alerts to JSON: {filePath}");
        }

        /// <summary>
        /// Show save file dialog and export results.
        /// </summary>
        public static void ShowExportDialog(SimulationResult result, string defaultName = "simulation_results")
        {
            if (result == null)
            {
                EditorUtility.DisplayDialog("Export Error", "No simulation results to export.", "OK");
                return;
            }

            // CSV export
            string csvPath = EditorUtility.SaveFilePanel(
                "Export Results as CSV",
                "",
                $"{defaultName}.csv",
                "csv");

            if (!string.IsNullOrEmpty(csvPath))
            {
                try
                {
                    ExportToCsv(result, csvPath);
                    EditorUtility.DisplayDialog("Export Success", 
                        $"Results exported successfully to:\n{csvPath}", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Export Error", 
                        $"Failed to export CSV:\n{ex.Message}", "OK");
                }
            }

            // JSON export
            string jsonPath = EditorUtility.SaveFilePanel(
                "Export Results as JSON",
                "",
                $"{defaultName}.json",
                "json");

            if (!string.IsNullOrEmpty(jsonPath))
            {
                try
                {
                    ExportToJson(result, jsonPath);
                    EditorUtility.DisplayDialog("Export Success", 
                        $"Results exported successfully to:\n{jsonPath}", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Export Error", 
                        $"Failed to export JSON:\n{ex.Message}", "OK");
                }
            }
        }

        /// <summary>
        /// Show save file dialog and export alerts only.
        /// </summary>
        public static void ShowAlertsExportDialog(List<EssAlert> alerts, string defaultName = "simulation_alerts")
        {
            if (alerts == null || alerts.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Error", "No alerts to export.", "OK");
                return;
            }

            // CSV export
            string csvPath = EditorUtility.SaveFilePanel(
                "Export Alerts as CSV",
                "",
                $"{defaultName}.csv",
                "csv");

            if (!string.IsNullOrEmpty(csvPath))
            {
                try
                {
                    ExportAlertsToCsv(alerts, csvPath);
                    EditorUtility.DisplayDialog("Export Success", 
                        $"Alerts exported successfully to:\n{csvPath}", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Export Error", 
                        $"Failed to export CSV:\n{ex.Message}", "OK");
                }
            }

            // JSON export
            string jsonPath = EditorUtility.SaveFilePanel(
                "Export Alerts as JSON",
                "",
                $"{defaultName}.json",
                "json");

            if (!string.IsNullOrEmpty(jsonPath))
            {
                try
                {
                    ExportAlertsToJson(alerts, jsonPath);
                    EditorUtility.DisplayDialog("Export Success", 
                        $"Alerts exported successfully to:\n{jsonPath}", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Export Error", 
                        $"Failed to export JSON:\n{ex.Message}", "OK");
                }
            }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}