using System;
using System.Threading.Tasks;
using LiveGameDev.Core;
using LiveGameDev.RSV;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Editor-specific JSON source binding.
    /// Inherits from JsonSourceBindingBase for runtime compatibility.
    /// Provides Editor-specific functionality like schema references and async resolution.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Runtime Schema/JSON Source Binding",
        fileName = "NewJsonSourceBinding")]
    public class JsonSourceBinding : JsonSourceBindingBase
    {
        [Tooltip("The schema to validate the JSON source against.")]
        [SerializeField] public DataSchemaDefinition Schema;

        /// <summary>
        /// Validates that this binding is properly configured.
        /// </summary>
        public override bool IsValid()
        {
            if (!base.IsValid())
                return false;

            if (Schema == null)
            {
                Debug.LogWarning($"[RSV] Binding '{name}' has no schema assigned.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the display name for this binding.
        /// </summary>
        public override string GetDisplayName()
        {
            if (Schema != null && !string.IsNullOrEmpty(Schema.DisplayName))
                return $"{name} ({Schema.DisplayName})";
            return base.GetDisplayName();
        }

        /// <summary>
        /// Gets the asset path of this binding in the project.
        /// </summary>
        public string GetAssetPath()
        {
            return AssetDatabase.GetAssetPath(this);
        }

        /// <summary>
        /// Resolve and return the raw JSON text from the configured source.
        /// Returns null if the source cannot be resolved; logs a warning.
        /// </summary>
        public string ResolveJson()
        {
            return JsonSourceBindingEditorExtension.ResolveJson(this);
        }

        /// <summary>
        /// Resolve JSON asynchronously from the configured source.
        /// Preferred over ResolveJson() to avoid blocking.
        /// </summary>
        public Task<string> ResolveJsonAsync()
        {
            return JsonSourceBindingEditorExtension.ResolveJsonAsync(this);
        }

        /// <summary>
        /// Validates the JSON against the assigned schema.
        /// </summary>
        public LGD_ValidationReport Validate()
        {
            if (Schema == null)
            {
                var report = new LGD_ValidationReport("RSV");
                report.Add(ValidationStatus.Error, "Setup",
                    $"Binding '{name}' has no schema assigned.",
                    assetPath: GetAssetPath());
                return report;
            }

            return RsvValidator.ValidateBinding(this);
        }

        /// <summary>
        /// Validates the JSON asynchronously against the assigned schema.
        /// </summary>
        public async Task<LGD_ValidationReport> ValidateAsync()
        {
            if (Schema == null)
            {
                var report = new LGD_ValidationReport("RSV");
                report.Add(ValidationStatus.Error, "Setup",
                    $"Binding '{name}' has no schema assigned.",
                    assetPath: GetAssetPath());
                return report;
            }

            var json = await ResolveJsonAsync();
            if (json == null)
            {
                var report = new LGD_ValidationReport("RSV");
                report.Add(ValidationStatus.Error, "Source",
                    $"Could not resolve JSON from binding '{name}'.",
                    assetPath: GetAssetPath());
                return report;
            }

            return RsvValidator.Validate(Schema, json);
        }

        private void OnValidate()
        {
            // Sync the base class SchemaId with the Editor Schema reference
            if (Schema != null)
            {
                SchemaId = Schema.SchemaId;
            }
            else
            {
                SchemaId = null;
            }
        }
    }
}
