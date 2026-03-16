using System.Collections.Generic;
using System.Linq;
using LiveGameDev.ESS;
using LiveGameDev.ESS.Editor;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ESS.Samples
{
    /// <summary>
    /// Loads the demo economy configuration into the ESS main window.
    /// </summary>
    public static class DemoConfigLoader
    {
        [MenuItem("Tools/ForgeWarden/ESS/Demos/Load Config")]
        public static void LoadDemoConfig()
        {
            // Find all demo assets
            var items = FindAssets<ItemDefinition>("Demo_Economy");
            var sources = FindAssets<SourceDefinition>("Demo_Economy");
            var sinks = FindAssets<SinkDefinition>("Demo_Economy");
            var profiles = FindAssets<PlayerProfileDefinition>("Demo_Economy");
            var recipes = FindAssets<CraftingRecipeDefinition>("Demo_Economy");

            if (items.Count == 0)
            {
                EditorUtility.DisplayDialog("Demo Not Found",
                    "Demo economy assets not found. Please run 'Create Demo Economy' first.",
                    "OK");
                return;
            }

            // Create config
            var config = new SimConfig
            {
                SimulationDays = 90,
                PlayerCount = 1000,
                Seed = 42,
                TrackedItems = items.ToArray(),
                Sources = sources.ToArray(),
                Sinks = sinks.ToArray(),
                PlayerMix = new (PlayerProfileDefinition, float)[]
                {
                    (profiles.FirstOrDefault(p => p.name == "Casual"), 0.4f),
                    (profiles.FirstOrDefault(p => p.name == "Regular"), 0.4f),
                    (profiles.FirstOrDefault(p => p.name == "Hardcore"), 0.2f)
                }
            };

            // Open ESS window and set config
            var window = EditorWindow.GetWindow<ESS_MainWindow>("Economy Simulation");
            window.SetConfig(config, recipes);
            window.Show();

            EditorUtility.DisplayDialog("Demo Config Loaded",
                $"Loaded demo configuration:\n" +
                $"- {items.Count} items\n" +
                $"- {sources.Count} sources\n" +
                $"- {sinks.Count} sinks\n" +
                $"- {profiles.Count} profiles\n" +
                $"- {recipes.Count} recipes\n\n" +
                "Ready to run simulation!",
                "OK");
        }

        private static List<T> FindAssets<T>(string folderFilter) where T : ScriptableObject
        {
            var results = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(folderFilter))
                {
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null) results.Add(asset);
                }
            }

            return results;
        }
    }

    /// <summary>
    /// Extension methods for ESS_MainWindow to support demo loading.
    /// </summary>
    public static class ESS_MainWindowExtensions
    {
        /// <summary>
        /// Sets the simulation configuration (used by demo loader).
        /// </summary>
        public static void SetConfig(this ESS_MainWindow window, SimConfig config, List<CraftingRecipeDefinition> recipes)
        {
            // Use reflection to set private fields
            var configField = typeof(ESS_MainWindow).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recipesField = typeof(ESS_MainWindow).GetField("_recipes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            configField?.SetValue(window, config);
            recipesField?.SetValue(window, recipes);
        }
    }
}
