using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.ZDHG
{
    /// <summary>
    /// Blittable data for Burst jobs.
    /// </summary>
    public struct DensityCellData
    {
        public float DensityScore;
        public bool  IsDesert;
        public int   ZoneIndex; // -1 if none
    }

    /// <summary>
    /// One grid cell in the heatmap (Managed wrapper).
    /// Stores world bounds and per-layer contributions.
    /// </summary>
    [Serializable]
    public class DensityCell
    {
        public Vector2Int GridPos      { get; }
        public Bounds     WorldBounds  { get; }
        
        public float DensityScore { get; set; }
        public bool  IsDesert     { get; set; }
        public string ZoneName    { get; set; }

        /// <summary>Per-layer density contributions keyed by LayerDefinition.LayerName.</summary>
        public Dictionary<string, float> LayerContributions { get; } = new();

        public DensityCell(Vector2Int gridPos, Bounds worldBounds)
        {
            GridPos    = gridPos;
            WorldBounds = worldBounds;
        }

        public void SyncFrom(DensityCellData data, string zoneName = null)
        {
            DensityScore = data.DensityScore;
            IsDesert     = data.IsDesert;
            ZoneName     = zoneName;
        }
    }
}
