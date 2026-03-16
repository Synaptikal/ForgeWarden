using System.Collections.Generic;
using UnityEditor;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Self-contained AssetDatabase helper for editor operations.
    /// </summary>
    /// <remarks>
    /// No external dependencies — works without any other packages.
    /// Provides common asset operations used throughout the NLM editor.
    /// </remarks>
    public static class NLM_EditorAssetUtility
    {
        /// <summary>
        /// Finds all assets of a specific type in the project.
        /// </summary>
        /// <typeparam name="T">The type of asset to find (must inherit from UnityEngine.Object)</typeparam>
        /// <returns>Array of all found assets of the specified type</returns>
        /// <remarks>
        /// Uses AssetDatabase.FindAssets with type filtering.
        /// Safe to call during editor initialization.
        /// </remarks>
        public static T[] FindAllAssetsOfType<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var results = new List<T>(guids.Length);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) results.Add(asset);
            }
            return results.ToArray();
        }

        /// <summary>
        /// Pings an asset in the Project window to highlight it.
        /// </summary>
        /// <param name="obj">The object to ping</param>
        public static void PingAsset(UnityEngine.Object obj)
            => EditorGUIUtility.PingObject(obj);

        /// <summary>
        /// Selects an asset in the Project window and pings it.
        /// </summary>
        /// <param name="obj">The object to select</param>
        public static void SelectAsset(UnityEngine.Object obj)
        {
            UnityEditor.Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }
}
