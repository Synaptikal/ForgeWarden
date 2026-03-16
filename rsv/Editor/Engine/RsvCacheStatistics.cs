using System;
using System.Collections.Generic;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Tracks cache statistics for RSV operations.
    /// Provides simple counters for monitoring cache performance.
    /// </summary>
    public static class RsvCacheStatistics
    {
        private static readonly Dictionary<string, CacheStats> _stats = new Dictionary<string, CacheStats>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets statistics for a specific cache key.
        /// </summary>
        public static CacheStats GetStats(string cacheKey)
        {
            lock (_lock)
            {
                if (!_stats.ContainsKey(cacheKey))
                {
                    _stats[cacheKey] = new CacheStats();
                }
                return _stats[cacheKey];
            }
        }

        /// <summary>
        /// Records a cache hit.
        /// </summary>
        public static void RecordHit(string cacheKey)
        {
            lock (_lock)
            {
                var stats = GetStats(cacheKey);
                stats.Hits++;
                stats.LastAccessTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Records a cache miss.
        /// </summary>
        public static void RecordMiss(string cacheKey)
        {
            lock (_lock)
            {
                var stats = GetStats(cacheKey);
                stats.Misses++;
                stats.LastAccessTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Records a cache eviction.
        /// </summary>
        public static void RecordEviction(string cacheKey)
        {
            lock (_lock)
            {
                var stats = GetStats(cacheKey);
                stats.Evictions++;
            }
        }

        /// <summary>
        /// Resets statistics for a specific cache key.
        /// </summary>
        public static void ResetStats(string cacheKey)
        {
            lock (_lock)
            {
                if (_stats.ContainsKey(cacheKey))
                {
                    _stats[cacheKey] = new CacheStats();
                }
            }
        }

        /// <summary>
        /// Resets all statistics.
        /// </summary>
        public static void ResetAllStats()
        {
            lock (_lock)
            {
                _stats.Clear();
            }
        }

        /// <summary>
        /// Gets all cache statistics.
        /// </summary>
        public static Dictionary<string, CacheStats> GetAllStats()
        {
            lock (_lock)
            {
                return new Dictionary<string, CacheStats>(_stats);
            }
        }

        /// <summary>
        /// Gets a summary of all cache statistics.
        /// </summary>
        public static CacheSummary GetSummary()
        {
            lock (_lock)
            {
                var summary = new CacheSummary();
                foreach (var kvp in _stats)
                {
                    summary.TotalHits += kvp.Value.Hits;
                    summary.TotalMisses += kvp.Value.Misses;
                    summary.TotalEvictions += kvp.Value.Evictions;
                    summary.CacheCount++;
                }

                if (summary.TotalHits + summary.TotalMisses > 0)
                {
                    summary.HitRate = (double)summary.TotalHits / (summary.TotalHits + summary.TotalMisses);
                }

                return summary;
            }
        }
    }

    /// <summary>
    /// Statistics for a single cache.
    /// </summary>
    public class CacheStats
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long Evictions { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the hit rate for this cache.
        /// </summary>
        public double HitRate
        {
            get
            {
                var total = Hits + Misses;
                return total > 0 ? (double)Hits / total : 0.0;
            }
        }

        /// <summary>
        /// Gets the total number of requests to this cache.
        /// </summary>
        public long TotalRequests => Hits + Misses;
    }

    /// <summary>
    /// Summary of all cache statistics.
    /// </summary>
    public class CacheSummary
    {
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public long TotalEvictions { get; set; }
        public int CacheCount { get; set; }
        public double HitRate { get; set; }

        /// <summary>
        /// Gets the total number of requests across all caches.
        /// </summary>
        public long TotalRequests => TotalHits + TotalMisses;

        /// <summary>
        /// Gets a formatted summary string.
        /// </summary>
        public override string ToString()
        {
            return $"Caches: {CacheCount}, Hits: {TotalHits:N0}, Misses: {TotalMisses:N0}, " +
                   $"Evictions: {TotalEvictions:N0}, Hit Rate: {HitRate:P2}";
        }
    }
}
