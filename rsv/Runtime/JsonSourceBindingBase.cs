using System;
using UnityEngine;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Runtime-safe base class for JSON source binding.
    /// Contains only data fields that can be serialized and used at runtime.
    /// Editor-specific functionality is in JsonSourceBindingEditorExtension.
    /// </summary>
    [Serializable]
    public class JsonSourceBindingBase : ScriptableObject
    {
        [Tooltip("The schema ID to validate the JSON source against.")]
        [SerializeField] public string SchemaId;

        [Tooltip("How the JSON source is located.")]
        [SerializeField] public JsonSourceType SourceType = JsonSourceType.StreamingAssets;

        [Tooltip("File path, Resources path, or URL depending on SourceType.")]
        [SerializeField] public string SourcePathOrUrl;

        [Tooltip("Auto-validate this binding when entering Play Mode (if AutoValidateOnPlay is on).")]
        [SerializeField] public bool ValidateOnPlay = true;

        [Tooltip("Block builds and show report if this binding fails validation.")]
        [SerializeField] public bool ValidateOnBuild = true;

        [Tooltip("Stable identifier for this binding. Auto-assigned.")]
        [SerializeField, HideInInspector] public string BindingId;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(BindingId))
                BindingId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Validates that this binding is properly configured.
        /// </summary>
        public virtual bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(SchemaId))
            {
                Debug.LogWarning($"[RSV] Binding '{name}' has no SchemaId assigned.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(SourcePathOrUrl))
            {
                Debug.LogWarning($"[RSV] Binding '{name}' has no source path/URL assigned.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a display name for this binding.
        /// </summary>
        public virtual string GetDisplayName()
        {
            return string.IsNullOrEmpty(name) ? $"Binding-{BindingId}" : name;
        }
    }

    /// <summary>
    /// Defines how the JSON source is located.
    /// </summary>
    public enum JsonSourceType
    {
        FilePath,
        StreamingAssets,
        Resources,
        Url
    }
}
