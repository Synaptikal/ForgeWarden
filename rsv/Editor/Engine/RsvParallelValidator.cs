using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Parallel validation support for batch operations.
    /// Utilizes multiple CPU cores for faster validation of multiple bindings.
    /// </summary>
    public static class RsvParallelValidator
    {
        /// <summary>
        /// Validates all bindings in parallel.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Maximum number of parallel validations (default: Environment.ProcessorCount).</param>
        /// <returns>Array of validation reports.</returns>
        public static LGD_ValidationReport[] ValidateAllBindingsParallel(int maxDegreeOfParallelism = -1)
        {
            var bindings = LGD_AssetUtility.FindAllAssetsOfType<JsonSourceBinding>();

            if (bindings.Length == 0)
            {
                Debug.Log("[RSV] No bindings found to validate.");
                return Array.Empty<LGD_ValidationReport>();
            }

            if (maxDegreeOfParallelism <= 0)
            {
                maxDegreeOfParallelism = Environment.ProcessorCount;
            }

            Debug.Log($"[RSV] Validating {bindings.Length} bindings in parallel (max {maxDegreeOfParallelism} threads)...");

            // Start progress tracking
            var progress = new RsvValidationProgress("Parallel Binding Validation", bindings.Length);

            var results = new LGD_ValidationReport[bindings.Length];
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Pre-cache paths on main thread to avoid thread-safety violations in Parallel.For
            var cachedPaths = new string[bindings.Length];
            for (int i = 0; i < bindings.Length; i++)
            {
                cachedPaths[i] = UnityEditor.AssetDatabase.GetAssetPath(bindings[i]);
            }

            Parallel.For(0, bindings.Length, options, i =>
            {
                results[i] = RsvValidator.ValidateBinding(bindings[i], cachedPaths[i]);

                // Update progress
                lock (progress)
                {
                    progress.IncrementProgress(bindings[i].name);
                }
            });

            stopwatch.Stop();

            // Complete progress tracking
            progress.Complete();

            var passed = results.Count(r => r.OverallStatus == ValidationStatus.Pass);
            var failed = results.Count(r => r.HasErrors || r.HasCritical);
            var warnings = results.Count(r => r.OverallStatus == ValidationStatus.Warning);

            Debug.Log($"[RSV] Parallel validation completed in {stopwatch.ElapsedMilliseconds} ms");
            Debug.Log($"[RSV] Results: {passed} passed, {failed} failed, {warnings} warnings");

            return results;
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
            if (bindings == null || bindings.Length == 0)
            {
                Debug.LogWarning("[RSV] No bindings provided for parallel validation.");
                return Array.Empty<LGD_ValidationReport>();
            }

            if (maxDegreeOfParallelism <= 0)
            {
                maxDegreeOfParallelism = Environment.ProcessorCount;
            }

            Debug.Log($"[RSV] Validating {bindings.Length} bindings in parallel (max {maxDegreeOfParallelism} threads)...");

            var results = new LGD_ValidationReport[bindings.Length];
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            // Pre-cache paths on main thread to avoid thread-safety violations in Parallel.For
            var cachedPaths = new string[bindings.Length];
            for (int i = 0; i < bindings.Length; i++)
            {
                cachedPaths[i] = UnityEditor.AssetDatabase.GetAssetPath(bindings[i]);
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Parallel.For(0, bindings.Length, options, i =>
            {
                results[i] = RsvValidator.ValidateBinding(bindings[i], cachedPaths[i]);
            });

            stopwatch.Stop();

            Debug.Log($"[RSV] Parallel validation completed in {stopwatch.ElapsedMilliseconds} ms");

            return results;
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
            if (schema == null)
            {
                Debug.LogError("[RSV] Schema is null for parallel file validation.");
                return Array.Empty<LGD_ValidationReport>();
            }

            if (filePaths == null || filePaths.Length == 0)
            {
                Debug.LogWarning("[RSV] No files provided for parallel validation.");
                return Array.Empty<LGD_ValidationReport>();
            }

            if (maxDegreeOfParallelism <= 0)
            {
                maxDegreeOfParallelism = Environment.ProcessorCount;
            }

            Debug.Log($"[RSV] Validating {filePaths.Length} files in parallel (max {maxDegreeOfParallelism} threads)...");

            var results = new LGD_ValidationReport[filePaths.Length];
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Parallel.For(0, filePaths.Length, options, i =>
            {
                try
                {
                    var token = RsvJsonParser.ParseFile(filePaths[i], out var parseError);
                    if (token == null)
                    {
                        results[i] = new LGD_ValidationReport("RSV");
                        results[i].Add(ValidationStatus.Critical, "ParseError", parseError,
                            assetPath: filePaths[i]);
                        return;
                    }

                    results[i] = RsvValidator.Validate(schema, token.ToString());
                }
                catch (Exception ex)
                {
                    results[i] = new LGD_ValidationReport("RSV");
                    results[i].Add(ValidationStatus.Critical, "Exception", ex.Message,
                        assetPath: filePaths[i]);
                }
            });

            stopwatch.Stop();

            Debug.Log($"[RSV] Parallel file validation completed in {stopwatch.ElapsedMilliseconds} ms");

            return results;
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
            var report = new LGD_ValidationReport("RSV");

            if (!System.IO.Directory.Exists(folderPath))
            {
                report.Add(ValidationStatus.Error, "Source", $"Folder not found: {folderPath}");
                return report;
            }

            var files = System.IO.Directory.GetFiles(folderPath, "*.json", System.IO.SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                report.Add(ValidationStatus.Info, "Source", $"No JSON files found in: {folderPath}");
                return report;
            }

            Debug.Log($"[RSV] Found {files.Length} JSON files in {folderPath}");

            var results = ValidateFilesParallel(schema, files, maxDegreeOfParallelism);

            // Merge all results into a single report
            foreach (var result in results)
            {
                foreach (var entry in result.Entries)
                {
                    report.AddEntry(entry);
                }
            }

            return report;
        }

        /// <summary>
        /// Gets the recommended degree of parallelism for the current system.
        /// </summary>
        /// <returns>Recommended parallelism level.</returns>
        public static int GetRecommendedParallelism()
        {
            // Use configured ratio of available cores to leave some for the system
            return Math.Max(1, (int)(Environment.ProcessorCount * RsvConfiguration.DefaultParallelismRatio));
        }
    }
}
