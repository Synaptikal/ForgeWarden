using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Progress reporting for validation operations.
    /// Provides real-time feedback during long-running validations.
    /// </summary>
    public class RsvValidationProgress
    {
        private string _operationName;
        private int _totalItems;
        private int _completedItems;
        private float _progress;
        private string _currentItem;
        private DateTime _startTime;
        private DateTime _endTime;
        private bool _isComplete;
        private List<ProgressEvent> _events;

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        public string OperationName => _operationName;

        /// <summary>
        /// Gets the total number of items to validate.
        /// </summary>
        public int TotalItems => _totalItems;

        /// <summary>
        /// Gets the number of completed items.
        /// </summary>
        public int CompletedItems => _completedItems;

        /// <summary>
        /// Gets the current progress (0.0 to 1.0).
        /// </summary>
        public float Progress => _progress;

        /// <summary>
        /// Gets the current item being validated.
        /// </summary>
        public string CurrentItem => _currentItem;

        /// <summary>
        /// Gets the start time of the operation.
        /// </summary>
        public DateTime StartTime => _startTime;

        /// <summary>
        /// Gets the end time of the operation.
        /// </summary>
        public DateTime EndTime => _endTime;

        /// <summary>
        /// Gets whether the operation is complete.
        /// </summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// Gets the elapsed time since the operation started.
        /// </summary>
        public TimeSpan ElapsedTime => _isComplete ? _endTime - _startTime : DateTime.UtcNow - _startTime;

        /// <summary>
        /// Gets the estimated time remaining.
        /// </summary>
        public TimeSpan EstimatedTimeRemaining
        {
            get
            {
                if (_isComplete || _progress <= 0)
                    return TimeSpan.Zero;

                var elapsed = ElapsedTime;
                var estimatedTotal = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / _progress);
                return estimatedTotal - elapsed;
            }
        }

        /// <summary>
        /// Gets all progress events.
        /// </summary>
        public IReadOnlyList<ProgressEvent> Events => _events;

        /// <summary>
        /// Event raised when progress updates.
        /// </summary>
        public event Action<RsvValidationProgress> OnProgressUpdate;

        /// <summary>
        /// Event raised when the operation completes.
        /// </summary>
        public event Action<RsvValidationProgress> OnComplete;

        /// <summary>
        /// Creates a new validation progress tracker.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="totalItems">Total number of items to validate.</param>
        public RsvValidationProgress(string operationName, int totalItems)
        {
            _operationName = operationName;
            _totalItems = totalItems;
            _completedItems = 0;
            _progress = 0f;
            _currentItem = string.Empty;
            _startTime = DateTime.UtcNow;
            _endTime = DateTime.MinValue;
            _isComplete = false;
            _events = new List<ProgressEvent>();

            AddEvent("Started", $"Operation '{operationName}' started with {_totalItems} items");
        }

        /// <summary>
        /// Updates the progress.
        /// </summary>
        /// <param name="completedItems">Number of completed items.</param>
        /// <param name="currentItem">Current item being processed.</param>
        public void UpdateProgress(int completedItems, string currentItem = null)
        {
            _completedItems = completedItems;
            _currentItem = currentItem ?? string.Empty;
            _progress = _totalItems > 0 ? (float)_completedItems / _totalItems : 1f;

            OnProgressUpdate?.Invoke(this);
        }

        /// <summary>
        /// Increments the progress by one item.
        /// </summary>
        /// <param name="currentItem">Current item being processed.</param>
        public void IncrementProgress(string currentItem = null)
        {
            UpdateProgress(_completedItems + 1, currentItem);
        }

        /// <summary>
        /// Marks the operation as complete.
        /// </summary>
        public void Complete()
        {
            if (_isComplete)
                return;

            _isComplete = true;
            _endTime = DateTime.UtcNow;
            _progress = 1f;

            AddEvent("Completed", $"Operation completed in {ElapsedTime.TotalSeconds:F2} seconds");

            OnComplete?.Invoke(this);
        }

        /// <summary>
        /// Adds a progress event.
        /// </summary>
        /// <param name="type">Event type.</param>
        /// <param name="message">Event message.</param>
        public void AddEvent(string type, string message)
        {
            var evt = new ProgressEvent
            {
                Timestamp = DateTime.UtcNow,
                Type = type,
                Message = message
            };

            _events.Add(evt);
        }

        /// <summary>
        /// Gets a summary of the progress.
        /// </summary>
        /// <returns>Summary string.</returns>
        public string GetSummary()
        {
            var status = _isComplete ? "Complete" : "In Progress";
            var elapsed = ElapsedTime.TotalSeconds;
            var remaining = EstimatedTimeRemaining.TotalSeconds;

            return $"{_operationName}: {status} ({_completedItems}/{_totalItems}) - " +
                   $"Elapsed: {elapsed:F2}s, Remaining: {remaining:F2}s";
        }

        /// <summary>
        /// Gets detailed progress information.
        /// </summary>
        /// <returns>Detailed information string.</returns>
        public string GetDetailedInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"Operation: {_operationName}");
            info.AppendLine($"Status: {(_isComplete ? "Complete" : "In Progress")}");
            info.AppendLine($"Progress: {_completedItems}/{_totalItems} ({_progress * 100:F1}%)");
            info.AppendLine($"Current Item: {_currentItem}");
            info.AppendLine($"Elapsed Time: {ElapsedTime.TotalSeconds:F2}s");
            info.AppendLine($"Estimated Remaining: {EstimatedTimeRemaining.TotalSeconds:F2}s");
            info.AppendLine($"Events: {_events.Count}");

            return info.ToString();
        }
    }

    /// <summary>
    /// Represents a progress event.
    /// </summary>
    public class ProgressEvent
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] {Type}: {Message}";
        }
    }

    /// <summary>
    /// Progress reporter for validation operations.
    /// </summary>
    public static class RsvProgressReporter
    {
        private static RsvValidationProgress _currentProgress;

        /// <summary>
        /// Gets the current progress tracker.
        /// </summary>
        public static RsvValidationProgress CurrentProgress => _currentProgress;

        /// <summary>
        /// Starts a new progress tracker.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="totalItems">Total number of items.</param>
        /// <returns>The new progress tracker.</returns>
        public static RsvValidationProgress Start(string operationName, int totalItems)
        {
            _currentProgress = new RsvValidationProgress(operationName, totalItems);
            return _currentProgress;
        }

        /// <summary>
        /// Ends the current progress tracker.
        /// </summary>
        public static void End()
        {
            _currentProgress?.Complete();
            _currentProgress = null;
        }

        /// <summary>
        /// Logs progress to console.
        /// </summary>
        /// <param name="progress">The progress tracker.</param>
        public static void LogProgress(RsvValidationProgress progress)
        {
            if (progress == null)
                return;

            Debug.Log($"[RSV] {progress.GetSummary()}");
        }

        /// <summary>
        /// Logs detailed progress to console.
        /// </summary>
        /// <param name="progress">The progress tracker.</param>
        public static void LogDetailedProgress(RsvValidationProgress progress)
        {
            if (progress == null)
                return;

            Debug.Log($"[RSV] Progress:\n{progress.GetDetailedInfo()}");
        }
    }
}
