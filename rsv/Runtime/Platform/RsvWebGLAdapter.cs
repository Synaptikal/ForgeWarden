using System;
using System.IO;
using UnityEngine;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// WebGL-specific platform adapter.
    /// Handles WebGL's limitations: no threading, no synchronous file operations, no synchronous HTTP.
    /// </summary>
    public class RsvWebGLAdapter : RsvPlatformAdapter
    {
        /// <inheritdoc/>
        public override bool SupportsThreading => false;

        /// <inheritdoc/>
        public override bool SupportsReflection => true;

        /// <inheritdoc/>
        public override bool SupportsSyncFileOperations => false;

        /// <inheritdoc/>
        public override bool SupportsSyncHttp => false;

        /// <inheritdoc/>
        public override char PathSeparator => '/';

        /// <inheritdoc/>
        public override string LoadJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            // WebGL doesn't support synchronous file operations
            // This is a placeholder - in production, use async loading with UnityWebRequest
            Debug.LogWarning($"[RSV] Synchronous file loading is not supported on WebGL. Use async loading with UnityWebRequest.");
            throw new NotSupportedException("Synchronous operations are not supported on WebGL. Use async methods instead.");
        }

        /// <inheritdoc/>
        public override string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            // WebGL always uses forward slashes
            path = path.Replace('\\', '/');

            return path;
        }

        /// <inheritdoc/>
        public override bool FileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // WebGL file system is virtual - we can't check synchronously
            // Return false to indicate uncertainty; caller should attempt async loading
            return false;
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
            // WebGL-specific path safety checks
            if (!base.IsPathSafe(path))
                return false;

            // WebGL has a virtual file system - additional checks may be needed
            // based on the specific file system implementation

            return true;
        }

        /// <inheritdoc/>
        public override string GetErrorMessage(int errorCode)
        {
            // WebGL-specific error messages
            return errorCode switch
            {
                0 => "Success",
                1 => "File not found",
                2 => "Security error",
                3 => "Abort error",
                4 => "Not readable error",
                5 => "Encoding error",
                _ => base.GetErrorMessage(errorCode)
            };
        }
    }
}