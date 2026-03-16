using LiveGameDev.Core.Editor;
using UnityEngine.UIElements;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>Lists all ZoneDefinition assets and allows adding them to the active settings.</summary>
    public class ZDHG_ZonesPanel : VisualElement
    {
        private readonly HeatmapSettings _settings;

        public ZDHG_ZonesPanel(HeatmapSettings settings)
        {
            _settings = settings;
            AddToClassList("zdhg-panel");
            var header = new Label("Zones"); header.AddToClassList("zdhg-panel-header");
            var addAllBtn = new Button(AddAllZones) { text = "Add All Zone Assets" };
            Add(header);
            Add(addAllBtn);
        }

        private void AddAllZones()
        {
            _settings.Zones.Clear();
            var zones = LGD_AssetUtility.FindAllAssetsOfType<ZDHG.ZoneDefinition>();
            _settings.Zones.AddRange(zones);
        }
    }
}
