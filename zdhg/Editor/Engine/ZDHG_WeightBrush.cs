using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Handles manual density weight painting in the Scene View.
    /// Updates the ActiveWeightTexture in HeatmapSettings.
    /// </summary>
    [InitializeOnLoad]
    public static class ZDHG_WeightBrush
    {
        public static bool IsActive { get; set; }
        public static float BrushRadius = 25f;
        public static float BrushStrength = 0.1f;

        static ZDHG_WeightBrush()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sv)
        {
            if (!IsActive) return;

            var settings = ZDHG_SceneOverlay.CurrentSettings;
            if (settings == null || settings.ActiveWeightTexture == null) return;

            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                GUIUtility.hotControl = controlID;
                Paint(e.mousePosition, sv, settings.ActiveWeightTexture);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
            {
                Paint(e.mousePosition, sv, settings.ActiveWeightTexture);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                EditorUtility.SetDirty(settings.ActiveWeightTexture);
                e.Use();
            }

            // Draw brush cursor
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Handles.color = new Color(1, 1, 0, 0.5f);
                Handles.DrawWireDisc(hit.point, Vector3.up, BrushRadius);
                sv.Repaint();
            }

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 200, 100), "ZDHG Brush Settings", "window");
            BrushRadius = EditorGUILayout.Slider("Radius", BrushRadius, 1f, 100f);
            BrushStrength = EditorGUILayout.Slider("Strength", BrushStrength, 0.01f, 1f);
            if (GUILayout.Button("Clear All Weights"))
            {
                var tex = settings.ActiveWeightTexture;
                System.Array.Clear(tex.Weights, 0, tex.Weights.Length);
                EditorUtility.SetDirty(tex);
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void Paint(Vector2 mousePos, SceneView sv, ZoneWeightTexture tex)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            Vector3 center = hit.point;
            float rSq = BrushRadius * BrushRadius;

            // Optimized: only iterate cells within the brush's bounding box
            Vector2Int minGrid = ZDHG_GridBuilder.WorldToGrid(center - new Vector3(BrushRadius, 0, BrushRadius), tex.SceneBounds, tex.CellSize);
            Vector2Int maxGrid = ZDHG_GridBuilder.WorldToGrid(center + new Vector3(BrushRadius, 0, BrushRadius), tex.SceneBounds, tex.CellSize);

            int minX = Mathf.Clamp(minGrid.x, 0, tex.Width - 1);
            int maxX = Mathf.Clamp(maxGrid.x, 0, tex.Width - 1);
            int minY = Mathf.Clamp(minGrid.y, 0, tex.Height - 1);
            int maxY = Mathf.Clamp(maxGrid.y, 0, tex.Height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector3 cellPos = ZDHG_GridBuilder.GridToWorld(new Vector2Int(x, y), tex.SceneBounds, tex.CellSize).center;
                    float distSq = (cellPos - center).sqrMagnitude;
                    if (distSq < rSq)
                    {
                        float current = tex.GetWeight(x, y);
                        float dist = Mathf.Sqrt(distSq);
                        float falloff = 1f - dist / BrushRadius;
                        tex.SetWeight(x, y, current + falloff * BrushStrength);
                    }
                }
            }
        }
    }
}
