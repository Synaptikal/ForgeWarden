using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Schema versioning and migration support.
    /// Allows schemas to evolve over time and provides migration paths for data.
    /// </summary>
    public static class RsvSchemaVersioning
    {
        private static readonly Dictionary<string, List<SchemaVersion>> _schemaVersions = new Dictionary<string, List<SchemaVersion>>();
        private static readonly Dictionary<string, List<MigrationRule>> _migrationRules = new Dictionary<string, List<MigrationRule>>();

        /// <summary>
        /// Registers a schema version.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="version">Version string.</param>
        /// <param name="schema">Schema definition.</param>
        public static void RegisterVersion(string schemaId, string version, DataSchemaDefinition schema)
        {
            if (!_schemaVersions.ContainsKey(schemaId))
            {
                _schemaVersions[schemaId] = new List<SchemaVersion>();
            }

            var schemaVersion = new SchemaVersion
            {
                SchemaId = schemaId,
                Version = version,
                Schema = schema,
                RegisteredAt = DateTime.UtcNow
            };

            _schemaVersions[schemaId].Add(schemaVersion);

            // Sort versions by semantic version
            _schemaVersions[schemaId].Sort((a, b) => CompareVersions(a.Version, b.Version));

            Debug.Log($"[RSV] Registered schema version: {schemaId} v{version}");
        }

        /// <summary>
        /// Registers a migration rule.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="fromVersion">Source version.</param>
        /// <param name="toVersion">Target version.</param>
        /// <param name="migration">Migration function.</param>
        public static void RegisterMigration(string schemaId, string fromVersion, string toVersion, Func<JToken, JToken> migration)
        {
            if (!_migrationRules.ContainsKey(schemaId))
            {
                _migrationRules[schemaId] = new List<MigrationRule>();
            }

            var rule = new MigrationRule
            {
                SchemaId = schemaId,
                FromVersion = fromVersion,
                ToVersion = toVersion,
                Migration = migration
            };

            _migrationRules[schemaId].Add(rule);

            Debug.Log($"[RSV] Registered migration: {schemaId} v{fromVersion} -> v{toVersion}");
        }

        /// <summary>
        /// Gets the latest version of a schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <returns>Latest schema version, or null if not found.</returns>
        public static SchemaVersion GetLatestVersion(string schemaId)
        {
            if (!_schemaVersions.ContainsKey(schemaId) || _schemaVersions[schemaId].Count == 0)
                return null;

            return _schemaVersions[schemaId].Last();
        }

        /// <summary>
        /// Gets a specific version of a schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="version">Version string.</param>
        /// <returns>Schema version, or null if not found.</returns>
        public static SchemaVersion GetVersion(string schemaId, string version)
        {
            if (!_schemaVersions.ContainsKey(schemaId))
                return null;

            return _schemaVersions[schemaId].FirstOrDefault(v => v.Version == version);
        }

        /// <summary>
        /// Gets all versions of a schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <returns>List of schema versions.</returns>
        public static List<SchemaVersion> GetAllVersions(string schemaId)
        {
            if (!_schemaVersions.ContainsKey(schemaId))
                return new List<SchemaVersion>();

            return new List<SchemaVersion>(_schemaVersions[schemaId]);
        }

        /// <summary>
        /// Migrates JSON data from one schema version to another.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="json">JSON data to migrate.</param>
        /// <param name="fromVersion">Source version.</param>
        /// <param name="toVersion">Target version.</param>
        /// <returns>Migrated JSON data, or null if migration failed.</returns>
        public static string Migrate(string schemaId, string json, string fromVersion, string toVersion)
        {
            try
            {
                var token = JToken.Parse(json);
                var migrated = MigrateToken(schemaId, token, fromVersion, toVersion);
                return migrated?.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RSV] Migration failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Migrates a JToken from one schema version to another.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="token">Token to migrate.</param>
        /// <param name="fromVersion">Source version.</param>
        /// <param name="toVersion">Target version.</param>
        /// <returns>Migrated token, or null if migration failed.</returns>
        public static JToken MigrateToken(string schemaId, JToken token, string fromVersion, string toVersion)
        {
            if (token == null)
                return null;

            // If versions are the same, no migration needed
            if (fromVersion == toVersion)
                return token;

            // Get migration path
            var path = FindMigrationPath(schemaId, fromVersion, toVersion);
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[RSV] No migration path found from {fromVersion} to {toVersion}");
                return null;
            }

            // Apply migrations in order
            var current = token;
            foreach (var rule in path)
            {
                try
                {
                    current = rule.Migration(current);
                    Debug.Log($"[RSV] Applied migration: {schemaId} v{rule.FromVersion} -> v{rule.ToVersion}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RSV] Migration step failed: {ex.Message}");
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// Finds a migration path between two versions.
        /// </summary>
        private static List<MigrationRule> FindMigrationPath(string schemaId, string fromVersion, string toVersion)
        {
            if (!_migrationRules.ContainsKey(schemaId))
                return null;

            var rules = _migrationRules[schemaId];
            var visited = new HashSet<string>();
            var path = new List<MigrationRule>();

            if (FindMigrationPathRecursive(schemaId, fromVersion, toVersion, rules, visited, path))
            {
                return path;
            }

            return null;
        }

        /// <summary>
        /// Recursively finds a migration path.
        /// </summary>
        private static bool FindMigrationPathRecursive(
            string schemaId,
            string currentVersion,
            string targetVersion,
            List<MigrationRule> rules,
            HashSet<string> visited,
            List<MigrationRule> path)
        {
            if (currentVersion == targetVersion)
                return true;

            if (visited.Contains(currentVersion))
                return false;

            visited.Add(currentVersion);

            // Find all rules that start from current version
            var applicableRules = rules.Where(r => r.FromVersion == currentVersion).ToList();

            foreach (var rule in applicableRules)
            {
                path.Add(rule);

                if (FindMigrationPathRecursive(schemaId, rule.ToVersion, targetVersion, rules, visited, path))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        /// <summary>
        /// Compares two version strings.
        /// </summary>
        private static int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
            var v2Parts = version2.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
            {
                var v1 = i < v1Parts.Length ? v1Parts[i] : 0;
                var v2 = i < v2Parts.Length ? v2Parts[i] : 0;

                if (v1 < v2) return -1;
                if (v1 > v2) return 1;
            }

            return 0;
        }

        /// <summary>
        /// Auto-registers all schemas in the project.
        /// </summary>
        public static void AutoRegisterSchemas()
        {
            var schemas = LGD_AssetUtility.FindAllAssetsOfType<DataSchemaDefinition>();

            foreach (var schema in schemas)
            {
                if (!string.IsNullOrWhiteSpace(schema.SchemaId) && !string.IsNullOrWhiteSpace(schema.Version))
                {
                    RegisterVersion(schema.SchemaId, schema.Version, schema);
                }
            }

            Debug.Log($"[RSV] Auto-registered {schemas.Length} schemas");
        }

        /// <summary>
        /// Clears all registered versions and migration rules.
        /// </summary>
        public static void ClearAll()
        {
            _schemaVersions.Clear();
            _migrationRules.Clear();
            Debug.Log("[RSV] Cleared all schema versions and migration rules");
        }
    }

    /// <summary>
    /// Represents a schema version.
    /// </summary>
    public class SchemaVersion
    {
        public string SchemaId { get; set; }
        public string Version { get; set; }
        public DataSchemaDefinition Schema { get; set; }
        public DateTime RegisteredAt { get; set; }

        public override string ToString()
        {
            return $"{SchemaId} v{Version}";
        }
    }

    /// <summary>
    /// Represents a migration rule.
    /// </summary>
    public class MigrationRule
    {
        public string SchemaId { get; set; }
        public string FromVersion { get; set; }
        public string ToVersion { get; set; }
        public Func<JToken, JToken> Migration { get; set; }

        public override string ToString()
        {
            return $"{SchemaId} v{FromVersion} -> v{ToVersion}";
        }
    }
}