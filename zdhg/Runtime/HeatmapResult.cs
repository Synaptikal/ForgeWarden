using System;
using System.Collections.Generic;
using LiveGameDev.Core;
using Unity.Collections;
using UnityEngine;

namespace LiveGameDev.ZDHG
{
    /// <summary>
    /// Immutable result of a completed heatmap generation run.
    /// Uses NativeArray for memory-efficient storage of 1M+ cells.
    /// Caller must call Dispose() when finished.
    /// </summary>
    public class HeatmapResult : IDisposable
    {
        public float                CellSize      { get; }
        public Bounds               SceneBounds   { get; }
        public LGD_ValidationReport Report        { get; }
        
        // Native data
        public NativeArray<DensityCellData> CellData { get; private set; }
        
        private readonly int _cols;
        private readonly int _rows;

        public int TotalCells => CellData.Length;
        public int DesertCellCount { get; private set; }

        public HeatmapResult(
            float cellSize,
            Bounds sceneBounds,
            NativeArray<DensityCellData> cellData,
            LGD_ValidationReport report)
        {
            CellSize    = cellSize;
            SceneBounds = sceneBounds;
            CellData    = cellData;
            Report      = report;

            _cols = Mathf.CeilToInt(SceneBounds.size.x / CellSize);
            _rows = Mathf.CeilToInt(SceneBounds.size.z / CellSize);

            // Calculate desert count in constructor (fast)
            int count = 0;
            for (int i = 0; i < CellData.Length; i++)
                if (CellData[i].IsDesert) count++;
            DesertCellCount = count;
        }

        public bool IsCreated => CellData.IsCreated;

        public DensityCellData GetData(Vector2Int gridPos)
        {
            int idx = GetIndex(gridPos);
            if (idx >= 0 && idx < CellData.Length) return CellData[idx];
            return default;
        }

        public int GetIndex(Vector2Int gridPos)
        {
            if (gridPos.x < 0 || gridPos.x >= _cols || gridPos.y < 0 || gridPos.y >= _rows)
                return -1;
            return gridPos.y * _cols + gridPos.x;
        }

        public void Dispose()
        {
            if (CellData.IsCreated) CellData.Dispose();
        }

        // For legacy UI support — only use for small subsets or rendering
        public List<DensityCell> GetAllCellsManaged()
        {
            var list = new List<DensityCell>(CellData.Length);
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _cols; col++)
                {
                    var data = CellData[row * _cols + col];
                    var worldPos = GridToWorld(new Vector2Int(col, row), SceneBounds, CellSize);
                    var bounds = new Bounds(worldPos, new Vector3(CellSize, SceneBounds.size.y, CellSize));
                    var cell = new DensityCell(new Vector2Int(col, row), bounds);
                    cell.SyncFrom(data);
                    list.Add(cell);
                }
            }
            return list;
        }

        private static Vector3 GridToWorld(Vector2Int gridPos, Bounds sceneBounds, float cellSize)
        {
            var min = sceneBounds.min;
            return new Vector3(
                min.x + gridPos.x * cellSize + cellSize * 0.5f,
                min.y,
                min.z + gridPos.y * cellSize + cellSize * 0.5f
            );
        }
    }
}
