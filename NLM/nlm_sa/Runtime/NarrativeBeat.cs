using System;
using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// Represents a single moment or checkpoint in a narrative timeline.
    /// </summary>
    /// <remarks>
    /// A beat contains a narrative state snapshot and metadata for display in the NLM timeline.
    /// Beats are ordered within a <see cref="NarrativeLayerDefinition"/> to form a complete narrative arc.
    /// </remarks>
    [Serializable]
    public class NarrativeBeat
    {
        [Tooltip("Short name displayed on the timeline tick mark.")]
        public string BeatName;

        [Tooltip("Auto-assigned list position index. Do not modify manually.")]
        public int Index;

        [Tooltip("Narrative variable snapshot at this beat.")]
        public NarrativeStateDefinition State;

        [Tooltip("Designer notes shown in the beat inspector.")]
        [TextArea(2, 4)] public string DesignerNotes;

        [Tooltip("Color of this beat's tick mark on the NLM timeline.")]
        public Color TimelineColor = Color.white;
    }
}
