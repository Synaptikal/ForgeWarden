using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Utility for coordinate conversions between world space and the grid.
    /// </summary>
    public static class ZDHG_GridBuilder
    {
        /// <summary>Build the full grid of cell Bounds for a scene.</summary>
        public static List<Bounds> BuildGrid(Bounds sceneBounds, float cellSize)
        {
            var cells = new List<Bounds>();
            if (cellSize <= 0f) return cells;

            int cols = Mathf.FloorToInt(sceneBounds.size.x / cellSize);
            int rows = Mathf.FloorToInt(sceneBounds.size.z / cellSize);

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cells.Add(GridToWorld(new Vector2Int(c, r), sceneBounds, cellSize));

            return cells;
        }

        /// <summary>Convert a world-space XZ position to a grid coordinate.</summary>
        public static Vector2Int WorldToGrid(Vector3 worldPos, Bounds sceneBounds, float cellSize)
        {
            float relX = worldPos.x - sceneBounds.min.x;
            float relZ = worldPos.z - sceneBounds.min.z;
            
            int col = Mathf.FloorToInt(relX / cellSize);
            int row = Mathf.FloorToInt(relZ / cellSize);
            
            return new Vector2Int(col, row);
        }

        /// <summary>Convert a grid coordinate to world-space Bounds.</summary>
        public static Bounds GridToWorld(Vector2Int gridPos, Bounds sceneBounds, float cellSize)
        {
            float x = sceneBounds.min.x + gridPos.x * cellSize + cellSize * 0.5f;
            float z = sceneBounds.min.z + gridPos.y * cellSize + cellSize * 0.5f;
            float y = sceneBounds.center.y;
            
            return new Bounds(
                new Vector3(x, y, z),
                new Vector3(cellSize, sceneBounds.size.y, cellSize));
        }
    }
}
