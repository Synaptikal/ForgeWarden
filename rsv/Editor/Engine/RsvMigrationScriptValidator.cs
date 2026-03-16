using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LiveGameDev.Core;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Validates migration scripts for security and safety.
    /// Prevents execution of potentially malicious code.
    /// </summary>
    internal static class RsvMigrationScriptValidator
    {
        // Allowed namespaces for migration scripts
        private static readonly string[] AllowedNamespaces = new[]
        {
            "LiveGameDev.RSV",
            "LiveGameDev",
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq"
        };

        // Blocked types and methods
        private static readonly string[] BlockedTypes = new[]
        {
            "System.IO.File",
            "System.IO.Directory",
            "System.Diagnostics.Process",
            "System.Net.WebClient",
            "System.Net.Http.HttpClient",
            "System.Reflection.Assembly",
            "System.CodeDom",
            "Microsoft.CSharp",
            "UnityEngine.Application"
        };

        // Blocked method names
        private static readonly string[] BlockedMethods = new[]
        {
            "Execute",
            "Start",
            "Load",
            "Save",
            "Delete",
            "WriteAllText",
            "ReadAllText",
            "Open",
            "Create",
            "Compile",
            "Invoke"
        };

        /// <summary>
        /// Validates a migration script for security issues.
        /// </summary>
        /// <param name="scriptPath">Path to the migration script.</param>
        /// <returns>Validation result with success status and error message if failed.</returns>
        public static RsvEditorValidationResult<bool> ValidateScript(string scriptPath)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                return RsvEditorValidationResult<bool>.Failure("Script path is empty", ValidationStatus.Error);
            }

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script == null)
            {
                return RsvEditorValidationResult<bool>.Failure($"Script not found at path: {scriptPath}", ValidationStatus.Error);
            }

            var scriptClass = script.GetClass();
            if (scriptClass == null)
            {
                return RsvEditorValidationResult<bool>.Failure("Could not get class from script", ValidationStatus.Error);
            }

            // Check namespace
            if (!IsNamespaceAllowed(scriptClass.Namespace))
            {
                return RsvEditorValidationResult<bool>.Failure(
                    $"Script namespace '{scriptClass.Namespace}' is not in the allowed list",
                    ValidationStatus.Error,
                    new Dictionary<string, object>
                    {
                        { "Namespace", scriptClass.Namespace },
                        { "AllowedNamespaces", string.Join(", ", AllowedNamespaces) }
                    }
                );
            }

            // Check for blocked types in the script
            var scriptText = NormalizeScriptText(script.text);
            var blockedTypeUsage = FindBlockedTypes(scriptText);
            if (blockedTypeUsage.Count > 0)
            {
                return RsvEditorValidationResult<bool>.Failure(
                    $"Script contains blocked types: {string.Join(", ", blockedTypeUsage)}",
                    ValidationStatus.Critical,
                    new Dictionary<string, object>
                    {
                        { "BlockedTypes", blockedTypeUsage }
                    }
                );
            }

            // Check for blocked methods
            var blockedMethodUsage = FindBlockedMethods(scriptText);
            if (blockedMethodUsage.Count > 0)
            {
                return RsvEditorValidationResult<bool>.Failure(
                    $"Script contains blocked methods: {string.Join(", ", blockedMethodUsage)}",
                    ValidationStatus.Critical,
                    new Dictionary<string, object>
                    {
                        { "BlockedMethods", blockedMethodUsage }
                    }
                );
            }

            // Check for suspicious patterns
            var suspiciousPatterns = FindSuspiciousPatterns(scriptText);
            if (suspiciousPatterns.Count > 0)
            {
                return RsvEditorValidationResult<bool>.Failure(
                    $"Script contains suspicious patterns: {string.Join(", ", suspiciousPatterns)}",
                    ValidationStatus.Warning,
                    new Dictionary<string, object>
                    {
                        { "SuspiciousPatterns", suspiciousPatterns }
                    }
                );
            }

            // Verify the Migrate method signature
            var migrateMethod = scriptClass.GetMethod(
                "Migrate",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);

            if (migrateMethod == null)
            {
                return RsvEditorValidationResult<bool>.Failure(
                    "Script does not have a static 'Migrate(string json)' method",
                    ValidationStatus.Error
                );
            }

            // Verify return type
            if (migrateMethod.ReturnType != typeof(string))
            {
                return RsvEditorValidationResult<bool>.Failure(
                    $"Migrate method has invalid return type: {migrateMethod.ReturnType.Name}. Expected: string",
                    ValidationStatus.Error
                );
            }

            return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
        }

        /// <summary>
        /// Checks if a namespace is allowed.
        /// </summary>
        private static bool IsNamespaceAllowed(string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                return true; // Allow scripts without namespace

            return AllowedNamespaces.Any(allowed => namespaceName.StartsWith(allowed));
        }

        /// <summary>
        /// Finds usage of blocked types in script text.
        /// </summary>
        private static List<string> FindBlockedTypes(string scriptText)
        {
            var found = new List<string>();

            foreach (var blockedType in BlockedTypes)
            {
                if (scriptText.Contains(blockedType))
                {
                    found.Add(blockedType);
                }
            }

            return found;
        }

        /// <summary>
        /// Finds usage of blocked methods in script text.
        /// </summary>
        private static List<string> FindBlockedMethods(string scriptText)
        {
            var found = new List<string>();

            foreach (var blockedMethod in BlockedMethods)
            {
                // Look for method calls (e.g., "File.WriteAllText(" or ".Execute(")
                if (System.Text.RegularExpressions.Regex.IsMatch(scriptText, $@"\.{blockedMethod}\s*\(", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
                {
                    found.Add(blockedMethod);
                }
            }

            return found;
        }

        /// <summary>
        /// Finds suspicious patterns in script text.
        /// </summary>
        private static List<string> FindSuspiciousPatterns(string scriptText)
        {
            var found = new List<string>();

            // Check for eval-like patterns
            if (System.Text.RegularExpressions.Regex.IsMatch(scriptText, @"\b(eval|Execute|Compile|Invoke)\s*\(", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                found.Add("Dynamic code execution");
            }

            // Check for reflection usage
            if (System.Text.RegularExpressions.Regex.IsMatch(scriptText, @"\b(Assembly|GetType|MethodInfo|Activator)\.", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                found.Add("Reflection usage");
            }

            // Check for network operations
            if (System.Text.RegularExpressions.Regex.IsMatch(scriptText, @"\b(WebClient|HttpClient|WebRequest|FtpWebRequest)\.", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                found.Add("Network operations");
            }

            // Check for file operations
            if (System.Text.RegularExpressions.Regex.IsMatch(scriptText, @"\b(File|Directory)\.", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                found.Add("File system operations");
            }

            // Check for process operations
            if (System.Text.RegularExpressions.Regex.IsMatch(scriptText, @"\bProcess\.", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                found.Add("Process operations");
            }

            return found;
        }

        /// <summary>
        /// Normalizes script text by removing comments, string literals, and collapsing whitespace.
        /// This prevents common bypass techniques like comment injection or string literal hiding.
        /// </summary>
        private static string NormalizeScriptText(string scriptText)
        {
            if (string.IsNullOrEmpty(scriptText))
                return string.Empty;

            // Remove single-line comments (// ...)
            var normalized = Regex.Replace(scriptText, @"//.*$", string.Empty, RegexOptions.Multiline, TimeSpan.FromSeconds(2));

            // Remove multi-line comments (/* ... */)
            normalized = Regex.Replace(normalized, @"/\*[\s\S]*?\*/", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(2));

            // Remove string literals (double quotes) - match "..." with escaped chars
            normalized = Regex.Replace(normalized, "\"[^\"\\\\]*(?:\\\\.[^\"\\\\]*)*\"", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(2));

            // Remove character literals (single quotes) - match '...' with escaped chars
            normalized = Regex.Replace(normalized, @"'[^'\\]*(?:\\.[^'\\]*)*'", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(2));

            // Collapse all whitespace to single spaces
            normalized = Regex.Replace(normalized, @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(2));

            return normalized.Trim();
        }

        /// <summary>
        /// Gets the list of allowed namespaces.
        /// </summary>
        public static string[] GetAllowedNamespaces()
        {
            return (string[])AllowedNamespaces.Clone();
        }

        /// <summary>
        /// Adds a namespace to the allowed list.
        /// </summary>
        public static void AddAllowedNamespace(string namespaceName)
        {
            if (!string.IsNullOrWhiteSpace(namespaceName) && !AllowedNamespaces.Contains(namespaceName))
            {
                Debug.Log($"[RSV] Added allowed namespace: {namespaceName}");
            }
        }

        /// <summary>
        /// Gets the list of blocked types.
        /// </summary>
        public static string[] GetBlockedTypes()
        {
            return (string[])BlockedTypes.Clone();
        }

        /// <summary>
        /// Gets the list of blocked methods.
        /// </summary>
        public static string[] GetBlockedMethods()
        {
            return (string[])BlockedMethods.Clone();
        }
    }
}