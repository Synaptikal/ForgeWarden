using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// UI panel displaying the list of available narrative layers.
    /// </summary>
    /// <remarks>
    /// Left panel of the NLM main window. Shows all NarrativeLayerDefinition assets
    /// and allows selection and creation of new layers.
    /// </remarks>
    public class NLM_LayerListPanel : VisualElement
    {
        private readonly Action<NarrativeLayerDefinition> _onSelected;
        private readonly ScrollView _scroll;
        private NarrativeLayerDefinition _active;

        /// <summary>
        /// Creates a new layer list panel.
        /// </summary>
        /// <param name="onSelected">Callback when a layer is selected</param>
        public NLM_LayerListPanel(Action<NarrativeLayerDefinition> onSelected)
        {
            _onSelected = onSelected;
            style.minWidth = 180;
            style.maxWidth = 220;
            style.borderRightWidth = 1;
            style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);
            style.paddingRight = 6;

            var header = new Label("Layers") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
            var refresh = new Button(Refresh) { text = "↺ Refresh" };
            var create = new Button(CreateNew) { text = "+ New Layer" };

            Add(header); Add(refresh); Add(create);
            _scroll = new ScrollView(); Add(_scroll);
        }

        /// <summary>
        /// Refreshes the layer list from the asset database.
        /// </summary>
        public void Refresh()
        {
            _scroll.Clear();
            var layers = NLM_EditorAssetUtility.FindAllAssetsOfType<NarrativeLayerDefinition>();
            foreach (var layer in layers)
            {
                var captured = layer;
                var isActive = ReferenceEquals(captured, _active);
                var btn = new Button(() => { _active = captured; _onSelected?.Invoke(captured); Refresh(); })
                {
                    text = layer.name,
                    style =
                    {
                        unityFontStyleAndWeight = isActive ? FontStyle.Bold : FontStyle.Normal,
                        backgroundColor = isActive
                            ? new StyleColor(new Color(0.2f, 0.35f, 0.55f))
                            : new StyleColor(Color.clear)
                    }
                };
                _scroll.Add(btn);
            }
        }

        private void CreateNew()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "New Narrative Layer", "NewNarrativeLayer", "asset",
                "Choose location for the new NarrativeLayerDefinition asset.");
            if (string.IsNullOrEmpty(path)) return;
            var asset = UnityEngine.ScriptableObject.CreateInstance<NarrativeLayerDefinition>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Refresh();
        }
    }
}
