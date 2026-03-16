using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.Core.Editor
{
    /// <summary>
    /// Utility helpers for ScriptableObject asset management in the suite.
    /// </summary>
    public static class LGD_AssetUtility
    {
        /// <summary>
        /// Find a ScriptableObject asset of type T anywhere in the project,
        /// or create it at <paramref name="path"/> if none exists.
        /// </summary>
        public static T FindOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }

            var instance = ScriptableObject.CreateInstance<T>();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            return instance;
        }

        /// <summary>Find all ScriptableObject assets of type T in the project.</summary>
        public static T[] FindAllAssetsOfType<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToArray();
        }

        /// <summary>Convert an absolute path to a project-relative Assets/ path.</summary>
        public static string GetRelativePath(string absolutePath)
        {
            var fullBase = System.IO.Path.GetFullPath(Application.dataPath + "/../");
            return absolutePath.Replace(fullBase, "").Replace("\\", "/");
        }
    }
}
