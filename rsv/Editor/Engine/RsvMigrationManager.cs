using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Version comparison and migration path utilities.
    /// Execution logic (Migrate, RunMigrationScript, ApplyBasicTransformations,
    /// GenerateMigrationReport) lives in RsvMigrationManager.Execution.cs.
    /// Data types (MigrationStepStatus, MigrationStepResult, MigrationResult)
    /// live in RsvMigrationTypes.cs.
    /// </summary>
    public static partial class RsvMigrationManager
    {
        /// <summary>
        /// Compares two semantic version strings.
        /// Returns negative if v1 &lt; v2, zero if equal, positive if v1 &gt; v2.
        /// </summary>
        public static int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1)) v1 = "0.0.0";
            if (string.IsNullOrEmpty(v2)) v2 = "0.0.0";

            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            for (int i = 0; i < 3; i++)
            {
                var n1 = i < parts1.Length && int.TryParse(parts1[i], out var num1) ? num1 : 0;
                var n2 = i < parts2.Length && int.TryParse(parts2[i], out var num2) ? num2 : 0;
                if (n1 != n2) return n1.CompareTo(n2);
            }

            return 0;
        }

        /// <summary>Returns true if the version change increments the major component.</summary>
        public static bool IsBreakingChange(string fromVersion, string toVersion)
        {
            if (string.IsNullOrEmpty(fromVersion) || string.IsNullOrEmpty(toVersion)) return false;

            var major1 = ParseMajor(fromVersion);
            var major2 = ParseMajor(toVersion);
            return major2 > major1;
        }

        /// <summary>
        /// Returns migration hints ordered from fromVersion to toVersion.
        /// </summary>
        public static List<RsvMigrationHint> GetMigrationPath(
            DataSchemaDefinition schema, string fromVersion, string toVersion)
        {
            var hints = new List<RsvMigrationHint>();
            if (schema?.MigrationHints == null) return hints;

            return schema.MigrationHints
                .Where(h => !string.IsNullOrEmpty(h.TargetVersion))
                .OrderBy(h => h.TargetVersion, new VersionComparer())
                .Where(h => CompareVersions(h.TargetVersion, fromVersion) > 0 &&
                            CompareVersions(h.TargetVersion, toVersion) <= 0)
                .ToList();
        }

        /// <summary>Validates migration hint configuration on a schema asset.</summary>
        public static LGD_ValidationReport ValidateMigrationHints(DataSchemaDefinition schema)
        {
            var report = new LGD_ValidationReport("RSV");

            if (schema == null)
            {
                report.Add(ValidationStatus.Error, "Migration", "Schema is null.");
                return report;
            }

            if (schema.MigrationHints == null || schema.MigrationHints.Count == 0)
                return report;

            CheckDuplicateVersions(schema, report);
            CheckAscendingOrder(schema, report);
            CheckScriptPaths(schema, report);
            return report;
        }

        // ── Private validation helpers ────────────────────────────
        private static void CheckDuplicateVersions(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            var versionCounts = new Dictionary<string, int>();
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(schema);

            foreach (var hint in schema.MigrationHints)
            {
                if (string.IsNullOrEmpty(hint.TargetVersion))
                {
                    report.Add(ValidationStatus.Warning, "Migration",
                        "Migration hint has empty TargetVersion.", assetPath: assetPath);
                    continue;
                }
                versionCounts.TryGetValue(hint.TargetVersion, out var count);
                versionCounts[hint.TargetVersion] = count + 1;
            }

            foreach (var kvp in versionCounts.Where(k => k.Value > 1))
            {
                report.Add(ValidationStatus.Warning, "Migration",
                    $"Multiple hints for version '{kvp.Key}'. Only one per version recommended.",
                    assetPath: UnityEditor.AssetDatabase.GetAssetPath(schema));
            }
        }

        private static void CheckAscendingOrder(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(schema);
            for (int i = 1; i < schema.MigrationHints.Count; i++)
            {
                var prev = schema.MigrationHints[i - 1];
                var curr = schema.MigrationHints[i];
                if (!string.IsNullOrEmpty(prev.TargetVersion) &&
                    !string.IsNullOrEmpty(curr.TargetVersion) &&
                    CompareVersions(curr.TargetVersion, prev.TargetVersion) <= 0)
                {
                    report.Add(ValidationStatus.Warning, "Migration",
                        $"Hints out of order: '{prev.TargetVersion}' should precede '{curr.TargetVersion}'.",
                        assetPath: assetPath);
                }
            }
        }

        private static void CheckScriptPaths(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(schema);
            foreach (var hint in schema.MigrationHints)
            {
                if (!string.IsNullOrEmpty(hint.MigrationScriptPath))
                {
                    var script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.MonoBehaviour>(hint.MigrationScriptPath);
                    if (script == null)
                        report.Add(ValidationStatus.Warning, "Migration",
                            $"Migration script not found: {hint.MigrationScriptPath}", assetPath: assetPath);
                }
            }
        }

        private static int ParseMajor(string version)
        {
            var parts = version.Split('.');
            return parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        }

        private class VersionComparer : IComparer<string>
        {
            public int Compare(string x, string y) => CompareVersions(x, y);
        }
    }
}
