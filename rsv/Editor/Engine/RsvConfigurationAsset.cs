using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// ScriptableObject that stores RSV configuration settings.
    /// Provides persistence across Editor sessions with EditorPrefs fallback.
    /// </summary>
    [CreateAssetMenu(fileName = "RSVConfiguration", menuName = "Live Game Dev/RSV/Configuration", order = 100)]
    public class RsvConfigurationAsset : ScriptableObject
    {
        [Header("File Size Limits")]
        [Tooltip("Maximum allowed size for remote HTTP responses in MB")]
        [Range(1, 100)]
        public int maxRemoteResponseSizeMB = 10;

        [Tooltip("Maximum allowed size for local JSON files in MB")]
        [Range(1, 500)]
        public int maxLocalFileSizeMB = 100;

        [Tooltip("Threshold in MB for using streaming JSON parser")]
        [Range(1, 50)]
        public int streamingThresholdMB = 10;

        [Header("Validation Limits")]
        [Tooltip("Maximum nesting depth allowed in JSON structures")]
        [Range(1, 100)]
        public int maxNestingDepth = 20;

        [Tooltip("Maximum number of nodes allowed in a schema definition")]
        [Range(100, 50000)]
        public int maxSchemaNodes = 10000;

        [Tooltip("Maximum number of enum values allowed per field")]
        [Range(10, 5000)]
        public int maxEnumValues = 1000;

        [Header("Cache Settings")]
        [Tooltip("Duration in minutes for caching compiled schemas")]
        [Range(1, 120)]
        public int schemaCacheDurationMinutes = 30;

        [Tooltip("Duration in minutes for caching URL responses")]
        [Range(1, 60)]
        public int urlCacheDurationMinutes = 5;

        [Tooltip("Maximum number of entries in schema cache")]
        [Range(10, 5000)]
        public int maxSchemaCacheSize = 1000;

        [Tooltip("Maximum number of entries in URL cache")]
        [Range(10, 500)]
        public int maxUrlCacheSize = 100;

        [Tooltip("Interval in minutes for automatic cache cleanup")]
        [Range(1, 60)]
        public int cacheCleanupIntervalMinutes = 10;

        [Header("HTTP Settings")]
        [Tooltip("Timeout in seconds for HTTP requests")]
        [Range(5, 300)]
        public int httpTimeoutSeconds = 30;

        [Tooltip("Maximum number of retry attempts for HTTP requests")]
        [Range(0, 10)]
        public int maxHttpRetries = 3;

        [Tooltip("Initial delay in seconds for HTTP retry with exponential backoff")]
        [Range(0.1f, 10f)]
        public float httpRetryInitialDelaySeconds = 1f;

        [Header("History Settings")]
        [Tooltip("Maximum number of validation history entries to keep")]
        [Range(100, 10000)]
        public int maxHistoryEntries = 1000;

        [Tooltip("File path for validation history storage (relative to project root)")]
        public string historyFilePath = "Library/RSV/ValidationHistory.json";

        [Header("Parallel Validation")]
        [Tooltip("Ratio of CPU cores to use for parallel validation (0.1 to 1.0)")]
        [Range(0.1f, 1f)]
        public float defaultParallelismRatio = 0.75f;

        [Header("URL Validation")]
        [Tooltip("Whitelist of allowed URL patterns (one per line). Empty = allow all HTTPS.")]
        [TextArea(3, 5)]
        public string urlWhitelist = "";

        [Tooltip("Blacklist of blocked URL patterns (one per line)")]
        [TextArea(3, 5)]
        public string urlBlacklist = "";

        [Header("Regex Settings")]
        [Tooltip("Timeout in seconds for regex pattern matching to prevent ReDoS attacks")]
        [Range(0.1f, 30f)]
        public float regexTimeoutSeconds = 5f;

        [Header("Auto-Save")]
        [Tooltip("Automatically save configuration changes")]
        public bool autoSave = true;

        [Tooltip("Delay in seconds before auto-saving")]
        [Range(0.5f, 10f)]
        public float autoSaveDelaySeconds = 1f;

        // Non-serialized fields for runtime state
        [NonSerialized] private bool _isDirty;
        [NonSerialized] private double _lastSaveTime;

        /// <summary>
        /// Marks the configuration as dirty, triggering auto-save.
        /// </summary>
        public new void SetDirty()
        {
            _isDirty = true;
            _lastSaveTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Checks if auto-save should be triggered.
        /// </summary>
        public bool ShouldAutoSave()
        {
            if (!_isDirty || !autoSave) return false;
            
            var elapsed = EditorApplication.timeSinceStartup - _lastSaveTime;
            return elapsed >= autoSaveDelaySeconds;
        }

        /// <summary>
        /// Resets the dirty flag after saving.
        /// </summary>
        public void MarkSaved()
        {
            _isDirty = false;
        }

        /// <summary>
        /// Resets all settings to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            maxRemoteResponseSizeMB = 10;
            maxLocalFileSizeMB = 100;
            streamingThresholdMB = 10;
            maxNestingDepth = 20;
            maxSchemaNodes = 10000;
            maxEnumValues = 1000;
            schemaCacheDurationMinutes = 30;
            urlCacheDurationMinutes = 5;
            maxSchemaCacheSize = 1000;
            maxUrlCacheSize = 100;
            cacheCleanupIntervalMinutes = 10;
            httpTimeoutSeconds = 30;
            maxHttpRetries = 3;
            httpRetryInitialDelaySeconds = 1f;
            maxHistoryEntries = 1000;
            historyFilePath = "Library/RSV/ValidationHistory.json";
            defaultParallelismRatio = 0.75f;
            urlWhitelist = "";
            urlBlacklist = "";
            regexTimeoutSeconds = 5f;
            autoSave = true;
            autoSaveDelaySeconds = 1f;

            SetDirty();
        }

        /// <summary>
        /// Gets the default asset path for the configuration.
        /// </summary>
        public static string GetDefaultAssetPath()
        {
            return "Assets/Settings/RSV/RSVConfiguration.asset";
        }

        /// <summary>
        /// Gets or creates the configuration asset.
        /// </summary>
        public static RsvConfigurationAsset GetOrCreate()
        {
            var path = GetDefaultAssetPath();
            var asset = AssetDatabase.LoadAssetAtPath<RsvConfigurationAsset>(path);

            if (asset == null)
            {
                // Ensure directory exists
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // Create new asset
                asset = CreateInstance<RsvConfigurationAsset>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                Debug.Log($"[RSV] Created configuration asset at: {path}");
            }

            return asset;
        }

        /// <summary>
        /// Applies this configuration to the static RsvConfiguration class.
        /// </summary>
        public void ApplyToStaticConfiguration()
        {
            RsvConfiguration.MaxRemoteResponseSizeBytes = maxRemoteResponseSizeMB * 1024 * 1024;
            RsvConfiguration.MaxLocalFileSizeBytes = maxLocalFileSizeMB * 1024 * 1024;
            RsvConfiguration.StreamingThresholdBytes = streamingThresholdMB * 1024 * 1024;
            RsvConfiguration.MaxNestingDepth = maxNestingDepth;
            RsvConfiguration.MaxSchemaNodes = maxSchemaNodes;
            RsvConfiguration.MaxEnumValues = maxEnumValues;
            RsvConfiguration.SchemaCacheDuration = TimeSpan.FromMinutes(schemaCacheDurationMinutes);
            RsvConfiguration.UrlCacheDuration = TimeSpan.FromMinutes(urlCacheDurationMinutes);
            RsvConfiguration.MaxSchemaCacheSize = maxSchemaCacheSize;
            RsvConfiguration.MaxUrlCacheSize = maxUrlCacheSize;
            RsvConfiguration.CacheCleanupInterval = TimeSpan.FromMinutes(cacheCleanupIntervalMinutes);
            RsvConfiguration.HttpTimeout = TimeSpan.FromSeconds(httpTimeoutSeconds);
            RsvConfiguration.MaxHttpRetries = maxHttpRetries;
            RsvConfiguration.HttpRetryInitialDelay = TimeSpan.FromSeconds(httpRetryInitialDelaySeconds);
            RsvConfiguration.MaxHistoryEntries = maxHistoryEntries;
            RsvConfiguration.HistoryFilePath = historyFilePath;
            RsvConfiguration.DefaultParallelismRatio = defaultParallelismRatio;
            RsvConfiguration.UrlWhitelist = ParseUrlPatterns(urlWhitelist);
            RsvConfiguration.UrlBlacklist = ParseUrlPatterns(urlBlacklist);
            RsvConfiguration.RegexTimeout = TimeSpan.FromSeconds(regexTimeoutSeconds);
        }

        /// <summary>
        /// Loads settings from the static RsvConfiguration class.
        /// </summary>
        public void LoadFromStaticConfiguration()
        {
            maxRemoteResponseSizeMB = RsvConfiguration.MaxRemoteResponseSizeBytes / (1024 * 1024);
            maxLocalFileSizeMB = RsvConfiguration.MaxLocalFileSizeBytes / (1024 * 1024);
            streamingThresholdMB = RsvConfiguration.StreamingThresholdBytes / (1024 * 1024);
            maxNestingDepth = RsvConfiguration.MaxNestingDepth;
            maxSchemaNodes = RsvConfiguration.MaxSchemaNodes;
            maxEnumValues = RsvConfiguration.MaxEnumValues;
            schemaCacheDurationMinutes = (int)RsvConfiguration.SchemaCacheDuration.TotalMinutes;
            urlCacheDurationMinutes = (int)RsvConfiguration.UrlCacheDuration.TotalMinutes;
            maxSchemaCacheSize = RsvConfiguration.MaxSchemaCacheSize;
            maxUrlCacheSize = RsvConfiguration.MaxUrlCacheSize;
            cacheCleanupIntervalMinutes = (int)RsvConfiguration.CacheCleanupInterval.TotalMinutes;
            httpTimeoutSeconds = (int)RsvConfiguration.HttpTimeout.TotalSeconds;
            maxHttpRetries = RsvConfiguration.MaxHttpRetries;
            httpRetryInitialDelaySeconds = (float)RsvConfiguration.HttpRetryInitialDelay.TotalSeconds;
            maxHistoryEntries = RsvConfiguration.MaxHistoryEntries;
            historyFilePath = RsvConfiguration.HistoryFilePath;
            defaultParallelismRatio = (float)RsvConfiguration.DefaultParallelismRatio;
            urlWhitelist = string.Join("\n", RsvConfiguration.UrlWhitelist);
            urlBlacklist = string.Join("\n", RsvConfiguration.UrlBlacklist);
            regexTimeoutSeconds = (float)RsvConfiguration.RegexTimeout.TotalSeconds;
        }

        /// <summary>
        /// Parses URL patterns from a multi-line string.
        /// </summary>
        private string[] ParseUrlPatterns(string patterns)
        {
            if (string.IsNullOrWhiteSpace(patterns)) return Array.Empty<string>();
            
            return patterns.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }

        /// <summary>
        /// Saves the configuration to EditorPrefs as fallback.
        /// </summary>
        public void SaveToEditorPrefs()
        {
            EditorPrefs.SetInt("RSV_MaxRemoteResponseSizeMB", maxRemoteResponseSizeMB);
            EditorPrefs.SetInt("RSV_MaxLocalFileSizeMB", maxLocalFileSizeMB);
            EditorPrefs.SetInt("RSV_StreamingThresholdMB", streamingThresholdMB);
            EditorPrefs.SetInt("RSV_MaxNestingDepth", maxNestingDepth);
            EditorPrefs.SetInt("RSV_MaxSchemaNodes", maxSchemaNodes);
            EditorPrefs.SetInt("RSV_MaxEnumValues", maxEnumValues);
            EditorPrefs.SetInt("RSV_SchemaCacheDurationMinutes", schemaCacheDurationMinutes);
            EditorPrefs.SetInt("RSV_UrlCacheDurationMinutes", urlCacheDurationMinutes);
            EditorPrefs.SetInt("RSV_MaxSchemaCacheSize", maxSchemaCacheSize);
            EditorPrefs.SetInt("RSV_MaxUrlCacheSize", maxUrlCacheSize);
            EditorPrefs.SetInt("RSV_CacheCleanupIntervalMinutes", cacheCleanupIntervalMinutes);
            EditorPrefs.SetInt("RSV_HttpTimeoutSeconds", httpTimeoutSeconds);
            EditorPrefs.SetInt("RSV_MaxHttpRetries", maxHttpRetries);
            EditorPrefs.SetFloat("RSV_HttpRetryInitialDelaySeconds", httpRetryInitialDelaySeconds);
            EditorPrefs.SetInt("RSV_MaxHistoryEntries", maxHistoryEntries);
            EditorPrefs.SetString("RSV_HistoryFilePath", historyFilePath);
            EditorPrefs.SetFloat("RSV_DefaultParallelismRatio", defaultParallelismRatio);
            EditorPrefs.SetString("RSV_UrlWhitelist", urlWhitelist);
            EditorPrefs.SetString("RSV_UrlBlacklist", urlBlacklist);
            EditorPrefs.SetFloat("RSV_RegexTimeoutSeconds", regexTimeoutSeconds);
            EditorPrefs.SetBool("RSV_AutoSave", autoSave);
            EditorPrefs.SetFloat("RSV_AutoSaveDelaySeconds", autoSaveDelaySeconds);
        }

        /// <summary>
        /// Loads the configuration from EditorPrefs.
        /// </summary>
        public void LoadFromEditorPrefs()
        {
            maxRemoteResponseSizeMB = EditorPrefs.GetInt("RSV_MaxRemoteResponseSizeMB", 10);
            maxLocalFileSizeMB = EditorPrefs.GetInt("RSV_MaxLocalFileSizeMB", 100);
            streamingThresholdMB = EditorPrefs.GetInt("RSV_StreamingThresholdMB", 10);
            maxNestingDepth = EditorPrefs.GetInt("RSV_MaxNestingDepth", 20);
            maxSchemaNodes = EditorPrefs.GetInt("RSV_MaxSchemaNodes", 10000);
            maxEnumValues = EditorPrefs.GetInt("RSV_MaxEnumValues", 1000);
            schemaCacheDurationMinutes = EditorPrefs.GetInt("RSV_SchemaCacheDurationMinutes", 30);
            urlCacheDurationMinutes = EditorPrefs.GetInt("RSV_UrlCacheDurationMinutes", 5);
            maxSchemaCacheSize = EditorPrefs.GetInt("RSV_MaxSchemaCacheSize", 1000);
            maxUrlCacheSize = EditorPrefs.GetInt("RSV_MaxUrlCacheSize", 100);
            cacheCleanupIntervalMinutes = EditorPrefs.GetInt("RSV_CacheCleanupIntervalMinutes", 10);
            httpTimeoutSeconds = EditorPrefs.GetInt("RSV_HttpTimeoutSeconds", 30);
            maxHttpRetries = EditorPrefs.GetInt("RSV_MaxHttpRetries", 3);
            httpRetryInitialDelaySeconds = EditorPrefs.GetFloat("RSV_HttpRetryInitialDelaySeconds", 1f);
            maxHistoryEntries = EditorPrefs.GetInt("RSV_MaxHistoryEntries", 1000);
            historyFilePath = EditorPrefs.GetString("RSV_HistoryFilePath", "Library/RSV/ValidationHistory.json");
            defaultParallelismRatio = EditorPrefs.GetFloat("RSV_DefaultParallelismRatio", 0.75f);
            urlWhitelist = EditorPrefs.GetString("RSV_UrlWhitelist", "");
            urlBlacklist = EditorPrefs.GetString("RSV_UrlBlacklist", "");
            regexTimeoutSeconds = EditorPrefs.GetFloat("RSV_RegexTimeoutSeconds", 5f);
            autoSave = EditorPrefs.GetBool("RSV_AutoSave", true);
            autoSaveDelaySeconds = EditorPrefs.GetFloat("RSV_AutoSaveDelaySeconds", 1f);
        }
    }
}
