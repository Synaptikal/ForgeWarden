using System;
using System.IO;
using UnityEngine;

namespace LiveGameDev.Core.Editor
{
    /// <summary>
    /// File path helpers for report export and output directory management.
    /// </summary>
    public static class LGD_PathUtility
    {
        /// <summary>Returns the configured output path from suite settings, defaulting to Assets/LiveGameDevSuite/Output.</summary>
        public static string GetDefaultOutputPath()
        {
            var settings = LGD_SuiteSettings.Instance;
            return string.IsNullOrEmpty(settings?.DefaultOutputPath)
                ? "Assets/LiveGameDevSuite/Output"
                : settings.DefaultOutputPath;
        }

        /// <summary>Ensures a directory exists, creating it if necessary. Returns the path.</summary>
        public static string EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Returns a timestamped filename like "RSV_Report_2026-03-12_13-00-00.md".
        /// </summary>
        public static string GetTimestampedFileName(string baseName, string extension)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{baseName}_{timestamp}{extension}";
        }
    }
}
