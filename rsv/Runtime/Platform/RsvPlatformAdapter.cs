using System;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Abstract base class for platform-specific adapters.
    /// Handles platform differences in file loading, threading, and reflection.
    /// </summary>
    public abstract class RsvPlatformAdapter
    {
        /// <summary>
        /// Gets the current platform adapter instance.
        /// </summary>
        public static RsvPlatformAdapter Current { get; private set; }

        /// <summary>
        /// Initializes the platform adapter for the current platform.
        /// </summary>
        public static void Initialize()
        {
#if UNITY_WEBGL
            Current = new RsvWebGLAdapter();
#elif UNITY_ANDROID
            Current = new RsvAndroidAdapter();
#else
            Current = new RsvDefaultAdapter();
#endif

            UnityEngine.Debug.Log($"[RSV] Platform adapter initialized: {Current.GetType().Name}");
        }

        /// <summary>
        /// Loads JSON content from a file path.
        /// </summary>
        /// <param name="path">The path to the JSON file.</param>
        /// <returns>The JSON content, or null if loading failed.</returns>
        public abstract string LoadJson(string path);

        /// <summary>
        /// Gets whether the platform supports threading.
        /// </summary>
        public abstract bool SupportsThreading { get; }

        /// <summary>
        /// Gets whether the platform supports reflection.
        /// </summary>
        public abstract bool SupportsReflection { get; }

        /// <summary>
        /// Gets whether the platform supports synchronous file operations.
        /// </summary>
        public abstract bool SupportsSyncFileOperations { get; }

        /// <summary>
        /// Gets whether the platform supports synchronous HTTP requests.
        /// </summary>
        public abstract bool SupportsSyncHttp { get; }

        /// <summary>
        /// Gets the platform-specific path separator.
        /// </summary>
        public abstract char PathSeparator { get; }

        /// <summary>
        /// Normalizes a file path for the current platform.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public abstract string NormalizePath(string path);

        /// <summary>
        /// Checks if a file exists at the given path.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        public abstract bool FileExists(string path);

        /// <summary>
        /// Gets the StreamingAssets path for the current platform.
        /// </summary>
        /// <returns>The StreamingAssets path.</returns>
        public abstract string GetStreamingAssetsPath();

        /// <summary>
        /// Gets the persistent data path for the current platform.
        /// </summary>
        /// <returns>The persistent data path.</returns>
        public abstract string GetPersistentDataPath();

        /// <summary>
        /// Validates that a path is safe to use (no path traversal, etc.).
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the path is safe, false otherwise.</returns>
        public virtual bool IsPathSafe(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Check for path traversal
            if (path.Contains("..") || path.Contains("~/"))
                return false;

            // Check for absolute paths (only allow relative paths)
            if (System.IO.Path.IsPathRooted(path))
                return false;

            // Check for invalid characters
            var invalidChars = new[] { '<', '>', ':', '"', '|', '?', '*' };
            foreach (var c in invalidChars)
            {
                if (path.Contains(c.ToString()))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a platform-specific error message for a given error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <returns>A platform-specific error message.</returns>
        public virtual string GetErrorMessage(int errorCode)
        {
            return $"Error code: {errorCode}";
        }
    }
}