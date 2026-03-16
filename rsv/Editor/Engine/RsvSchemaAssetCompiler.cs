using System.Collections.Generic;
using System.IO;
using LiveGameDev.Core;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Compiles DataSchemaDefinition assets into RsvCompiledSchemaAsset for runtime use.
    /// This is the bridge between Editor schema definitions and runtime validation.
    /// </summary>
    public static class RsvSchemaAssetCompiler
    {
        /// <summary>
        /// Compiles a DataSchemaDefinition into a RsvCompiledSchemaAsset.
        /// </summary>
        /// <param name="definition">The schema definition to compile.</param>
        /// <returns>A compiled schema asset, or null if compilation fails.</returns>
        public static RsvCompiledSchemaAsset CompileToAsset(DataSchemaDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[RSV] Cannot compile null schema definition.");
                return null;
            }

            // Validate the schema first
            var validationReport = RsvSchemaValidator.ValidateSchema(definition);
            if (validationReport.HasErrors || validationReport.HasCritical)
            {
                Debug.LogError($"[RSV] Schema '{definition.SchemaId}' has validation errors. Cannot compile.");
                foreach (var entry in validationReport.Entries)
                {
                    if (entry.Status == ValidationStatus.Error || entry.Status == ValidationStatus.Critical)
                    {
                        Debug.LogError($"  [{entry.Status}] {entry.Category}: {entry.Message}");
                    }
                }
                return null;
            }

            // Create the compiled schema asset
            var compiledAsset = ScriptableObject.CreateInstance<RsvCompiledSchemaAsset>();
            compiledAsset.SchemaId = definition.SchemaId;
            compiledAsset.Version = definition.Version;
            compiledAsset.Description = $"Compiled from {definition.name}";

            // Copy configuration
            compiledAsset.MaxNestingDepth = RsvConfiguration.MaxNestingDepth;

            // Compile root nodes
            compiledAsset.RootNodes = CompileNodes(definition.RootNodes);

            return compiledAsset;
        }

        /// <summary>
        /// Compiles a list of schema nodes to compiled nodes.
        /// </summary>
        private static List<RsvCompiledNode> CompileNodes(List<RsvSchemaNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return new List<RsvCompiledNode>();

            var compiledNodes = new List<RsvCompiledNode>(nodes.Count);

            foreach (var node in nodes)
            {
                var compiledNode = CompileNode(node);
                if (compiledNode != null)
                {
                    compiledNodes.Add(compiledNode);
                }
            }

            return compiledNodes;
        }

        /// <summary>
        /// Compiles a single schema node to a compiled node.
        /// </summary>
        private static RsvCompiledNode CompileNode(RsvSchemaNode node)
        {
            if (node == null || node.Constraint == null)
                return null;

            var c = node.Constraint;
            var compiledNode = new RsvCompiledNode
            {
                Name = node.Name,
                FieldType = c.FieldType,
                IsRequired = c.IsRequired,
                DefaultValue = c.DefaultValue,
                Description = c.Description,
                HasMinMax = c.HasMinMax,
                Min = c.Min,
                Max = c.Max,
                MinLength = c.MinLength,
                MaxLength = c.MaxLength,
                Pattern = c.Pattern,
                EnumValues = c.EnumValues != null ? (string[])c.EnumValues.Clone() : null,
                MinItems = c.MinItems,
                MaxItems = c.MaxItems,
                UniqueItems = c.UniqueItems,
                Children = CompileNodes(node.Children)
            };

            return compiledNode;
        }

        /// <summary>
        /// Compiles a schema and saves it as an asset.
        /// </summary>
        /// <param name="definition">The schema definition to compile.</param>
        /// <param name="outputPath">The path to save the compiled asset (relative to Assets/).</param>
        /// <returns>The compiled schema asset, or null if compilation fails.</returns>
        public static RsvCompiledSchemaAsset CompileAndSave(DataSchemaDefinition definition, string outputPath)
        {
            var compiledAsset = CompileToAsset(definition);
            if (compiledAsset == null)
                return null;

            // Ensure the output directory exists
            var fullOutputPath = Path.Combine("Assets", outputPath);
            var directory = Path.GetDirectoryName(fullOutputPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if asset already exists
            var existingAsset = AssetDatabase.LoadAssetAtPath<RsvCompiledSchemaAsset>(fullOutputPath);
            if (existingAsset != null)
            {
                // Update existing asset
                EditorUtility.CopySerialized(compiledAsset, existingAsset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[RSV] Updated compiled schema: {fullOutputPath}");
                Object.DestroyImmediate(compiledAsset);
                return existingAsset;
            }
            else
            {
                // Create new asset
                AssetDatabase.CreateAsset(compiledAsset, fullOutputPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[RSV] Created compiled schema: {fullOutputPath}");
                return compiledAsset;
            }
        }

        /// <summary>
        /// Compiles a schema and saves it to the default location (Resources/RSV/CompiledSchemas/).
        /// </summary>
        /// <param name="definition">The schema definition to compile.</param>
        /// <returns>The compiled schema asset, or null if compilation fails.</returns>
        public static RsvCompiledSchemaAsset CompileAndSaveToDefaultLocation(DataSchemaDefinition definition)
        {
            if (definition == null)
                return null;

            var sanitizedName = SanitizeFileName(definition.name);
            var outputPath = $"Resources/RSV/CompiledSchemas/{sanitizedName}.asset";
            return CompileAndSave(definition, outputPath);
        }

        /// <summary>
        /// Batch compiles all DataSchemaDefinition assets in the project.
        /// </summary>
        /// <returns>The number of schemas successfully compiled.</returns>
        public static int BatchCompileAllSchemas()
        {
            var guids = AssetDatabase.FindAssets("t:DataSchemaDefinition");
            int compiledCount = 0;
            int errorCount = 0;

            Debug.Log($"[RSV] Batch compiling {guids.Length} schema definitions...");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<DataSchemaDefinition>(path);

                if (definition == null)
                    continue;

                var compiled = CompileAndSaveToDefaultLocation(definition);
                if (compiled != null)
                {
                    compiledCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            AssetDatabase.Refresh();

            if (errorCount > 0)
            {
                Debug.LogWarning($"[RSV] Batch compile complete: {compiledCount} succeeded, {errorCount} failed.");
            }
            else
            {
                Debug.Log($"[RSV] Batch compile complete: {compiledCount} schemas compiled successfully.");
            }

            return compiledCount;
        }

        /// <summary>
        /// Compiles schemas on build (pre-build step).
        /// </summary>
        public static void CompileOnBuild()
        {
            Debug.Log("[RSV] Compiling schemas for build...");
            var count = BatchCompileAllSchemas();
            Debug.Log($"[RSV] Compiled {count} schemas for build.");
        }

        /// <summary>
        /// Sanitizes a file name by removing invalid characters.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "UnnamedSchema";

            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }

            return name;
        }
    }

    /// <summary>
    /// Build processor that compiles schemas before building.
    /// </summary>
    public class RsvSchemaBuildPreprocessor : UnityEditor.Build.IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            RsvSchemaAssetCompiler.CompileOnBuild();
        }
    }
}
