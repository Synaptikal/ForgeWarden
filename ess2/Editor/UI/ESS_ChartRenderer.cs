using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Chart rendering utilities for economy simulation visualization.
    /// Supports line charts, histograms, and bar charts using Unity's GUI system.
    /// </summary>
    public static class ESS_ChartRenderer
    {
        private static readonly Color ColorLine = new Color(0.2f, 0.6f, 1f);
        private static readonly Color ColorBar = new Color(0.3f, 0.8f, 0.4f);
        private static readonly Color ColorBarWarning = new Color(1f, 0.7f, 0.2f);
        private static readonly Color ColorBarCritical = new Color(1f, 0.3f, 0.3f);
        private static readonly Color ColorGrid = new Color(0.4f, 0.4f, 0.4f, 0.3f);
        private static readonly Color ColorText = new Color(0.9f, 0.9f, 0.9f);
        private static readonly Color ColorBackground = new Color(0.15f, 0.15f, 0.15f);

        /// <summary>
        /// Chart rendering state for interactivity.
        /// </summary>
        public class ChartState
        {
            public Vector2 ScrollPosition;
            public float ZoomLevel = 1f;
            public string HoveredTooltip;
            public Vector2 HoverPosition;
        }

        #region Line Chart

        /// <summary>
        /// Render a line chart for time-series data (e.g., price history).
        /// </summary>
        public static void DrawLineChart(
            Rect rect,
            Dictionary<string, List<float>> seriesData,
            string title,
            string xLabel,
            string yLabel,
            ChartState state = null)
        {
            if (seriesData == null || seriesData.Count == 0)
            {
                DrawEmptyChart(rect, title);
                return;
            }

            // Background
            EditorGUI.DrawRect(rect, ColorBackground);

            // Title
            var titleRect = new Rect(rect.x, rect.y, rect.width, 24);
            GUI.Label(titleRect, title, EditorStyles.boldLabel);

            // Chart area
            var chartRect = new Rect(rect.x + 40, rect.y + 30, rect.width - 60, rect.height - 50);

            // Find data bounds
            float minX = 0;
            float maxX = seriesData.Values.First().Count - 1;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var series in seriesData.Values)
            {
                for (int i = 0; i < series.Count; i++)
                {
                    minY = Mathf.Min(minY, series[i]);
                    maxY = Mathf.Max(maxY, series[i]);
                }
            }

            if (maxY <= minY) maxY = minY + 1f;

            // Draw grid
            DrawGrid(chartRect, minX, maxX, minY, maxY);

            // Draw lines for each series
            int colorIndex = 0;
            foreach (var kvp in seriesData)
            {
                var seriesColor = GetSeriesColor(colorIndex++);
                DrawSeriesLine(chartRect, kvp.Value, minX, maxX, minY, maxY, seriesColor);
            }

            // Axis labels
            DrawAxisLabels(chartRect, xLabel, yLabel);

            // Handle hover tooltip
            if (state != null && Event.current.type == EventType.MouseMove)
            {
                state.HoverPosition = Event.current.mousePosition;
                UpdateTooltip(chartRect, seriesData, minX, maxX, minY, maxY, state);
                Event.current.Use();
            }

            // Draw tooltip
            if (state != null && !string.IsNullOrEmpty(state.HoveredTooltip))
            {
                DrawTooltip(state.HoverPosition, state.HoveredTooltip);
            }
        }

        private static void DrawSeriesLine(
            Rect rect,
            List<float> data,
            float minX, float maxX,
            float minY, float maxY,
            Color color)
        {
            if (data.Count < 2) return;

            Handles.color = color;
            Handles.BeginGUI();

            Vector3[] points = new Vector3[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                float x = Mathf.Lerp(rect.x, rect.x + rect.width, (i - minX) / (maxX - minX));
                float y = Mathf.Lerp(rect.y + rect.height, rect.y, (data[i] - minY) / (maxY - minY));
                points[i] = new Vector3(x, y, 0);
            }

            Handles.DrawAAPolyLine(2f, points);
            Handles.EndGUI();
        }

        #endregion

        #region Histogram

        /// <summary>
        /// Render a histogram for distribution data (e.g., wealth distribution).
        /// </summary>
        public static void DrawHistogram(
            Rect rect,
            List<float> values,
            int binCount,
            string title,
            string xLabel,
            string yLabel,
            ChartState state = null)
        {
            if (values == null || values.Count == 0)
            {
                DrawEmptyChart(rect, title);
                return;
            }

            // Background
            EditorGUI.DrawRect(rect, ColorBackground);

            // Title
            var titleRect = new Rect(rect.x, rect.y, rect.width, 24);
            GUI.Label(titleRect, title, EditorStyles.boldLabel);

            // Chart area
            var chartRect = new Rect(rect.x + 40, rect.y + 30, rect.width - 60, rect.height - 50);

            // Calculate bins
            float minVal = values.Min();
            float maxVal = values.Max();
            if (maxVal <= minVal) maxVal = minVal + 1f;

            float binWidth = (maxVal - minVal) / binCount;
            int[] bins = new int[binCount];

            foreach (float val in values)
            {
                int binIndex = Mathf.Min((int)((val - minVal) / binWidth), binCount - 1);
                bins[binIndex]++;
            }

            int maxBinCount = bins.Max();
            if (maxBinCount == 0) maxBinCount = 1;

            // Draw grid
            DrawGrid(chartRect, 0, binCount, 0, maxBinCount);

            // Draw bars
            float barWidth = (chartRect.width / binCount) * 0.8f;
            float barSpacing = (chartRect.width / binCount) * 0.1f;

            for (int i = 0; i < binCount; i++)
            {
                float barHeight = (bins[i] / (float)maxBinCount) * chartRect.height;
                float x = chartRect.x + i * (barWidth + barSpacing) + barSpacing;
                float y = chartRect.y + chartRect.height - barHeight;

                var barRect = new Rect(x, y, barWidth, barHeight);
                Color barColor = ColorBar;
                if (i > binCount * 0.7f) barColor = ColorBarWarning;
                if (i > binCount * 0.9f) barColor = ColorBarCritical;

                EditorGUI.DrawRect(barRect, barColor);

                // Bin label
                var labelRect = new Rect(x, chartRect.y + chartRect.height + 5, barWidth, 20);
                string binLabel = $"{minVal + i * binWidth:F1}";
                GUI.Label(labelRect, binLabel, EditorStyles.miniLabel);
            }

            // Axis labels
            DrawAxisLabels(chartRect, xLabel, yLabel);

            // Handle hover tooltip
            if (state != null && Event.current.type == EventType.MouseMove)
            {
                state.HoverPosition = Event.current.mousePosition;
                UpdateHistogramTooltip(chartRect, bins, minVal, binWidth, maxBinCount, state);
                Event.current.Use();
            }

            // Draw tooltip
            if (state != null && !string.IsNullOrEmpty(state.HoveredTooltip))
            {
                DrawTooltip(state.HoverPosition, state.HoveredTooltip);
            }
        }

        #endregion

        #region Bar Chart

        /// <summary>
        /// Render a bar chart for categorical data (e.g., supply ratios).
        /// </summary>
        public static void DrawBarChart(
            Rect rect,
            Dictionary<string, float> data,
            string title,
            string xLabel,
            string yLabel,
            float warningThreshold = 2f,
            float criticalThreshold = 3f,
            ChartState state = null)
        {
            if (data == null || data.Count == 0)
            {
                DrawEmptyChart(rect, title);
                return;
            }

            // Background
            EditorGUI.DrawRect(rect, ColorBackground);

            // Title
            var titleRect = new Rect(rect.x, rect.y, rect.width, 24);
            GUI.Label(titleRect, title, EditorStyles.boldLabel);

            // Chart area
            var chartRect = new Rect(rect.x + 40, rect.y + 30, rect.width - 60, rect.height - 50);

            // Find max value
            float maxValue = data.Values.Max();
            if (maxValue <= 0) maxValue = 1f;

            // Draw grid
            DrawGrid(chartRect, 0, data.Count, 0, maxValue);

            // Draw bars
            float barWidth = (chartRect.width / data.Count) * 0.7f;
            float barSpacing = (chartRect.width / data.Count) * 0.15f;

            int index = 0;
            foreach (var kvp in data)
            {
                float barHeight = (kvp.Value / maxValue) * chartRect.height;
                float x = chartRect.x + index * (barWidth + barSpacing) + barSpacing;
                float y = chartRect.y + chartRect.height - barHeight;

                var barRect = new Rect(x, y, barWidth, barHeight);
                Color barColor = ColorBar;
                if (kvp.Value >= warningThreshold) barColor = ColorBarWarning;
                if (kvp.Value >= criticalThreshold) barColor = ColorBarCritical;

                EditorGUI.DrawRect(barRect, barColor);

                // Category label
                var labelRect = new Rect(x, chartRect.y + chartRect.height + 5, barWidth, 20);
                string categoryLabel = kvp.Key;
                string shortLabel = categoryLabel.Length > 8 ? categoryLabel.Substring(0, 8) + "..." : categoryLabel;
                GUI.Label(labelRect, shortLabel, EditorStyles.miniLabel);

                // Value label on top of bar
                if (barHeight > 20)
                {
                    var valueRect = new Rect(x, y - 18, barWidth, 16);
                    GUI.Label(valueRect, $"{kvp.Value:F2}", EditorStyles.miniLabel);
                }

                index++;
            }

            // Axis labels
            DrawAxisLabels(chartRect, xLabel, yLabel);

            // Handle hover tooltip
            if (state != null && Event.current.type == EventType.MouseMove)
            {
                state.HoverPosition = Event.current.mousePosition;
                UpdateBarChartTooltip(chartRect, data, maxValue, state);
                Event.current.Use();
            }

            // Draw tooltip
            if (state != null && !string.IsNullOrEmpty(state.HoveredTooltip))
            {
                DrawTooltip(state.HoverPosition, state.HoveredTooltip);
            }
        }

        #endregion

        #region Helper Methods

        private static void DrawEmptyChart(Rect rect, string title)
        {
            EditorGUI.DrawRect(rect, ColorBackground);
            var titleRect = new Rect(rect.x, rect.y, rect.width, 24);
            GUI.Label(titleRect, title, EditorStyles.boldLabel);

            var messageRect = new Rect(rect.x, rect.y + rect.height / 2 - 10, rect.width, 20);
            GUI.Label(messageRect, "No data available", EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawGrid(Rect rect, float minX, float maxX, float minY, float maxY)
        {
            Handles.color = ColorGrid;
            Handles.BeginGUI();

            // Vertical lines
            int vLines = 5;
            for (int i = 0; i <= vLines; i++)
            {
                float x = Mathf.Lerp(rect.x, rect.x + rect.width, (float)i / vLines);
                Vector3 start = new Vector3(x, rect.y, 0);
                Vector3 end = new Vector3(x, rect.y + rect.height, 0);
                Handles.DrawLine(start, end);
            }

            // Horizontal lines
            int hLines = 5;
            for (int i = 0; i <= hLines; i++)
            {
                float y = Mathf.Lerp(rect.y, rect.y + rect.height, (float)i / hLines);
                Vector3 start = new Vector3(rect.x, y, 0);
                Vector3 end = new Vector3(rect.x + rect.width, y, 0);
                Handles.DrawLine(start, end);
            }

            Handles.EndGUI();
        }

        private static void DrawAxisLabels(Rect rect, string xLabel, string yLabel)
        {
            // X-axis label
            var xLabelRect = new Rect(rect.x + rect.width / 2 - 50, rect.y + rect.height + 25, 100, 20);
            GUI.Label(xLabelRect, xLabel, EditorStyles.miniLabel);

            // Y-axis label (rotated)
            var yLabelRect = new Rect(rect.x - 35, rect.y + rect.height / 2 - 10, 30, 20);
            GUI.Label(yLabelRect, yLabel, EditorStyles.miniLabel);
        }

        private static Color GetSeriesColor(int index)
        {
            Color[] colors = new Color[]
            {
                new Color(0.2f, 0.6f, 1f),
                new Color(1f, 0.5f, 0.2f),
                new Color(0.3f, 0.8f, 0.4f),
                new Color(0.8f, 0.2f, 0.8f),
                new Color(0.2f, 0.8f, 0.8f),
                new Color(1f, 0.8f, 0.2f)
            };
            return colors[index % colors.Length];
        }

        private static void UpdateTooltip(
            Rect rect,
            Dictionary<string, List<float>> seriesData,
            float minX, float maxX,
            float minY, float maxY,
            ChartState state)
        {
            if (!rect.Contains(state.HoverPosition))
            {
                state.HoveredTooltip = null;
                return;
            }

            float xRatio = (state.HoverPosition.x - rect.x) / rect.width;
            int dataIndex = Mathf.RoundToInt(Mathf.Lerp(minX, maxX, xRatio));

            if (dataIndex < 0 || dataIndex >= seriesData.Values.First().Count)
            {
                state.HoveredTooltip = null;
                return;
            }

            var tooltip = new System.Text.StringBuilder();
            tooltip.AppendLine($"Day {dataIndex}");
            foreach (var kvp in seriesData)
            {
                if (dataIndex < kvp.Value.Count)
                {
                    tooltip.AppendLine($"{kvp.Key}: {kvp.Value[dataIndex]:F2}");
                }
            }

            state.HoveredTooltip = tooltip.ToString();
        }

        private static void UpdateHistogramTooltip(
            Rect rect,
            int[] bins,
            float minVal,
            float binWidth,
            int maxBinCount,
            ChartState state)
        {
            if (!rect.Contains(state.HoverPosition))
            {
                state.HoveredTooltip = null;
                return;
            }

            float xRatio = (state.HoverPosition.x - rect.x) / rect.width;
            int binIndex = Mathf.FloorToInt(xRatio * bins.Length);

            if (binIndex < 0 || binIndex >= bins.Length)
            {
                state.HoveredTooltip = null;
                return;
            }

            float binStart = minVal + binIndex * binWidth;
            float binEnd = binStart + binWidth;
            float percentage = (bins[binIndex] / (float)maxBinCount) * 100f;

            state.HoveredTooltip = $"Range: {binStart:F1} - {binEnd:F1}\nCount: {bins[binIndex]}\nPercentage: {percentage:F1}%";
        }

        private static void UpdateBarChartTooltip(
            Rect rect,
            Dictionary<string, float> data,
            float maxValue,
            ChartState state)
        {
            if (!rect.Contains(state.HoverPosition))
            {
                state.HoveredTooltip = null;
                return;
            }

            float xRatio = (state.HoverPosition.x - rect.x) / rect.width;
            int index = Mathf.FloorToInt(xRatio * data.Count);

            if (index < 0 || index >= data.Count)
            {
                state.HoveredTooltip = null;
                return;
            }

            var kvp = data.ElementAt(index);
            state.HoveredTooltip = $"{kvp.Key}\nValue: {kvp.Value:F2}";
        }

        private static void DrawTooltip(Vector2 position, string text)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };

            var content = new GUIContent(text);
            var size = style.CalcSize(content);

            var rect = new Rect(position.x + 10, position.y + 10, size.x + 10, size.y + 10);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.95f));
            GUI.Label(rect, content, style);
        }

        #endregion
    }
}