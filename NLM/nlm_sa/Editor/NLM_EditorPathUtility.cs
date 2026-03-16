using System;
using System.IO;
using UnityEditor;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Self-contained export path helper for report generation.
    /// </summary>
    /// <remarks>
    /// Manages the output directory for validation reports and exports.
    /// Uses EditorPrefs for persistent storage of user preferences.
    /// </remarks>
    public static class NLM_EditorPathUtility
    {
        private const string DefaultOutputFolder = "Assets/NLM_Reports";
        private const string PrefsKey = "NLM_OutputPath";

        /// <summary>
        /// Gets the current output path for reports.
        /// </summary>
        /// <returns>The configured output path, or default if not set</returns>
        public static string GetOutputPath()
        {
            var path = EditorPrefs.GetString(PrefsKey, DefaultOutputFolder);
            return string.IsNullOrWhiteSpace(path) ? DefaultOutputFolder : path;
        }

        /// <summary>
        /// Sets the output path for reports.
        /// </summary>
        /// <param name="path">The new output path</param>
        public static void SetOutputPath(string path)
            => EditorPrefs.SetString(PrefsKey, path);

        /// <summary>
        /// Ensures the specified directory exists, creating it if necessary.
        /// </summary>
        /// <param name="path">The directory path</param>
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Generates a timestamped filename.
        /// </summary>
        /// <param name="prefix">Filename prefix</param>
        /// <param name="ext">File extension (including dot)</param>
        /// <returns>Formatted filename like "Prefix_20250313_143022.ext"</returns>
        public static string TimestampedFileName(string prefix, string ext)
            => $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";

        /// <summary>
        /// Writes a report file to the configured output directory.
        /// </summary>
        /// <param name="content">The file content</param>
        /// <param name="prefix">Filename prefix</param>
        /// <param name="ext">File extension</param>
        /// <returns>The full path to the written file</returns>
        public static string WriteReport(string content, string prefix, string ext)
        {
            var dir = GetOutputPath();
            EnsureDirectoryExists(dir);
            var file = Path.Combine(dir, TimestampedFileName(prefix, ext));
            File.WriteAllText(file, content);
            AssetDatabase.Refresh();
            return file;
        }
    }
}
