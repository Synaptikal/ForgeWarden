using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Caches compiled schemas to avoid repeated compilation overhead.
    /// Thread-safe implementation with automatic cache management, LRU eviction,
    /// memory pressure monitoring, and weak references for large objects.
    /// </summary>
    public static class RsvSchemaCache
    {
        // Primary cache with LRU tracking
        private static readonly Dictionary<string, CachedSchema> _cache = new Dictionary<string, CachedSchema>();
        
        // Weak reference cache for large schemas to allow GC collection under memory pressure
        private static readonly ConditionalWeakTable<DataSchemaDefinition, CompiledSchema> _weakCache = new ConditionalWeakTable<DataSchemaDefinition, CompiledSchema>();
        
        // LRU tracking - maintains access order for eviction
        private static readonly LinkedList<string> _lruList = new LinkedList<string>();
        private static readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new Dictionary<string, LinkedListNode<string>>();
        
        private static readonly object _lock = new object();
        private static TimeSpan _cacheDuration = RsvConfiguration.SchemaCacheDuration;
        private static int _maxCacheSize = RsvConfiguration.MaxSchemaCacheSize;
        
        // Memory pressure thresholds
        private static long _memoryPressureThresholdBytes = 500 * 1024 * 1024; // 500MB
        private static long _criticalMemoryThresholdBytes = 1024 * 1024 * 1024; // 1GB
        private static DateTime _lastMemoryCheck = DateTime.MinValue;
        private static readonly TimeSpan _memoryCheckInterval = TimeSpan.FromSeconds(30);
        
        // Cache statistics
        private static long _totalHits = 0;
        private static long _totalMisses = 0;
        private static long _totalEvictions = 0;
        private static long _memoryPressureEvictions = 0;

        /// <summary>
        /// Gets or creates a cached compiled schema.
        /// </summary>
        /// <param name="schema">The schema to compile and cache.</param>
        /// <returns>The compiled schema from cache or newly compiled.</returns>
        public static CompiledSchema GetOrCompile(DataSchemaDefinition schema)
        {
            if (schema == null)
                return null;

            var cacheKey = GetCacheKey(schema);
            
            // Check memory pressure periodically
            CheckMemoryPressure();

            lock (_lock)
            {
                // Check if schema is in cache and not expired
                if (_cache.TryGetValue(cacheKey, out var cached) && !IsExpired(cached))
                {
                    // Update LRU order
                    UpdateLruOrder(cacheKey);
                    
                    RsvCacheStatistics.RecordHit("SchemaCache");
                    _totalHits++;
                    return cached.CompiledSchema;
                }

                // Try weak cache as fallback
                if (_weakCache.TryGetValue(schema, out var weakCompiled))
                {
                    RsvCacheStatistics.RecordHit("SchemaCache_Weak");
                    _totalHits++;
                    
                    // Re-add to strong cache
                    AddToCache(cacheKey, schema, weakCompiled);
                    return weakCompiled;
                }

                // Compile and cache the schema
                var compiled = RsvSchemaCompiler.Compile(schema);
                if (compiled != null)
                {
                    AddToCache(cacheKey, schema, compiled);
                    RsvCacheStatistics.RecordMiss("SchemaCache");
                    _totalMisses++;
                }

                return compiled;
            }
        }

        /// <summary>
        /// Adds a compiled schema to the cache with LRU management.
        /// </summary>
        private static void AddToCache(string cacheKey, DataSchemaDefinition schema, CompiledSchema compiled)
        {
            // Enforce cache size limit with LRU eviction
            while (_cache.Count >= _maxCacheSize && _lruList.Count > 0)
            {
                EvictLruEntry();
            }

            // Add to strong cache
            var cached = new CachedSchema
            {
                CompiledSchema = compiled,
                CachedTime = DateTime.UtcNow,
                SchemaGuid = Guid.TryParse(schema.Guid, out var parsedGuid) ? parsedGuid : Guid.NewGuid(),
                SchemaVersion = schema.Version,
                EstimatedSizeBytes = EstimateSchemaSize(compiled)
            };

            _cache[cacheKey] = cached;
            
            // Add to LRU tracking
            var node = _lruList.AddLast(cacheKey);
            _lruNodes[cacheKey] = node;
            
            // Add to weak cache for memory pressure fallback
            _weakCache.AddOrUpdate(schema, compiled);
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
                RsvCacheStatistics.RecordEviction("SchemaCache");
                
                // Log large schema evictions for debugging
                if (cached.EstimatedSizeBytes > 1024 * 1024) // > 1MB
                {
                    Debug.Log($"[RSV] Evicted large schema from cache: {cached.EstimatedSizeBytes / 1024}KB");
                }
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
                Debug.LogWarning($"[RSV] Critical memory pressure detected ({memoryUsed / 1024 / 1024}MB). Clearing 50% of schema cache.");
                EvictPercentage(0.5);
                _memoryPressureEvictions++;
                
                // Force garbage collection
                GC.Collect(2, GCCollectionMode.Optimized, false);
            }
            else if (memoryUsed > _memoryPressureThresholdBytes)
            {
                // Moderate memory pressure - clear 25% of cache
                Debug.Log($"[RSV] Memory pressure detected ({memoryUsed / 1024 / 1024}MB). Clearing 25% of schema cache.");
                EvictPercentage(0.25);
                _memoryPressureEvictions++;
            }
        }

        /// <summary>
        /// Evicts a percentage of the cache (oldest entries first).
        /// </summary>
        private static void EvictPercentage(double percentage)
        {
            lock (_lock)
            {
                int entriesToEvict = (int)(_cache.Count * percentage);
                for (int i = 0; i < entriesToEvict && _lruList.Count > 0; i++)
                {
                    EvictLruEntry();
                }
            }
        }

        /// <summary>
        /// Estimates the memory size of a compiled schema.
        /// </summary>
        private static long EstimateSchemaSize(CompiledSchema schema)
        {
            if (schema == null)
                return 0;

            // Rough estimation based on node count
            long size = 1024; // Base overhead
            
            if (schema.Nodes != null)
            {
                foreach (var node in schema.Nodes)
                {
                    size += EstimateNodeSize(node);
                }
            }

            return size;
        }

        /// <summary>
        /// Estimates the size of a compiled node.
        /// </summary>
        private static long EstimateNodeSize(RsvSchemaNode node)
        {
            if (node == null)
                return 0;

            long size = 256; // Base node overhead

            if (!string.IsNullOrEmpty(node.Name))
                size += node.Name.Length * 2;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    size += EstimateNodeSize(child);
                }
            }

            return size;
        }

        /// <summary>
        /// Invalidates a specific schema from the cache.
        /// </summary>
        /// <param name="schema">The schema to invalidate.</param>
        public static void Invalidate(DataSchemaDefinition schema)
        {
            if (schema == null)
                return;

            var cacheKey = GetCacheKey(schema);

            lock (_lock)
            {
                if (_cache.ContainsKey(cacheKey))
                {
                    _cache.Remove(cacheKey);
                    RsvCacheStatistics.RecordEviction("SchemaCache");
                }

                if (_lruNodes.TryGetValue(cacheKey, out var node))
                {
                    _lruList.Remove(node);
                    _lruNodes.Remove(cacheKey);
                }
            }
        }

        /// <summary>
        /// Invalidates all schemas from the cache.
        /// </summary>
        public static void InvalidateAll()
        {
            lock (_lock)
            {
                var count = _cache.Count;
                _cache.Clear();
                _lruList.Clear();
                _lruNodes.Clear();
                RsvCacheStatistics.RecordEviction("SchemaCache");
                Debug.Log($"[RSV] Schema cache cleared ({count} entries invalidated).");
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
                    RsvCacheStatistics.RecordEviction("SchemaCache");
                    Debug.Log($"[RSV] Removed {expiredKeys.Count} expired schema cache entries.");
                }

                return expiredKeys.Count;
            }
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        /// <returns>Statistics about the schema cache.</returns>
        public static SchemaCacheStats GetStats()
        {
            lock (_lock)
            {
                var stats = new SchemaCacheStats
                {
                    TotalEntries = _cache.Count,
                    ExpiredEntries = 0,
                    CacheDuration = _cacheDuration,
                    MaxCacheSize = _maxCacheSize,
                    TotalHits = _totalHits,
                    TotalMisses = _totalMisses,
                    TotalEvictions = _totalEvictions,
                    MemoryPressureEvictions = _memoryPressureEvictions,
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
        /// <param name="duration">The duration to cache schemas.</param>
        public static void SetCacheDuration(TimeSpan duration)
        {
            _cacheDuration = duration;
            Debug.Log($"[RSV] Schema cache duration set to {duration.TotalMinutes:F1} minutes.");
        }

        /// <summary>
        /// Sets the maximum cache size.
        /// </summary>
        /// <param name="maxSize">The maximum number of entries to cache.</param>
        public static void SetMaxCacheSize(int maxSize)
        {
            _maxCacheSize = maxSize;
            Debug.Log($"[RSV] Schema cache max size set to {maxSize} entries.");
        }

        /// <summary>
        /// Sets memory pressure thresholds.
        /// </summary>
        public static void SetMemoryThresholds(long pressureBytes, long criticalBytes)
        {
            _memoryPressureThresholdBytes = pressureBytes;
            _criticalMemoryThresholdBytes = criticalBytes;
            Debug.Log($"[RSV] Memory thresholds set: Pressure={pressureBytes/1024/1024}MB, Critical={criticalBytes/1024/1024}MB");
        }

        /// <summary>
        /// Generates a unique cache key for a schema.
        /// </summary>
        private static string GetCacheKey(DataSchemaDefinition schema)
        {
            // Use GUID, version, and content hash for cache key to handle schema updates
            var contentHash = ComputeContentHash(schema);
            return $"{schema.Guid}_{schema.Version}_{contentHash}";
        }

        /// <summary>
        /// Computes a hash of the schema content for cache invalidation.
        /// </summary>
        private static string ComputeContentHash(DataSchemaDefinition schema)
        {
            if (schema?.RootNodes == null)
                return "0";

            // Use stable SHA256 hashing instead of GetHashCode()
            using (var sha256 = SHA256.Create())
            {
                var sb = new StringBuilder();
                foreach (var node in schema.RootNodes)
                {
                    sb.Append(node?.Name ?? "");
                    sb.Append(node?.Constraint?.FieldType.ToString() ?? "");
                }
                
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Checks if a cached schema has expired.
        /// </summary>
        private static bool IsExpired(CachedSchema cached)
        {
            return DateTime.UtcNow - cached.CachedTime > _cacheDuration;
        }
    }

    /// <summary>
    /// Represents a cached compiled schema.
    /// </summary>
    internal class CachedSchema
    {
        public CompiledSchema CompiledSchema { get; set; }
        public DateTime CachedTime { get; set; }
        public Guid SchemaGuid { get; set; }
        public string SchemaVersion { get; set; }
        public long EstimatedSizeBytes { get; set; }
    }

    /// <summary>
    /// Statistics for the schema cache.
    /// </summary>
    public class SchemaCacheStats
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
        public long EstimatedMemoryBytes { get; set; }
        public double HitRate => TotalHits + TotalMisses > 0 ? (double)TotalHits / (TotalHits + TotalMisses) : 0;

        public override string ToString()
        {
            return $"Schema Cache: {ActiveEntries} active, {ExpiredEntries} expired, {TotalEntries}/{MaxCacheSize} total " +
                   $"(HitRate: {HitRate:P1}, Memory: {EstimatedMemoryBytes/1024/1024}MB, " +
                   $"PressureEvictions: {MemoryPressureEvictions})";
        }
    }
}
