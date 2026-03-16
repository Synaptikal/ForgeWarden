using System;
using System.Text.RegularExpressions;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Sanitizes error messages to prevent information leakage.
    /// Removes sensitive information like absolute file paths, internal paths, and system details.
    /// Preserves relative project paths for debugging purposes.
    /// </summary>
    internal static class RsvErrorSanitizer
    {
        // Patterns to redact from error messages (absolute paths only)
        private static readonly Regex[] SanitizationPatterns = new Regex[]
        {
            // Absolute Windows paths (drive letter)
            new Regex(@"[A-Za-z]:\\[^\r\n]*", RegexOptions.Compiled, TimeSpan.FromSeconds(1)),

            // Absolute Unix paths starting with /
            new Regex(@"^/[^\r\n]*(?:/[^\r\n]*)*", RegexOptions.Compiled, TimeSpan.FromSeconds(1)),

            // User-specific paths
            new Regex(@"C:\\Users\\[^\r\n]*", RegexOptions.Compiled, TimeSpan.FromSeconds(1)),
            new Regex(@"/home/[^\r\n]*", RegexOptions.Compiled, TimeSpan.FromSeconds(1)),
            new Regex(@"/Users/[^\r\n]*", RegexOptions.Compiled, TimeSpan.FromSeconds(1)),

            // GUIDs
            new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled, TimeSpan.FromSeconds(1)),

            // Exception stack traces (simplified)
            new Regex(@"at [^\r\n]*\.[^\r\n]*\(.*?\)", RegexOptions.Compiled, TimeSpan.FromSeconds(1)),
        };

        // Patterns that are safe to preserve (relative project paths)
        private static readonly string[] SafePathPrefixes = new[]
        {
            "Assets/",
            "Packages/",
            "StreamingAssets/",
            "Resources/"
        };

        /// <summary>
        /// Sanitizes an error message by removing sensitive information.
        /// Preserves relative project paths for debugging.
        /// </summary>
        /// <param name="message">The error message to sanitize.</param>
        /// <returns>A sanitized version of the error message.</returns>
        public static string Sanitize(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            string sanitized = message;

            // First, preserve safe relative paths
            var preservedPaths = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var prefix in SafePathPrefixes)
            {
                var matches = Regex.Matches(sanitized, $@"{prefix}[^\r\n""'<>]*\.(json|asset|meta)", RegexOptions.None, TimeSpan.FromSeconds(1));
                foreach (Match match in matches)
                {
                    string placeholder = $"__SAFE_PATH_{preservedPaths.Count}__";
                    preservedPaths[placeholder] = match.Value;
                    sanitized = sanitized.Replace(match.Value, placeholder);
                }
            }

            // Sanitize the message
            foreach (var pattern in SanitizationPatterns)
            {
                sanitized = pattern.Replace(sanitized, "[REDACTED]");
            }

            // Restore preserved paths
            foreach (var kvp in preservedPaths)
            {
                sanitized = sanitized.Replace(kvp.Key, kvp.Value);
            }

            return sanitized;
        }

        /// <summary>
        /// Sanitizes an error message but preserves the asset filename for user reference.
        /// </summary>
        /// <param name="message">The error message to sanitize.</param>
        /// <returns>A sanitized version with filename preserved.</returns>
        public static string SanitizePreserveFilename(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            string sanitized = message;

            // Extract filename before sanitizing
            string filename = ExtractFilename(message);

            // Sanitize the message
            foreach (var pattern in SanitizationPatterns)
            {
                sanitized = pattern.Replace(sanitized, "[REDACTED]");
            }

            // Re-add filename if it was found
            if (!string.IsNullOrEmpty(filename))
            {
                // Replace only first occurrence (string.Replace with count not available in .NET Standard 2.0)
                int redactedIdx = sanitized.IndexOf("[REDACTED]", StringComparison.Ordinal);
                if (redactedIdx >= 0)
                    sanitized = sanitized.Substring(0, redactedIdx) + filename + sanitized.Substring(redactedIdx + "[REDACTED]".Length);
            }

            return sanitized;
        }

        /// <summary>
        /// Extracts a simple filename from a path for user reference.
        /// </summary>
        private static string ExtractFilename(string message)
        {
            // Try to extract a .json or .asset filename
            var match = Regex.Match(message, @"[\w-]+\.(json|asset)", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (match.Success)
            {
                return match.Value;
            }

            return null;
        }

        /// <summary>
        /// Checks if a path is a safe relative project path.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path is safe to preserve, false otherwise.</returns>
        public static bool IsSafePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            foreach (var prefix in SafePathPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}