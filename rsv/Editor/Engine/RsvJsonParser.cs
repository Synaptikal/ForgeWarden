using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Thin wrapper around Newtonsoft.Json for RSV's parsing needs.
    /// Isolates the Newtonsoft dependency to this single class.
    /// Uses streaming for large files to avoid memory issues.
    /// </summary>
    internal static class RsvJsonParser
    {
        /// <summary>
        /// Parse raw JSON text into a JToken tree.
        /// Returns null and sets parseError on syntax failure.
        /// Uses streaming for large files (>10MB).
        /// </summary>
        internal static JToken Parse(string jsonText, out string parseError)
        {
            return RsvStreamingJsonParser.ParseString(jsonText, out parseError);
        }

        /// <summary>
        /// Parse JSON from a file path into a JToken tree.
        /// Returns null and sets parseError on syntax failure.
        /// Uses streaming for large files (>10MB).
        /// </summary>
        internal static JToken ParseFile(string filePath, out string parseError)
        {
            return RsvStreamingJsonParser.ParseFile(filePath, out parseError);
        }

        /// <summary>
        /// Attempt to navigate a dot-notation path within a JToken tree.
        /// Example: "abilities[0].damage" → returns the 'damage' JToken.
        /// </summary>
        internal static bool TryGetValue(JToken root, string path, out JToken value)
        {
            value = null;
            try
            {
                value = root.SelectToken(path);
                return value != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
