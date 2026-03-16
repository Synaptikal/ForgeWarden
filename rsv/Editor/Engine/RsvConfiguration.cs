using System;
using UnityEditor;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Centralized configuration for RSV validation settings.
    /// Provides configurable limits and thresholds for validation operations.
    /// Backed by ScriptableObject with EditorPrefs fallback.
    /// </summary>
    public static class RsvConfiguration
    {
        private static RsvConfigurationAsset _asset;
        private static bool _initialized;

        /// <summary>
        /// Gets the configuration asset, loading from disk or creating if needed.
        /// </summary>
        public static RsvConfigurationAsset Asset
        {
            get
            {
                if (_asset == null)
                {
                    _asset = RsvConfigurationAsset.GetOrCreate();
                }
                return _asset;
            }
        }

        /// <summary>
        /// Initializes the configuration system.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Try to load from asset first
            _asset = RsvConfigurationAsset.GetOrCreate();
            
            // If asset is at defaults, try loading from EditorPrefs
            if (IsAtDefaults(_asset))
            {
                _asset.LoadFromEditorPrefs();
            }

            // Apply to static fields
            _asset.ApplyToStaticConfiguration();

            // Register for auto-save updates
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// Checks if the configuration is at default values.
        /// </summary>
        private static bool IsAtDefaults(RsvConfigurationAsset config)
        {
            return config.maxRemoteResponseSizeMB == 10 &&
                   config.maxLocalFileSizeMB == 100 &&
                   config.maxNestingDepth == 20;
        }

        /// <summary>
        /// Called on each Editor update to handle auto-save.
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (_asset != null && _asset.ShouldAutoSave())
            {
                _asset.ApplyToStaticConfiguration();
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();
                _asset.SaveToEditorPrefs();
                _asset.MarkSaved();
            }

            // Update debounced fields
            RSV_DebouncedField.Update();
        }

        /// <summary>
        /// Saves the current configuration to disk and EditorPrefs.
        /// </summary>
        public static void Save()
        {
            if (_asset != null)
            {
                _asset.ApplyToStaticConfiguration();
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();
                _asset.SaveToEditorPrefs();
            }
        }

        /// <summary>
        /// Reloads configuration from disk.
        /// </summary>
        public static void Reload()
        {
            _asset = null; // Force reload
            var config = Asset;
            config.LoadFromStaticConfiguration();
        }
        // File size limits
        private static int _maxRemoteResponseSizeBytes = 10 * 1024 * 1024; // 10MB
        private static int _maxLocalFileSizeBytes = 100 * 1024 * 1024; // 100MB
        private static int _streamingThresholdBytes = 10 * 1024 * 1024; // 10MB

        // Validation limits
        private static int _maxNestingDepth = 20;
        private static int _maxSchemaNodes = 10000;
        private static int _maxEnumValues = 1000;

        // Cache settings
        private static TimeSpan _schemaCacheDuration = TimeSpan.FromMinutes(30);
        private static TimeSpan _urlCacheDuration = TimeSpan.FromMinutes(5);
        private static int _maxSchemaCacheSize = 1000;
        private static int _maxUrlCacheSize = 100;
        private static TimeSpan _cacheCleanupInterval = TimeSpan.FromMinutes(10);

        // HTTP settings
        private static TimeSpan _httpTimeout = TimeSpan.FromSeconds(30);
        private static int _maxHttpRetries = 3;
        private static TimeSpan _httpRetryInitialDelay = TimeSpan.FromSeconds(1);

        // History settings
        private static int _maxHistoryEntries = 1000;
        private static string _historyFilePath = "Library/RSV/ValidationHistory.json";

        // Parallel validation settings
        private static double _defaultParallelismRatio = 0.75; // Use 75% of available cores

        // URL validation settings
        private static string[] _urlWhitelist = new string[0]; // Empty = allow all HTTPS
        private static string[] _urlBlacklist = new string[0]; // Empty = no blacklist

        // Regex settings
        private static TimeSpan _regexTimeout = TimeSpan.FromSeconds(5); // Timeout for regex matching

        /// <summary>
        /// Maximum allowed size for remote HTTP responses in bytes.
        /// </summary>
        public static int MaxRemoteResponseSizeBytes
        {
            get => _maxRemoteResponseSizeBytes;
            set => _maxRemoteResponseSizeBytes = Math.Max(1024, value); // Minimum 1KB
        }

        /// <summary>
        /// Maximum allowed size for local JSON files in bytes.
        /// </summary>
        public static int MaxLocalFileSizeBytes
        {
            get => _maxLocalFileSizeBytes;
            set => _maxLocalFileSizeBytes = Math.Max(1024, value); // Minimum 1KB
        }

        /// <summary>
        /// Threshold in bytes for using streaming JSON parser.
        /// </summary>
        public static int StreamingThresholdBytes
        {
            get => _streamingThresholdBytes;
            set => _streamingThresholdBytes = Math.Max(1024, value); // Minimum 1KB
        }

        /// <summary>
        /// Maximum nesting depth allowed in JSON structures.
        /// </summary>
        public static int MaxNestingDepth
        {
            get => _maxNestingDepth;
            set => _maxNestingDepth = Math.Max(1, value); // Minimum 1
        }

        /// <summary>
        /// Maximum number of nodes allowed in a schema definition.
        /// </summary>
        public static int MaxSchemaNodes
        {
            get => _maxSchemaNodes;
            set => _maxSchemaNodes = Math.Max(1, value); // Minimum 1
        }

        /// <summary>
        /// Maximum number of enum values allowed per field.
        /// </summary>
        public static int MaxEnumValues
        {
            get => _maxEnumValues;
            set => _maxEnumValues = Math.Max(1, value); // Minimum 1
        }

        /// <summary>
        /// Duration for caching compiled schemas.
        /// </summary>
        public static TimeSpan SchemaCacheDuration
        {
            get => _schemaCacheDuration;
            set => _schemaCacheDuration = TimeSpan.FromMilliseconds(Math.Max(1000, value.TotalMilliseconds)); // Minimum 1 second
        }

        /// <summary>
        /// Duration for caching URL responses.
        /// </summary>
        public static TimeSpan UrlCacheDuration
        {
            get => _urlCacheDuration;
            set => _urlCacheDuration = TimeSpan.FromMilliseconds(Math.Max(1000, value.TotalMilliseconds)); // Minimum 1 second
        }

        /// <summary>
        /// Maximum number of entries in schema cache.
        /// </summary>
        public static int MaxSchemaCacheSize
        {
            get => _maxSchemaCacheSize;
            set => _maxSchemaCacheSize = Math.Max(1, value); // Minimum 1
        }

        /// <summary>
        /// Maximum number of entries in URL cache.
        /// </summary>
        public static int MaxUrlCacheSize
        {
            get => _maxUrlCacheSize;
            set => _maxUrlCacheSize = Math.Max(1, value); // Minimum 1
        }

        /// <summary>
        /// Interval for automatic cache cleanup.
        /// </summary>
        public static TimeSpan CacheCleanupInterval
        {
            get => _cacheCleanupInterval;
            set => _cacheCleanupInterval = TimeSpan.FromMilliseconds(Math.Max(1000, value.TotalMilliseconds)); // Minimum 1 second
        }

        /// <summary>
        /// Timeout for HTTP requests.
        /// </summary>
        public static TimeSpan HttpTimeout
        {
            get => _httpTimeout;
            set => _httpTimeout = TimeSpan.FromMilliseconds(Math.Max(1000, value.TotalMilliseconds)); // Minimum 1 second
        }

        /// <summary>
        /// Maximum number of retry attempts for HTTP requests.
        /// </summary>
        public static int MaxHttpRetries
        {
            get => _maxHttpRetries;
            set => _maxHttpRetries = Math.Max(0, value); // Minimum 0
        }

        /// <summary>
        /// Initial delay for HTTP retry with exponential backoff.
        /// </summary>
        public static TimeSpan HttpRetryInitialDelay
        {
            get => _httpRetryInitialDelay;
            set => _httpRetryInitialDelay = TimeSpan.FromMilliseconds(Math.Max(100, value.TotalMilliseconds)); // Minimum 100ms
        }

        /// <summary>
        /// Maximum number of validation history entries to keep.
        /// </summary>
        public static int MaxHistoryEntries
        {
            get => _maxHistoryEntries;
            set => _maxHistoryEntries = Math.Max(1, value); // Minimum 1
        }

        /// <summary>
        /// File path for validation history storage.
        /// </summary>
        public static string HistoryFilePath
        {
            get => _historyFilePath;
            set => _historyFilePath = !string.IsNullOrWhiteSpace(value) ? value : "Library/RSV/ValidationHistory.json";
        }

        /// <summary>
        /// Ratio of CPU cores to use for parallel validation (0.0 to 1.0).
        /// </summary>
        public static double DefaultParallelismRatio
        {
            get => _defaultParallelismRatio;
            set => _defaultParallelismRatio = Math.Clamp(value, 0.1, 1.0); // Between 10% and 100%
        }

        /// <summary>
        /// Whitelist of allowed URL patterns. Empty array means all HTTPS URLs are allowed.
        /// Patterns can be exact domains (*.example.com), or regex patterns (regex:.*\.example\.com).
        /// </summary>
        public static string[] UrlWhitelist
        {
            get => _urlWhitelist;
            set => _urlWhitelist = value ?? Array.Empty<string>();
        }

        /// <summary>
        /// Blacklist of blocked URL patterns. URLs matching these patterns will be rejected.
        /// Patterns can be exact domains (*.example.com), or regex patterns (regex:.*\.example\.com).
        /// </summary>
        public static string[] UrlBlacklist
        {
            get => _urlBlacklist;
            set => _urlBlacklist = value ?? Array.Empty<string>();
        }

        /// <summary>
        /// Timeout for regex pattern matching to prevent ReDoS attacks.
        /// </summary>
        public static TimeSpan RegexTimeout
        {
            get => _regexTimeout;
            set => _regexTimeout = TimeSpan.FromMilliseconds(Math.Max(100, value.TotalMilliseconds)); // Minimum 100ms
        }

        /// <summary>
        /// Resets all configuration values to defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            _maxRemoteResponseSizeBytes = 10 * 1024 * 1024;
            _maxLocalFileSizeBytes = 100 * 1024 * 1024;
            _streamingThresholdBytes = 10 * 1024 * 1024;
            _maxNestingDepth = 20;
            _maxSchemaNodes = 10000;
            _maxEnumValues = 1000;
            _schemaCacheDuration = TimeSpan.FromMinutes(30);
            _urlCacheDuration = TimeSpan.FromMinutes(5);
            _maxSchemaCacheSize = 1000;
            _maxUrlCacheSize = 100;
            _cacheCleanupInterval = TimeSpan.FromMinutes(10);
            _httpTimeout = TimeSpan.FromSeconds(30);
            _maxHttpRetries = 3;
            _httpRetryInitialDelay = TimeSpan.FromSeconds(1);
            _maxHistoryEntries = 1000;
            _historyFilePath = "Library/RSV/ValidationHistory.json";
            _defaultParallelismRatio = 0.75;
            _urlWhitelist = new string[0];
            _urlBlacklist = new string[0];
            _regexTimeout = TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// Gets a summary of current configuration.
        /// </summary>
        public static string GetSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("RSV Configuration Summary:");
            summary.AppendLine("==========================");
            summary.AppendLine($"File Size Limits:");
            summary.AppendLine($"  Max Remote Response: {MaxRemoteResponseSizeBytes / (1024 * 1024)} MB");
            summary.AppendLine($"  Max Local File: {MaxLocalFileSizeBytes / (1024 * 1024)} MB");
            summary.AppendLine($"  Streaming Threshold: {StreamingThresholdBytes / (1024 * 1024)} MB");
            summary.AppendLine($"Validation Limits:");
            summary.AppendLine($"  Max Nesting Depth: {MaxNestingDepth}");
            summary.AppendLine($"  Max Schema Nodes: {MaxSchemaNodes}");
            summary.AppendLine($"  Max Enum Values: {MaxEnumValues}");
            summary.AppendLine($"Cache Settings:");
            summary.AppendLine($"  Schema Cache Duration: {SchemaCacheDuration.TotalMinutes:F1} min");
            summary.AppendLine($"  URL Cache Duration: {UrlCacheDuration.TotalMinutes:F1} min");
            summary.AppendLine($"  Max Schema Cache Size: {MaxSchemaCacheSize}");
            summary.AppendLine($"  Max URL Cache Size: {MaxUrlCacheSize}");
            summary.AppendLine($"  Cleanup Interval: {CacheCleanupInterval.TotalMinutes:F1} min");
            summary.AppendLine($"HTTP Settings:");
            summary.AppendLine($"  Timeout: {HttpTimeout.TotalSeconds:F1} sec");
            summary.AppendLine($"  Max Retries: {MaxHttpRetries}");
            summary.AppendLine($"  Retry Initial Delay: {HttpRetryInitialDelay.TotalSeconds:F1} sec");
            summary.AppendLine($"History Settings:");
            summary.AppendLine($"  Max Entries: {MaxHistoryEntries}");
            summary.AppendLine($"  History File: {HistoryFilePath}");
            summary.AppendLine($"Parallel Validation:");
            summary.AppendLine($"  Parallelism Ratio: {DefaultParallelismRatio:P0}");
            summary.AppendLine($"URL Validation:");
            summary.AppendLine($"  Whitelist Entries: {UrlWhitelist.Length}");
            summary.AppendLine($"  Blacklist Entries: {UrlBlacklist.Length}");
            summary.AppendLine($"Regex Settings:");
            summary.AppendLine($"  Timeout: {RegexTimeout.TotalSeconds:F1} sec");
            return summary.ToString();
        }
    }
}
