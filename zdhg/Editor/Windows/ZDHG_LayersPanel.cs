using UnityEngine.UIElements;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>Shows the list of density layers with toggles and weight sliders.</summary>
    public class ZDHG_LayersPanel : VisualElement
    {
        private readonly HeatmapSettings _settings;

        public ZDHG_LayersPanel(HeatmapSettings settings)
        {
            _settings = settings;
            AddToClassList("zdhg-panel");
            var header = new Label("Density Layers"); header.AddToClassList("zdhg-panel-header");
            var addBtn = new Button(AddLayer) { text = "+ Add Layer" };
            Add(header);
            Add(addBtn);
            Rebuild();
        }

        private void Rebuild()
        {
            // Remove all children except header and button (first two)
            while (childCount > 2) RemoveAt(2);

            for (int i = 0; i < _settings.Layers.Count; i++)
            {
                var layer = _settings.Layers[i];
                var row   = new VisualElement(); row.AddToClassList("zdhg-layer-row");

                var toggle = new Toggle(layer.LayerName) { value = layer.IsVisible };
                toggle.RegisterValueChangedCallback(e => { layer.IsVisible = e.newValue; });

                var weight = new Slider(0f, 1f) { value = layer.Weight, label = "Weight" };
                weight.RegisterValueChangedCallback(e => { layer.Weight = e.newValue; });

                row.Add(toggle);
                row.Add(weight);
                Add(row);
            }
        }

        private void AddLayer()
        {
            _settings.Layers.Add(new LayerDefinition());
            Rebuild();
        }
    }
}
