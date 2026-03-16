using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    public partial class ESS_MainWindow
    {
        // ── Results Panel ────────────────────────────────────────
        private void DrawResultsPanel()
        {
            if (_lastResult == null)
            {
                EditorGUILayout.HelpBox(
                    "No simulation results available. Run a simulation first.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            foreach (ResultsTab tab in Enum.GetValues(typeof(ResultsTab)))
            {
                GUIStyle style = _currentResultsTab == tab
                    ? EditorStyles.toolbarButton
                    : EditorStyles.miniButton;
                if (GUILayout.Button(tab.ToString(), style))
                    _currentResultsTab = tab;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            _resultsScroll = EditorGUILayout.BeginScrollView(_resultsScroll);

            switch (_currentResultsTab)
            {
                case ResultsTab.Overview: DrawResultsOverview(); break;
                case ResultsTab.Prices:   DrawPriceCharts();     break;
                case ResultsTab.Supply:   DrawSupplyCharts();    break;
                case ResultsTab.Wealth:   DrawWealthCharts();    break;
                case ResultsTab.Metrics:  DrawMetricsTable();    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawResultsOverview()
        {
            EditorGUILayout.LabelField("Simulation Summary", _headerStyle);

            var metrics = _lastResult.Metrics?.History;
            if (metrics == null || metrics.Count == 0) return;

            var lastDay = metrics[metrics.Count - 1];

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Final State", _subHeaderStyle);
            EditorGUILayout.LabelField($"Total Currency: {lastDay.TotalCurrencySupply:N0}");
            EditorGUILayout.LabelField($"Gini Coefficient: {lastDay.GiniCoefficient:F3}");
            EditorGUILayout.LabelField($"Money Velocity: {lastDay.MoneyVelocity:F2}");
            EditorGUILayout.LabelField($"Active Players: {lastDay.ActivePlayerCount}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Alert Summary", _subHeaderStyle);

            var alertCounts = _lastResult.Alerts
                .GroupBy(a => a.Severity)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in alertCounts)
                EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value} alerts");

            if (_lastResult.Alerts.Count == 0)
                EditorGUILayout.LabelField("No alerts generated.");

            EditorGUILayout.EndVertical();
        }

        private void DrawPriceCharts()
        {
            EditorGUILayout.LabelField("Price History", _headerStyle);

            var history = _lastResult.History;
            if (history == null || history.Count == 0) return;

            var priceData = BuildSeriesData(history,
                state => state.ItemPrices,
                _selectedItemFilter);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(50));
            _selectedItemFilter = EditorGUILayout.TextField(_selectedItemFilter);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            Rect chartRect = GUILayoutUtility.GetRect(position.width - 40, 300);
            ESS_ChartRenderer.DrawLineChart(chartRect, priceData,
                "Item Prices Over Time", "Day", "Price", _chartState);
        }

        private void DrawSupplyCharts()
        {
            EditorGUILayout.LabelField("Supply History", _headerStyle);

            var history = _lastResult.History;
            if (history == null || history.Count == 0) return;

            var supplyData = BuildSeriesData(history, state => state.ItemSupply, filter: null);

            Rect chartRect = GUILayoutUtility.GetRect(position.width - 40, 300);
            ESS_ChartRenderer.DrawLineChart(chartRect, supplyData,
                "Item Supply Over Time", "Day", "Supply", _chartState);

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Final Supply Ratios", _subHeaderStyle);

            var lastMetrics = _lastResult.Metrics?.History?.LastOrDefault();
            if (lastMetrics != null)
            {
                Rect barRect = GUILayoutUtility.GetRect(position.width - 40, 200);
                ESS_ChartRenderer.DrawBarChart(barRect, lastMetrics.SupplyRatios,
                    "Supply Ratio (Actual/Expected)", "Item", "Ratio",
                    warningThreshold: 2f, criticalThreshold: 3f, state: _chartState);
            }
        }

        private void DrawWealthCharts()
        {
            EditorGUILayout.LabelField("Wealth Distribution", _headerStyle);

            var metrics = _lastResult.Metrics?.History;
            if (metrics == null || metrics.Count == 0) return;

            var giniData = new Dictionary<string, List<float>>
            {
                ["Gini Coefficient"] = metrics.Select(m => m.GiniCoefficient).ToList()
            };

            Rect giniRect = GUILayoutUtility.GetRect(position.width - 40, 200);
            ESS_ChartRenderer.DrawLineChart(giniRect, giniData,
                "Wealth Inequality (Gini)", "Day", "Gini", _chartState);

            EditorGUILayout.Space(20);

            var gapData = new Dictionary<string, List<float>>
            {
                ["Wealth Gap (Top/Bottom 10%)"] = metrics.Select(m => m.WealthGapRatio).ToList()
            };

            Rect gapRect = GUILayoutUtility.GetRect(position.width - 40, 200);
            ESS_ChartRenderer.DrawLineChart(gapRect, gapData,
                "Wealth Gap Ratio", "Day", "Ratio", _chartState);
        }

        private void DrawMetricsTable()
        {
            EditorGUILayout.LabelField("Daily Metrics", _headerStyle);

            var metrics = _lastResult.Metrics?.History;
            if (metrics == null || metrics.Count == 0) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Day:", GUILayout.Width(40));
            _selectedDay = EditorGUILayout.IntSlider(_selectedDay, 1, metrics.Count);
            EditorGUILayout.EndHorizontal();

            if (_selectedDay < 1 || _selectedDay > metrics.Count) _selectedDay = 1;
            var day = metrics[_selectedDay - 1];

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField($"Day {day.Day} Metrics", _subHeaderStyle);
            EditorGUILayout.LabelField($"Total Currency: {day.TotalCurrencySupply:N0}");
            EditorGUILayout.LabelField($"Currency Destroyed: {day.CurrencyDestroyedToday:N0}");
            EditorGUILayout.LabelField($"Currency Transacted: {day.CurrencyTransactedToday:N0}");
            EditorGUILayout.LabelField($"Gini Coefficient: {day.GiniCoefficient:F4}");
            EditorGUILayout.LabelField($"Wealth Gap Ratio: {day.WealthGapRatio:F2}");
            EditorGUILayout.LabelField($"Money Velocity: {day.MoneyVelocity:F4}");
            EditorGUILayout.LabelField($"Active Players: {day.ActivePlayerCount}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Inflation Velocities", _subHeaderStyle);
            foreach (var kvp in day.InflationVelocity)
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value:P2}");
        }

        /// <summary>Builds a per-item float series from simulation history snapshots.</summary>
        private static Dictionary<string, List<float>> BuildSeriesData(
            IList<SimState> history,
            Func<SimState, IDictionary<string, float>> selector,
            string filter)
        {
            var data = new Dictionary<string, List<float>>();
            var firstState = history[0];

            foreach (var itemName in selector(firstState).Keys)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    !itemName.ToLower().Contains(filter.ToLower())) continue;

                var series = new List<float>();
                foreach (var state in history)
                {
                    if (selector(state).TryGetValue(itemName, out float val))
                        series.Add(val);
                }
                data[itemName] = series;
            }

            return data;
        }
    }
}
