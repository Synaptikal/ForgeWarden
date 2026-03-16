using System;
using System.IO;
using System.Threading.Tasks;
using LiveGameDev.Core;
using LiveGameDev.RSV;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Editor extension for JsonSourceBindingBase.
    /// Provides Editor-specific functionality like asset resolution and async fetching.
    /// This class is only available in the Editor.
    /// </summary>
    [InitializeOnLoad]
    public static class JsonSourceBindingEditorExtension
    {
        static JsonSourceBindingEditorExtension()
        {
            // Register the resolver with the runtime
            JsonSourceResolver.RegisterResolver(ResolveJson);
        }

        /// <summary>
        /// Resolves JSON from a binding based on its source type.
        /// This is the Editor implementation that can access Editor APIs.
        /// </summary>
        public static string ResolveJson(JsonSourceBindingBase binding)
        {
            if (binding == null)
            {
                Debug.LogWarning("[RSV] Cannot resolve JSON from null binding.");
                return null;
            }

            try
            {
                return binding.SourceType switch
                {
                    JsonSourceType.FilePath =>
                        ResolveFilePath(binding.SourcePathOrUrl),

                    JsonSourceType.StreamingAssets =>
                        ResolveStreamingAssets(binding.SourcePathOrUrl),

                    JsonSourceType.Resources =>
                        ResolveResources(binding.SourcePathOrUrl),

                    JsonSourceType.Url =>
                        ResolveUrl(binding.SourcePathOrUrl),

                    _ => LogAndReturnNull($"Unknown SourceType: {binding.SourceType}")
                };
            }
            catch (Exception ex)
            {
                var sanitizedMessage = RsvErrorSanitizer.Sanitize(ex.Message);
                Debug.LogWarning($"[RSV] Failed to resolve JSON source '{binding.name}': {sanitizedMessage}");
                return null;
            }
        }

        /// <summary>
        /// Resolves JSON from a file path asynchronously.
        /// </summary>
        public static async Task<string> ResolveJsonAsync(JsonSourceBindingBase binding)
        {
            if (binding == null)
            {
                Debug.LogWarning("[RSV] Cannot resolve JSON from null binding.");
                return null;
            }

            try
            {
                return binding.SourceType switch
                {
                    JsonSourceType.FilePath =>
                        await ResolveFilePathAsync(binding.SourcePathOrUrl),

                    JsonSourceType.StreamingAssets =>
                        await ResolveStreamingAssetsAsync(binding.SourcePathOrUrl),

                    JsonSourceType.Resources =>
                        ResolveResources(binding.SourcePathOrUrl), // Resources is synchronous

                    JsonSourceType.Url =>
                        await ResolveUrlAsync(binding.SourcePathOrUrl),

                    _ => LogAndReturnNull($"Unknown SourceType: {binding.SourceType}")
                };
            }
            catch (Exception ex)
            {
                var sanitizedMessage = RsvErrorSanitizer.Sanitize(ex.Message);
                Debug.LogWarning($"[RSV] Failed to resolve JSON source '{binding.name}': {sanitizedMessage}");
                return null;
            }
        }

        #region File Path Resolution

        private static string ResolveFilePath(string filePath)
        {
            var pathValidation = RsvPathValidator.ValidatePath(filePath);
            if (pathValidation.IsFailure)
            {
                if (pathValidation.Status == ValidationStatus.Critical)
                {
                    Debug.LogError($"[RSV] Security violation in file path: {pathValidation.ErrorMessage}");
                }
                return LogAndReturnNull(pathValidation.ErrorMessage);
            }

            if (!File.Exists(filePath))
                return LogAndReturnNull($"File not found: {filePath}");

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > RsvConfiguration.MaxLocalFileSizeBytes)
                return LogAndReturnNull($"File too large: {filePath} ({fileInfo.Length:N0} bytes, max {RsvConfiguration.MaxLocalFileSizeBytes:N0} bytes)");

            // Use streaming parser for large files
            var token = RsvJsonParser.ParseFile(filePath, out var parseError);
            if (token == null)
                return LogAndReturnNull($"Failed to parse file: {parseError}");

            return token.ToString();
        }

        private static async Task<string> ResolveFilePathAsync(string filePath)
        {
            var pathValidation = RsvPathValidator.ValidatePath(filePath);
            if (pathValidation.IsFailure)
            {
                if (pathValidation.Status == ValidationStatus.Critical)
                {
                    Debug.LogError($"[RSV] Security violation in file path: {pathValidation.ErrorMessage}");
                }
                return LogAndReturnNull(pathValidation.ErrorMessage);
            }

            if (!File.Exists(filePath))
                return LogAndReturnNull($"File not found: {filePath}");

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > RsvConfiguration.MaxLocalFileSizeBytes)
                return LogAndReturnNull($"File too large: {filePath} ({fileInfo.Length:N0} bytes, max {RsvConfiguration.MaxLocalFileSizeBytes:N0} bytes)");

            // For async, read file asynchronously
            using var reader = new StreamReader(filePath);
            var content = await reader.ReadToEndAsync();
            return content;
        }

        #endregion

        #region StreamingAssets Resolution

        private static string ResolveStreamingAssets(string relativePath)
        {
            var pathValidation = RsvPathValidator.ValidateStreamingAssetsPath(relativePath);
            if (pathValidation.IsFailure)
            {
                if (pathValidation.Status == ValidationStatus.Critical)
                {
                    Debug.LogError($"[RSV] Security violation in StreamingAssets path: {pathValidation.ErrorMessage}");
                }
                return LogAndReturnNull(pathValidation.ErrorMessage);
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

            if (!File.Exists(fullPath))
                return LogAndReturnNull($"StreamingAssets file not found: {relativePath}");

            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > RsvConfiguration.MaxLocalFileSizeBytes)
                return LogAndReturnNull($"File too large: {relativePath} ({fileInfo.Length:N0} bytes, max {RsvConfiguration.MaxLocalFileSizeBytes:N0} bytes)");

            var token = RsvJsonParser.ParseFile(fullPath, out var parseError);
            if (token == null)
                return LogAndReturnNull($"Failed to parse file: {parseError}");

            return token.ToString();
        }

        private static async Task<string> ResolveStreamingAssetsAsync(string relativePath)
        {
            var pathValidation = RsvPathValidator.ValidateStreamingAssetsPath(relativePath);
            if (pathValidation.IsFailure)
            {
                if (pathValidation.Status == ValidationStatus.Critical)
                {
                    Debug.LogError($"[RSV] Security violation in StreamingAssets path: {pathValidation.ErrorMessage}");
                }
                return LogAndReturnNull(pathValidation.ErrorMessage);
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

            if (!File.Exists(fullPath))
                return LogAndReturnNull($"StreamingAssets file not found: {relativePath}");

            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > RsvConfiguration.MaxLocalFileSizeBytes)
                return LogAndReturnNull($"File too large: {relativePath} ({fileInfo.Length:N0} bytes, max {RsvConfiguration.MaxLocalFileSizeBytes:N0} bytes)");

            using var reader = new StreamReader(fullPath);
            return await reader.ReadToEndAsync();
        }

        #endregion

        #region Resources Resolution

        private static string ResolveResources(string resourcePath)
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
                return LogAndReturnNull($"Resources asset not found: {resourcePath}");

            return textAsset.text;
        }

        #endregion

        #region URL Resolution

        private static string ResolveUrl(string url)
        {
            // Validate URL for security (SSRF protection)
            var urlValidation = RsvUrlValidator.ValidateUrl(url);
            if (urlValidation.IsFailure)
            {
                if (urlValidation.Status == ValidationStatus.Critical)
                {
                    Debug.LogError($"[RSV] Security violation in URL: {urlValidation.ErrorMessage}");
                }
                return LogAndReturnNull(urlValidation.ErrorMessage);
            }

            // Check cache only - synchronous HTTP is disabled
            var cached = RsvUrlResponseCache.Get(url);
            if (cached != null)
            {
                return cached;
            }

            Debug.LogError($"[RSV] Synchronous URL fetch is disabled. Use ResolveJsonAsync() for URL: {url}");
            return null;
        }

        private static async Task<string> ResolveUrlAsync(string url)
        {
            // Validate URL for security (SSRF protection)
            var urlValidation = RsvUrlValidator.ValidateUrl(url);
            if (urlValidation.IsFailure)
            {
                if (urlValidation.Status == ValidationStatus.Critical)
                {
                    Debug.LogError($"[RSV] Security violation in URL: {urlValidation.ErrorMessage}");
                }
                return LogAndReturnNull(urlValidation.ErrorMessage);
            }

            // Check cache first
            if (RsvUrlResponseCache.TryGet(url, out var cached))
            {
                return cached;
            }

            // Fetch asynchronously
            var result = await RsvAsyncHttpFetcher.FetchWithRetryAsync(
                url,
                RsvConfiguration.MaxRemoteResponseSizeBytes,
                RsvConfiguration.MaxHttpRetries);

            if (result != null)
            {
                RsvUrlResponseCache.Set(url, result);
            }

            return result;
        }

        #endregion

        private static string LogAndReturnNull(string message)
        {
            var sanitizedMessage = RsvErrorSanitizer.SanitizePreserveFilename(message);
            Debug.LogWarning($"[RSV] {sanitizedMessage}");
            return null;
        }
    }

    /// <summary>
    /// Runtime resolver for JSON sources.
    /// This is used by the runtime to resolve JSON without Editor dependencies.
    /// </summary>
    public static class JsonSourceResolver
    {
        private static Func<JsonSourceBindingBase, string> _resolver;

        /// <summary>
        /// Registers the resolver function.
        /// Called by the Editor extension during initialization.
        /// </summary>
        public static void RegisterResolver(Func<JsonSourceBindingBase, string> resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Resolves JSON from a binding.
        /// Returns null if no resolver is registered (runtime without Editor).
        /// </summary>
        public static string Resolve(JsonSourceBindingBase binding)
        {
            if (_resolver == null)
            {
                Debug.LogWarning("[RSV] No JSON source resolver registered. Are you running in the Editor?");
                return null;
            }

            return _resolver(binding);
        }
    }
}
