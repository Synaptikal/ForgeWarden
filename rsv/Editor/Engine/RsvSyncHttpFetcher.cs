using System;
using System.IO;
using System.Net;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Synchronous HTTP fetcher for remote JSON data with retry logic.
    /// Provides synchronous fallback for contexts where async/await cannot be used safely.
    /// </summary>
    internal static class RsvSyncHttpFetcher
    {
        /// <summary>
        /// Fetches JSON content from a remote URL synchronously.
        /// </summary>
        /// <param name="url">The URL to fetch from.</param>
        /// <param name="maxSizeBytes">Maximum allowed response size in bytes.</param>
        /// <returns>The fetched JSON content, or null if the fetch fails.</returns>
        public static string Fetch(string url, int maxSizeBytes)
        {
            // Validate URL format and enforce HTTPS
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("[RSV] URL is empty or null.");
                return null;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Debug.LogWarning($"[RSV] Invalid URL format: {url}");
                return null;
            }

            if (uri.Scheme != "https")
            {
                Debug.LogWarning($"[RSV] Only HTTPS URLs are allowed for security. URL: {url}");
                return null;
            }

            // Validate URL against whitelist/blacklist
            var urlValidation = RsvUrlValidator.ValidateUrl(url);
            if (urlValidation.IsFailure)
            {
                if (urlValidation.Status == ValidationStatus.Critical)
                {
                    Debug.LogError($"[RSV] URL validation failed: {urlValidation.ErrorMessage}");
                }
                else
                {
                    Debug.LogWarning($"[RSV] URL validation failed: {urlValidation.ErrorMessage}");
                }
                return null;
            }

            try
            {
                // Create web request with timeout
                var request = WebRequest.CreateHttp(url);
                request.Method = "GET";
                request.Timeout = (int)RsvConfiguration.HttpTimeout.TotalMilliseconds;
                request.ReadWriteTimeout = (int)RsvConfiguration.HttpTimeout.TotalMilliseconds;
                request.AllowAutoRedirect = true;
                request.MaximumAutomaticRedirections = 5;
                request.UserAgent = "Unity-RSV-Editor/1.0";

                using var response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Debug.LogWarning($"[RSV] HTTP request failed with status: {response.StatusCode}");
                    return null;
                }

                // Check content length before reading
                var contentLength = response.ContentLength;
                if (contentLength > maxSizeBytes)
                {
                    Debug.LogWarning($"[RSV] Response too large: {contentLength:N0} bytes (max {maxSizeBytes:N0} bytes)");
                    return null;
                }

                // Read content with size limit enforcement
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream);
                var content = ReadContentWithSizeLimit(reader, maxSizeBytes);

                if (content == null)
                {
                    Debug.LogWarning($"[RSV] Response content exceeded size limit during reading (max {maxSizeBytes:N0} bytes)");
                    return null;
                }

                return content;
            }
            catch (WebException ex)
            {
                Debug.LogWarning($"[RSV] HTTP request failed: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RSV] Unexpected error during HTTP request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches JSON content from a remote URL with retry logic.
        /// </summary>
        /// <param name="url">The URL to fetch from.</param>
        /// <param name="maxSizeBytes">Maximum allowed response size in bytes.</param>
        /// <param name="maxRetries">Maximum number of retry attempts.</param>
        /// <returns>The fetched JSON content, or null if all retries fail.</returns>
        public static string FetchWithRetry(string url, int maxSizeBytes, int maxRetries = 3)
        {
            int attempt = 0;
            var delay = TimeSpan.FromSeconds(1);

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    var content = Fetch(url, maxSizeBytes);
                    if (content != null)
                    {
                        if (attempt > 1)
                        {
                            Debug.Log($"[RSV] Successfully fetched after {attempt} attempts.");
                        }
                        return content;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RSV] Attempt {attempt} failed: {ex.Message}");
                }

                // Don't delay after the last attempt
                if (attempt >= maxRetries)
                    break;

                // Bounded exponential backoff to prevent tight-looping
                int delayMs = (int)Math.Min(1000 * Math.Pow(2, attempt - 1), 5000);
                Debug.Log($"[RSV] Retrying in {delayMs}ms... (Attempt {attempt + 1}/{maxRetries})");
                
                // Note: This blocks the current thread. In Editor UI context, this will cause a hang,
                // but it's preferable to a tight CPU-burning loop.
                System.Threading.Thread.Sleep(delayMs);
            }

            Debug.LogError($"[RSV] Failed to fetch after {maxRetries} attempts: {url}");
            return null;
        }

        /// <summary>
        /// Reads response content with size limit enforcement.
        /// </summary>
        private static string ReadContentWithSizeLimit(StreamReader reader, int maxSizeBytes)
        {
            var buffer = new char[8192]; // 8KB buffer
            var sb = new System.Text.StringBuilder();
            int totalBytes = 0;
            int charsRead;

            while ((charsRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Estimate bytes (UTF-8: 1-4 bytes per char, use 2 as average)
                totalBytes += charsRead * 2;

                if (totalBytes > maxSizeBytes)
                {
                    return null; // Size limit exceeded
                }

                sb.Append(buffer, 0, charsRead);
            }

            return sb.ToString();
        }
    }
}
