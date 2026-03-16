using UnityEditor;
using UnityEngine.UIElements;

namespace LiveGameDev.Core.Editor
{
    /// <summary>
    /// Registers the ForgeWarden settings panel under
    /// Edit > Project Settings > ForgeWarden.
    /// </summary>
    public class LGD_SuiteSettingsProvider : SettingsProvider
    {
        [MenuItem("Tools/ForgeWarden/Core/Suite Settings")]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/ForgeWarden");
        }

        private LGD_SuiteSettings _settings;
        private UnityEditor.Editor _settingsEditor;

        public LGD_SuiteSettingsProvider()
            : base("Project/ForgeWarden", SettingsScope.Project) { }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
            => new LGD_SuiteSettingsProvider
            {
                keywords = new[] { "live game", "forgewarden", "schema", "heatmap", "economy" }
            };

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings       = LGD_SuiteSettingsLoader.FindOrCreateSettings();
            _settingsEditor = UnityEditor.Editor.CreateEditor(_settings);
        }

        public override void OnGUI(string searchContext)
        {
            if (_settings == null || _settingsEditor == null) return;
            EditorGUILayout.LabelField("ForgeWarden Suite", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            _settingsEditor.OnInspectorGUI();
        }

        public override void OnDeactivate()
        {
            if (_settingsEditor != null)
                UnityEngine.Object.DestroyImmediate(_settingsEditor);
        }
    }
}
