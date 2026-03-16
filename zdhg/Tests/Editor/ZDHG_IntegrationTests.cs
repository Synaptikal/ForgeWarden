using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using LiveGameDev.ZDHG.Editor;

namespace LiveGameDev.ZDHG.Tests
{
    public class ZDHG_IntegrationTests
    {
        [UnityTest]
        public IEnumerator Generator_AsyncRun_CompletesWithValidResult()
        {
            var settings = new HeatmapSettings
            {
                CellSize = 10f,
                Layers = new System.Collections.Generic.List<LayerDefinition>()
            };

            var cts = new CancellationTokenSource();
            HeatmapResult result = default;

            var task = ZDHG_Generator.GenerateHeatmapAsync(settings, null, cts.Token);

            while (!task.IsCompleted)
                yield return null;

            try
            {
                Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
                result = task.Result;

                Assert.IsNotNull(result);
                Assert.IsTrue(result.IsCreated);
                Assert.AreEqual(100, result.CellData.Length); // 10x10
            }
            finally
            {
                if (result.IsCreated)
                    result.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator Generator_Cancellation_DisposesCorrectly()
        {
            var settings = new HeatmapSettings();
            var cts = new CancellationTokenSource();

            var task = ZDHG_Generator.GenerateHeatmapAsync(settings, null, cts.Token);
            cts.Cancel();

            while (!task.IsCompleted)
                yield return null;

            Assert.IsTrue(task.IsFaulted || task.IsCanceled,
                "Task should be faulted or canceled after cancellation");
        }

        [Test]
        public void HeatmapResult_GetIndex_ReturnsValues()
        {
            var data = new NativeArray<DensityCellData>(1, Allocator.Persistent);
            HeatmapResult result = default;
            try
            {
                data[0] = new DensityCellData { DensityScore = 0.5f, ZoneIndex = 1 };

                result = new HeatmapResult(10f, new Bounds(Vector3.zero, Vector3.one), data, null);

                Assert.AreEqual(0.5f, result.CellData[0].DensityScore);
                Assert.AreEqual(1, result.CellData[0].ZoneIndex);
            }
            finally
            {
                if (result.IsCreated)
                    result.Dispose();
                else if (data.IsCreated)
                    data.Dispose();
            }
        }
    }
}
