using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Unity 6 Scene View Overlay for ZDHG.
    /// Provides quick controls directly in the Scene View toolbar.
    /// The heavy heatmap quad is rendered via ZDHG_SceneRenderer on SceneView.duringSceneGui.
    /// </summary>
    [Overlay(typeof(SceneView), "Zone Density Heatmap", defaultDockZone = DockZone.TopToolbar)]
    public class ZDHG_SceneOverlay : ToolbarOverlay
    {
        public static bool          IsVisible    { get; private set; }
        public static HeatmapResult CurrentResult { get; set; }
        public static HeatmapSettings CurrentSettings { get; set; }

        private Toggle _visibilityToggle;

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems   = Align.Center;

            _visibilityToggle = new Toggle("Show Heatmap") { value = IsVisible };
            _visibilityToggle.RegisterValueChangedCallback(evt =>
            {
                IsVisible = evt.newValue;
                SceneView.RepaintAll();
            });

            var brushToggle = new Toggle("Weight Brush") { value = ZDHG_WeightBrush.IsActive };
            brushToggle.tooltip = "Enable manual density weight painting";
            brushToggle.RegisterValueChangedCallback(evt =>
            {
                ZDHG_WeightBrush.IsActive = evt.newValue;
                SceneView.RepaintAll();
            });

            var regenerateBtn = new Button(OnRegenerate) { text = "↺" };
            regenerateBtn.tooltip = "Regenerate heatmap";

            root.Add(_visibilityToggle);
            root.Add(brushToggle);
            root.Add(regenerateBtn);
            return root;
        }

        private void OnRegenerate()
        {
            ZDHG_MainWindow.RequestRegenerate();
        }

        [InitializeOnLoadMethod]
        private static void RegisterSceneGUI()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static DensityCellData _selectedCell;
        private static Vector2Int      _selectedGridPos;
        private static bool            _hasSelection;
        private static Vector2         _selectedMousePos;

        private static void OnSceneGUI(SceneView sv)
        {
            if (!IsVisible || CurrentResult == null || !CurrentResult.IsCreated || CurrentSettings == null) return;
            
            ZDHG_SceneRenderer.DrawHeatmap(CurrentResult, CurrentSettings, sv);
            ZDHG_SceneRenderer.DrawZoneBoundaries(CurrentSettings.Zones);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !ZDHG_WeightBrush.IsActive)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var gp = ZDHG_GridBuilder.WorldToGrid(hit.point, CurrentResult.SceneBounds, CurrentResult.CellSize);
                    int idx = CurrentResult.GetIndex(gp);
                    if (idx >= 0 && idx < CurrentResult.CellData.Length)
                    {
                        _selectedCell = CurrentResult.CellData[idx];
                        _selectedGridPos = gp;
                        _hasSelection = true;
                        _selectedMousePos = e.mousePosition;
                    }
                    else _hasSelection = false;
                    sv.Repaint();
                }
                else
                {
                    _hasSelection = false;
                    sv.Repaint();
                }
            }

            if (_hasSelection)
            {
                ZDHG_SceneRenderer.DrawCellTooltip(_selectedCell, _selectedGridPos, CurrentSettings, _selectedMousePos);
            }
        }
    }
}
