using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace LiveGameDev.ZDHG.Editor
{
    [InitializeOnLoad]
    internal static class ZDHG_NavMeshCacheInitializer
    {
        static ZDHG_NavMeshCacheInitializer()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => ZDHG_NavMeshReader.ClearCache();
            EditorSceneManager.sceneClosed += _ => ZDHG_NavMeshReader.ClearCache();
        }
    }
    /// <summary>
    /// Reads NavMesh triangulation and distributes triangle area coverage
    /// across grid cells using a Burst-compiled parallel job.
    ///
    /// IMPORTANT: NavMesh.CalculateTriangulation() returns MANAGED arrays.
    /// They are copied into NativeArray (Allocator.TempJob) before scheduling.
    /// This is a one-time allocation per heatmap generation run, bounded by
    /// NavMesh complexity — fast in practice. Always Dispose after Complete().
    /// </summary>
    internal static class ZDHG_NavMeshReader
    {
        private static NativeArray<Vector3> _cachedCentroids;
        private static NativeArray<float>   _cachedAreas;
        private static int _cachedHash = -1;

        /// <summary>
        /// Clears the triangulation cache. Call this if NavMesh changes without ZDHG noticing.
        /// </summary>
        public static void ClearCache()
        {
            if (_cachedCentroids.IsCreated) _cachedCentroids.Dispose();
            if (_cachedAreas.IsCreated) _cachedAreas.Dispose();
            _cachedHash = -1;
        }

        /// <summary>
        /// Returns a NativeArray of NavMesh coverage values (area sum) per cell.
        /// Caller is responsible for Disposing the returned array.
        /// </summary>
        internal static NativeArray<float> GetNavMeshCoverage(
            Bounds sceneBounds, float cellSize, Allocator allocator)
        {
            // ── Step 1: Get managed triangulation ────────────────
            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices == null || tri.vertices.Length == 0)
                return new NativeArray<float>(0, allocator);

            // Simple hash for "dirty" check
            int currentHash = tri.vertices.Length ^ tri.indices.Length;
            
            if (_cachedHash != currentHash || !_cachedCentroids.IsCreated)
            {
                ClearCache();
                int triCount = tri.indices.Length / 3;
                _cachedCentroids = new NativeArray<Vector3>(triCount, Allocator.Persistent);
                _cachedAreas = new NativeArray<float>(triCount, Allocator.Persistent);

                for (int i = 0; i < triCount; i++)
                {
                    var a = tri.vertices[tri.indices[i * 3]];
                    var b = tri.vertices[tri.indices[i * 3 + 1]];
                    var c = tri.vertices[tri.indices[i * 3 + 2]];
                    _cachedCentroids[i] = (a + b + c) / 3f;
                    _cachedAreas[i] = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                }
                _cachedHash = currentHash;
            }

            int cols = Mathf.CeilToInt(sceneBounds.size.x / cellSize);
            int rows = Mathf.CeilToInt(sceneBounds.size.z / cellSize);
            var nativeCoverage = new NativeArray<float>(cols * rows, allocator);
            var boundsMin = sceneBounds.min;

            try
            {
                // ── Step 3: Schedule Burst job ────────────────────────
                var job = new NavMeshCoverageJob
                {
                    TriangleCentroids = _cachedCentroids,
                    TriangleAreas     = _cachedAreas,
                    CellCoverage      = nativeCoverage,
                    BoundsMinX        = boundsMin.x,
                    BoundsMinZ        = boundsMin.z,
                    CellSize          = cellSize,
                    Cols              = cols,
                    Rows              = rows
                };

                job.Run(); // Serial Burst job for safety (atomic writes)
                return nativeCoverage;
            }
            catch
            {
                if (nativeCoverage.IsCreated) nativeCoverage.Dispose();
                throw;
            }
        }
    }

    /// <summary>
    /// Burst-compiled parallel job: for each NavMesh triangle, find its grid cell
    /// by centroid and accumulate triangle area into that cell's coverage value.
    /// </summary>
    [BurstCompile]
    internal struct NavMeshCoverageJob : IJob
    {
        [ReadOnly] public NativeArray<Vector3> TriangleCentroids;
        [ReadOnly] public NativeArray<float>   TriangleAreas;

        public NativeArray<float> CellCoverage;

        public float BoundsMinX;
        public float BoundsMinZ;
        public float CellSize;
        public int   Cols;
        public int   Rows;

        public void Execute()
        {
            for (int i = 0; i < TriangleCentroids.Length; i++)
            {
                var centroid = TriangleCentroids[i];
                int col = (int)((centroid.x - BoundsMinX) / CellSize);
                int row = (int)((centroid.z - BoundsMinZ) / CellSize);

                if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                    continue;

                int cellIndex = row * Cols + col;
                CellCoverage[cellIndex] += TriangleAreas[i];
            }
        }
    }
}
