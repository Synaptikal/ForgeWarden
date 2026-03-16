using System;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    public enum LayerType
    {
        SpawnPoints,
        NavMesh,
        StaticObjects,
        ManualPaint
    }

    /// <summary>
    /// One data layer contributing to overall cell density scores.
    /// Each layer has an independent weight and can be toggled.
    /// </summary>
    [Serializable]
    public class LayerDefinition
    {
        [Tooltip("Display name shown in the Layers panel.")]
        public string LayerName = "New Layer";

        [Tooltip("What data source this layer reads.")]
        public LayerType Type = LayerType.SpawnPoints;

        [Tooltip("Contribution weight of this layer to total density (0–1).")]
        [Range(0f, 1f)] public float Weight = 1.0f;

        [Tooltip("Only count objects/spawns with these tags. Empty = all.")]
        public string[] FilterTags = Array.Empty<string>();

        [Tooltip("Toggle this layer on/off in the Scene View overlay.")]
        public bool IsVisible = true;
    }
}
