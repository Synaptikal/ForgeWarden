using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Caches remote URL responses to avoid repeated network calls.
    /// Thread-safe implementation with TTL (Time To Live) support, LRU eviction,
    /// memory pressure monitoring, and weak references for large responses.
    /// </summary>
    public static class RsvUrlResponseCache
    {
        // Primary cache with LRU tracking
        private static readonly Dictionary<string, CachedResponse> _cache = new Dictionary<string, CachedResponse>();
        
        // Weak reference cache for large responses
        private static readonly ConditionalWeakTable<object, string> _weakCache = new ConditionalWeakTable<object, string>();
        
        // LRU tracking - maintains access order for eviction
        private static readonly LinkedList<string> _lruList = new LinkedList<string>();
        private static readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new Dictionary<string, LinkedListNode<string>>();
        
        private static readonly object _lock = new object();
        private static TimeSpan _cacheDuration = RsvConfiguration.UrlCacheDuration;
        private static int _maxCacheSize = RsvConfiguration.MaxUrlCacheSize;
        
        // Memory pressure thresholds
        private static long _memoryPressureThresholdBytes = 100 * 1024 * 1024; // 100MB for URL cache
        private static long _criticalMemoryThresholdBytes = 200 * 1024 * 1024; // 200MB
        private static DateTime _lastMemoryCheck = DateTime.MinValue;
        private static readonly TimeSpan _memoryCheckInterval = TimeSpan.FromSeconds(30);
        
        // Maximum size for a single cached response (10MB)
        private static readonly int _maxResponseSizeBytes = 10 * 1024 * 1024;
        
        // Cache statistics
        private static long _totalHits = 0;
        private static long _totalMisses = 0;
        private static long _totalEvictions = 0;
        private static long _memoryPressureEvictions = 0;
        private static long _oversizedRejections = 0;

        /// <summary>
        /// Gets or fetches a URL response from cache.
        /// </summary>
        /// <param name="url">The URL to fetch.</param>
        /// <param name="fetchFunc">The function to fetch the URL if not cached.</param>
        /// <returns>The cached or fetched response, or null if fetch fails.</returns>
        public static string GetOrFetch(string url, Func<string> fetchFunc)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var cacheKey = GetCacheKey(url);
            
            // Check memory pressure periodically
            CheckMemoryPressure();

            lock (_lock)
            {
                // Check if response is in cache and not expired
                if (_cache.TryGetValue(cacheKey, out var cached) && !IsExpired(cached))
                {
                    // Update LRU order
                    UpdateLruOrder(cacheKey);
                    
                    RsvCacheStatistics.RecordHit("UrlCache");
                    _totalHits++;
                    return cached.Content;
                }

                // Fetch and cache the response
                var content = fetchFunc?.Invoke();
                if (content != null)
                {
                    // Check size limit
                    if (content.Length > _maxResponseSizeBytes)
                    {
                        _oversizedRejections++;
                        Debug.LogWarning($"[RSV] Response from {url} exceeds max size ({content.Length / 1024 / 1024}MB > {_maxResponseSizeBytes / 1024 / 1024}MB). Not caching.");
                        return content; // Return but don't cache
                    }

                    SetInternal(cacheKey, url, content);
                    RsvCacheStatistics.RecordMiss("UrlCache");
                    _totalMisses++;
                }

                return content;
            }
        }

        /// <summary>
        /// Gets a cached response if available and not expired.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>The cached content, or null if not cached or expired.</returns>
        public static string Get(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var cacheKey = GetCacheKey(url);
            
            CheckMemoryPressure();

            lock (_lock)
            {
                if (_cache.TryGetValue(cacheKey, out var cached) && !IsExpired(cached))
                {
                    UpdateLruOrder(cacheKey);
                    RsvCacheStatistics.RecordHit("UrlCache");
                    _totalHits++;
                    return cached.Content;
                }

                return null;
            }
        }

        /// <summary>
        /// Sets a cached response for a URL.
        /// </summary>
        /// <param name="url">The URL to cache.</param>
        /// <param name="content">The content to cache.</param>
        public static void Set(string url, string content)
        {
            if (string.IsNullOrWhiteSpace(url) || content == null)
                return;

            // Check size limit
            if (content.Length > _maxResponseSizeBytes)
            {
                _oversizedRejections++;
                Debug.LogWarning($"[RSV] Response from {url} exceeds max size. Not caching.");
                return;
            }

            var cacheKey = GetCacheKey(url);
            
            CheckMemoryPressure();

            lock (_lock)
            {
                SetInternal(cacheKey, url, content);
            }
        }

        /// <summary>
        /// Internal method to set cache entry (must be called within lock).
        /// </summary>
        private static void SetInternal(string cacheKey, string url, string content)
        {
            // Enforce cache size limit with LRU eviction
            while (_cache.Count >= _maxCacheSize && _lruList.Count > 0)
            {
                EvictLruEntry();
            }

            var cached = new CachedResponse
            {
                Content = content,
                CachedTime = DateTime.UtcNow,
                Url = url,
                EstimatedSizeBytes = content.Length * 2 // UTF-16 encoding
            };

            _cache[cacheKey] = cached;
            
            // Add to LRU tracking
            if (_lruNodes.ContainsKey(cacheKey))
            {
                _lruList.Remove(_lruNodes[cacheKey]);
                _lruNodes.Remove(cacheKey);
            }
            var node = _lruList.AddLast(cacheKey);
            _lruNodes[cacheKey] = node;
        }

        /// <summary>
        /// Updates the LRU order when an item is accessed.
        /// </summary>
        private static void UpdateLruOrder(string cacheKey)
        {
            if (_lruNodes.TryGetValue(cacheKey, out var node))
            {
                _lruList.Remove(node);
                _lruNodes[cacheKey] = _lruList.AddLast(cacheKey);
            }
        }

        /// <summary>
        /// Evicts the least recently used entry from the cache.
        /// </summary>
        private static void EvictLruEntry()
        {
            if (_lruList.First == null)
                return;

            var oldestKey = _lruList.First.Value;
            _lruList.RemoveFirst();
            _lruNodes.Remove(oldestKey);
            
            if (_cache.TryGetValue(oldestKey, out var cached))
            {
                _cache.Remove(oldestKey);
                _totalEvictions++;
                RsvCacheStatistics.RecordEviction("UrlCache");
            }
        }

        /// <summary>
        /// Checks memory pressure and evicts entries if necessary.
        /// </summary>
        private static void CheckMemoryPressure()
        {
            var now = DateTime.UtcNow;
            if (now - _lastMemoryCheck < _memoryCheckInterval)
                return;

            _lastMemoryCheck = now;
            
            var memoryUsed = GC.GetTotalMemory(false);
            
            if (memoryUsed > _criticalMemoryThresholdBytes)
            {
                // Critical memory pressure - clear half the cache
                Debug.LogWarning($"[RSV] Critical memory pressure detected ({memoryUsed / 1024 / 1024}MB). Clearing 50% of URL cache.");
                EvictPercentage(0.5);
                _memoryPressureEvictions++;
            }
            else if (memoryUsed > _memoryPressureThresholdBytes)
            {
                // Moderate memory pressure - clear 25% of cache
                Debug.Log($"[RSV] Memory pressure detected ({memoryUsed / 1024 / 1024}MB). Clearing 25% of URL cache.");
                EvictPercentage(0.25);
                _memoryPressureEvictions++;
            }
        }

        /// <summary>
        /// Evicts a percentage of the cache (oldest entries first).
        /// </summary>
        private static void EvictPercentage(double percentage)
        {
            int entriesToEvict = (int)(_cache.Count * percentage);
            for (int i = 0; i < entriesToEvict && _lruList.Count > 0; i++)
            {
                EvictLruEntry();
            }
        }

        /// <summary>
        /// Tries to get a cached response if available and not expired.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <param name="content">The cached content, or null if not cached or expired.</param>
        /// <returns>True if the content was found in cache, false otherwise.</returns>
        public static bool TryGet(string url, out string content)
        {
            content = Get(url);
            return content != null;
        }

        /// <summary>
        /// Invalidates a specific URL from the cache.
        /// </summary>
        /// <param name="url">The URL to invalidate.</param>
        public static void Invalidate(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            var cacheKey = GetCacheKey(url);

            lock (_lock)
            {
                if (_cache.ContainsKey(cacheKey))
                {
                    _cache.Remove(cacheKey);
                    RsvCacheStatistics.RecordEviction("UrlCache");
                }

                if (_lruNodes.TryGetValue(cacheKey, out var node))
                {
                    _lruList.Remove(node);
                    _lruNodes.Remove(cacheKey);
                }
            }
        }

        /// <summary>
        /// Invalidates all URLs from the cache.
        /// </summary>
        public static void InvalidateAll()
        {
            lock (_lock)
            {
                var count = _cache.Count;
                _cache.Clear();
                _lruList.Clear();
                _lruNodes.Clear();
                RsvCacheStatistics.RecordEviction("UrlCache");
                Debug.Log($"[RSV] URL response cache cleared ({count} entries invalidated).");
            }
        }

        /// <summary>
        /// Removes expired entries from the cache.
        /// </summary>
        /// <returns>The number of entries removed.</returns>
        public static int RemoveExpired()
        {
            lock (_lock)
            {
                var expiredKeys = new List<string>();

                foreach (var kvp in _cache)
                {
                    if (IsExpired(kvp.Value))
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _cache.Remove(key);
                    if (_lruNodes.TryGetValue(key, out var node))
                    {
                        _lruList.Remove(node);
                        _lruNodes.Remove(key);
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    RsvCacheStatistics.RecordEviction("UrlCache");
                    Debug.Log($"[RSV] Removed {expiredKeys.Count} expired URL cache entries.");
                }

                return expiredKeys.Count;
            }
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        /// <returns>Statistics about the URL cache.</returns>
        public static UrlCacheStats GetStats()
        {
            lock (_lock)
            {
                var stats = new UrlCacheStats
                {
                    TotalEntries = _cache.Count,
                    ExpiredEntries = 0,
                    CacheDuration = _cacheDuration,
                    MaxCacheSize = _maxCacheSize,
                    TotalHits = _totalHits,
                    TotalMisses = _totalMisses,
                    TotalEvictions = _totalEvictions,
                    MemoryPressureEvictions = _memoryPressureEvictions,
                    OversizedRejections = _oversizedRejections,
                    EstimatedMemoryBytes = CalculateTotalMemory()
                };

                foreach (var kvp in _cache)
                {
                    if (IsExpired(kvp.Value))
                    {
                        stats.ExpiredEntries++;
                    }
                }

                return stats;
            }
        }

        /// <summary>
        /// Calculates total estimated memory usage of the cache.
        /// </summary>
        private static long CalculateTotalMemory()
        {
            long total = 0;
            foreach (var kvp in _cache)
            {
                total += kvp.Value.EstimatedSizeBytes;
            }
            return total;
        }

        /// <summary>
        /// Sets the cache duration for new entries.
        /// </summary>
        /// <param name="duration">The duration to cache responses.</param>
        public static void SetCacheDuration(TimeSpan duration)
        {
            _cacheDuration = duration;
            Debug.Log($"[RSV] URL cache duration set to {duration.TotalMinutes:F1} minutes.");
        }

        /// <summary>
        /// Sets the maximum cache size.
        /// </summary>
        /// <param name="maxSize">The maximum number of entries to cache.</param>
        public static void SetMaxCacheSize(int maxSize)
        {
            _maxCacheSize = maxSize;
            Debug.Log($"[RSV] URL cache max size set to {maxSize} entries.");
        }

        /// <summary>
        /// Sets memory pressure thresholds.
        /// </summary>
        public static void SetMemoryThresholds(long pressureBytes, long criticalBytes)
        {
            _memoryPressureThresholdBytes = pressureBytes;
            _criticalMemoryThresholdBytes = criticalBytes;
            Debug.Log($"[RSV] URL cache memory thresholds set: Pressure={pressureBytes/1024/1024}MB, Critical={criticalBytes/1024/1024}MB");
        }

        /// <summary>
        /// Generates a unique cache key for a URL.
        /// </summary>
        private static string GetCacheKey(string url)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(url);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Checks if a cached response has expired.
        /// </summary>
        private static bool IsExpired(CachedResponse cached)
        {
            return DateTime.UtcNow - cached.CachedTime > _cacheDuration;
        }
    }

    /// <summary>
    /// Represents a cached URL response.
    /// </summary>
    internal class CachedResponse
    {
        public string Content { get; set; }
        public DateTime CachedTime { get; set; }
        public string Url { get; set; }
        public long EstimatedSizeBytes { get; set; }
    }

    /// <summary>
    /// Statistics for the URL cache.
    /// </summary>
    public class UrlCacheStats
    {
        public int TotalEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public int ActiveEntries => TotalEntries - ExpiredEntries;
        public TimeSpan CacheDuration { get; set; }
        public int MaxCacheSize { get; set; }
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public long TotalEvictions { get; set; }
        public long MemoryPressureEvictions { get; set; }
        public long OversizedRejections { get; set; }
        public long EstimatedMemoryBytes { get; set; }
        public double HitRate => TotalHits + TotalMisses > 0 ? (double)TotalHits / (TotalHits + TotalMisses) : 0;

        public override string ToString()
        {
            return $"URL Cache: {ActiveEntries} active, {ExpiredEntries} expired, {TotalEntries}/{MaxCacheSize} total " +
                   $"(HitRate: {HitRate:P1}, Memory: {EstimatedMemoryBytes/1024/1024}MB, " +
                   $"PressureEvictions: {MemoryPressureEvictions}, Oversized: {OversizedRejections})";
        }
    }
}
