using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Async HTTP fetcher for remote JSON data with retry logic and cancellation support.
    /// Provides better Editor responsiveness compared to synchronous blocking calls.
    /// </summary>
    internal static class RsvAsyncHttpFetcher
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = RsvConfiguration.HttpTimeout
        };

        /// <summary>
        /// Fetches JSON content from a remote URL asynchronously.
        /// </summary>
        /// <param name="url">The URL to fetch from.</param>
        /// <param name="maxSizeBytes">Maximum allowed response size in bytes.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The fetched JSON content, or null if the fetch fails.</returns>
        public static async Task<string> FetchAsync(
            string url,
            int maxSizeBytes,
            CancellationToken cancellationToken = default)
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
                // Send request with cancellation support
                using var response = await _httpClient.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogWarning($"[RSV] HTTP request failed with status: {response.StatusCode}");
                    return null;
                }

                // Check response size before reading content
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > maxSizeBytes)
                {
                    Debug.LogWarning($"[RSV] Response too large: {contentLength.Value:N0} bytes (max {maxSizeBytes:N0} bytes)");
                    return null;
                }

                // Stream content with size limit enforcement
                var content = await ReadContentWithSizeLimitAsync(response, maxSizeBytes, cancellationToken);

                if (content == null)
                {
                    Debug.LogWarning($"[RSV] Response content exceeded size limit during streaming (max {maxSizeBytes:N0} bytes)");
                    return null;
                }

                return content;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogWarning($"[RSV] HTTP request failed: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                Debug.LogWarning("[RSV] HTTP request was cancelled.");
                return null;
            }
            catch (TaskCanceledException)
            {
                Debug.LogWarning("[RSV] HTTP request timed out.");
                return null;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[RSV] HTTP request was cancelled.");
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
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>The fetched JSON content, or null if all retries fail.</returns>
        public static async Task<string> FetchWithRetryAsync(
            string url,
            int maxSizeBytes,
            int maxRetries = 3,
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            while (attempt < maxRetries)
            {
                attempt++;

                try
                {
                    var content = await FetchAsync(url, maxSizeBytes, cancellationToken);
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

                // Exponential backoff
                Debug.Log($"[RSV] Retrying in {delay.TotalSeconds:F1} seconds... (Attempt {attempt + 1}/{maxRetries})");
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
            }

            Debug.LogError($"[RSV] Failed to fetch after {maxRetries} attempts: {url}");
            return null;
        }

        /// <summary>
        /// Reads HTTP response content with size limit enforcement during streaming.
        /// </summary>
        private static async Task<string> ReadContentWithSizeLimitAsync(
            HttpResponseMessage response,
            int maxSizeBytes,
            CancellationToken cancellationToken)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);
            using var buffer = new System.IO.StringWriter();

            int totalBytes = 0;
            var charBuffer = new char[8192]; // 8KB buffer
            int charsRead;

            while ((charsRead = await reader.ReadAsync(charBuffer, 0, charBuffer.Length)) > 0)
            {
                // Estimate bytes (UTF-8: 1-4 bytes per char, use 2 as average)
                totalBytes += charsRead * 2;

                if (totalBytes > maxSizeBytes)
                {
                    return null; // Size limit exceeded
                }

                await buffer.WriteAsync(charBuffer, 0, charsRead);
            }

            return buffer.ToString();
        }
    }
}
