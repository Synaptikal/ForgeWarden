using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Validates all JsonSourceBindings with ValidateOnBuild = true
    /// before each build. Fails the build if any binding has Error or Critical severity.
    /// </summary>
    internal class RSV_BuildHook : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport buildReport)
        {
            var bindings = LGD_AssetUtility.FindAllAssetsOfType<JsonSourceBinding>();
            bool anyFailed = false;
            var failedBindings = new System.Collections.Generic.List<string>();

            foreach (var binding in bindings)
            {
                if (!binding.ValidateOnBuild) continue;

                var report = RsvValidator.ValidateBinding(binding);
                if (report.HasErrors || report.HasCritical)
                {
                    Debug.LogError($"[RSV] BUILD BLOCKED — Binding '{binding.name}' failed validation ({report.OverallStatus}).\n" + report.ToMarkdown());
                    anyFailed = true;
                    failedBindings.Add(binding.name);
                }
            }

            if (anyFailed)
                throw new BuildFailedException(
                    "[RSV] Build blocked by RSV validation errors. " +
                    $"Failed bindings: {string.Join(", ", failedBindings)}. " +
                    "Fix all schema binding errors before building.");
        }
    }
}
