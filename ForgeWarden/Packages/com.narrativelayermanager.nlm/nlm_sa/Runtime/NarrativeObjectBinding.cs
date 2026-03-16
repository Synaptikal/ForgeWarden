using System;
using System.Collections.Generic;
using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// Attach to any GameObject to declare how it looks/behaves at each narrative beat.
    /// </summary>
    /// <remarks>
    /// <para>Rules are evaluated top-to-bottom by <see cref="NLM_Evaluator"/>. 
    /// The first matching rule wins unless FallThrough = true.</para>
    /// 
    /// <para>Pattern: Add a no-condition rule at the bottom as your default/fallback.</para>
    /// 
    /// <para>Example rule setup:
    /// <code>
    /// Rule 0: Chapter >= 5 → ShowRuins      [FallThrough=false]
    /// Rule 1: Chapter >= 2 → ShowDamaged    [FallThrough=false]
    /// Rule 2: (no conds)   → ShowIntact     ← always-true fallback
    /// </code></para>
    /// 
    /// <para>At runtime this component is data-only. Your runtime NarrativeManager
    /// reads it to apply the correct visual state for the current beat.</para>
    /// </remarks>
    [AddComponentMenu("Narrative Layer Manager/Narrative Object Binding")]
    [DisallowMultipleComponent]
    public class NarrativeObjectBinding : MonoBehaviour
    {
        /// <summary>
        /// A single rule that defines when and how to modify this object.
        /// </summary>
        [Serializable]
        public class BindingRule
        {
            [Tooltip("Shown in the NLM Object List and conflict reports.")]
            public string RuleName = "New Rule";

            [Tooltip("Conditions that must be satisfied for this rule to apply.")]
            public NarrativeConditionGroup Condition = new();

            [Tooltip("Property changes applied when Condition evaluates true.")]
            public List<NarrativePropertyOverride> Overrides = new();

            [Tooltip("If true, evaluation continues to lower-priority rules after this one fires.")]
            public bool FallThrough = false;
        }

        [Tooltip("Rules evaluated top-to-bottom. First match wins unless FallThrough is set.")]
        public List<BindingRule> Rules = new();

        [Tooltip("Hide this object from the NLM Object List to reduce visual clutter.")]
        public bool HideInNLMList = false;

        #region Preview Cache - NonSerialized so scene is never dirtied
        [NonSerialized] public bool CachedActive;
        [NonSerialized] public Vector3 CachedLocalPosition;
        [NonSerialized] public Vector3 CachedLocalScale;
        [NonSerialized] public Material[] CachedSharedMaterials;
        [NonSerialized] public bool IsStateCached = false;
        #endregion

        /// <summary>
        /// Caches the current state of this object for later restoration.
        /// </summary>
        /// <remarks>
        /// Called by <see cref="NLM_ScenePreviewApplicator"/> before applying preview state.
        /// Stores active state, transform, and materials.
        /// </remarks>
        public void CacheState()
        {
            if (IsStateCached) return;
            CachedActive = gameObject.activeSelf;
            CachedLocalPosition = transform.localPosition;
            CachedLocalScale = transform.localScale;

            var r = GetComponent<Renderer>();
            if (r != null)
            {
                CachedSharedMaterials = new Material[r.sharedMaterials.Length];
                Array.Copy(r.sharedMaterials, CachedSharedMaterials, CachedSharedMaterials.Length);
            }
            IsStateCached = true;
        }

        /// <summary>
        /// Restores the object to its cached state.
        /// </summary>
        /// <remarks>
        /// Called by <see cref="NLM_ScenePreviewApplicator"/> when ending preview.
        /// </remarks>
        public void RestoreState()
        {
            if (!IsStateCached) return;
            gameObject.SetActive(CachedActive);
            transform.localPosition = CachedLocalPosition;
            transform.localScale = CachedLocalScale;

            var r = GetComponent<Renderer>();
            if (r != null && CachedSharedMaterials != null)
                r.sharedMaterials = CachedSharedMaterials;

            IsStateCached = false;
        }

        /// <summary>
        /// Apply this binding's rules against the given state immediately (runtime use).
        /// </summary>
        /// <param name="state">The narrative state to evaluate against</param>
        /// <remarks>
        /// Does NOT cache state — call from your runtime NarrativeManager.
        /// For editor preview, use <see cref="NLM_ScenePreviewApplicator"/> instead.
        /// </remarks>
        public void ApplyState(NarrativeStateDefinition state)
        {
            var rules = NLM_Evaluator.ResolveBinding(this, state);
            foreach (var rule in rules)
                foreach (var ovr in rule.Overrides)
                    NLM_Applicator.ApplyOverride(gameObject, ovr);
        }
    }
}
