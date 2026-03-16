using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Tracks validation metrics for monitoring and analysis.
    /// Provides insights into validation performance and trends.
    /// </summary>
    public static class RsvValidationMetrics
    {
        private static readonly Dictionary<string, MetricEntry> _metrics = new Dictionary<string, MetricEntry>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Records a validation metric.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="category">Metric category.</param>
        public static void RecordMetric(string name, double value, string category = "General")
        {
            lock (_lock)
            {
                if (!_metrics.ContainsKey(name))
                {
                    _metrics[name] = new MetricEntry
                    {
                        Name = name,
                        Category = category,
                        Values = new List<double>(),
                        Count = 0,
                        Sum = 0,
                        Min = double.MaxValue,
                        Max = double.MinValue
                    };
                }

                var entry = _metrics[name];
                entry.Values.Add(value);
                entry.Count++;
                entry.Sum += value;
                entry.Min = Math.Min(entry.Min, value);
                entry.Max = Math.Max(entry.Max, value);
                entry.LastUpdated = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Records a validation duration.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        public static void RecordValidationDuration(string schemaId, long durationMs)
        {
            RecordMetric($"ValidationDuration_{schemaId}", durationMs, "Performance");
        }

        /// <summary>
        /// Records a cache hit.
        /// </summary>
        /// <param name="cacheName">Cache name.</param>
        public static void RecordCacheHit(string cacheName)
        {
            RecordMetric($"CacheHit_{cacheName}", 1, "Cache");
        }

        /// <summary>
        /// Records a cache miss.
        /// </summary>
        /// <param name="cacheName">Cache name.</param>
        public static void RecordCacheMiss(string cacheName)
        {
            RecordMetric($"CacheMiss_{cacheName}", 1, "Cache");
        }

        /// <summary>
        /// Records a validation error.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="errorType">Error type.</param>
        public static void RecordValidationError(string schemaId, string errorType)
        {
            RecordMetric($"ValidationError_{schemaId}_{errorType}", 1, "Errors");
        }

        /// <summary>
        /// Gets a metric by name.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <returns>Metric entry, or null if not found.</returns>
        public static MetricEntry GetMetric(string name)
        {
            lock (_lock)
            {
                return _metrics.TryGetValue(name, out var entry) ? entry : null;
            }
        }

        /// <summary>
        /// Gets all metrics.
        /// </summary>
        /// <returns>All metric entries.</returns>
        public static IReadOnlyList<MetricEntry> GetAllMetrics()
        {
            lock (_lock)
            {
                return _metrics.Values.ToList();
            }
        }

        /// <summary>
        /// Gets metrics by category.
        /// </summary>
        /// <param name="category">Category name.</param>
        /// <returns>Metric entries in the category.</returns>
        public static List<MetricEntry> GetMetricsByCategory(string category)
        {
            lock (_lock)
            {
                return _metrics.Values.Where(m => m.Category == category).ToList();
            }
        }

        /// <summary>
        /// Gets metric statistics.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <returns>Metric statistics, or null if not found.</returns>
        public static MetricStatistics GetMetricStatistics(string name)
        {
            lock (_lock)
            {
                if (!_metrics.TryGetValue(name, out var entry))
                    return null;

                var stats = new MetricStatistics
                {
                    Name = name,
                    Category = entry.Category,
                    Count = entry.Count,
                    Sum = entry.Sum,
                    Average = entry.Count > 0 ? entry.Sum / entry.Count : 0,
                    Min = entry.Min,
                    Max = entry.Max,
                    LastUpdated = entry.LastUpdated
                };

                // Calculate standard deviation
                if (entry.Count > 1)
                {
                    var variance = entry.Values.Sum(v => Math.Pow(v - stats.Average, 2)) / entry.Count;
                    stats.StandardDeviation = Math.Sqrt(variance);
                }

                return stats;
            }
        }

        /// <summary>
        /// Gets all metric statistics.
        /// </summary>
        /// <returns>All metric statistics.</returns>
        public static List<MetricStatistics> GetAllStatistics()
        {
            lock (_lock)
            {
                return _metrics.Values.Select(m => GetMetricStatistics(m.Name)).Where(s => s != null).ToList();
            }
        }

        /// <summary>
        /// Gets cache hit rate.
        /// </summary>
        /// <param name="cacheName">Cache name.</param>
        /// <returns>Hit rate (0.0 to 1.0), or null if no data.</returns>
        public static double? GetCacheHitRate(string cacheName)
        {
            var hits = GetMetric($"CacheHit_{cacheName}");
            var misses = GetMetric($"CacheMiss_{cacheName}");

            if (hits == null || misses == null || hits.Count == 0)
                return null;

            var total = hits.Count + misses.Count;
            return (double)hits.Count / total;
        }

        /// <summary>
        /// Gets average validation duration.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <returns>Average duration in milliseconds, or null if no data.</returns>
        public static double? GetAverageValidationDuration(string schemaId)
        {
            var stats = GetMetricStatistics($"ValidationDuration_{schemaId}");
            return stats?.Average;
        }

        /// <summary>
        /// Gets error rate for a schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <returns>Error rate (0.0 to 1.0), or null if no data.</returns>
        public static double? GetErrorRate(string schemaId)
        {
            var errorMetrics = GetMetricsByCategory("Errors")
                .Where(m => m.Name.StartsWith($"ValidationError_{schemaId}_"))
                .ToList();

            if (errorMetrics.Count == 0)
                return null;

            var totalErrors = errorMetrics.Sum(m => m.Count);
            var totalValidations = GetMetric($"ValidationDuration_{schemaId}")?.Count ?? 0;

            if (totalValidations == 0)
                return null;

            return (double)totalErrors / totalValidations;
        }

        /// <summary>
        /// Resets all metrics.
        /// </summary>
        public static void ResetAll()
        {
            lock (_lock)
            {
                _metrics.Clear();
            }
            Debug.Log("[RSV] All validation metrics reset.");
        }

        /// <summary>
        /// Resets metrics for a specific category.
        /// </summary>
        /// <param name="category">Category name.</param>
        public static void ResetCategory(string category)
        {
            lock (_lock)
            {
                var keys = _metrics.Keys.Where(k => _metrics[k].Category == category).ToList();
                foreach (var key in keys)
                {
                    _metrics.Remove(key);
                }
            }
            Debug.Log($"[RSV] Metrics for category '{category}' reset.");
        }

        /// <summary>
        /// Gets a summary of all metrics.
        /// </summary>
        /// <returns>Summary string.</returns>
        public static string GetSummary()
        {
            var stats = GetAllStatistics();
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("Validation Metrics Summary:");
            summary.AppendLine("==========================");

            var categories = stats.GroupBy(s => s.Category);
            foreach (var category in categories)
            {
                summary.AppendLine($"\n{category.Key}:");
                foreach (var stat in category)
                {
                    summary.AppendLine($"  {stat.Name}: Count={stat.Count}, Avg={stat.Average:F2}, Min={stat.Min:F2}, Max={stat.Max:F2}");
                }
            }

            return summary.ToString();
        }
    }

    /// <summary>
    /// Represents a metric entry.
    /// </summary>
    public class MetricEntry
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public List<double> Values { get; set; }
        public int Count { get; set; }
        public double Sum { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents metric statistics.
    /// </summary>
    public class MetricStatistics
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int Count { get; set; }
        public double Sum { get; set; }
        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double StandardDeviation { get; set; }
        public DateTime LastUpdated { get; set; }

        public override string ToString()
        {
            return $"{Name}: Count={Count}, Avg={Average:F2}, Min={Min:F2}, Max={Max:F2}, StdDev={StandardDeviation:F2}";
        }
    }
}
