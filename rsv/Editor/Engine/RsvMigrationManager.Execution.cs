using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using LiveGameDev.Core;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    public static partial class RsvMigrationManager
    {
        /// <summary>
        /// Attempts to run a migration script for the given hint.
        /// Returns the migrated JSON string, or null on failure.
        /// </summary>
        public static string RunMigrationScript(RsvMigrationHint hint, string jsonText)
        {
            if (hint == null || string.IsNullOrEmpty(hint.MigrationScriptPath)) return null;

            try
            {
                var validationResult = RsvMigrationScriptValidator.ValidateScript(hint.MigrationScriptPath);
                if (validationResult.IsFailure)
                {
                    Debug.LogError($"[RSV] Migration script validation failed: {validationResult.ErrorMessage}");
                    if (validationResult.Status == ValidationStatus.Critical)
                        Debug.LogError("[RSV] CRITICAL: Script contains security vulnerabilities and will not run.");
                    return null;
                }

                var script = UnityEditor.AssetDatabase.LoadAssetAtPath<MonoScript>(hint.MigrationScriptPath);
                if (script == null)
                {
                    Debug.LogError($"[RSV] Migration script not found: {hint.MigrationScriptPath}");
                    return null;
                }

                var scriptClass = script.GetClass();
                if (scriptClass == null)
                {
                    Debug.LogError($"[RSV] Could not get class from: {hint.MigrationScriptPath}");
                    return null;
                }

                var migrateMethod = scriptClass.GetMethod(
                    "Migrate",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                if (migrateMethod == null)
                {
                    Debug.LogError($"[RSV] '{scriptClass.Name}' must have: public static string Migrate(string json)");
                    return null;
                }

                var task = System.Threading.Tasks.Task.Run(() =>
                    migrateMethod.Invoke(null, new object[] { jsonText }));

                if (!task.Wait(TimeSpan.FromSeconds(30)))
                {
                    Debug.LogError($"[RSV] Migration script '{scriptClass.Name}' timed out after 30s.");
                    return null;
                }

                return task.Result as string;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RSV] Error running migration script: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Migrates JSON data from fromVersion to toVersion using the schema's migration hints.
        /// </summary>
        public static RsvEditorValidationResult<MigrationResult> Migrate(
            DataSchemaDefinition schema,
            string fromVersion,
            string toVersion,
            string jsonText)
        {
            if (schema == null)
                return Fail("Schema is null", RsvErrorCode.SchemaNull);

            if (string.IsNullOrWhiteSpace(jsonText))
                return Fail("JSON text is null or empty", RsvErrorCode.JsonTextNullOrEmpty);

            if (string.IsNullOrWhiteSpace(fromVersion) || string.IsNullOrWhiteSpace(toVersion))
                return Fail("Source or target version is empty", RsvErrorCode.EmptyVersion);

            if (CompareVersions(fromVersion, toVersion) == 0)
                return RsvEditorValidationResult<MigrationResult>.Success(
                    new MigrationResult
                    {
                        MigratedJson = jsonText, FromVersion = fromVersion, ToVersion = toVersion,
                        StepsExecuted = 0, Warnings = new List<string> { "Versions are identical. No migration needed." }
                    },
                    ValidationStatus.Info);

            var migrationPath = GetMigrationPath(schema, fromVersion, toVersion);
            if (migrationPath.Count == 0 && IsBreakingChange(fromVersion, toVersion))
                return Fail($"No migration path from {fromVersion} to {toVersion} (breaking change).",
                    RsvErrorCode.MigrationScriptNotFound);

            return ExecuteMigration(schema, fromVersion, toVersion, jsonText, migrationPath);
        }

        private static RsvEditorValidationResult<MigrationResult> ExecuteMigration(
            DataSchemaDefinition schema,
            string fromVersion,
            string toVersion,
            string jsonText,
            List<RsvMigrationHint> migrationPath)
        {
            var result = new MigrationResult
            {
                FromVersion = fromVersion, ToVersion = toVersion,
                CurrentJson = jsonText, MigratedJson = jsonText,
                StepsExecuted = 0,
                Steps    = new List<MigrationStepResult>(),
                Warnings = new List<string>(),
                Errors   = new List<string>()
            };

            try
            {
                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(jsonText);

                foreach (var hint in migrationPath)
                {
                    var stepResult = new MigrationStepResult
                    {
                        TargetVersion = hint.TargetVersion,
                        Description   = hint.Description,
                        IsRequired    = hint.IsRequired,
                        Status        = MigrationStepStatus.Pending
                    };

                    var failResult = ApplyMigrationStep(hint, ref result, ref jsonObject, stepResult);
                    result.Steps.Add(stepResult);

                    if (failResult != null) return failResult;
                    result.StepsExecuted++;
                }

                try
                {
                    Newtonsoft.Json.Linq.JToken.Parse(result.MigratedJson);
                    result.IsValid = true;
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Migrated JSON is invalid: {ex.Message}");
                }

                return RsvEditorValidationResult<MigrationResult>.Success(
                    result,
                    result.Errors.Count > 0 ? ValidationStatus.Warning : ValidationStatus.Pass);
            }
            catch (Exception ex)
            {
                return RsvEditorValidationResult<MigrationResult>.FromException(ex, ValidationStatus.Critical);
            }
        }

        private static RsvEditorValidationResult<MigrationResult> ApplyMigrationStep(
            RsvMigrationHint hint,
            ref MigrationResult result,
            ref Newtonsoft.Json.Linq.JObject jsonObject,
            MigrationStepResult stepResult)
        {
            try
            {
                if (!string.IsNullOrEmpty(hint.MigrationScriptPath))
                {
                    var scriptResult = RunMigrationScript(hint, result.MigratedJson);
                    if (scriptResult != null)
                    {
                        result.MigratedJson = scriptResult;
                        jsonObject = Newtonsoft.Json.Linq.JObject.Parse(result.MigratedJson);
                        stepResult.Status = MigrationStepStatus.Completed;
                        stepResult.Output = "Migration script executed successfully";
                    }
                    else
                    {
                        stepResult.Status = MigrationStepStatus.Failed;
                        stepResult.Error  = "Migration script returned null";
                        result.Errors.Add($"Migration to {hint.TargetVersion} failed: {stepResult.Error}");

                        if (hint.IsRequired)
                            return FailStep($"Required step to {hint.TargetVersion} failed",
                                RsvErrorCode.MigrationScriptExecutionFailed, stepResult);
                    }
                }
                else
                {
                    ApplyBasicTransformations(jsonObject, hint);
                    result.MigratedJson = jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
                    stepResult.Status   = MigrationStepStatus.Completed;
                    stepResult.Output   = "Basic transformations applied";
                }

                return null;
            }
            catch (Exception ex)
            {
                stepResult.Status = MigrationStepStatus.Failed;
                stepResult.Error  = ex.Message;
                result.Errors.Add($"Migration to {hint.TargetVersion} failed: {ex.Message}");

                return hint.IsRequired
                    ? FailStep($"Required step to {hint.TargetVersion} failed: {ex.Message}",
                        RsvErrorCode.MigrationScriptExecutionFailed, stepResult)
                    : null;
            }
        }

        /// <summary>
        /// Applies heuristic text-based transformations (rename, remove, array wrapping)
        /// from migration hint description when no script is provided.
        /// </summary>
        private static void ApplyBasicTransformations(
            Newtonsoft.Json.Linq.JObject jsonObject, RsvMigrationHint hint)
        {
            if (string.IsNullOrEmpty(hint.Description)) return;
            var desc = hint.Description.ToLowerInvariant();

            ApplyRenames(jsonObject, desc);
            ApplyRemovals(jsonObject, desc);
            ApplyArrayWrap(jsonObject, desc);
        }

        private static void ApplyRenames(Newtonsoft.Json.Linq.JObject obj, string desc)
        {
            var patterns = new[] { @"renamed?\s+(\w+)\s+to\s+(\w+)", @"(\w+)\s+is\s+now\s+(\w+)", @"(\w+)\s+->\s+(\w+)" };
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(desc, pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
                if (!match.Success) continue;
                var oldName = match.Groups[1].Value;
                var newName = match.Groups[2].Value;
                if (obj[oldName] != null)
                {
                    obj[newName] = obj[oldName];
                    obj.Remove(oldName);
                }
            }
        }

        private static void ApplyRemovals(Newtonsoft.Json.Linq.JObject obj, string desc)
        {
            var patterns = new[] { @"removed\s+(\w+)", @"(\w+)\s+is\s+removed", @"removed\s+field\s+(\w+)" };
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(desc, pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
                if (match.Success) obj.Remove(match.Groups[1].Value);
            }
        }

        private static void ApplyArrayWrap(Newtonsoft.Json.Linq.JObject obj, string desc)
        {
            if (!desc.Contains("is now an array") && !desc.Contains("changed to array")) return;

            var match = Regex.Match(desc, @"(\w+)\s+(?:is now an array|changed to array)",
                RegexOptions.None, TimeSpan.FromSeconds(2));
            if (!match.Success) return;

            var fieldName = match.Groups[1].Value;
            if (obj[fieldName] != null && obj[fieldName].Type != Newtonsoft.Json.Linq.JTokenType.Array)
                obj[fieldName] = new Newtonsoft.Json.Linq.JArray(obj[fieldName]);
        }

        /// <summary>Generates a markdown migration report between two versions.</summary>
        public static string GenerateMigrationReport(
            DataSchemaDefinition schema, string fromVersion, string toVersion)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Migration Report: {schema?.DisplayName ?? schema?.name ?? "Unknown"}");
            sb.AppendLine();
            sb.AppendLine($"**From Version:** {fromVersion ?? "Unknown"}");
            sb.AppendLine($"**To Version:** {toVersion ?? "Unknown"}");
            sb.AppendLine();
            sb.AppendLine($"**Breaking Change:** {(IsBreakingChange(fromVersion, toVersion) ? "⚠️ Yes" : "✅ No")}");
            sb.AppendLine();

            var path = GetMigrationPath(schema, fromVersion, toVersion);
            if (path.Count == 0)
            {
                sb.AppendLine("No migration steps required.");
            }
            else
            {
                sb.AppendLine($"**Migration Steps ({path.Count}):**");
                sb.AppendLine();
                foreach (var hint in path)
                {
                    sb.AppendLine($"### Version {hint.TargetVersion}");
                    sb.AppendLine();
                    sb.AppendLine($"**Required:** {(hint.IsRequired ? "Yes" : "No")}");
                    sb.AppendLine($"**Created:** {hint.CreatedDate}");
                    sb.AppendLine();
                    sb.AppendLine(hint.Description);
                    sb.AppendLine();
                    if (!string.IsNullOrEmpty(hint.MigrationScriptPath))
                        sb.AppendLine($"**Migration Script:** `{hint.MigrationScriptPath}`\n");
                }
            }

            return sb.ToString();
        }

        // ── Convenience failure factories ────────────────────────
        private static RsvEditorValidationResult<MigrationResult> Fail(string msg, RsvErrorCode code) =>
            RsvEditorValidationResult<MigrationResult>.Failure(msg, ValidationStatus.Error,
                new System.Collections.Generic.Dictionary<string, object> { { "ErrorCode", code } });

        private static RsvEditorValidationResult<MigrationResult> FailStep(
            string msg, RsvErrorCode code, MigrationStepResult step) =>
            RsvEditorValidationResult<MigrationResult>.Failure(msg, ValidationStatus.Error,
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "ErrorCode", code },
                    { "StepResult", step }
                });
    }
}
