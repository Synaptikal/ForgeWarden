using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Custom VisualElement: horizontal timeline scrubber for narrative beats.
    /// </summary>
    /// <remarks>
    /// <para>Interactions:</para>
    /// <list type="bullet">
    /// <item>Click = select beat</item>
    /// <item>Shift+click = set diff target (yellow ring)</item>
    /// <item>Left/Right arrows = step through beats when focused</item>
    /// </list>
    /// </remarks>
    public class NLM_TimelineTrack : VisualElement
    {
        private NarrativeLayerDefinition _layer;
        private int _selected = 0;
        private int _diff = -1;

        private readonly Action<int> _onSelect;
        private readonly Action<int> _onDiff;

        private const float H = 64f;
        private const float Pad = 28f;
        private const float R = 7f;
        private const float LabelH = 18f;

        /// <summary>
        /// Creates a new timeline track.
        /// </summary>
        /// <param name="onSelect">Callback when a beat is selected</param>
        /// <param name="onDiff">Callback when a diff target is set</param>
        public NLM_TimelineTrack(Action<int> onSelect, Action<int> onDiff)
        {
            _onSelect = onSelect;
            _onDiff = onDiff;

            style.height = H + LabelH;
            style.flexShrink = 0;
            focusable = true;

            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <summary>
        /// Sets the layer to display and resets selection.
        /// </summary>
        /// <param name="layer">The layer to display</param>
        public void SetLayer(NarrativeLayerDefinition layer)
        {
            _layer = layer;
            _selected = 0;
            _diff = -1;
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Sets the selected beat index.
        /// </summary>
        /// <param name="i">Beat index to select</param>
        public void SetSelected(int i) { _selected = i; MarkDirtyRepaint(); }

        /// <summary>
        /// Sets the diff target beat index.
        /// </summary>
        /// <param name="i">Beat index for diff comparison</param>
        public void SetDiff(int i) { _diff = i; MarkDirtyRepaint(); }

        #region Drawing

        private void Draw(MeshGenerationContext ctx)
        {
            int n = _layer?.BeatCount ?? 0;
            if (n == 0) return;

            var p = ctx.painter2D;
            var rect = contentRect;
            float useW = rect.width - Pad * 2f;
            float cy = H * 0.50f;

            // Baseline
            p.strokeColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            p.lineWidth = 2f;
            p.BeginPath();
            p.MoveTo(new Vector2(Pad, cy));
            p.LineTo(new Vector2(rect.width - Pad, cy));
            p.Stroke();

            for (int i = 0; i < n; i++)
            {
                var beat = _layer.GetBeat(i);
                float x = Pad + (n == 1 ? useW * 0.5f : (float)i / (n - 1) * useW);
                Color tc = beat?.TimelineColor ?? Color.white;
                bool sel = i == _selected;
                bool diff = i == _diff;

                // Connector segments
                if (i > 0)
                {
                    float prevX = Pad + (n == 1 ? useW * 0.5f : (float)(i - 1) / (n - 1) * useW);
                    p.strokeColor = new Color(tc.r, tc.g, tc.b, 0.35f);
                    p.lineWidth = 1.5f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(prevX + R, cy));
                    p.LineTo(new Vector2(x - R, cy));
                    p.Stroke();
                }

                // Outer ring for diff target
                if (diff)
                {
                    p.strokeColor = Color.yellow;
                    p.fillColor = new Color(0, 0, 0, 0);
                    p.lineWidth = 1.5f;
                    p.BeginPath();
                    p.Arc(new Vector2(x, cy), R + 5f, 0f, 360f);
                    p.Stroke();
                }

                // Main circle
                p.fillColor = sel ? tc : new Color(tc.r, tc.g, tc.b, 0.25f);
                p.strokeColor = tc;
                p.lineWidth = sel ? 2f : 1.5f;
                p.BeginPath();
                p.Arc(new Vector2(x, cy), R, 0f, 360f);
                p.Fill();
                p.Stroke();
            }
        }

        #endregion

        #region Input Handling

        private void OnPointerDown(PointerDownEvent evt)
        {
            int n = _layer?.BeatCount ?? 0;
            if (n == 0) return;
            this.Focus();

            float useW = contentRect.width - Pad * 2f;
            float click = evt.localPosition.x;
            int nearest = 0;
            float minD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                float x = Pad + (n == 1 ? useW * 0.5f : (float)i / (n - 1) * useW);
                float d = Mathf.Abs(x - click);
                if (d < minD) { minD = d; nearest = i; }
            }

            if (evt.shiftKey) { _diff = nearest; _onDiff?.Invoke(nearest); }
            else { _selected = nearest; _onSelect?.Invoke(nearest); }
            MarkDirtyRepaint();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            int n = _layer?.BeatCount ?? 0;
            if (n == 0) return;
            if (evt.keyCode == KeyCode.RightArrow && _selected < n - 1)
            { _selected++; _onSelect?.Invoke(_selected); MarkDirtyRepaint(); evt.StopPropagation(); }
            if (evt.keyCode == KeyCode.LeftArrow && _selected > 0)
            { _selected--; _onSelect?.Invoke(_selected); MarkDirtyRepaint(); evt.StopPropagation(); }
        }

        #endregion
    }
}
