using System.Collections.Generic;
using LiveGameDev.ZDHG;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// All parameters governing a single heatmap generation run.
    /// Authored in the ZDHG_ControlsPanel and passed to ZDHG_Generator.
    /// </summary>
    [System.Serializable]
    public class HeatmapSettings
    {
        [Tooltip("World-space size of each grid cell in metres.")]
        public float CellSize = 10f;

        [Tooltip("Cells with normalised density below this are flagged as Desert.")]
        [Range(0f, 1f)] public float DesertThreshold = 0.05f;

        [Tooltip("Data layers contributing to density.")]
        public List<LayerDefinition> Layers = new();

        [Tooltip("Colour gradient applied to normalised density (0=cold, 1=hot).")]
        public Gradient ColorGradient = DefaultGradient();

        [Tooltip("Zones to evaluate density within. Can be empty.")]
        public List<ZoneDefinition> Zones = new();

        [Tooltip("Draw grid cell outlines in Scene View overlay.")]
        public bool ShowGridLines = false;

        [Tooltip("Snapshot to diff against.")]
        public HeatmapSnapshot DiffSnapshot;

        [Tooltip("Enable diff mode (Current - Snapshot).")]
        public bool IsDiffMode = false;

        [Tooltip("Active weight texture for manual painting.")]
        public ZoneWeightTexture ActiveWeightTexture;

        [Tooltip("Opacity of the Scene View overlay (0–1).")]
        [Range(0f, 1f)] public float OverlayOpacity = 0.65f;

        private static Gradient DefaultGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.8f), 0.0f),
                    new GradientColorKey(new Color(0.1f, 0.8f, 0.1f), 0.4f),
                    new GradientColorKey(new Color(0.9f, 0.6f, 0.0f), 0.7f),
                    new GradientColorKey(new Color(0.9f, 0.1f, 0.1f), 1.0f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return g;
        }
    }
}
