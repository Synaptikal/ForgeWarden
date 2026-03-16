using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Manages automatic cache invalidation and cleanup.
    /// Periodically removes expired entries from all caches.
    /// </summary>
    internal static class RsvCacheInvalidationManager
    {
        private static Timer _cleanupTimer;
        private static readonly TimeSpan CleanupInterval = RsvConfiguration.CacheCleanupInterval;
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes the cache invalidation manager.
        /// Called automatically when the Editor loads.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            // Start periodic cleanup timer
            _cleanupTimer = new Timer(
                CleanupExpiredEntries,
                null,
                CleanupInterval,
                CleanupInterval);

            // Register for domain reload to clean up timer
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            Debug.Log("[RSV] Cache invalidation manager initialized (cleanup interval: 10 minutes).");
        }

        /// <summary>
        /// Cleans up expired entries from all caches.
        /// </summary>
        private static void CleanupExpiredEntries(object state)
        {
            try
            {
                // Skip cleanup during play mode or when compiling
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                {
                    return;
                }

                int schemaExpired = RsvSchemaCache.RemoveExpired();
                int urlExpired = RsvUrlResponseCache.RemoveExpired();

                if (schemaExpired > 0 || urlExpired > 0)
                {
                    Debug.Log($"[RSV] Cache cleanup: {schemaExpired} schema entries, {urlExpired} URL entries removed.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RSV] Cache cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called before assembly reload to clean up resources.
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            // Dispose of the timer
            _cleanupTimer?.Dispose();
            _cleanupTimer = null;

            Debug.Log("[RSV] Cache invalidation manager disposed.");
        }

        /// <summary>
        /// Forces an immediate cleanup of all expired cache entries.
        /// </summary>
        public static void ForceCleanup()
        {
            CleanupExpiredEntries(null);
        }

        /// <summary>
        /// Gets the current cache status.
        /// </summary>
        public static string GetCacheStatus()
        {
            var schemaStats = RsvSchemaCache.GetStats();
            var urlStats = RsvUrlResponseCache.GetStats();

            return $"Schema Cache: {schemaStats}\nURL Cache: {urlStats}";
        }

        /// <summary>
        /// Clears all caches immediately.
        /// </summary>
        public static void ClearAllCaches()
        {
            RsvSchemaCache.InvalidateAll();
            RsvUrlResponseCache.InvalidateAll();
            Debug.Log("[RSV] All caches cleared.");
        }
    }
}
