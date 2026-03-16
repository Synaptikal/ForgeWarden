using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.ZDHG
{
    /// <summary>
    /// Captures a static state of a heatmap for later comparison (diffing).
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Zone Density/Heatmap Snapshot",
        fileName = "NewHeatmapSnapshot")]
    public class HeatmapSnapshot : ScriptableObject
    {
        public string SceneName;
        public System.DateTime Timestamp;
        public float CellSize;
        public Bounds SceneBounds;
        
        [HideInInspector] public List<float> Scores = new();
        [HideInInspector] public List<Vector2Int> GridPositions = new();

        public void Capture(HeatmapResult result)
        {
            SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Timestamp = System.DateTime.Now;
            CellSize = result.CellSize;
            SceneBounds = result.SceneBounds;
            
            Scores.Clear();
            GridPositions.Clear();
            
            int cols = Mathf.CeilToInt(result.SceneBounds.size.x / result.CellSize);

            for (int i = 0; i < result.CellData.Length; i++)
            {
                var data = result.CellData[i];
                Scores.Add(data.DensityScore);
                
                var gp = new Vector2Int(i % cols, i / cols);
                GridPositions.Add(gp);
            }
        }
    }
}
