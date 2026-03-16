using UnityEngine;

namespace LiveGameDev.ZDHG
{
    /// <summary>
    /// Stores manually painted density weights for the ZDHG system.
    /// Weights are stored as a flat float array representing a 2D grid.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Zone Density/Zone Weight Texture",
        fileName = "NewZoneWeight")]
    public class ZoneWeightTexture : ScriptableObject
    {
        [HideInInspector] public float[] Weights;
        [HideInInspector] public int Width;
        [HideInInspector] public int Height;
        
        // World-space context for this weight set
        public Bounds SceneBounds;
        public float CellSize;

        public void Initialize(int width, int height, Bounds bounds, float cellSize)
        {
            Width = width;
            Height = height;
            SceneBounds = bounds;
            CellSize = cellSize;
            Weights = new float[width * height];
        }

        public float GetWeight(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 0f;
            return Weights[y * Width + x];
        }

        public void SetWeight(int x, int y, float value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            Weights[y * Width + x] = Mathf.Clamp01(value);
        }
    }
}
