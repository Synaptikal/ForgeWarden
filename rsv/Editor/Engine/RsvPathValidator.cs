using System;
using System.IO;
using System.Text.RegularExpressions;
using LiveGameDev.Core;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Validates and sanitizes file paths to prevent path traversal attacks.
    /// Ensures file access is restricted to the project directory.
    /// </summary>
    internal static class RsvPathValidator
    {
        // Patterns that indicate path traversal attempts
        private static readonly string[] PathTraversalPatterns = new[]
        {
            "../",
            "..\\",
            "%2e%2e%2f",
            "%2e%2e%5c",
            "..%2f",
            "..%5c",
            "%2e%2e/",
            "%2e%2e\\",
            @"%c0%ae%c0%ae[\\/]",
            @"\x00",
            @"\u0000",
            "%00"
        };

        // Allowed file extensions for JSON files
        private static readonly string[] AllowedExtensions = new[]
        {
            ".json",
            ".jsonc"
        };

        /// <summary>
        /// Validates that a file path is safe and within the project directory.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <param name="allowProjectRoot">Whether to allow files directly in project root.</param>
        /// <returns>A validation result indicating success or failure with error message.</returns>
        public static RsvEditorValidationResult<bool> ValidatePath(string filePath, bool allowProjectRoot = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return RsvEditorValidationResult<bool>.Failure("File path is empty", ValidationStatus.Error);
            }

            // Check for path traversal patterns
            if (ContainsPathTraversal(filePath))
            {
                return RsvEditorValidationResult<bool>.Failure(
                    "File path contains path traversal patterns",
                    ValidationStatus.Critical,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "FilePath", filePath },
                        { "Reason", "Path traversal detected" }
                    });
            }

            // Get absolute path
            string absolutePath;
            try
            {
                absolutePath = Path.GetFullPath(filePath);
            }
            catch (Exception ex)
            {
                return RsvEditorValidationResult<bool>.Failure(
                    $"Invalid file path: {ex.Message}",
                    ValidationStatus.Error);
            }

            // Get project directory
            string projectPath = Directory.GetCurrentDirectory();

            // Normalize paths for comparison
            string normalizedProjectPath = NormalizePath(projectPath);
            string normalizedAbsolutePath = NormalizePath(absolutePath);

            // Check if path is within project directory
            if (!normalizedAbsolutePath.StartsWith(normalizedProjectPath, StringComparison.OrdinalIgnoreCase))
            {
                return RsvEditorValidationResult<bool>.Failure(
                    "File path is outside the project directory",
                    ValidationStatus.Critical,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "FilePath", filePath },
                        { "AbsolutePath", absolutePath },
                        { "ProjectPath", projectPath }
                    });
            }

            // Check if file is directly in project root (if not allowed)
            if (!allowProjectRoot)
            {
                string relativePath = GetRelativePath(projectPath, absolutePath);
                if (!relativePath.Contains(Path.DirectorySeparatorChar) &&
                    !relativePath.Contains(Path.AltDirectorySeparatorChar))
                {
                    return RsvEditorValidationResult<bool>.Failure(
                        "Files directly in project root are not allowed. Use a subfolder.",
                        ValidationStatus.Warning);
                }
            }

            // Check file extension
            string extension = Path.GetExtension(absolutePath).ToLowerInvariant();
            bool isAllowedExtension = false;
            foreach (var allowedExt in AllowedExtensions)
            {
                if (extension == allowedExt)
                {
                    isAllowedExtension = true;
                    break;
                }
            }

            if (!isAllowedExtension && !string.IsNullOrEmpty(extension))
            {
                return RsvEditorValidationResult<bool>.Failure(
                    $"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}",
                    ValidationStatus.Warning);
            }

            // Check if file exists
            if (!File.Exists(absolutePath))
            {
                return RsvEditorValidationResult<bool>.Failure(
                    "File does not exist",
                    ValidationStatus.Error,
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "FilePath", absolutePath }
                    });
            }

            return RsvEditorValidationResult<bool>.Success(true, ValidationStatus.Pass);
        }

        /// <summary>
        /// Validates a path relative to the Assets folder.
        /// </summary>
        /// <param name="relativePath">The relative path from Assets folder.</param>
        /// <returns>A validation result indicating success or failure.</returns>
        public static RsvEditorValidationResult<bool> ValidateAssetsPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return RsvEditorValidationResult<bool>.Failure("Assets path is empty", ValidationStatus.Error);
            }

            // Check for path traversal
            if (ContainsPathTraversal(relativePath))
            {
                return RsvEditorValidationResult<bool>.Failure(
                    "Assets path contains path traversal patterns",
                    ValidationStatus.Critical);
            }

            // Construct full path
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", relativePath);

            // Validate the full path
            return ValidatePath(fullPath, allowProjectRoot: false);
        }

        /// <summary>
        /// Validates a StreamingAssets path.
        /// </summary>
        /// <param name="relativePath">The relative path from StreamingAssets folder.</param>
        /// <returns>A validation result indicating success or failure.</returns>
        public static RsvEditorValidationResult<bool> ValidateStreamingAssetsPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return RsvEditorValidationResult<bool>.Failure("StreamingAssets path is empty", ValidationStatus.Error);
            }

            // Check for path traversal
            if (ContainsPathTraversal(relativePath))
            {
                return RsvEditorValidationResult<bool>.Failure(
                    "StreamingAssets path contains path traversal patterns",
                    ValidationStatus.Critical);
            }

            // Construct full path
            string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, relativePath);

            // Validate the full path
            return ValidatePath(streamingAssetsPath, allowProjectRoot: false);
        }

        /// <summary>
        /// Checks if a path contains path traversal patterns.
        /// </summary>
        private static bool ContainsPathTraversal(string path)
        {
            string lowerPath = path.ToLowerInvariant();
            foreach (var pattern in PathTraversalPatterns)
            {
                if (lowerPath.Contains(pattern.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Normalizes a path for comparison (handles different directory separators).
        /// </summary>
        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToLowerInvariant();
        }

        /// <summary>
        /// Gets the relative path from base to target.
        /// </summary>
        private static string GetRelativePath(string basePath, string targetPath)
        {
            Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            Uri targetUri = new Uri(targetPath);
            Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
            return Uri.UnescapeDataString(relativeUri.ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Sanitizes a file path by removing dangerous characters and patterns.
        /// </summary>
        /// <param name="path">The path to sanitize.</param>
        /// <returns>A sanitized version of the path.</returns>
        public static string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            // Remove path traversal patterns
            string sanitized = path;
            foreach (var pattern in PathTraversalPatterns)
            {
                sanitized = sanitized.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
            }

            // Remove null bytes and control characters
            sanitized = Regex.Replace(sanitized, @"[<>:""/\\|?*]", "_", RegexOptions.None, TimeSpan.FromSeconds(1));
            
            // Remove null bytes and control characters
            sanitized = Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", "", RegexOptions.None, TimeSpan.FromSeconds(1));

            return sanitized;
        }

        /// <summary>
        /// Gets the safe absolute path for a given path.
        /// Returns null if the path is unsafe.
        /// </summary>
        public static string GetSafeAbsolutePath(string path)
        {
            var result = ValidatePath(path);
            if (result.IsFailure)
                return null;

            return Path.GetFullPath(path);
        }
    }
}
