using System;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Manages automatic cache invalidation and cleanup.
    /// Periodically removes expired entries from all caches.
    /// Uses EditorApplication.update (main thread) rather than System.Threading.Timer
    /// so that Unity Editor APIs can be safely called inside the cleanup routine.
    /// </summary>
    internal static class RsvCacheInvalidationManager
    {
        private static readonly TimeSpan CleanupInterval = RsvConfiguration.CacheCleanupInterval;
        private static bool _isInitialized = false;
        private static DateTime _lastCleanup;

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
            _lastCleanup = DateTime.UtcNow;

            EditorApplication.update += OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            Debug.Log("[RSV] Cache invalidation manager initialized (cleanup interval: 10 minutes).");
        }

        /// <summary>
        /// Called every editor frame. Triggers cleanup once per CleanupInterval.
        /// Runs on the main thread — safe to call all Unity Editor APIs.
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (DateTime.UtcNow - _lastCleanup < CleanupInterval)
                return;

            _lastCleanup = DateTime.UtcNow;
            CleanupExpiredEntries();
        }

        /// <summary>
        /// Cleans up expired entries from all caches.
        /// Must be called from the main thread.
        /// </summary>
        private static void CleanupExpiredEntries()
        {
            try
            {
                // Skip cleanup during play mode or when compiling
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                    return;

                int schemaExpired = RsvSchemaCache.RemoveExpired();
                int urlExpired = RsvUrlResponseCache.RemoveExpired();

                if (schemaExpired > 0 || urlExpired > 0)
                    Debug.Log($"[RSV] Cache cleanup: {schemaExpired} schema entries, {urlExpired} URL entries removed.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RSV] Cache cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called before assembly reload to unsubscribe from EditorApplication.update.
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            EditorApplication.update -= OnEditorUpdate;
            _isInitialized = false;

            Debug.Log("[RSV] Cache invalidation manager disposed.");
        }

        /// <summary>
        /// Forces an immediate cleanup of all expired cache entries.
        /// Must be called from the main thread.
        /// </summary>
        public static void ForceCleanup()
        {
            _lastCleanup = DateTime.UtcNow;
            CleanupExpiredEntries();
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
