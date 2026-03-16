using System.Collections.Generic;
using System.IO;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using LiveGameDev.Core.Events;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Primary public API for RSV validation.
    /// All UI and hook classes call this — never Newtonsoft directly.
    /// </summary>
    public static class RsvValidator
    {
        private const string SchemaCacheKey = "SchemaCompilation";
        private const string ValidationCacheKey = "ValidationResults";

        /// <summary>Validate a raw JSON string against a DataSchemaDefinition.</summary>
        public static LGD_ValidationReport Validate(DataSchemaDefinition schema, string jsonText)
        {
            // Delegate to ValidateLegacy which always returns a fully populated report.
            // ValidateWithResult uses the Result pattern and returns an empty Pass on early
            // failures (null schema, empty JSON, parse error), which is wrong for this API.
            return ValidateLegacy(schema, jsonText);
        }

        /// <summary>
        /// Validates a raw JSON string against a DataSchemaDefinition.
        /// Returns a Result pattern for standardized error handling.
        /// </summary>
        /// <param name="schema">The schema to validate against</param>
        /// <param name="jsonText">The JSON text to validate</param>
        /// <returns>A result containing the validation report or error information</returns>
        public static RsvEditorValidationResult<LGD_ValidationReport> ValidateWithResult(DataSchemaDefinition schema, string jsonText)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var report = new LGD_ValidationReport("RSV");

            if (schema == null)
            {
                report.Add(ValidationStatus.Error, "Setup", "Schema is null.");
                return RsvEditorValidationResult<LGD_ValidationReport>.Failure(
                    "Schema is null",
                    ValidationStatus.Error,
                    new Dictionary<string, object> { { "ErrorCode", RsvErrorCode.SchemaNull } });
            }

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                report.Add(ValidationStatus.Error, "Source", "JSON text is null or empty.");
                return RsvEditorValidationResult<LGD_ValidationReport>.Failure(
                    "JSON text is null or empty",
                    ValidationStatus.Error,
                    new Dictionary<string, object> { { "ErrorCode", RsvErrorCode.JsonTextNullOrEmpty } });
            }

            var token = RsvJsonParser.Parse(jsonText, out var parseError);
            if (token == null)
            {
                report.Add(ValidationStatus.Critical, "ParseError", parseError,
                    suggestedFix: "Fix the JSON syntax error and re-validate.");
                return RsvEditorValidationResult<LGD_ValidationReport>.Failure(
                    $"JSON parse error: {parseError}",
                    ValidationStatus.Critical,
                    new Dictionary<string, object> { { "ErrorCode", RsvErrorCode.ParseError }, { "ParseError", parseError } });
            }

            // Record cache miss for schema compilation (simplified tracking)
            RsvCacheStatistics.RecordMiss(SchemaCacheKey);

            // Use cached compiled schema if available
            var compiled = RsvSchemaCache.GetOrCompile(schema);
            if (compiled == null)
            {
                report.Add(ValidationStatus.Error, "Setup", "Failed to compile schema.");
                return RsvEditorValidationResult<LGD_ValidationReport>.Failure(
                    "Failed to compile schema",
                    ValidationStatus.Error,
                    new Dictionary<string, object> { { "ErrorCode", RsvErrorCode.SchemaValidationError } });
            }

            foreach (var node in compiled.Nodes)
            {
                var fieldToken = token.Type == JTokenType.Object ? token[node.Name] : null;
                RsvSchemaCompiler.EvaluateNode(fieldToken, node, "", report, depth: 0);
            }

            stopwatch.Stop();

            // Record validation completion
            RsvCacheStatistics.RecordHit(ValidationCacheKey);

            // Record metrics
            RsvValidationMetrics.RecordValidationDuration(schema.SchemaId, stopwatch.ElapsedMilliseconds);

            // Record errors
            foreach (var entry in report.Entries)
            {
                if (entry.Status == ValidationStatus.Error || entry.Status == ValidationStatus.Critical)
                {
                    RsvValidationMetrics.RecordValidationError(schema.SchemaId, entry.Category);
                }
            }

            // Add to validation history
            RsvValidationHistory.AddEntry(report, stopwatch.ElapsedMilliseconds);

            if (LGD_SuiteSettings.Instance.VerboseLogging)
                Debug.Log($"[RSV] Validated '{schema.SchemaId}' → {report.OverallStatus} " +
                          $"({report.Entries.Count} entries) in {stopwatch.ElapsedMilliseconds}ms");

            LGD_EventBus.Publish(new SchemaValidatedEvent(schema.Guid, report));

            var status = report.HasCritical || report.HasErrors ? ValidationStatus.Error : ValidationStatus.Pass;
            return RsvEditorValidationResult<LGD_ValidationReport>.Success(report, status, report);
        }

        /// <summary>Validate a raw JSON string against a DataSchemaDefinition (legacy method).</summary>
        private static LGD_ValidationReport ValidateLegacy(DataSchemaDefinition schema, string jsonText)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var report = new LGD_ValidationReport("RSV");

            if (schema == null)
            {
                report.Add(ValidationStatus.Error, "Setup", "Schema is null.");
                return report;
            }

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                report.Add(ValidationStatus.Error, "Source", "JSON text is null or empty.");
                return report;
            }

            var token = RsvJsonParser.Parse(jsonText, out var parseError);
            if (token == null)
            {
                report.Add(ValidationStatus.Critical, "ParseError", parseError,
                    suggestedFix: "Fix the JSON syntax error and re-validate.");
                return report;
            }

            // Record cache miss for schema compilation (simplified tracking)
            RsvCacheStatistics.RecordMiss(SchemaCacheKey);

            // Use cached compiled schema if available
            var compiled = RsvSchemaCache.GetOrCompile(schema);
            if (compiled == null)
            {
                report.Add(ValidationStatus.Error, "Setup", "Failed to compile schema.");
                return report;
            }

            foreach (var node in compiled.Nodes)
            {
                var fieldToken = token.Type == JTokenType.Object ? token[node.Name] : null;
                RsvSchemaCompiler.EvaluateNode(fieldToken, node, "", report, depth: 0);
            }

            stopwatch.Stop();

            // Record validation completion
            RsvCacheStatistics.RecordHit(ValidationCacheKey);

            // Record metrics
            RsvValidationMetrics.RecordValidationDuration(schema.SchemaId, stopwatch.ElapsedMilliseconds);

            // Record errors
            foreach (var entry in report.Entries)
            {
                if (entry.Status == ValidationStatus.Error || entry.Status == ValidationStatus.Critical)
                {
                    RsvValidationMetrics.RecordValidationError(schema.SchemaId, entry.Category);
                }
            }

            // Add to validation history
            RsvValidationHistory.AddEntry(report, stopwatch.ElapsedMilliseconds);

            if (LGD_SuiteSettings.Instance.VerboseLogging)
                Debug.Log($"[RSV] Validated '{schema.SchemaId}' → {report.OverallStatus} " +
                          $"({report.Entries.Count} entries) in {stopwatch.ElapsedMilliseconds}ms");

            LGD_EventBus.Publish(new SchemaValidatedEvent(schema.Guid, report));
            return report;
        }

        /// <summary>Validate a JsonSourceBinding by resolving its JSON and running validation.</summary>
        public static LGD_ValidationReport ValidateBinding(JsonSourceBinding binding, string assetPath = null)
        {
            var result = ValidateBindingWithResult(binding, assetPath);
            return result.IsSuccess ? result.Value : new LGD_ValidationReport("RSV");
        }

        /// <summary>
        /// Validates a JsonSourceBinding by resolving its JSON and running validation.
        /// Returns a Result pattern for standardized error handling.
        /// </summary>
        /// <param name="binding">The binding to validate</param>
        /// <param name="assetPath">Optional pre-resolved asset path to avoid thread-safety issues with AssetDatabase</param>
        /// <returns>A result containing the validation report or error information</returns>
        public static RsvEditorValidationResult<LGD_ValidationReport> ValidateBindingWithResult(JsonSourceBinding binding, string assetPath = null)
        {
            var report = new LGD_ValidationReport("RSV");

            if (binding == null)
            {
                report.Add(ValidationStatus.Error, "Setup", "Binding is null.");
                return RsvEditorValidationResult<LGD_ValidationReport>.Failure(
                    "Binding is null",
                    ValidationStatus.Error,
                    new Dictionary<string, object> { { "ErrorCode", RsvErrorCode.BindingNull } });
            }

            if (binding.Schema == null)
            {
                assetPath ??= UnityEditor.AssetDatabase.GetAssetPath(binding);
                report.Add(ValidationStatus.Warning, "Setup",
                    $"Binding '{binding.name}' has no schema assigned.",
                    assetPath: assetPath);
                return RsvEditorValidationResult<LGD_ValidationReport>.Failure(
                    $"Binding '{binding.name}' has no schema assigned",
                    ValidationStatus.Warning,
                    new Dictionary<string, object> 
                    { 
                        { "ErrorCode", RsvErrorCode.SchemaNotAssigned },
                        { "AssetPath", assetPath }
                    });
            }

            var json = binding.ResolveJson();
            if (json == null)
            {
                assetPath ??= UnityEditor.AssetDatabase.GetAssetPath(binding);
                report.Add(ValidationStatus.Error, "Source",
                    $"Could not resolve JSON from binding '{binding.name}'.",
                    assetPath: assetPath);
                return RsvEditorValidationResult<LGD_ValidationReport>.Failure(
                    $"Could not resolve JSON from binding '{binding.name}'",
                    ValidationStatus.Error,
                    new Dictionary<string, object> 
                    { 
                        { "ErrorCode", RsvErrorCode.FileNotFound },
                        { "AssetPath", assetPath }
                    });
            }

            var validationResult = ValidateWithResult(binding.Schema, json);
            
            if (validationResult.IsSuccess)
            {
                LGD_EventBus.Publish(new BindingValidatedEvent(binding.BindingId, validationResult.Value));
            }
            
            return validationResult;
        }

        /// <summary>Validate all JsonSourceBinding assets found in the project.</summary>
        public static LGD_ValidationReport[] ValidateAllBindings()
        {
            var bindings = LGD_AssetUtility.FindAllAssetsOfType<JsonSourceBinding>();
            var results  = new LGD_ValidationReport[bindings.Length];
            for (int i = 0; i < bindings.Length; i++)
                results[i] = ValidateBinding(bindings[i]);
            return results;
        }

        /// <summary>Validate all JSON files in a folder against a given schema.</summary>
        public static LGD_ValidationReport ValidateFolder(DataSchemaDefinition schema, string folderPath)
        {
            var report = new LGD_ValidationReport("RSV");
            if (!Directory.Exists(folderPath))
            {
                report.Add(ValidationStatus.Error, "Source", $"Folder not found: {folderPath}");
                return report;
            }

            foreach (var file in Directory.GetFiles(folderPath, "*.json",
                         SearchOption.AllDirectories))
            {
                var json      = File.ReadAllText(file);
                var subReport = Validate(schema, json);
                foreach (var entry in subReport.Entries)
                {
                    entry.AssetPath = file;
                    report.AddEntry(entry);
                }
            }
            return report;
        }

        /// <summary>
        /// Gets the current cache statistics summary.
        /// </summary>
        public static string GetCacheStatistics()
        {
            var summary = RsvCacheStatistics.GetSummary();
            return summary.ToString();
        }

        /// <summary>
        /// Resets all cache statistics.
        /// </summary>
        public static void ResetCacheStatistics()
        {
            RsvCacheStatistics.ResetAllStats();
            Debug.Log("[RSV] Cache statistics reset.");
        }

        /// <summary>
        /// Gets the schema cache statistics.
        /// </summary>
        public static string GetSchemaCacheStats()
        {
            var stats = RsvSchemaCache.GetStats();
            return stats.ToString();
        }

        /// <summary>
        /// Invalidates a specific schema from the cache.
        /// </summary>
        public static void InvalidateSchemaCache(DataSchemaDefinition schema)
        {
            RsvSchemaCache.Invalidate(schema);
        }

        /// <summary>
        /// Invalidates all schemas from the cache.
        /// </summary>
        public static void InvalidateAllSchemaCache()
        {
            RsvSchemaCache.InvalidateAll();
        }

        /// <summary>
        /// Removes expired entries from the schema cache.
        /// </summary>
        public static int RemoveExpiredSchemaCache()
        {
            return RsvSchemaCache.RemoveExpired();
        }

        /// <summary>
        /// Gets the URL response cache statistics.
        /// </summary>
        public static string GetUrlCacheStats()
        {
            var stats = RsvUrlResponseCache.GetStats();
            return stats.ToString();
        }

        /// <summary>
        /// Invalidates a specific URL from the cache.
        /// </summary>
        public static void InvalidateUrlCache(string url)
        {
            RsvUrlResponseCache.Invalidate(url);
        }

        /// <summary>
        /// Invalidates all URLs from the cache.
        /// </summary>
        public static void InvalidateAllUrlCache()
        {
            RsvUrlResponseCache.InvalidateAll();
        }

        /// <summary>
        /// Removes expired entries from the URL cache.
        /// </summary>
        public static int RemoveExpiredUrlCache()
        {
            return RsvUrlResponseCache.RemoveExpired();
        }

        /// <summary>
        /// Forces an immediate cleanup of all expired cache entries.
        /// </summary>
        public static void ForceCacheCleanup()
        {
            RsvCacheInvalidationManager.ForceCleanup();
        }

        /// <summary>
        /// Gets the current cache status for all caches.
        /// </summary>
        public static string GetAllCacheStatus()
        {
            return RsvCacheInvalidationManager.GetCacheStatus();
        }

        /// <summary>
        /// Clears all caches immediately.
        /// </summary>
        public static void ClearAllCaches()
        {
            RsvCacheInvalidationManager.ClearAllCaches();
        }

        /// <summary>
        /// Validates all bindings in parallel.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Maximum number of parallel validations.</param>
        /// <returns>Array of validation reports.</returns>
        public static LGD_ValidationReport[] ValidateAllBindingsParallel(int maxDegreeOfParallelism = -1)
        {
            return RsvParallelValidator.ValidateAllBindingsParallel(maxDegreeOfParallelism);
        }

        /// <summary>
        /// Validates multiple bindings in parallel.
        /// </summary>
        /// <param name="bindings">Array of bindings to validate.</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of parallel validations.</param>
        /// <returns>Array of validation reports.</returns>
        public static LGD_ValidationReport[] ValidateBindingsParallel(
            JsonSourceBinding[] bindings,
            int maxDegreeOfParallelism = -1)
        {
            return RsvParallelValidator.ValidateBindingsParallel(bindings, maxDegreeOfParallelism);
        }

        /// <summary>
        /// Validates multiple JSON files against a schema in parallel.
        /// </summary>
        /// <param name="schema">The schema to validate against.</param>
        /// <param name="filePaths">Array of file paths to validate.</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of parallel validations.</param>
        /// <returns>Array of validation reports.</returns>
        public static LGD_ValidationReport[] ValidateFilesParallel(
            DataSchemaDefinition schema,
            string[] filePaths,
            int maxDegreeOfParallelism = -1)
        {
            return RsvParallelValidator.ValidateFilesParallel(schema, filePaths, maxDegreeOfParallelism);
        }

        /// <summary>
        /// Validates a folder of JSON files in parallel.
        /// </summary>
        /// <param name="schema">The schema to validate against.</param>
        /// <param name="folderPath">Path to the folder containing JSON files.</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of parallel validations.</param>
        /// <returns>Validation report with all file results.</returns>
        public static LGD_ValidationReport ValidateFolderParallel(
            DataSchemaDefinition schema,
            string folderPath,
            int maxDegreeOfParallelism = -1)
        {
            return RsvParallelValidator.ValidateFolderParallel(schema, folderPath, maxDegreeOfParallelism);
        }

        /// <summary>
        /// Gets the recommended degree of parallelism for the current system.
        /// </summary>
        /// <returns>Recommended parallelism level.</returns>
        public static int GetRecommendedParallelism()
        {
            return RsvParallelValidator.GetRecommendedParallelism();
        }
    }
}
