using System;
using UnityEngine;

namespace LiveGameDev.Core
{
    /// <summary>
    /// Abstract base ScriptableObject for all Live Game Dev Suite definition assets.
    /// Provides a stable GUID, display name, tags, and ILgdValidatable contract.
    /// </summary>
    public abstract class LGD_BaseDefinition : ScriptableObject, ILgdValidatable
    {
        [Tooltip("Human-readable name shown in suite Editor windows.")]
        [SerializeField] public string DisplayName;

        [Tooltip("Auto-assigned stable GUID for cross-tool references. Do not edit manually.")]
        [SerializeField, HideInInspector] public string Guid;

        [Tooltip("Tags used for filtering in suite windows.")]
        [SerializeField] public string[] Tags = Array.Empty<string>();

        [Tooltip("Optional description for documentation purposes.")]
        [SerializeField, TextArea(2, 5)] public string Description;

        /// <summary>
        /// Called by Unity when the asset is first created or loaded.
        /// Assigns a stable GUID if one has not yet been set.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (string.IsNullOrEmpty(Guid))
                Guid = System.Guid.NewGuid().ToString();
        }

        /// <inheritdoc/>
        public abstract ValidationStatus Validate(LGD_ValidationReport report);
    }
}
