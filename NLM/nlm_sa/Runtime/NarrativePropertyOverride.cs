using System;
using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// Defines the types of property overrides that can be applied to GameObjects.
    /// </summary>
    public enum OverrideType
    {
        /// <summary>Sets the GameObject's active state (enabled/disabled)</summary>
        SetActive,
        /// <summary>Swaps a material on the object's Renderer</summary>
        SwapMaterial,
        /// <summary>Adds an offset to the object's local position (additive)</summary>
        PositionOffset,
        /// <summary>Multiplies the object's local scale per-axis</summary>
        ScaleMultiplier,
        /// <summary>Enables a specific component (must inherit from Behaviour)</summary>
        EnableComponent,
        /// <summary>Disables a specific component (must inherit from Behaviour)</summary>
        DisableComponent,
        /// <summary>Sets the color of the object's material (creates instance)</summary>
        SetColor,
        /// <summary>Deactivates this object and activates a replacement</summary>
        ReplaceGameObject
    }

    /// <summary>
    /// Defines a property change to apply to a GameObject when a narrative rule is triggered.
    /// </summary>
    /// <remarks>
    /// Each override type uses different fields. Only the fields relevant to the selected Type are used.
    /// The <see cref="NLM_Applicator"/> class handles applying these overrides at runtime or in editor preview.
    /// </remarks>
    [Serializable]
    public class NarrativePropertyOverride
    {
        [Tooltip("The type of property change to apply.")]
        public OverrideType Type = OverrideType.SetActive;

        #region SetActive Fields
        [Tooltip("Target active state for SetActive override")]
        public bool ActiveState = true;
        #endregion

        #region SwapMaterial Fields
        [Tooltip("Index into Renderer.sharedMaterials array.")]
        [Min(0)] public int MaterialIndex = 0;

        [Tooltip("Material to assign at the specified index.")]
        public Material TargetMaterial;
        #endregion

        #region PositionOffset Fields
        [Tooltip("Added to localPosition (does not replace).")]
        public Vector3 PositionOffset = Vector3.zero;
        #endregion

        #region ScaleMultiplier Fields
        [Tooltip("Multiplied against each axis of localScale.")]
        public Vector3 ScaleMultiplier = Vector3.one;
        #endregion

        #region Enable/DisableComponent Fields
        [Tooltip("Assembly-qualified or short type name. E.g. 'UnityEngine.ParticleSystem' or 'MyNamespace.MyComponent'")]
        public string ComponentTypeName;
        #endregion

        #region SetColor Fields
        [Tooltip("Target color for the material.")]
        public Color TargetColor = Color.white;
        #endregion

        #region ReplaceGameObject Fields
        [Tooltip("Object activated when condition is true. Owner is deactivated.")]
        public GameObject ReplacementObject;
        #endregion

        /// <summary>
        /// Returns a human-readable summary of this override.
        /// </summary>
        /// <returns>A string describing what this override will do</returns>
        public string Summary() => Type switch
        {
            OverrideType.SetActive => $"SetActive({ActiveState})",
            OverrideType.SwapMaterial => $"SwapMaterial[{MaterialIndex}]={TargetMaterial?.name ?? "null"}",
            OverrideType.PositionOffset => $"PosOffset({PositionOffset})",
            OverrideType.ScaleMultiplier => $"Scale×({ScaleMultiplier})",
            OverrideType.EnableComponent => $"Enable:{ComponentTypeName}",
            OverrideType.DisableComponent => $"Disable:{ComponentTypeName}",
            OverrideType.SetColor => $"Color={TargetColor}",
            OverrideType.ReplaceGameObject => $"Replace→{ReplacementObject?.name ?? "null"}",
            _ => Type.ToString()
        };
    }
}
