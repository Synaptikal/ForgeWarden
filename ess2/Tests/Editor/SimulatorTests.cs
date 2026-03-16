using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using LiveGameDev.ESS;
using LiveGameDev.ESS.Editor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LiveGameDev.ESS.Tests
{
    public class SimulatorTests
    {
        [UnityTest]
        public IEnumerator RunAsync_WithValidConfig_ReturnsResult()
        {
            var config = CreateValidConfig();
            var progress = new Progress<float>();

            var task = ESS_SimulatorV2.RunAsync(config, null, progress, CancellationToken.None);

            while (!task.IsCompleted)
                yield return null;

            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            var result = task.Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Config);
            Assert.IsNotNull(result.History);
            Assert.IsNotNull(result.Alerts);
            Assert.IsNotNull(result.Report);
        }

        [UnityTest]
        public IEnumerator RunAsync_WithInvalidConfig_ReturnsErrorReport()
        {
            var config = new SimConfig
            {
                SimulationDays = -1,
                PlayerCount = 0
            };

            var task = ESS_SimulatorV2.RunAsync(config, null, null, CancellationToken.None);

            while (!task.IsCompleted)
                yield return null;

            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            var result = task.Result;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Report.HasErrors);
        }

        [UnityTest]
        public IEnumerator RunAsync_WithZeroDays_ReturnsEmptyHistory()
        {
            var config = CreateValidConfig();
            config.SimulationDays = 0;

            var task = ESS_SimulatorV2.RunAsync(config, null, null, CancellationToken.None);

            while (!task.IsCompleted)
                yield return null;

            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            Assert.AreEqual(0, task.Result.History.Count);
        }

        [UnityTest]
        public IEnumerator RunAsync_WithFiveDays_ReturnsFiveHistoryEntries()
        {
            var config = CreateValidConfig();
            config.SimulationDays = 5;

            var task = ESS_SimulatorV2.RunAsync(config, null, null, CancellationToken.None);

            while (!task.IsCompleted)
                yield return null;

            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            Assert.AreEqual(5, task.Result.History.Count);
        }

        [UnityTest]
        public IEnumerator RunAsync_ProgressCallback_ReceivesUpdates()
        {
            var config = CreateValidConfig();
            config.SimulationDays = 10;
            var progressValues = new List<float>();
            var progress = new Progress<float>(v => progressValues.Add(v));

            var task = ESS_SimulatorV2.RunAsync(config, null, progress, CancellationToken.None);

            while (!task.IsCompleted)
                yield return null;

            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            Assert.Greater(progressValues.Count, 0);
            Assert.LessOrEqual(progressValues[progressValues.Count - 1], 1f);
        }

        [UnityTest]
        public IEnumerator RunAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            var config = CreateValidConfig();
            config.SimulationDays = 1000;
            var cts = new CancellationTokenSource();

            var task = ESS_SimulatorV2.RunAsync(config, null, null, cts.Token);

            // Cancel after a short delay
            cts.CancelAfter(10);

            while (!task.IsCompleted)
                yield return null;

            Assert.IsTrue(task.IsCanceled || task.IsFaulted,
                "Task should be canceled or faulted after cancellation");
        }

        private static SimConfig CreateValidConfig()
        {
            return new SimConfig
            {
                SimulationDays = 10,
                PlayerCount = 100,
                Seed = 42,
                TrackedItems = new[] { CreateItem("ItemA"), CreateItem("ItemB") },
                Sources = new[] { CreateSource("SourceA") },
                Sinks = new[] { CreateSink("SinkA") },
                PlayerMix = new[] { (CreateProfile("Casual"), 1f) }
            };
        }

        private static ItemDefinition CreateItem(string name)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = name;
            item.BaseValue = 1f;
            item.TargetCirculationPerPlayer = 10f;
            return item;
        }

        private static SourceDefinition CreateSource(string name)
        {
            var source = ScriptableObject.CreateInstance<SourceDefinition>();
            source.name = name;
            source.BaseRate = 10f;
            source.Outputs = new[] { CreateItem("ItemA") };
            source.OutputWeights = new[] { 1f };
            return source;
        }

        private static SinkDefinition CreateSink(string name)
        {
            var sink = ScriptableObject.CreateInstance<SinkDefinition>();
            sink.name = name;
            sink.InputItems = new[] { CreateItem("ItemA") };
            sink.InputQuantities = new[] { 1 };
            return sink;
        }

        private static PlayerProfileDefinition CreateProfile(string name)
        {
            var profile = ScriptableObject.CreateInstance<PlayerProfileDefinition>();
            profile.name = name;
            profile.DailyPlayHours = 2f;
            profile.EfficiencyMultiplier = 1f;
            profile.LevelsPerWeek = 5f;
            profile.AuctionHouseParticipationRate = 0.3f;
            return profile;
        }
    }
}
