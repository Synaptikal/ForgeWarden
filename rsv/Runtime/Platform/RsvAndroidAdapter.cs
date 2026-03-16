using System;
using System.IO;
using UnityEngine;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Android-specific platform adapter.
    /// Handles Android's unique file system structure (jar:file:// for StreamingAssets).
    /// </summary>
    public class RsvAndroidAdapter : RsvPlatformAdapter
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
        public override char PathSeparator => '/';

        /// <inheritdoc/>
        public override string LoadJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            // Handle StreamingAssets on Android (stored in APK, need to use UnityWebRequest)
            if (path.StartsWith(Application.streamingAssetsPath, StringComparison.OrdinalIgnoreCase))
            {
                return LoadFromStreamingAssets(path);
            }

            // Handle regular files
            if (!FileExists(path))
            {
                Debug.LogWarning($"[RSV] File not found: {path}");
                return null;
            }

            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RSV] Failed to load file '{path}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a file from StreamingAssets on Android.
        /// </summary>
        private string LoadFromStreamingAssets(string path)
        {
            // On Android, StreamingAssets are inside the APK and need UnityWebRequest
            // This is a simplified version - in production, use async loading
            var relativePath = path.Substring(Application.streamingAssetsPath.Length);
            if (relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
            {
                relativePath = relativePath.Substring(1);
            }

            // For now, return null as sync loading from StreamingAssets on Android
            // requires UnityWebRequest which is async-only
            Debug.LogWarning($"[RSV] Sync loading from StreamingAssets on Android is not supported. Use async loading or copy to persistent data path first.");
            throw new NotSupportedException("Synchronous StreamingAssets loading is not supported on Android. Use LoadFromStreamingAssetsAsync instead.");
        }

        /// <inheritdoc/>
        public override string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            // Android always uses forward slashes
            path = path.Replace('\\', '/');

            return path;
        }

        /// <inheritdoc/>
        public override bool FileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // StreamingAssets files exist in the APK but cannot be loaded synchronously
            // Return false to indicate sync loading is not supported; use async methods instead
            if (path.StartsWith(Application.streamingAssetsPath, StringComparison.OrdinalIgnoreCase))
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
            return Application.streamingAssetsPath;
        }

        /// <inheritdoc/>
        public override string GetPersistentDataPath()
        {
            return Application.persistentDataPath;
        }

        /// <inheritdoc/>
        public override bool IsPathSafe(string path)
        {
            // Android-specific path safety checks
            if (!base.IsPathSafe(path))
                return false;

            // Additional Android-specific checks
            if (path.Contains("/data/data/") && !path.Contains(Application.persistentDataPath))
                return false;

            return true;
        }
    }
}