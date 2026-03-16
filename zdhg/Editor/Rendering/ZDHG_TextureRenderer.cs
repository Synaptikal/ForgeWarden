using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Draws the heatmap as a single procedural Texture2D on a scene-spanning quad.
    /// One draw call per frame — safe for large cell counts (1M+).
    /// Texture is rebuilt only when heatmap data changes.
    /// </summary>
    internal static class ZDHG_TextureRenderer
    {
        private static Texture2D    _cachedTexture;
        private static HeatmapResult _cachedResult;
        private static Material     _material;
        private static Mesh         _mesh;

        private static Material GetMaterial()
        {
            if (_material == null)
            {
                _material = new Material(Shader.Find("Unlit/Transparent"))
                    { name = "ZDHG_HeatmapMaterial" };
            }
            return _material;
        }

        /// <summary>
        /// Draw the heatmap overlay for the given SceneView camera.
        /// Call from SceneView.duringSceneGui.
        /// </summary>
        internal static void Draw(
            HeatmapResult result,
            HeatmapSettings settings,
            SceneView sceneView)
        {
            if (result == null || !result.IsCreated) return;

            // Rebuild texture only when result changes
            if (_cachedResult != result || _cachedTexture == null)
            {
                if (_cachedTexture == null)
                    _cachedTexture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);

                ZDHG_Generator.BakeToTextureNative(result, settings, _cachedTexture);
                _cachedResult  = result;
            }

            var bounds = result.SceneBounds;
            float yPos = bounds.center.y + (bounds.size.y * 0.5f); // Draw at top of bounds

            if (_mesh == null || _cachedResult != result)
            {
                if (_mesh != null) UnityEngine.Object.DestroyImmediate(_mesh);
                _mesh = BuildQuad(bounds, yPos);
            }

            var mat = GetMaterial();
            mat.mainTexture = _cachedTexture;
            mat.SetFloat("_ZWrite", 0);

            mat.SetPass(0);
            Graphics.DrawMeshNow(_mesh, Matrix4x4.identity);
        }

        internal static void Invalidate()
        {
            _cachedResult = null;
            if (_cachedTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(_cachedTexture);
                _cachedTexture = null;
            }
            if (_mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(_mesh);
                _mesh = null;
            }
        }

        private static Mesh BuildQuad(Bounds b, float y)
        {
            var mesh = new Mesh { name = "ZDHG_Quad" };
            mesh.vertices = new[]
            {
                new Vector3(b.min.x, y, b.min.z),
                new Vector3(b.max.x, y, b.min.z),
                new Vector3(b.max.x, y, b.max.z),
                new Vector3(b.min.x, y, b.max.z)
            };
            mesh.uv = new[]
            {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
