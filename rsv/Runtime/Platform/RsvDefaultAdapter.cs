using System;
using System.IO;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Default platform adapter for most platforms (Windows, Mac, Linux, iOS, Console).
    /// Supports full file system access, threading, and reflection.
    /// </summary>
    public class RsvDefaultAdapter : RsvPlatformAdapter
    {
        /// <inheritdoc/>
        public override bool SupportsThreading => true;

        /// <inheritdoc/>
        public override bool SupportsReflection => true;

        /// <inheritdoc/>
        public override bool SupportsSyncFileOperations => true;

        /// <inheritdoc/>
        public override bool SupportsSyncHttp => true;

        /// <inheritdoc/>
        public override char PathSeparator => Path.DirectorySeparatorChar;

        /// <inheritdoc/>
        public override string LoadJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            if (!FileExists(path))
            {
                UnityEngine.Debug.LogWarning($"[RSV] File not found: {path}");
                return null;
            }

            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[RSV] Failed to load file '{path}': {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public override string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            // Replace forward slashes with backslashes on Windows
            if (PathSeparator == '\\')
            {
                path = path.Replace('/', '\\');
            }
            // Replace backslashes with forward slashes on Unix-like systems
            else if (PathSeparator == '/')
            {
                path = path.Replace('\\', '/');
            }

            return path;
        }

        /// <inheritdoc/>
        public override bool FileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                return File.Exists(path);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override string GetStreamingAssetsPath()
        {
            return UnityEngine.Application.streamingAssetsPath;
        }

        /// <inheritdoc/>
        public override string GetPersistentDataPath()
        {
            return UnityEngine.Application.persistentDataPath;
        }

        /// <inheritdoc/>
        public override string GetErrorMessage(int errorCode)
        {
            // Map common error codes to messages
            return errorCode switch
            {
                2 => "File not found",
                3 => "Path too long",
                5 => "Access denied",
                32 => "File in use",
                _ => base.GetErrorMessage(errorCode)
            };
        }
    }
}