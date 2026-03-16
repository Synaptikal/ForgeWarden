using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Tracks validation history over time.
    /// Stores validation results for analysis and trend tracking.
    /// </summary>
    public static class RsvValidationHistory
    {
        private static readonly string HistoryFilePath = RsvConfiguration.HistoryFilePath;
        private static readonly int MaxHistoryEntries = RsvConfiguration.MaxHistoryEntries;
        private static List<ValidationHistoryEntry> _history = new List<ValidationHistoryEntry>();
        private static bool _isLoaded = false;

        /// <summary>
        /// Gets all history entries.
        /// </summary>
        public static IReadOnlyList<ValidationHistoryEntry> History => _history;

        /// <summary>
        /// Gets the number of history entries.
        /// </summary>
        public static int Count => _history.Count;

        /// <summary>
        /// Loads validation history from disk.
        /// </summary>
        public static void Load()
        {
            if (_isLoaded)
                return;

            try
            {
                var directory = Path.GetDirectoryName(HistoryFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(HistoryFilePath))
                {
                    var json = File.ReadAllText(HistoryFilePath);
                    _history = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ValidationHistoryEntry>>(json);
                    Debug.Log($"[RSV] Loaded {_history.Count} validation history entries.");
                }
                else
                {
                    _history = new List<ValidationHistoryEntry>();
                }

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RSV] Failed to load validation history: {ex.Message}");
                _history = new List<ValidationHistoryEntry>();
                _isLoaded = true;
            }
        }

        /// <summary>
        /// Saves validation history to disk.
        /// </summary>
        public static void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(HistoryFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_history, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(HistoryFilePath, json);
                Debug.Log($"[RSV] Saved {_history.Count} validation history entries.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RSV] Failed to save validation history: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a validation result to history.
        /// </summary>
        /// <param name="report">The validation report.</param>
        /// <param name="durationMs">The duration of the validation in milliseconds.</param>
        public static void AddEntry(LGD_ValidationReport report, long durationMs = 0)
        {
            Load();

            var entry = new ValidationHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Source = report.ToolId,
                OverallStatus = report.OverallStatus,
                EntryCount = report.Entries.Count,
                ErrorCount = report.Entries.Count(e => e.Status == ValidationStatus.Error),
                WarningCount = report.Entries.Count(e => e.Status == ValidationStatus.Warning),
                CriticalCount = report.Entries.Count(e => e.Status == ValidationStatus.Critical),
                PassCount = report.Entries.Count(e => e.Status == ValidationStatus.Pass),
                DurationMs = durationMs
            };

            _history.Add(entry);

            // Enforce max history size
            if (_history.Count > MaxHistoryEntries)
            {
                _history.RemoveAt(0);
            }

            Save();
        }

        /// <summary>
        /// Gets history entries for a specific source.
        /// </summary>
        /// <param name="source">The source name.</param>
        /// <returns>List of history entries.</returns>
        public static List<ValidationHistoryEntry> GetEntriesForSource(string source)
        {
            Load();
            return _history.Where(e => e.Source == source).ToList();
        }

        /// <summary>
        /// Gets history entries within a date range.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>List of history entries.</returns>
        public static List<ValidationHistoryEntry> GetEntriesInRange(DateTime startDate, DateTime endDate)
        {
            Load();
            return _history.Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate).ToList();
        }

        /// <summary>
        /// Gets history entries from the last N days.
        /// </summary>
        /// <param name="days">Number of days.</param>
        /// <returns>List of history entries.</returns>
        public static List<ValidationHistoryEntry> GetEntriesFromLastDays(int days)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            return GetEntriesInRange(startDate, DateTime.UtcNow);
        }

        /// <summary>
        /// Gets validation statistics for a time period.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>Validation statistics.</returns>
        public static ValidationStatistics GetStatistics(DateTime startDate, DateTime endDate)
        {
            Load();
            var entries = GetEntriesInRange(startDate, endDate);

            var stats = new ValidationStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalValidations = entries.Count,
                TotalErrors = entries.Sum(e => e.ErrorCount),
                TotalWarnings = entries.Sum(e => e.WarningCount),
                TotalCritical = entries.Sum(e => e.CriticalCount),
                TotalPasses = entries.Sum(e => e.PassCount)
            };

            if (entries.Count > 0)
            {
                stats.AverageErrors = (float)stats.TotalErrors / entries.Count;
                stats.AverageWarnings = (float)stats.TotalWarnings / entries.Count;
                stats.AverageCritical = (float)stats.TotalCritical / entries.Count;
                stats.AveragePasses = (float)stats.TotalPasses / entries.Count;
            }

            return stats;
        }

        /// <summary>
        /// Clears all history entries.
        /// </summary>
        public static void Clear()
        {
            _history.Clear();
            Save();
            Debug.Log("[RSV] Validation history cleared.");
        }

        /// <summary>
        /// Exports history to CSV file.
        /// </summary>
        /// <param name="filePath">Output file path.</param>
        public static void ExportToCsv(string filePath)
        {
            Load();

            try
            {
                var csv = "Timestamp,Source,Status,Entries,Errors,Warnings,Critical,Passes,DurationMs\n";

                foreach (var entry in _history)
                {
                    csv += $"{entry.Timestamp:O},{entry.Source},{entry.OverallStatus},{entry.EntryCount}," +
                           $"{entry.ErrorCount},{entry.WarningCount},{entry.CriticalCount},{entry.PassCount},{entry.DurationMs}\n";
                }

                File.WriteAllText(filePath, csv);
                Debug.Log($"[RSV] Exported {_history.Count} history entries to {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RSV] Failed to export history: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets validation trends over time.
        /// </summary>
        /// <param name="days">Number of days to analyze.</param>
        /// <returns>List of daily statistics.</returns>
        public static List<DailyStatistics> GetDailyTrends(int days = 30)
        {
            Load();
            var trends = new List<DailyStatistics>();
            var startDate = DateTime.UtcNow.AddDays(-days);

            for (int i = 0; i < days; i++)
            {
                var dayStart = startDate.AddDays(i);
                var dayEnd = dayStart.AddDays(1);
                var entries = GetEntriesInRange(dayStart, dayEnd);

                var dailyStats = new DailyStatistics
                {
                    Date = dayStart,
                    TotalValidations = entries.Count,
                    TotalErrors = entries.Sum(e => e.ErrorCount),
                    TotalWarnings = entries.Sum(e => e.WarningCount),
                    TotalCritical = entries.Sum(e => e.CriticalCount)
                };

                trends.Add(dailyStats);
            }

            return trends;
        }
    }

    /// <summary>
    /// Represents a validation history entry.
    /// </summary>
    public class ValidationHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public ValidationStatus OverallStatus { get; set; }
        public int EntryCount { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
        public int PassCount { get; set; }
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// Validation statistics for a time period.
    /// </summary>
    public class ValidationStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalValidations { get; set; }
        public int TotalErrors { get; set; }
        public int TotalWarnings { get; set; }
        public int TotalCritical { get; set; }
        public int TotalPasses { get; set; }
        public float AverageErrors { get; set; }
        public float AverageWarnings { get; set; }
        public float AverageCritical { get; set; }
        public float AveragePasses { get; set; }

        public override string ToString()
        {
            return $"Statistics ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}):\n" +
                   $"  Total Validations: {TotalValidations}\n" +
                   $"  Total Errors: {TotalErrors} (avg: {AverageErrors:F2})\n" +
                   $"  Total Warnings: {TotalWarnings} (avg: {AverageWarnings:F2})\n" +
                   $"  Total Critical: {TotalCritical} (avg: {AverageCritical:F2})\n" +
                   $"  Total Passes: {TotalPasses} (avg: {AveragePasses:F2})";
        }
    }

    /// <summary>
    /// Daily validation statistics.
    /// </summary>
    public class DailyStatistics
    {
        public DateTime Date { get; set; }
        public int TotalValidations { get; set; }
        public int TotalErrors { get; set; }
        public int TotalWarnings { get; set; }
        public int TotalCritical { get; set; }
    }
}
