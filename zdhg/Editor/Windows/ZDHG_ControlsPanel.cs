using UnityEngine.UIElements;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>Cell size, desert threshold, overlay opacity, and grid line toggle.</summary>
    public class ZDHG_ControlsPanel : VisualElement
    {
        public ZDHG_ControlsPanel(HeatmapSettings settings)
        {
            AddToClassList("zdhg-panel");
            var header = new Label("Settings"); header.AddToClassList("zdhg-panel-header");

            var cellSizeField = new FloatField("Cell Size (m)") { value = settings.CellSize };
            cellSizeField.RegisterValueChangedCallback(e => settings.CellSize = e.newValue);

            var desertField = new Slider(0f, 1f)
                { label = "Desert Threshold", value = settings.DesertThreshold };
            desertField.RegisterValueChangedCallback(e => settings.DesertThreshold = e.newValue);

            var opacityField = new Slider(0f, 1f)
                { label = "Overlay Opacity", value = settings.OverlayOpacity };
            opacityField.RegisterValueChangedCallback(e =>
            {
                settings.OverlayOpacity = e.newValue;
                ZDHG_TextureRenderer.Invalidate();
            });

            var gridToggle = new Toggle("Show Grid Lines") { value = settings.ShowGridLines };
            gridToggle.RegisterValueChangedCallback(e => settings.ShowGridLines = e.newValue);

            Add(header);
            Add(cellSizeField);
            Add(desertField);
            Add(opacityField);
            Add(gridToggle);
        }
    }
}
