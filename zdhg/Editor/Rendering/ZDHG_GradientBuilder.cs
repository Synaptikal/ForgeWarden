using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>Utility for sampling a Gradient and producing cell colours at a given opacity.</summary>
    internal static class ZDHG_GradientBuilder
    {
        internal static Color Sample(Gradient gradient, float normalizedValue, float opacity)
        {
            var c = gradient.Evaluate(Mathf.Clamp01(normalizedValue));
            c.a = opacity;
            return c;
        }

        internal static Color DesertColor(float opacity)
            => new Color(0.4f, 0.4f, 0.4f, opacity * 0.5f);
    }
}
