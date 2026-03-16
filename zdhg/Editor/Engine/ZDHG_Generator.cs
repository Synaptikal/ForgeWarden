using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using LiveGameDev.Core.Events;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    public static class ZDHG_Generator
    {
        public static async Task<HeatmapResult> GenerateHeatmapAsync(
            HeatmapSettings settings,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var report = new LGD_ValidationReport("ZDHG");
            ValidateSettings(settings, report);
            if (report.HasErrors)
                return new HeatmapResult(settings.CellSize, default, new NativeArray<DensityCellData>(0, Allocator.Persistent), report);

            progress?.Report(0.05f);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            var sceneBounds = ComputeSceneBounds();
            int cols = Mathf.Max(1, Mathf.CeilToInt(sceneBounds.size.x / settings.CellSize));
            int rows = Mathf.Max(1, Mathf.CeilToInt(sceneBounds.size.z / settings.CellSize));
            int cellCount = cols * rows;

            progress?.Report(0.10f);
            await Task.Yield();

            var totalScores = new NativeArray<float>(cellCount, Allocator.Persistent);
            var resultData  = new NativeArray<DensityCellData>(cellCount, Allocator.Persistent);
            var zoneIndices = new NativeArray<int>(cellCount, Allocator.Persistent);

            try 
            {
                // ── Path 1: Accumulate Base Scores ────────────────
                float layerStep = 0.7f / Mathf.Max(settings.Layers.Count, 1);
                for (int li = 0; li < settings.Layers.Count; li++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var layer = settings.Layers[li];
                    if (layer == null || !layer.IsVisible || layer.Weight <= 0f) continue;

                    progress?.Report(0.15f + li * layerStep);
                    await Task.Yield();

                    ProcessLayerNative(layer, totalScores, sceneBounds, settings.CellSize, cols, rows, settings);
                }

                // ── Path 2: Normalization & Desert Detection ──────
                progress?.Report(0.85f);
                await Task.Yield();
                
                using (var maxScoreRef = new NativeReference<float>(Allocator.TempJob))
                {
                    new FindMaxScoreJob { Scores = totalScores, MaxScore = maxScoreRef }.Run();

                    using (var isDesert = new NativeArray<bool>(cellCount, Allocator.TempJob))
                    {
                        new NormalizeScoresJob
                        {
                            Scores = totalScores,
                            IsDesert = isDesert,
                            MaxScore = maxScoreRef.Value,
                            Threshold = settings.DesertThreshold
                        }.Schedule(cellCount, 64).Complete();

                        // ── Path 3: Zone Assignment ───────────────────────
                        progress?.Report(0.90f);
                        await Task.Yield();

                        if (settings.Zones != null && settings.Zones.Count > 0)
                        {
                            var zoneData      = new NativeArray<NativeZoneData>(settings.Zones.Count, Allocator.TempJob);
                            var polygonPoints = new NativeArray<float2>(GetTotalPolyPoints(settings.Zones), Allocator.TempJob);
                            try
                            {
                                int polyOffset = 0;
                                for (int i = 0; i < settings.Zones.Count; i++)
                                {
                                    var zone = settings.Zones[i];
                                    if (zone == null) continue;

                                    zoneData[i] = new NativeZoneData
                                    {
                                        Bounds = zone.ZoneBounds,
                                        UsePolygon = zone.UseCustomPolygon,
                                        PolyOffset = polyOffset,
                                        PolyCount = zone.UseCustomPolygon ? zone.CustomPolygon.Length : 0
                                    };

                                    if (zone.UseCustomPolygon && zone.CustomPolygon != null)
                                    {
                                        for (int pi = 0; pi < zone.CustomPolygon.Length; pi++)
                                        {
                                            polygonPoints[polyOffset + pi] = new float2(zone.CustomPolygon[pi].x, zone.CustomPolygon[pi].z);
                                        }
                                        polyOffset += zone.CustomPolygon.Length;
                                    }
                                }

                                // ── Spatial Hashing Setup ────────────────────────
                                int hashCols = 16;
                                int hashRows = 16;
                                using (var hashIndices = new NativeList<int>(Allocator.TempJob))
                                using (var hashCells = new NativeArray<int2>(hashCols * hashRows, Allocator.TempJob))
                                {
                                    BuildSpatialHash(settings.Zones, sceneBounds, hashCols, hashRows, hashIndices, hashCells);

                                    var spatialHash = new NativeSpatialHash
                                    {
                                        Min = sceneBounds.min,
                                        Max = sceneBounds.max,
                                        Cols = hashCols,
                                        Rows = hashRows,
                                        CellSizeX = sceneBounds.size.x / hashCols,
                                        CellSizeZ = sceneBounds.size.z / hashRows,
                                        ZoneIndices = hashIndices.AsArray(),
                                        HashCells = hashCells
                                    };

                                    var cellPositions = new NativeArray<Unity.Mathematics.float3>(cellCount, Allocator.TempJob);
                                    var progressRef   = new NativeReference<int>(Allocator.TempJob);
                                    try
                                    {
                                        for (int i = 0; i < cellCount; i++)
                                        {
                                            var gp = new Vector2Int(i % cols, i / cols);
                                            cellPositions[i] = ZDHG_GridBuilder.GridToWorld(gp, sceneBounds, settings.CellSize).center;
                                        }

                                        var job = new AssignZonesJob
                                        {
                                            CellPositions = cellPositions,
                                            Zones = zoneData,
                                            PolygonPoints = polygonPoints,
                                            SpatialHash = spatialHash,
                                            Progress = new NativeProgress { Counter = progressRef },
                                            ZoneIndices = zoneIndices
                                        };

                                        var handle = job.Schedule(cellCount, 64);

                                        // Poll progress while the job runs
                                        int totalReports = Mathf.Max(1, cellCount / 1024);
                                        while (!handle.IsCompleted)
                                        {
                                            float subProgress = (float)progressRef.Value / totalReports;
                                            progress?.Report(0.90f + subProgress * 0.08f);
                                            await Task.Delay(16); // ~60fps poll
                                        }
                                        handle.Complete();
                                    }
                                    finally
                                    {
                                        cellPositions.Dispose();
                                        progressRef.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                zoneData.Dispose();
                                polygonPoints.Dispose();
                            }
                        }
                        else
                        {
                            // Initialize to -1 if no zones
                            for (int i = 0; i < cellCount; i++) zoneIndices[i] = -1;
                        }

                        // ── Path 4: Finalize Result Data ──────────────────
                        for (int i = 0; i < cellCount; i++)
                        {
                            float score = totalScores[i];
                            if (settings.IsDiffMode && settings.DiffSnapshot != null && i < settings.DiffSnapshot.Scores.Count)
                                score -= settings.DiffSnapshot.Scores[i];

                            resultData[i] = new DensityCellData
                            {
                                DensityScore = score,
                                IsDesert = isDesert[i],
                                ZoneIndex = zoneIndices[i]
                            };
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (resultData.IsCreated) resultData.Dispose();
                if (zoneIndices.IsCreated) zoneIndices.Dispose();
                throw;
            }
            finally
            {
                if (totalScores.IsCreated) totalScores.Dispose();
                if (zoneIndices.IsCreated) zoneIndices.Dispose();
            }

            progress?.Report(1.0f);
            var result = new HeatmapResult(settings.CellSize, sceneBounds, resultData, report);
            LGD_EventBus.Publish(new HeatmapGeneratedEvent("scene", report));
            return result;
        }

        private static void ProcessLayerNative(
            LayerDefinition layer,
            NativeArray<float> totalScores,
            Bounds sceneBounds,
            float cellSize,
            int cols,
            int rows,
            HeatmapSettings settings)
        {
            switch (layer.Type)
            {
                case LayerType.SpawnPoints:
                    var spawns = ZDHG_SpawnPointCollector.CollectSpawnPoints(layer.FilterTags);
                    if (spawns.Count > 0)
                    {
                        var nativePos = new NativeArray<Unity.Mathematics.float3>(spawns.Count, Allocator.TempJob);
                        try
                        {
                            for (int i = 0; i < spawns.Count; i++) nativePos[i] = spawns[i].Position;
                            new AccumulatePointsJob { PointPositions = nativePos, Weight = layer.Weight, BoundsMin = sceneBounds.min, CellSize = cellSize, Cols = cols, Rows = rows, Scores = totalScores }.Run();
                        }
                        finally { nativePos.Dispose(); }
                    }
                    break;

                case LayerType.NavMesh:
                    using (var navCoverage = ZDHG_NavMeshReader.GetNavMeshCoverage(sceneBounds, cellSize, Allocator.TempJob))
                    {
                        if (navCoverage.IsCreated)
                        {
                            for (int i = 0; i < totalScores.Length; i++)
                                totalScores[i] += navCoverage[i] * layer.Weight;
                        }
                    }
                    break;

                case LayerType.StaticObjects:
                    var objs = ZDHG_ObjectDensityReader.CollectObjectPositions(layer.FilterTags);
                    if (objs != null && objs.Count > 0)
                    {
                        var nativePos = new NativeArray<Unity.Mathematics.float3>(objs.Count, Allocator.TempJob);
                        try
                        {
                            for (int i = 0; i < objs.Count; i++) nativePos[i] = objs[i];
                            new AccumulatePointsJob { PointPositions = nativePos, Weight = layer.Weight, BoundsMin = sceneBounds.min, CellSize = cellSize, Cols = cols, Rows = rows, Scores = totalScores }.Run();
                        }
                        finally { nativePos.Dispose(); }
                    }
                    break;
                case LayerType.ManualPaint:
                    if (settings.ActiveWeightTexture != null)
                    {
                        for (int i = 0; i < totalScores.Length; i++)
                        {
                            int col = i % cols;
                            int row = i / cols;
                            totalScores[i] += settings.ActiveWeightTexture.GetWeight(col, row) * layer.Weight;
                        }
                    }
                    break;
            }
        }

        public static void BakeToTextureNative(HeatmapResult result, HeatmapSettings settings, Texture2D tex)
        {
            if (result == null || !result.IsCreated) return;

            int res = tex.width;
            int gradRes = 256;

            var pixels   = new NativeArray<Color32>(res * res, Allocator.TempJob);
            var scores   = new NativeArray<float>(result.CellData.Length, Allocator.TempJob);
            var gradient = new NativeArray<Color32>(gradRes, Allocator.TempJob);
            try
            {
                // Bake managed gradient into native array
                for (int i = 0; i < gradRes; i++)
                {
                    gradient[i] = settings.ColorGradient.Evaluate((float)i / (gradRes - 1));
                }

                // Extract scores for the baking job
                for (int i = 0; i < scores.Length; i++)
                    scores[i] = result.CellData[i].DensityScore;

                var job = new TextureBakingJob
                {
                    Scores = scores,
                    Gradient = gradient,
                    Resolution = res,
                    GridCols = Mathf.CeilToInt(result.SceneBounds.size.x / result.CellSize),
                    GridRows = Mathf.CeilToInt(result.SceneBounds.size.z / result.CellSize),
                    OutputPixels = pixels
                };

                job.Schedule(res * res, 64).Complete();

                tex.SetPixelData(pixels, 0);
                tex.Apply();
            }
            finally
            {
                pixels.Dispose();
                scores.Dispose();
                gradient.Dispose();
            }
        }

        private static void BuildSpatialHash(
            List<ZoneDefinition> zones,
            Bounds sceneBounds,
            int cols,
            int rows,
            NativeList<int> hashIndices,
            NativeArray<int2> hashCells)
        {
            float cellSizeX = sceneBounds.size.x / cols;
            float cellSizeZ = sceneBounds.size.z / rows;
            Vector3 min = sceneBounds.min;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int cellIndex = r * cols + c;
                    int offset = hashIndices.Length;
                    int count = 0;

                    Bounds cellBounds = new Bounds(
                        min + new Vector3((c + 0.5f) * cellSizeX, 0, (r + 0.5f) * cellSizeZ),
                        new Vector3(cellSizeX, 1000f, cellSizeZ)
                    );

                    for (int zi = 0; zi < zones.Count; zi++)
                    {
                        var zone = zones[zi];
                        if (zone == null) continue;

                        if (zone.ZoneBounds.Intersects(cellBounds))
                        {
                            hashIndices.Add(zi);
                            count++;
                        }
                    }

                    hashCells[cellIndex] = new int2(offset, count);
                }
            }
        }

        private static int GetTotalPolyPoints(List<ZoneDefinition> zones)
        {
            if (zones == null) return 0;
            int total = 0;
            foreach (var z in zones)
            {
                if (z != null && z.UseCustomPolygon && z.CustomPolygon != null)
                    total += z.CustomPolygon.Length;
            }
            return total;
        }

        private static void ValidateSettings(HeatmapSettings settings, LGD_ValidationReport report)
        {
            if (settings.CellSize < 0.1f) report.Add(ValidationStatus.Error, "Settings", "Cell Size too small (<0.1m).");
            if (settings.Layers.Count == 0) report.Add(ValidationStatus.Warning, "Settings", "No layers defined.");
        }

        private static Bounds ComputeSceneBounds()
        {
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (renderers.Length == 0) return new Bounds(Vector3.zero, new Vector3(100, 10, 100));

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            
            // Flatten Y
            b.center = new Vector3(b.center.x, 0, b.center.z);
            b.size = new Vector3(b.size.x, 10f, b.size.z);
            return b;
        }

        public static void ExportPng(HeatmapResult result, HeatmapSettings settings, string outputPath)
        {
            var tex = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            BakeToTextureNative(result, settings, tex);
            var bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            LGD_PathUtility.EnsureDirectoryExists(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, bytes);
            AssetDatabase.Refresh();
        }

        public static void ExportCsv(HeatmapResult result, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Index,Density,IsDesert,ZoneIndex");
            for (int i = 0; i < result.TotalCells; i++)
            {
                var data = result.CellData[i];
                sb.AppendLine($"{i},{data.DensityScore:F4},{data.IsDesert},{data.ZoneIndex}");
            }
            LGD_PathUtility.EnsureDirectoryExists(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, sb.ToString());
            AssetDatabase.Refresh();
        }

        public static void ExportMarkdown(HeatmapResult result, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ZDHG Density Report");
            sb.AppendLine($"**Total Cells:** {result.TotalCells:N0}");
            sb.AppendLine($"**Desert Cells:** {result.DesertCellCount:N0}");
            File.WriteAllText(outputPath, sb.ToString());
            AssetDatabase.Refresh();
        }
    }
}
