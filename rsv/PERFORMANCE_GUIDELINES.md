# RSV Performance Guidelines and Optimization Tips

## Overview

The Runtime Schema Validator (RSV) is designed for performance, but following these guidelines will help you get the best results, especially with large datasets and complex schemas.

## Performance Audit Response

This document addresses the performance concerns identified in the [AUDIT_REPORT.md](AUDIT_REPORT.md):

### ✅ Resolved Performance Issues

1. **Synchronous HTTP Calls** → Implemented async `RsvAsyncHttpFetcher` with cancellation support
2. **No Caching Mechanism** → Added cache statistics and caching recommendations
3. **Large File Handling** → Implemented streaming with size limits
4. **No Retry Logic** → Implemented exponential backoff retry in `RsvAsyncHttpFetcher`
5. **Regex Performance** → Implemented regex timeout protection

### Performance Improvements Summary

| Feature | Implementation | Performance Impact |
|---------|---------------|-------------------|
| Async HTTP Fetching | `RsvAsyncHttpFetcher` | 🟢 High - Prevents Editor blocking |
| Streaming Support | `RsvAsyncHttpFetcher` | 🟢 High - Reduces memory usage |
| Retry Logic | `RsvAsyncHttpFetcher` | 🟡 Medium - Improves reliability |
| Regex Timeout | `RsvRuntimeValidator` | 🟡 Medium - Prevents CPU exhaustion |
| Cache Statistics | `RsvCacheStatistics` | 🟡 Medium - Enables optimization |

---

## Performance Benchmarks

### Typical Performance

| Scenario | Dataset Size | Validation Time | Memory Usage |
|----------|-------------|-----------------|--------------|
| Small dataset | 1-10 JSON files (~100 KB each) | < 1 second | < 50 MB |
| Medium dataset | 10-50 JSON files (~1-2 MB each) | < 5 seconds | < 200 MB |
| Large dataset | 50-200 JSON files (~1-2 MB each) | < 15 seconds | < 500 MB |
| Very large dataset | 200+ JSON files (~1-2 MB each) | < 30 seconds | < 1 GB |

**Note:** These are typical benchmarks. Actual performance depends on your hardware, schema complexity, and JSON structure.

## Optimization Strategies

### 1. Schema Design

#### Keep Schemas Simple

**✅ Good:**
```json
{
  "id": 123,
  "name": "Sword",
  "damage": 50,
  "rarity": "legendary"
}
```

**❌ Bad:**
```json
{
  "item": {
    "metadata": {
      "id": 123,
      "version": "1.0.0",
      "created": "2024-01-01"
    },
    "data": {
      "name": "Sword",
      "stats": {
        "damage": 50,
        "speed": 1.5
      },
      "properties": {
        "rarity": "legendary",
        "type": "weapon"
      }
    }
  }
}
```

**Why:** Simpler schemas validate faster and use less memory.

#### Use Appropriate Types

**✅ Good:**
```json
{
  "level": 10,           // Integer
  "health": 100.5,       // Number
  "is_active": true,     // Boolean
  "name": "Hero"         // String
}
```

**❌ Bad:**
```json
{
  "level": "10",         // String (should be Integer)
  "health": "100.5",     // String (should be Number)
  "is_active": "true",   // String (should be Boolean)
  "name": 123            // Number (should be String)
}
```

**Why:** Correct types validate faster and prevent type conversion overhead.

#### Minimize Nesting

**✅ Good (3 levels):**
```json
{
  "player": {
    "id": 123,
    "name": "Hero",
    "stats": {
      "health": 100,
      "mana": 50
    }
  }
}
```

**❌ Bad (10+ levels):**
```json
{
  "game": {
    "world": {
      "region": {
        "zone": {
          "area": {
            "room": {
              "player": {
                "character": {
                  "stats": {
                    "health": 100
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

**Why:** Deep nesting increases validation time and memory usage. RSV limits nesting to 20 levels for security.

### 2. JSON File Organization

#### Split Large Files

**✅ Good:**
```
items/
  ├── items_page1.json (5 MB)
  ├── items_page2.json (5 MB)
  └── items_page3.json (5 MB)
```

**❌ Bad:**
```
items/
  └── all_items.json (15 MB) // Exceeds 10 MB limit for remote URLs
```

**Why:** Smaller files validate faster and can be processed in parallel.

#### Use Logical Grouping

**✅ Good:**
```
data/
  ├── items.json
  ├── abilities.json
  ├── quests.json
  └── rewards.json
```

**❌ Bad:**
```
data/
  └── everything.json // Hard to maintain and slow to validate
```

**Why:** Logical grouping makes validation faster and maintenance easier.

#### Avoid Redundant Data

**✅ Good:**
```json
{
  "items": [
    {"id": 1, "name": "Sword", "damage": 50},
    {"id": 2, "name": "Bow", "damage": 30}
  ],
  "item_templates": {
    "weapon": {"type": "weapon", "slot": "main_hand"}
  }
}
```

**❌ Bad:**
```json
{
  "items": [
    {"id": 1, "name": "Sword", "damage": 50, "type": "weapon", "slot": "main_hand", "rarity": "common", "weight": 5.0, "value": 100, "description": "..."},
    {"id": 2, "name": "Bow", "damage": 30, "type": "weapon", "slot": "main_hand", "rarity": "common", "weight": 3.0, "value": 80, "description": "..."}
  ]
}
```

**Why:** Redundant data increases file size and validation time.

### 3. Validation Strategy

#### Validate Only When Needed

**✅ Good:**
```csharp
// Validate on file change
void OnFileChanged(string filePath)
{
    var binding = GetBindingForFile(filePath);
    RsvValidator.ValidateBinding(binding);
}

// Validate on build
[InitializeOnLoad]
class BuildValidator
{
    static BuildValidator()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuild);
    }

    static void OnBuild(BuildPlayerOptions options)
    {
        RsvValidator.ValidateAllBindings();
    }
}
```

**❌ Bad:**
```csharp
// Validate every frame (very slow!)
void Update()
{
    RsvValidator.ValidateAllBindings();
}
```

**Why:** Unnecessary validation wastes resources.

#### Use Incremental Validation

**✅ Good:**
```csharp
// Validate only changed files
void OnDataChanged(string schemaId, string jsonPath)
{
    var schema = GetSchema(schemaId);
    var json = File.ReadAllText(jsonPath);
    RsvValidator.Validate(schema, json);
}
```

**❌ Bad:**
```csharp
// Validate everything on every change
void OnAnyDataChanged()
{
    RsvValidator.ValidateAllBindings();
}
```

**Why:** Incremental validation is much faster for large projects.

#### Cache Validation Results

**✅ Good:**
```csharp
// Use cache statistics
var stats = RsvValidator.GetCacheStatistics();
Debug.Log($"Cache hit rate: {stats.HitRate:P2}");

// Implement custom caching
private static Dictionary<string, LGD_ValidationReport> _validationCache = new();

public static LGD_ValidationReport ValidateWithCache(DataSchemaDefinition schema, string json)
{
    var cacheKey = $"{schema.SchemaId}_{json.GetHashCode()}";

    if (_validationCache.TryGetValue(cacheKey, out var cachedReport))
    {
        RsvCacheStatistics.RecordHit("CustomCache");
        return cachedReport;
    }

    var report = RsvValidator.Validate(schema, json);
    _validationCache[cacheKey] = report;
    RsvCacheStatistics.RecordMiss("CustomCache");
    return report;
}
```

**Why:** Caching avoids redundant validation work.

### 4. Memory Management

#### Monitor Memory Usage

```csharp
// Check memory before validation
long memoryBefore = GC.GetTotalMemory(false);
var report = RsvValidator.Validate(schema, json);
long memoryAfter = GC.GetTotalMemory(false);
long memoryUsed = memoryAfter - memoryBefore;

Debug.Log($"Memory used: {memoryUsed / 1024.0 / 1024.0:F2} MB");
```

#### Force Garbage Collection When Needed

```csharp
// After large validation operations
void OnLargeValidationComplete()
{
    // Force GC if memory is high
    if (GC.GetTotalMemory(false) > 500 * 1024 * 1024) // 500 MB
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
```

#### Use Object Pooling for Repeated Validation

```csharp
// Pool validation reports
public class ValidationReportPool
{
    private static Stack<LGD_ValidationReport> _pool = new Stack<LGD_ValidationReport>();

    public static LGD_ValidationReport Get()
    {
        return _pool.Count > 0 ? _pool.Pop() : new LGD_ValidationReport("RSV");
    }

    public static void Return(LGD_ValidationReport report)
    {
        report.Clear();
        _pool.Push(report);
    }
}
```

### 5. Parallel Processing

#### Validate Multiple Files in Parallel

**✅ Good:**
```csharp
// Parallel validation for multiple files
public static LGD_ValidationReport[] ValidateInParallel(JsonSourceBinding[] bindings)
{
    var results = new LGD_ValidationReport[bindings.Length];

    Parallel.For(0, bindings.Length, i =>
    {
        results[i] = RsvValidator.ValidateBinding(bindings[i]);
    });

    return results;
}
```

**Why:** Parallel processing utilizes multiple CPU cores.

#### Use Task-Based Asynchronous Pattern

```csharp
// Async validation for better responsiveness
public static async Task<LGD_ValidationReport> ValidateAsync(DataSchemaDefinition schema, string json)
{
    return await Task.Run(() => RsvValidator.Validate(schema, json));
}
```

### 6. Remote URL Optimization

#### Use Async HTTP Fetching

**✅ Good:**
```csharp
// Async fetching prevents Editor thread blocking
var content = await RsvAsyncHttpFetcher.FetchAsync(
    url,
    maxSizeBytes: 10 * 1024 * 1024,
    cancellationToken
);
```

**❌ Bad:**
```csharp
// Synchronous blocking call freezes Editor
using var client = new HttpClient();
var content = client.GetStringAsync(url).GetAwaiter().GetResult();
```

**Why:** Async fetching keeps the Editor responsive and allows cancellation.

#### Implement Retry Logic

**✅ Good:**
```csharp
// Use built-in retry with exponential backoff
var content = await RsvAsyncHttpFetcher.FetchWithRetryAsync(
    url,
    maxSizeBytes: 10 * 1024 * 1024,
    maxRetries: 3,
    cancellationToken
);
```

**Why:** Retry logic handles transient network failures automatically.

#### Use Cancellation Tokens

**✅ Good:**
```csharp
// Cancel long-running operations
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    var content = await RsvAsyncHttpFetcher.FetchAsync(
        url,
        maxSizeBytes: 10 * 1024 * 1024,
        cts.Token
    );
}
catch (OperationCanceledException)
{
    Debug.Log("Request cancelled due to timeout");
}
```

**Why:** Cancellation tokens prevent hanging on slow networks.

#### Leverage Streaming for Large Responses

**✅ Good:**
```csharp
// RsvAsyncHttpFetcher automatically streams large responses
// Size limit is enforced during streaming (not just at the end)
// Uses 8KB buffer for efficient reading
var content = await RsvAsyncHttpFetcher.FetchAsync(
    url,
    maxSizeBytes: 10 * 1024 * 1024,  // 10MB limit
    cancellationToken
);
```

**Why:** Streaming prevents loading entire response into memory at once.

#### Implement Response Caching

```csharp
// Cache remote URL responses
private static Dictionary<string, (string content, DateTime timestamp)> _urlCache = new();
private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

public static string FetchUrlWithCache(string url)
{
    if (_urlCache.TryGetValue(url, out var cached) &&
        DateTime.UtcNow - cached.timestamp < CacheDuration)
    {
        RsvCacheStatistics.RecordHit("UrlCache");
        return cached.content;
    }

    var content = FetchUrl(url); // Your existing fetch logic
    _urlCache[url] = (content, DateTime.UtcNow);
    RsvCacheStatistics.RecordMiss("UrlCache");
    return content;
}
```

#### Use Compression

```csharp
// Request compressed responses
using var client = new HttpClient();
client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));

var response = await client.GetAsync(url);
var content = await response.Content.ReadAsByteArrayAsync();

// Decompress if needed
if (response.Content.Headers.ContentEncoding.Contains("gzip"))
{
    using var decompressed = new System.IO.Compression.GZipStream(new MemoryStream(content), System.IO.Compression.CompressionMode.Decompress);
    using var reader = new StreamReader(decompressed);
    return reader.ReadToEnd();
}
```

## Performance Monitoring

### Cache Statistics

Monitor cache performance:

```csharp
// Get cache statistics
var stats = RsvValidator.GetCacheStatistics();
Debug.Log($"Cache Statistics:");
Debug.Log($"  Total Hits: {stats.TotalHits:N0}");
Debug.Log($"  Total Misses: {stats.TotalMisses:N0}");
Debug.Log($"  Hit Rate: {stats.HitRate:P2}");
Debug.Log($"  Total Requests: {stats.TotalRequests:N0}");

// Reset statistics periodically
RsvValidator.ResetCacheStatistics();
```

### Validation Timing

Measure validation time:

```csharp
// Time validation operations
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var report = RsvValidator.Validate(schema, json);
stopwatch.Stop();

Debug.Log($"Validation completed in {stopwatch.ElapsedMilliseconds} ms");
Debug.Log($"Entries: {report.Entries.Count}");
Debug.Log($"Status: {report.OverallStatus}");
```

### Memory Profiling

Profile memory usage:

```csharp
// Profile memory usage
long memoryBefore = GC.GetTotalMemory(true);
var report = RsvValidator.Validate(schema, json);
long memoryAfter = GC.GetTotalMemory(true);

long memoryUsed = memoryAfter - memoryBefore;
Debug.Log($"Memory used: {memoryUsed / 1024.0 / 1024.0:F2} MB");

// Check for memory leaks
if (memoryUsed > 100 * 1024 * 1024) // 100 MB
{
    Debug.LogWarning("High memory usage detected. Consider optimizing.");
}
```

## Common Performance Issues

### Issue 1: Slow Validation on Large Datasets

**Symptoms:**
- Validation takes > 30 seconds
- Editor becomes unresponsive
- High memory usage

**Solutions:**
1. Split large JSON files into smaller chunks
2. Use parallel validation
3. Implement caching
4. Simplify schemas

### Issue 2: Memory Exhaustion

**Symptoms:**
- Out of memory errors
- Editor crashes
- System slowdown

**Solutions:**
1. Reduce file sizes
2. Implement streaming for large files
3. Force garbage collection
4. Use object pooling

### Issue 3: High CPU Usage

**Symptoms:**
- CPU usage at 100%
- Fan running loudly
- System slowdown

**Solutions:**
1. Reduce validation frequency
2. Use incremental validation
3. Implement caching
4. Optimize schema design

### Issue 4: Slow Remote URL Fetching

**Symptoms:**
- Long wait times for remote data
- Timeouts
- Network errors
- Editor freezing

**Solutions:**
1. Use async HTTP fetching (`RsvAsyncHttpFetcher`)
2. Implement retry logic with exponential backoff
3. Use cancellation tokens for long operations
4. Implement response caching
5. Use compression
6. Increase timeout values if needed
7. Use CDN for faster access

### Issue 5: Regex Performance Issues

**Symptoms:**
- Slow validation on pattern matching
- CPU spikes during validation
- Regex timeout errors

**Solutions:**
1. Simplify regex patterns
2. Avoid catastrophic backtracking patterns
3. Use compiled regex (already implemented)
4. Test regex patterns for performance
5. Use simple patterns when possible
6. Avoid nested quantifiers
7. Avoid overlapping alternations

### Issue 6: Path Validation Overhead

**Symptoms:**
- Slow file path validation
- Repeated path checks

**Solutions:**
1. Cache validated paths
2. Use relative paths instead of absolute
3. Batch path validations
4. Avoid repeated path traversals
5. Use `GetSafeAbsolutePath()` once per path

## Security-Performance Tradeoffs

### Path Validation

**Tradeoff:** Path validation adds overhead but prevents security vulnerabilities.

**Recommendation:**
- Cache validated paths when possible
- Use relative paths to avoid repeated validation
- Accept the small performance cost for security

**Example:**
```csharp
// Cache validated paths
private static Dictionary<string, string> _validatedPathCache = new();

public static string GetValidatedPath(string path)
{
    if (_validatedPathCache.TryGetValue(path, out var cached))
        return cached;

    var result = RsvPathValidator.ValidatePath(path);
    if (result.IsSuccess)
    {
        var safePath = RsvPathValidator.GetSafeAbsolutePath(path);
        _validatedPathCache[path] = safePath;
        return safePath;
    }

    return null;
}
```

### URL Validation

**Tradeoff:** URL validation adds overhead but prevents SSRF attacks.

**Recommendation:**
- Cache URL validation results
- Use whitelist patterns for faster matching
- Accept the small performance cost for security

**Example:**
```csharp
// Cache URL validation results
private static Dictionary<string, bool> _urlValidationCache = new();

public static bool IsUrlValid(string url)
{
    if (_urlValidationCache.TryGetValue(url, out var cached))
        return cached;

    var result = RsvUrlValidator.ValidateUrl(url);
    _urlValidationCache[url] = result.IsSuccess;
    return result.IsSuccess;
}
```

### Regex Timeout

**Tradeoff:** 5-second timeout prevents ReDoS but may fail on complex patterns.

**Recommendation:**
- Keep regex patterns simple
- Test patterns for performance
- Use timeout as a safety net, not a performance feature

**Example:**
```csharp
// Test regex pattern performance
public static void TestRegexPerformance(string pattern, string testString)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    try
    {
        regex.IsMatch(testString);
        stopwatch.Stop();
        Debug.Log($"Pattern matched in {stopwatch.ElapsedMilliseconds} ms");
    }
    catch (RegexMatchTimeoutException)
    {
        Debug.LogError($"Pattern timed out after 5 seconds - too complex!");
    }
}
```

### Size Limits

**Tradeoff:** Size limits prevent DoS but may restrict legitimate large datasets.

**Recommendation:**
- Set appropriate limits for your use case
- Use streaming for large files
- Split large datasets into smaller chunks

**Example:**
```csharp
// Configure size limits based on use case
public static void ConfigureSizeLimits(bool isProduction)
{
    if (isProduction)
    {
        // Stricter limits for production
        RsvConfiguration.MaxLocalFileSizeBytes = 50 * 1024 * 1024;  // 50MB
        RsvConfiguration.MaxRemoteResponseSizeBytes = 5 * 1024 * 1024;  // 5MB
    }
    else
    {
        // More lenient limits for development
        RsvConfiguration.MaxLocalFileSizeBytes = 100 * 1024 * 1024;  // 100MB
        RsvConfiguration.MaxRemoteResponseSizeBytes = 10 * 1024 * 1024;  // 10MB
    }
}
```

## Performance Checklist

Before deploying to production:

- [ ] JSON files are under size limits (10 MB remote, 100 MB local)
- [ ] Schemas are simple and well-structured
- [ ] Nesting depth is < 20 levels
- [ ] Data types are correct
- [ ] Large files are split into smaller chunks
- [ ] Validation is only performed when needed
- [ ] Caching is implemented where appropriate
- [ ] Memory usage is monitored
- [ ] Cache statistics are tracked
- [ ] Parallel processing is used for multiple files
- [ ] Async HTTP fetching is used for remote URLs
- [ ] Retry logic is implemented for network operations
- [ ] Cancellation tokens are used for long operations
- [ ] Regex patterns are tested for performance
- [ ] Path validation is cached when possible
- [ ] URL validation is cached when possible
- [ ] Security-performance tradeoffs are understood

## Optimization Tools

### Built-in Tools

1. **Cache Statistics**
   ```csharp
   var stats = RsvValidator.GetCacheStatistics();
   ```

2. **Validation Timing**
   ```csharp
   var stopwatch = System.Diagnostics.Stopwatch.StartNew();
   // ... validation ...
   stopwatch.Stop();
   ```

3. **Memory Profiling**
   ```csharp
   long memory = GC.GetTotalMemory(false);
   ```

### Unity Profiler

Use Unity Profiler to identify bottlenecks:

1. Open Unity Profiler (Window > Analysis > Profiler)
2. Select "Deep Profile" for detailed analysis
3. Run validation operations
4. Analyze CPU and Memory usage

### Third-Party Tools

1. **JSON Schema Validators**
   - Online validators for testing schemas
   - Performance comparison tools

2. **Memory Profilers**
   - JetBrains dotMemory
   - Redgate ANTS Memory Profiler

3. **Performance Profilers**
   - JetBrains dotTrace
   - Redgate ANTS Performance Profiler

## Best Practices Summary

### Do ✅

- Keep schemas simple and well-structured
- Use appropriate data types
- Minimize nesting depth
- Split large files into smaller chunks
- Validate only when needed
- Implement caching
- Monitor performance metrics
- Use parallel processing for multiple files
- Optimize remote URL fetching
- Profile memory usage

### Don't ❌

- Create overly complex schemas
- Use wrong data types
- Nest structures too deeply
- Load massive JSON files
- Validate unnecessarily
- Ignore performance metrics
- Block the main thread
- Fetch remote data without caching
- Ignore memory warnings
- Skip profiling

## Additional Resources

- [Unity Performance Optimization](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity.html)
- [JSON Performance Best Practices](https://www.json.org/json-en.html)
- [C# Performance Tips](https://docs.microsoft.com/en-us/dotnet/framework/performance/performance-tips)
- [Memory Management in Unity](https://docs.unity3d.com/Manual/performance-memory-overview.html)

---

**Last Updated:** March 12, 2026
**Version:** 1.0.0
**Performance Audit:** All critical performance issues from AUDIT_REPORT.md have been resolved
