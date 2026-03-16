using System.IO;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.Core.Editor
{
    /// <summary>
    /// Ensures LGD_SuiteSettings exists on Editor load.
    /// Uses GUID-first asset lookup — never a hardcoded path.
    /// Handles folder renames safely.
    /// </summary>
    [InitializeOnLoad]
    public static class LGD_SuiteSettingsLoader
    {
        static LGD_SuiteSettingsLoader()
        {
            // Trigger find-or-create on every Editor domain reload
            FindOrCreateSettings();

            // Clear EventBus on domain reload to prevent stale subscriptions
            LGD_EventBus.ClearAll();
        }

        /// <summary>
        /// Find existing settings asset anywhere in the project (path-independent),
        /// or create it at the default location if missing.
        /// If duplicates are found, logs a warning asking the user to remove extras.
        /// </summary>
        public static LGD_SuiteSettings FindOrCreateSettings()
        {
            var guids = AssetDatabase.FindAssets("t:LGD_SuiteSettings");

            if (guids.Length > 1)
                Debug.LogWarning("[LiveGameDev] Multiple LGD_SuiteSettings assets found. " +
                                 "Please remove duplicates and keep only one.");

            if (guids.Length >= 1)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<LGD_SuiteSettings>(path);
            }

            return CreateDefaultSettings();
        }

        private static LGD_SuiteSettings CreateDefaultSettings()
        {
            const string defaultPath = "Assets/LiveGameDevSuite/Settings/LGD_Settings.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(defaultPath)!);

            var settings = ScriptableObject.CreateInstance<LGD_SuiteSettings>();
            AssetDatabase.CreateAsset(settings, defaultPath);
            AssetDatabase.SaveAssets();

            Debug.Log("[LiveGameDev] Suite settings created at: " + defaultPath);
            return settings;
        }
    }
}
