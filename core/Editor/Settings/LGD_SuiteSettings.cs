using UnityEngine;
using LiveGameDev.Core;

namespace LiveGameDev.Core.Editor
{
    /// <summary>
    /// Global settings for the ForgeWarden suite.
    /// Stored as a ScriptableObject at Assets/LiveGameDevSuite/Settings/LGD_Settings.asset.
    /// Access via LGD_SuiteSettings.Instance (Editor-only).
    /// </summary>
    [CreateAssetMenu(menuName = "ForgeWarden/Core/Suite Settings", fileName = "LGD_Settings")]
    public class LGD_SuiteSettings : ScriptableObject
    {
        [Tooltip("Auto-validate all registered bindings when entering Play Mode.")]
        public bool AutoValidateOnPlay = true;

        [Tooltip("Log additional debug info to the Console during validation runs.")]
        public bool VerboseLogging = false;

        [Tooltip("Default folder path for exported reports and baked textures.")]
        public string DefaultOutputPath = "Assets/LiveGameDevSuite/Output";

        [Tooltip("Gradient used by the Zone Density Heatmap overlay.")]
        public Gradient HeatmapGradient = new Gradient();

        [Tooltip("Your publisher namespace, used in documentation and exported report headers.")]
        public string PublisherNamespace = "com.forgegames";

        private static LGD_SuiteSettings _instance;

        /// <summary>
        /// Returns the active settings instance. Editor-only.
        /// Uses GUID-based lookup — survives folder renames.
        /// </summary>
        public static LGD_SuiteSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = LGD_SuiteSettingsLoader.FindOrCreateSettings();
                return _instance;
            }
        }
    }
}
