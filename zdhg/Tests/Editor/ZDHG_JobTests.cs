using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using LiveGameDev.ZDHG.Editor;

namespace LiveGameDev.ZDHG.Tests
{
    public class ZDHG_JobTests
    {
        [Test]
        public void AccumulatePointsJob_SumsWeightsCorrectly()
        {
            var points = new NativeArray<float3>(3, Allocator.TempJob);
            var scores = new NativeArray<float>(4, Allocator.TempJob);
            try
            {
                points[0] = new float3(5, 0, 5);
                points[1] = new float3(5, 0, 5); // Overlap
                points[2] = new float3(15, 0, 15);

                var job = new AccumulatePointsJob
                {
                    PointPositions = points,
                    Weight = 1.0f,
                    BoundsMin = new float3(0, 0, 0),
                    CellSize = 10f,
                    Cols = 2,
                    Rows = 2,
                    Scores = scores
                };

                job.Run();

                Assert.AreEqual(2.0f, scores[0]); // (5,5) is in cell 0
                Assert.AreEqual(1.0f, scores[3]); // (15,15) is in cell 3
            }
            finally
            {
                if (points.IsCreated) points.Dispose();
                if (scores.IsCreated) scores.Dispose();
            }
        }

        [Test]
        public void NormalizeScoresJob_FiltersDeserts()
        {
            var scores = new NativeArray<float>(2, Allocator.TempJob);
            var deserts = new NativeArray<bool>(2, Allocator.TempJob);
            try
            {
                scores[0] = 10f;
                scores[1] = 1f;

                var job = new NormalizeScoresJob
                {
                    Scores = scores,
                    IsDesert = deserts,
                    MaxScore = 10f,
                    Threshold = 0.2f
                };

                job.Schedule(2, 1).Complete();

                Assert.AreEqual(1.0f, scores[0]);
                Assert.AreEqual(0.1f, scores[1]);
                Assert.IsFalse(deserts[0]);
                Assert.IsTrue(deserts[1]); // 0.1 < 0.2
            }
            finally
            {
                if (scores.IsCreated) scores.Dispose();
                if (deserts.IsCreated) deserts.Dispose();
            }
        }

        [Test]
        public void TextureBakingJob_MapsCorrectColors()
        {
            var scores = new NativeArray<float>(1, Allocator.TempJob);
            var gradient = new NativeArray<Color32>(2, Allocator.TempJob);
            var pixels = new NativeArray<Color32>(1, Allocator.TempJob);
            try
            {
                scores[0] = 1.0f; // Max heat
                gradient[0] = Color.blue;
                gradient[1] = Color.red;

                var job = new TextureBakingJob
                {
                    Scores = scores,
                    Gradient = gradient,
                    Resolution = 1,
                    GridCols = 1,
                    GridRows = 1,
                    OutputPixels = pixels
                };

                job.Schedule(1, 1).Complete();

                Assert.AreEqual((Color32)Color.red, pixels[0]);
            }
            finally
            {
                if (scores.IsCreated) scores.Dispose();
                if (gradient.IsCreated) gradient.Dispose();
                if (pixels.IsCreated) pixels.Dispose();
            }
        }

        [Test]
        public void AccumulatePointsJob_IgnoresOutOfBounds()
        {
            var points = new NativeArray<float3>(1, Allocator.TempJob);
            var scores = new NativeArray<float>(1, Allocator.TempJob);
            try
            {
                points[0] = new float3(-100, 0, -100); // Way out

                var job = new AccumulatePointsJob
                {
                    PointPositions = points,
                    Weight = 1f,
                    BoundsMin = new float3(0, 0, 0),
                    CellSize = 10f,
                    Cols = 1, Rows = 1,
                    Scores = scores
                };

                job.Run();
                Assert.AreEqual(0f, scores[0]);
            }
            finally
            {
                if (points.IsCreated) points.Dispose();
                if (scores.IsCreated) scores.Dispose();
            }
        }

        [Test]
        public void NormalizeScoresJob_HandlesZeroMaxScore()
        {
            var scores = new NativeArray<float>(1, Allocator.TempJob);
            var deserts = new NativeArray<bool>(1, Allocator.TempJob);
            try
            {
                scores[0] = 0f;

                var job = new NormalizeScoresJob
                {
                    Scores = scores, IsDesert = deserts, MaxScore = 0f, Threshold = 0.5f
                };
                job.Schedule(1, 1).Complete();

                Assert.IsTrue(deserts[0]);
                Assert.AreEqual(0f, scores[0]);
            }
            finally
            {
                if (scores.IsCreated) scores.Dispose();
                if (deserts.IsCreated) deserts.Dispose();
            }
        }

        [Test]
        public void FindMaxScoreJob_FindsCorrectMax()
        {
            var scores = new NativeArray<float>(3, Allocator.TempJob);
            var maxRef = new NativeReference<float>(Allocator.TempJob);
            try
            {
                scores[0] = 5f; scores[1] = 12f; scores[2] = 3f;

                new FindMaxScoreJob { Scores = scores, MaxScore = maxRef }.Run();
                Assert.AreEqual(12f, maxRef.Value);
            }
            finally
            {
                if (scores.IsCreated) scores.Dispose();
                if (maxRef.IsCreated) maxRef.Dispose();
            }
        }
    }
}
