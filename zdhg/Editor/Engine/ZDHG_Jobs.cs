using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Burst-compiled jobs for spatial analysis and visualization.
    /// </summary>
    [BurstCompile]
    internal struct AccumulatePointsJob : IJob
    {
        [ReadOnly] public NativeArray<float3> PointPositions;
        [ReadOnly] public float               Weight;
        [ReadOnly] public float3              BoundsMin;
        [ReadOnly] public float               CellSize;
        [ReadOnly] public int                 Cols;
        [ReadOnly] public int                 Rows;

        // Using IJob (single thread) for now to fix the race condition 
        // until we implement a full multi-pass sorting solution.
        // This is still Burst-accelerated and sub-second for 100k points.
        public NativeArray<float> Scores;

        public void Execute()
        {
            for (int i = 0; i < PointPositions.Length; i++)
            {
                float3 pos = PointPositions[i];
                int col = (int)((pos.x - BoundsMin.x) / CellSize);
                int row = (int)((pos.z - BoundsMin.z) / CellSize);

                if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                    continue;

                int cellIndex = row * Cols + col;
                Scores[cellIndex] += Weight;
            }
        }
    }

    [BurstCompile]
    internal struct NormalizeScoresJob : IJobParallelFor
    {
        public NativeArray<float> Scores;
        public NativeArray<bool>  IsDesert;
        [ReadOnly] public float MaxScore;
        [ReadOnly] public float Threshold;

        public void Execute(int index)
        {
            if (MaxScore > 0f)
            {
                float normalized = Scores[index] / MaxScore;
                Scores[index] = normalized;
                IsDesert[index] = normalized < Threshold;
            }
            else
            {
                Scores[index] = 0f;
                IsDesert[index] = true;
            }
        }
    }

    [BurstCompile]
    internal struct FindMaxScoreJob : IJob
    {
        [ReadOnly] public NativeArray<float> Scores;
        public NativeReference<float> MaxScore;

        public void Execute()
        {
            float max = 0f;
            for (int i = 0; i < Scores.Length; i++)
            {
                if (Scores[i] > max) max = Scores[i];
            }
            MaxScore.Value = max;
        }
    }

    /// <summary>
    /// Zone metadata for Burst jobs.
    /// </summary>
    internal struct NativeZoneData
    {
        public Bounds Bounds;
        public int    PolyOffset;
        public int    PolyCount;
        public bool   UsePolygon;
    }

    /// <summary>
    /// Thread-safe progress counter for Burst jobs.
    /// </summary>
    internal struct NativeProgress
    {
        public NativeReference<int> Counter;
        
        public unsafe void Increment()
        {
            System.Threading.Interlocked.Increment(ref *(int*)Counter.GetUnsafeReadOnlyPtr());
        }
    }

    /// <summary>
    /// A simple grid-based spatial hash to speed up zone lookups.
    /// </summary>
    internal struct NativeSpatialHash
    {
        public float3 Min;
        public float3 Max;
        public int    Cols;
        public int    Rows;
        public float  CellSizeX;
        public float  CellSizeZ;

        [ReadOnly] public NativeArray<int> ZoneIndices;
        [ReadOnly] public NativeArray<int2> HashCells; // Offset, Count

        public int2 GetHashCell(float3 worldPos)
        {
            int col = (int)((worldPos.x - Min.x) / CellSizeX);
            int row = (int)((worldPos.z - Min.z) / CellSizeZ);
            col = math.clamp(col, 0, Cols - 1);
            row = math.clamp(row, 0, Rows - 1);
            return HashCells[row * Cols + col];
        }
    }

    [BurstCompile]
    internal struct AssignZonesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> CellPositions;
        [ReadOnly] public NativeArray<NativeZoneData> Zones;
        [ReadOnly] public NativeArray<float2> PolygonPoints;
        [ReadOnly] public NativeSpatialHash SpatialHash;
        public NativeProgress Progress;
        public NativeArray<int> ZoneIndices;

        public void Execute(int index)
        {
            float3 pos = CellPositions[index];
            float2 p2 = new float2(pos.x, pos.z);
            int assigned = -1;

            int2 hashCell = SpatialHash.GetHashCell(pos);
            for (int i = 0; i < hashCell.y; i++)
            {
                int zoneIdx = SpatialHash.ZoneIndices[hashCell.x + i];
                var zone = Zones[zoneIdx];
                if (!zone.Bounds.Contains(pos)) continue;

                if (!zone.UsePolygon)
                {
                    assigned = zoneIdx;
                    break;
                }

                if (IsPointInPolygon(p2, PolygonPoints, zone.PolyOffset, zone.PolyCount))
                {
                    assigned = zoneIdx;
                    break;
                }
            }
            ZoneIndices[index] = assigned;

            // Report progress every 1024 iterations to minimize atomic overhead
            if ((index & 1023) == 0) Progress.Increment();
        }

        private static bool IsPointInPolygon(float2 p, NativeArray<float2> points, int offset, int count)
        {
            bool inside = false;
            int j = count - 1;
            for (int i = 0; i < count; i++)
            {
                float2 pI = points[offset + i];
                float2 pJ = points[offset + j];

                if (((pI.y > p.y) != (pJ.y > p.y)) &&
                    (p.x < (pJ.x - pI.x) * (p.y - pI.y) / (pJ.y - pI.y) + pI.x))
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }
    }

    [BurstCompile]
    internal struct TextureBakingJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float>   Scores;
        [ReadOnly] public NativeArray<Color32> Gradient;
        [ReadOnly] public int Resolution;
        [ReadOnly] public int GridCols;
        [ReadOnly] public int GridRows;

        [WriteOnly] public NativeArray<Color32> OutputPixels;

        public void Execute(int index)
        {
            int texX = index % Resolution;
            int texY = index / Resolution;

            int col = (int)((float)texX / Resolution * GridCols);
            int row = (int)((float)texY / Resolution * GridRows);

            int cellIdx = row * GridCols + col;
            float score = (cellIdx >= 0 && cellIdx < Scores.Length) ? Scores[cellIdx] : 0f;

            // Sample gradient based on normalized score (0 to 1)
            int gradIdx = (int)math.clamp(score * (Gradient.Length - 1), 0, Gradient.Length - 1);
            OutputPixels[index] = Gradient[gradIdx];
        }
    }
}
