using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor.UI
{
    /// <summary>
    /// Validation dashboard window for RSV.
    /// Provides a visual interface for validation results, progress, and statistics.
    /// </summary>
    public class RsvDashboardWindow : EditorWindow
    {
        private static RsvDashboardWindow _instance;

        private Vector2 _scrollPosition;
        private List<ValidationResult> _results = new List<ValidationResult>();
        private RsvValidationProgress _currentProgress;
        private bool _showDetails = true;
        private bool _showProgress = true;
        private bool _autoRefresh = false;
        private float _refreshInterval = 5f;
        private float _lastRefreshTime;

        private GUIStyle _headerStyle;
        private GUIStyle _passStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _criticalStyle;

        [MenuItem("Tools/ForgeWarden/RSV/Validation Dashboard")]
        public static void ShowWindow()
        {
            _instance = GetWindow<RsvDashboardWindow>("RSV Dashboard");
            _instance.Show();
        }

        private void OnEnable()
        {
            // Subscribe to progress updates
            if (RsvProgressReporter.CurrentProgress != null)
            {
                RsvProgressReporter.CurrentProgress.OnProgressUpdate += OnProgressUpdate;
                RsvProgressReporter.CurrentProgress.OnComplete += OnProgressComplete;
            }

            // Initialize styles
            InitializeStyles();
        }

        private void OnDisable()
        {
            // Unsubscribe from progress updates
            if (RsvProgressReporter.CurrentProgress != null)
            {
                RsvProgressReporter.CurrentProgress.OnProgressUpdate -= OnProgressUpdate;
                RsvProgressReporter.CurrentProgress.OnComplete -= OnProgressComplete;
            }
        }

        private void Update()
        {
            // Auto-refresh if enabled
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                RefreshResults();
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            // Initialize styles if needed
            if (_headerStyle == null)
                InitializeStyles();

            // Header
            DrawHeader();

            // Toolbar
            DrawToolbar();

            // Progress section
            if (_showProgress && _currentProgress != null)
            {
                DrawProgressSection();
            }

            // Results section
            DrawResultsSection();

            // Footer
            DrawFooter();
        }

        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            _passStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.green }
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.yellow }
            };

            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.5f, 0f) }
            };

            _criticalStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red }
            };
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            GUILayout.Label("RSV Validation Dashboard", _headerStyle);
            EditorGUILayout.Space();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Validate All", GUILayout.Width(120)))
            {
                ValidateAllBindings();
            }

            if (GUILayout.Button("Clear Results", GUILayout.Width(120)))
            {
                _results.Clear();
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshResults();
            }

            _showDetails = GUILayout.Toggle(_showDetails, "Show Details", GUILayout.Width(100));
            _showProgress = GUILayout.Toggle(_showProgress, "Show Progress", GUILayout.Width(100));
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawProgressSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Validation Progress", EditorStyles.boldLabel);

            // Progress bar
            var progress = _currentProgress.Progress;
            var progressRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(progressRect, progress, $"{progress * 100:F1}%");

            // Progress details
            EditorGUILayout.LabelField($"Operation: {_currentProgress.OperationName}");
            EditorGUILayout.LabelField($"Items: {_currentProgress.CompletedItems}/{_currentProgress.TotalItems}");
            EditorGUILayout.LabelField($"Current: {_currentProgress.CurrentItem}");
            EditorGUILayout.LabelField($"Elapsed: {_currentProgress.ElapsedTime.TotalSeconds:F2}s");
            EditorGUILayout.LabelField($"Remaining: {_currentProgress.EstimatedTimeRemaining.TotalSeconds:F2}s");

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawResultsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Validation Results", EditorStyles.boldLabel);

            if (_results.Count == 0)
            {
                EditorGUILayout.HelpBox("No validation results yet. Click 'Validate All' to start.", MessageType.Info);
            }
            else
            {
                // Summary
                var passed = _results.Count(r => r.Status == ValidationStatus.Pass);
                var warnings = _results.Count(r => r.Status == ValidationStatus.Warning);
                var errors = _results.Count(r => r.Status == ValidationStatus.Error);
                var critical = _results.Count(r => r.Status == ValidationStatus.Critical);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Total: {_results.Count}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"Passed: {passed}", _passStyle, GUILayout.Width(80));
                EditorGUILayout.LabelField($"Warnings: {warnings}", _warningStyle, GUILayout.Width(80));
                EditorGUILayout.LabelField($"Errors: {errors}", _errorStyle, GUILayout.Width(80));
                EditorGUILayout.LabelField($"Critical: {critical}", _criticalStyle, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // Results list
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                foreach (var result in _results)
                {
                    DrawResultItem(result);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResultItem(ValidationResult result)
        {
            var style = result.Status switch
            {
                ValidationStatus.Pass => _passStyle,
                ValidationStatus.Warning => _warningStyle,
                ValidationStatus.Error => _errorStyle,
                ValidationStatus.Critical => _criticalStyle,
                _ => EditorStyles.label
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(result.Name, style);
            GUILayout.Label(result.Status.ToString(), style, GUILayout.Width(80));
            GUILayout.Label($"{result.Duration}ms", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            if (_showDetails && result.Entries.Count > 0)
            {
                EditorGUILayout.Space();

                foreach (var entry in result.Entries)
                {
                    var entryStyle = entry.Status switch
                    {
                        ValidationStatus.Pass => _passStyle,
                        ValidationStatus.Warning => _warningStyle,
                        ValidationStatus.Error => _errorStyle,
                        ValidationStatus.Critical => _criticalStyle,
                        _ => EditorStyles.label
                    };

                    EditorGUILayout.LabelField($"  [{entry.Status}] {entry.Category}: {entry.Message}", entryStyle);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space();

            // Cache statistics
            var schemaStats = RsvValidator.GetSchemaCacheStats();
            var urlStats = RsvValidator.GetUrlCacheStats();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Cache Stats:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(schemaStats, GUILayout.Width(200));
            EditorGUILayout.LabelField(urlStats);
            EditorGUILayout.EndHorizontal();
        }

        private void ValidateAllBindings()
        {
            _currentProgress = RsvProgressReporter.Start("Dashboard Validation", 0);
            _results.Clear();

            EditorApplication.delayCall += () =>
            {
                var reports = RsvValidator.ValidateAllBindingsParallel();

                foreach (var report in reports)
                {
                    _results.Add(new ValidationResult
                    {
                        Name = report.ToolId,
                        Status = report.OverallStatus,
                        Duration = 0, // Would need to track duration
                        Entries = report.Entries.Select(e => new ValidationEntry
                        {
                            Status = e.Status,
                            Category = e.Category,
                            Message = e.Message
                        }).ToList()
                    });
                }

                _currentProgress.Complete();
                Repaint();
            };
        }

        private void RefreshResults()
        {
            // Refresh results from cache or re-validate
            // For now, just re-validate
            ValidateAllBindings();
        }

        private void OnProgressUpdate(RsvValidationProgress progress)
        {
            _currentProgress = progress;
            Repaint();
        }

        private void OnProgressComplete(RsvValidationProgress progress)
        {
            _currentProgress = progress;
            Repaint();
        }

        private class ValidationResult
        {
            public string Name { get; set; }
            public ValidationStatus Status { get; set; }
            public long Duration { get; set; }
            public List<ValidationEntry> Entries { get; set; } = new List<ValidationEntry>();
        }

        private class ValidationEntry
        {
            public ValidationStatus Status { get; set; }
            public string Category { get; set; }
            public string Message { get; set; }
        }
    }
}
