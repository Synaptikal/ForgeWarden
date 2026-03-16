using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Streaming JSON parser for large files.
    /// Avoids loading entire files into memory.
    /// </summary>
    internal static class RsvStreamingJsonParser
    {
        // Use configuration for streaming threshold

        /// <summary>
        /// Parses JSON from a file, using streaming for large files.
        /// </summary>
        /// <param name="filePath">Path to the JSON file.</param>
        /// <param name="parseError">Output parameter for parse error message.</param>
        /// <returns>The parsed JToken, or null if parsing fails.</returns>
        public static JToken ParseFile(string filePath, out string parseError)
        {
            parseError = null;

            if (!File.Exists(filePath))
            {
                parseError = $"File not found: {filePath}";
                return null;
            }

            var fileInfo = new FileInfo(filePath);

            // Use streaming for large files
            if (fileInfo.Length > RsvConfiguration.StreamingThresholdBytes)
            {
                return ParseLargeFile(filePath, out parseError);
            }

            // Use regular parsing for small files
            try
            {
                var json = File.ReadAllText(filePath);
                return JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                parseError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Parses a large JSON file using streaming.
        /// </summary>
        private static JToken ParseLargeFile(string filePath, out string parseError)
        {
            parseError = null;

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(reader);

                // Parse the root token
                var token = JToken.ReadFrom(jsonReader);
                return token;
            }
            catch (JsonException ex)
            {
                parseError = ex.Message;
                return null;
            }
            catch (IOException ex)
            {
                parseError = $"IO error: {ex.Message}";
                return null;
            }
            catch (Exception ex)
            {
                parseError = $"Unexpected error: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Parses JSON from a string, using streaming for large strings.
        /// </summary>
        /// <param name="jsonText">The JSON text to parse.</param>
        /// <param name="parseError">Output parameter for parse error message.</param>
        /// <returns>The parsed JToken, or null if parsing fails.</returns>
        public static JToken ParseString(string jsonText, out string parseError)
        {
            parseError = null;

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                parseError = "JSON text is null or empty.";
                return null;
            }

            // Use streaming for large strings
            if (jsonText.Length > RsvConfiguration.StreamingThresholdBytes)
            {
                return ParseLargeString(jsonText, out parseError);
            }

            // Use regular parsing for small strings
            try
            {
                return JToken.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                parseError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Parses a large JSON string using streaming.
        /// </summary>
        private static JToken ParseLargeString(string jsonText, out string parseError)
        {
            parseError = null;

            try
            {
                using var reader = new StringReader(jsonText);
                using var jsonReader = new JsonTextReader(reader);

                var token = JToken.ReadFrom(jsonReader);
                return token;
            }
            catch (JsonException ex)
            {
                parseError = ex.Message;
                return null;
            }
            catch (Exception ex)
            {
                parseError = $"Unexpected error: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Gets the streaming threshold in bytes.
        /// </summary>
        public static int GetStreamingThreshold()
        {
            return RsvConfiguration.StreamingThresholdBytes;
        }
    }
}
