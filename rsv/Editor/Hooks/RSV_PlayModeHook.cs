using LiveGameDev.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Automatically validates all JsonSourceBindings with ValidateOnPlay = true
    /// when entering Play Mode, if AutoValidateOnPlay is enabled in Suite Settings.
    /// </summary>
    [InitializeOnLoad]
    internal static class RSV_PlayModeHook
    {
        static RSV_PlayModeHook()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if (!LGD_SuiteSettings.Instance.AutoValidateOnPlay) return;

            RunValidationAndReport();
        }

        private static void RunValidationAndReport()
        {
            var bindings = LGD_AssetUtility.FindAllAssetsOfType<JsonSourceBinding>();
            bool hasBlocker = false;

            foreach (var binding in bindings)
            {
                if (!binding.ValidateOnPlay) continue;

                var report = RsvValidator.ValidateBinding(binding);
                if (report.HasErrors || report.HasCritical)
                {
                    Debug.LogError($"[RSV] Binding '{binding.name}' failed validation " +
                                   $"({report.OverallStatus}). Enter Play Mode blocked. " +
                                   "Open Window > Live Game Dev > Runtime Schema Validator for details.");
                    hasBlocker = true;
                }
                else if (report.OverallStatus == LiveGameDev.Core.ValidationStatus.Warning)
                {
                    Debug.LogWarning($"[RSV] Binding '{binding.name}' has warnings. " +
                                     "Review in the Runtime Schema Validator window.");
                }
            }

            if (hasBlocker)
                EditorApplication.isPlaying = false; // block Play Mode on errors
        }
    }
}
