using System.Collections.Generic;
using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// An ordered sequence of <see cref="NarrativeBeat"/> forming a narrative arc or timeline.
    /// </summary>
    /// <remarks>
    /// Create via: Assets > Create > Narrative Layer Manager > Layer Definition
    /// 
    /// A layer represents a complete narrative thread (e.g., Main Story, Side Quest, Character Arc).
    /// Each beat in the layer represents a state change in that narrative thread.
    /// </remarks>
    [CreateAssetMenu(
        menuName = "Narrative Layer Manager/Layer Definition",
        fileName = "NewNarrativeLayer")]
    public class NarrativeLayerDefinition : ScriptableObject
    {
        [Tooltip("Display name shown in the NLM window layer list.")]
        public string DisplayName;

        [Tooltip("Description of this narrative layer's purpose.")]
        [TextArea(1, 2)] public string Description;

        [Tooltip("Ordered story beats. Indices are auto-assigned on Validate().")]
        public List<NarrativeBeat> Beats = new();

        /// <summary>
        /// Gets the number of beats in this layer.
        /// </summary>
        public int BeatCount => Beats?.Count ?? 0;

        /// <summary>
        /// Gets a beat by its index in the sequence.
        /// </summary>
        /// <param name="index">The beat index (0-based)</param>
        /// <returns>The beat at the specified index, or null if out of range</returns>
        public NarrativeBeat GetBeat(int index)
        {
            if (Beats == null || index < 0 || index >= Beats.Count) return null;
            return Beats[index];
        }

        /// <summary>
        /// Rebuilds the Index property for all beats to match their list position.
        /// </summary>
        /// <remarks>
        /// Call this after reordering beats to ensure indices are correct.
        /// </remarks>
        public void RebuildIndices()
        {
            if (Beats == null) return;
            for (int i = 0; i < Beats.Count; i++) Beats[i].Index = i;
        }

        /// <summary>
        /// Validates this layer and all its beats, returning a comprehensive report.
        /// </summary>
        /// <returns>A validation report containing errors, warnings, or pass status</returns>
        /// <remarks>
        /// Checks for:
        /// - Empty beat list
        /// - Beats without names
        /// - Beats without states assigned
        /// - Invalid state definitions
        /// </remarks>
        public NLM_ValidationReport Validate()
        {
            RebuildIndices();
            var report = new NLM_ValidationReport("Layer");

            if (Beats == null || Beats.Count == 0)
            {
                report.Add(NLM_Status.Warning, "Layer", $"Layer '{name}' has no beats.");
                return report;
            }

            foreach (var beat in Beats)
            {
                if (string.IsNullOrWhiteSpace(beat.BeatName))
                    report.Add(NLM_Status.Warning, "Beat",
                        $"Layer '{name}': Beat at index {beat.Index} has no name.");

                if (beat.State == null)
                    report.Add(NLM_Status.Error, "Beat",
                        $"Layer '{name}': Beat '{beat.BeatName}' (index {beat.Index}) has no State assigned.",
                        suggestedFix: "Assign a NarrativeStateDefinition to this beat.");
                else
                {
                    var stateReport = beat.State.Validate();
                    foreach (var e in stateReport.Entries)
                        report.Add(e.Status, e.Tag, e.Message, e.AssetPath, e.SuggestedFix);
                }
            }

            if (!report.HasErrors && !report.HasWarnings)
                report.Add(NLM_Status.Pass, "Layer",
                    $"Layer '{name}' is valid — {Beats.Count} beat(s), all states assigned.");

            return report;
        }
    }
}
