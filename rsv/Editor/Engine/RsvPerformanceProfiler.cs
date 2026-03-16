using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Performance profiling tools for validation operations.
    /// Provides detailed performance analysis and optimization insights.
    /// </summary>
    public static class RsvPerformanceProfiler
    {
        private static readonly Dictionary<string, ProfileSession> _sessions = new Dictionary<string, ProfileSession>();
        private static readonly object _lock = new object();
        private static ProfileSession _currentSession;

        /// <summary>
        /// Starts a new profiling session.
        /// </summary>
        /// <param name="sessionName">Name of the session.</param>
        /// <returns>The profiling session.</returns>
        public static ProfileSession StartSession(string sessionName)
        {
            lock (_lock)
            {
                var session = new ProfileSession
                {
                    Name = sessionName,
                    StartTime = DateTime.UtcNow,
                    Markers = new List<ProfileMarker>()
                };

                _currentSession = session;
                _sessions[sessionName] = session;

                Debug.Log($"[RSV Profiler] Started session: {sessionName}");
                return session;
            }
        }

        /// <summary>
        /// Ends the current profiling session.
        /// </summary>
        /// <returns>The completed session, or null if no active session.</returns>
        public static ProfileSession EndSession()
        {
            lock (_lock)
            {
                if (_currentSession == null)
                    return null;

                _currentSession.EndTime = DateTime.UtcNow;
                _currentSession.Duration = _currentSession.EndTime - _currentSession.StartTime;

                Debug.Log($"[RSV Profiler] Ended session: {_currentSession.Name} (Duration: {_currentSession.Duration.TotalMilliseconds:F2}ms)");

                var session = _currentSession;
                _currentSession = null;
                return session;
            }
        }

        /// <summary>
        /// Records a profile marker.
        /// </summary>
        /// <param name="name">Marker name.</param>
        /// <param name="category">Marker category.</param>
        public static void RecordMarker(string name, string category = "General")
        {
            lock (_lock)
            {
                if (_currentSession == null)
                    return;

                var marker = new ProfileMarker
                {
                    Name = name,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    ElapsedMs = (DateTime.UtcNow - _currentSession.StartTime).TotalMilliseconds
                };

                _currentSession.Markers.Add(marker);
            }
        }

        /// <summary>
        /// Records a profile marker with duration.
        /// </summary>
        /// <param name="name">Marker name.</param>
        /// <param name="durationMs">Duration in milliseconds.</param>
        /// <param name="category">Marker category.</param>
        public static void RecordMarkerWithDuration(string name, double durationMs, string category = "General")
        {
            lock (_lock)
            {
                if (_currentSession == null)
                    return;

                var marker = new ProfileMarker
                {
                    Name = name,
                    Category = category,
                    Timestamp = DateTime.UtcNow,
                    ElapsedMs = (DateTime.UtcNow - _currentSession.StartTime).TotalMilliseconds,
                    DurationMs = durationMs
                };

                _currentSession.Markers.Add(marker);
            }
        }

        /// <summary>
        /// Gets a profiling session by name.
        /// </summary>
        /// <param name="sessionName">Session name.</param>
        /// <returns>The session, or null if not found.</returns>
        public static ProfileSession GetSession(string sessionName)
        {
            lock (_lock)
            {
                return _sessions.TryGetValue(sessionName, out var session) ? session : null;
            }
        }

        /// <summary>
        /// Gets all profiling sessions.
        /// </summary>
        /// <returns>All sessions.</returns>
        public static IReadOnlyList<ProfileSession> GetAllSessions()
        {
            lock (_lock)
            {
                return _sessions.Values.ToList();
            }
        }

        /// <summary>
        /// Gets performance analysis for a session.
        /// </summary>
        /// <param name="sessionName">Session name.</param>
        /// <returns>Performance analysis, or null if session not found.</returns>
        public static PerformanceAnalysis GetAnalysis(string sessionName)
        {
            lock (_lock)
            {
                if (!_sessions.TryGetValue(sessionName, out var session))
                    return null;

                var analysis = new PerformanceAnalysis
                {
                    SessionName = sessionName,
                    TotalDuration = session.Duration,
                    TotalMarkers = session.Markers.Count
                };

                // Analyze by category
                var categories = session.Markers.GroupBy(m => m.Category);
                foreach (var category in categories)
                {
                    var categoryStats = new CategoryStatistics
                    {
                        Category = category.Key,
                        Count = category.Count(),
                        TotalDuration = category.Sum(m => m.DurationMs ?? 0),
                        AverageDuration = category.Average(m => m.DurationMs ?? 0),
                        MaxDuration = category.Max(m => m.DurationMs ?? 0),
                        MinDuration = category.Min(m => m.DurationMs ?? 0)
                    };

                    analysis.Categories.Add(categoryStats);
                }

                // Find slowest markers
                analysis.SlowestMarkers = session.Markers
                    .Where(m => m.DurationMs.HasValue)
                    .OrderByDescending(m => m.DurationMs)
                    .Take(10)
                    .ToList();

                return analysis;
            }
        }

        /// <summary>
        /// Clears all profiling sessions.
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _sessions.Clear();
                _currentSession = null;
            }
            Debug.Log("[RSV Profiler] All sessions cleared.");
        }

        /// <summary>
        /// Profiles a validation operation.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="action">Validation action.</param>
        /// <returns>Profile result.</returns>
        public static ProfileResult ProfileValidation(string schemaId, Action action)
        {
            var result = new ProfileResult
            {
                SchemaId = schemaId,
                StartTime = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                action?.Invoke();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.EndTime = DateTime.UtcNow;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Profiles a validation operation with return value.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="func">Validation function.</param>
        /// <returns>Profile result with return value.</returns>
        public static ProfileResult<T> ProfileValidation<T>(string schemaId, Func<T> func)
        {
            var result = new ProfileResult<T>
            {
                SchemaId = schemaId,
                StartTime = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                result.Value = func();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.EndTime = DateTime.UtcNow;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a profiling session.
    /// </summary>
    public class ProfileSession
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<ProfileMarker> Markers { get; set; }

        public override string ToString()
        {
            return $"Session: {Name}, Duration: {Duration.TotalMilliseconds:F2}ms, Markers: {Markers.Count}";
        }
    }

    /// <summary>
    /// Represents a profile marker.
    /// </summary>
    public class ProfileMarker
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public DateTime Timestamp { get; set; }
        public double ElapsedMs { get; set; }
        public double? DurationMs { get; set; }

        public override string ToString()
        {
            var durationStr = DurationMs.HasValue ? $" (Duration: {DurationMs.Value:F2}ms)" : "";
            return $"[{Category}] {Name} at {ElapsedMs:F2}ms{durationStr}";
        }
    }

    /// <summary>
    /// Represents performance analysis.
    /// </summary>
    public class PerformanceAnalysis
    {
        public string SessionName { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public int TotalMarkers { get; set; }
        public List<CategoryStatistics> Categories { get; set; } = new List<CategoryStatistics>();
        public List<ProfileMarker> SlowestMarkers { get; set; } = new List<ProfileMarker>();

        public override string ToString()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"Performance Analysis: {SessionName}");
            summary.AppendLine($"Total Duration: {TotalDuration.TotalMilliseconds:F2}ms");
            summary.AppendLine($"Total Markers: {TotalMarkers}");
            summary.AppendLine();

            summary.AppendLine("Categories:");
            foreach (var category in Categories)
            {
                summary.AppendLine($"  {category.Category}: Count={category.Count}, " +
                                  $"Avg={category.AverageDuration:F2}ms, " +
                                  $"Max={category.MaxDuration:F2}ms");
            }

            summary.AppendLine();
            summary.AppendLine("Slowest Markers:");
            foreach (var marker in SlowestMarkers)
            {
                summary.AppendLine($"  {marker}");
            }

            return summary.ToString();
        }
    }

    /// <summary>
    /// Represents category statistics.
    /// </summary>
    public class CategoryStatistics
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public double TotalDuration { get; set; }
        public double AverageDuration { get; set; }
        public double MaxDuration { get; set; }
        public double MinDuration { get; set; }
    }

    /// <summary>
    /// Represents a profile result.
    /// </summary>
    public class ProfileResult
    {
        public string SchemaId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }

        public override string ToString()
        {
            var status = Success ? "Success" : $"Failed: {Error}";
            return $"Profile: {SchemaId}, Duration: {DurationMs}ms, Status: {status}";
        }
    }

    /// <summary>
    /// Represents a profile result with return value.
    /// </summary>
    public class ProfileResult<T> : ProfileResult
    {
        public T Value { get; set; }
    }
}
