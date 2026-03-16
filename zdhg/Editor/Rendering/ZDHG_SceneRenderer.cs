using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LiveGameDev.ZDHG;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Handles all Scene View rendering for ZDHG:
    /// zone boundaries and hover tooltip.
    /// Heavy per-cell heatmap rendering is delegated to ZDHG_TextureRenderer.
    /// </summary>
    internal static class ZDHG_SceneRenderer
    {
        internal static void DrawHeatmap(
            HeatmapResult result,
            HeatmapSettings settings,
            SceneView sceneView)
            => ZDHG_TextureRenderer.Draw(result, settings, sceneView);

        internal static void DrawZoneBoundaries(List<ZoneDefinition> zones)
        {
            if (zones == null) return;
            var prevColor = Handles.color;
            Handles.color = new Color(1f, 1f, 0f, 0.8f);

            foreach (var zone in zones)
            {
                if (zone == null) continue;

                if (zone.UseCustomPolygon && zone.CustomPolygon?.Length >= 2)
                {
                    for (int i = 0; i < zone.CustomPolygon.Length; i++)
                    {
                        var a = zone.CustomPolygon[i];
                        var b = zone.CustomPolygon[(i + 1) % zone.CustomPolygon.Length];
                        Handles.DrawLine(a, b, 2f);
                    }
                }
                else
                {
                    Handles.DrawWireCube(zone.ZoneBounds.center, zone.ZoneBounds.size);
                }

                Handles.Label(zone.ZoneBounds.center, zone.ZoneId ?? zone.name);
            }
            Handles.color = prevColor;
        }

        internal static void DrawCellTooltip(DensityCellData cell, Vector2Int gridPos, HeatmapSettings settings, Vector2 mousePosition)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>Grid: {gridPos}</b>");
            sb.AppendLine($"Density: {cell.DensityScore:P1}");
            if (cell.IsDesert) sb.AppendLine("<color=orange>[DESERT]</color>");
            
            if (settings.Zones != null && cell.ZoneIndex >= 0 && cell.ZoneIndex < settings.Zones.Count)
            {
                var zone = settings.Zones[cell.ZoneIndex];
                sb.AppendLine($"Zone: {zone.ZoneId}");
            }

            var style = new GUIStyle(EditorStyles.helpBox) { richText = true };
            float width = 200f;
            float height = 60f;

            Handles.BeginGUI();
            GUI.Label(new Rect(mousePosition.x + 15, mousePosition.y - height * 0.5f, width, height),
                sb.ToString(), style);
            Handles.EndGUI();
        }
    }
}
