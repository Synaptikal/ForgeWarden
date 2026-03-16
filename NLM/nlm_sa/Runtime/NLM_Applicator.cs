using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// Applies a <see cref="NarrativePropertyOverride"/> to a GameObject.
    /// </summary>
    /// <remarks>
    /// Used by both the runtime <see cref="NarrativeObjectBinding.ApplyState()"/>
    /// and the Editor's <see cref="NLM_ScenePreviewApplicator"/>.
    /// 
    /// Kept in Runtime assembly so both can reference it without circular dependencies.
    /// </remarks>
    public static class NLM_Applicator
    {
        /// <summary>
        /// Applies a property override to the target GameObject.
        /// </summary>
        /// <param name="target">The GameObject to modify</param>
        /// <param name="ovr">The override definition</param>
        /// <remarks>
        /// Handles all OverrideType values. Safe to call with null parameters (no-op).
        /// </remarks>
        public static void ApplyOverride(GameObject target, NarrativePropertyOverride ovr)
        {
            if (target == null || ovr == null) return;

            switch (ovr.Type)
            {
                case OverrideType.SetActive:
                    target.SetActive(ovr.ActiveState);
                    break;

                case OverrideType.SwapMaterial:
                    ApplySwapMaterial(target, ovr);
                    break;

                case OverrideType.PositionOffset:
                    target.transform.localPosition += ovr.PositionOffset;
                    break;

                case OverrideType.ScaleMultiplier:
                    target.transform.localScale = Vector3.Scale(
                        target.transform.localScale, ovr.ScaleMultiplier);
                    break;

                case OverrideType.EnableComponent:
                case OverrideType.DisableComponent:
                    ApplyComponentToggle(target, ovr);
                    break;

                case OverrideType.SetColor:
                    ApplySetColor(target, ovr);
                    break;

                case OverrideType.ReplaceGameObject:
                    ApplyReplaceGameObject(target, ovr);
                    break;
            }
        }

        #region Private Apply Methods

        private static void ApplySwapMaterial(GameObject target, NarrativePropertyOverride ovr)
        {
            var rend = target.GetComponent<Renderer>();
            if (rend == null || ovr.TargetMaterial == null) return;

            var mats = rend.sharedMaterials;
            int idx = Mathf.Clamp(ovr.MaterialIndex, 0, mats.Length - 1);
            mats[idx] = ovr.TargetMaterial;
            rend.sharedMaterials = mats;
        }

        private static void ApplyComponentToggle(GameObject target, NarrativePropertyOverride ovr)
        {
            if (string.IsNullOrEmpty(ovr.ComponentTypeName)) return;

            var comp = target.GetComponent(ovr.ComponentTypeName) as Behaviour;
            if (comp != null)
                comp.enabled = (ovr.Type == OverrideType.EnableComponent);
        }

        private static void ApplySetColor(GameObject target, NarrativePropertyOverride ovr)
        {
            var rend = target.GetComponent<Renderer>();
            if (rend?.sharedMaterial == null) return;

            var mat = Object.Instantiate(rend.sharedMaterial);
            mat.color = ovr.TargetColor;
            rend.sharedMaterial = mat;
        }

        private static void ApplyReplaceGameObject(GameObject target, NarrativePropertyOverride ovr)
        {
            target.SetActive(false);
            if (ovr.ReplacementObject != null)
                ovr.ReplacementObject.SetActive(true);
        }

        #endregion
    }
}
