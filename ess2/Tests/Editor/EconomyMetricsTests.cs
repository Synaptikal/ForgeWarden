using System.Collections.Generic;
using NUnit.Framework;
using LiveGameDev.ESS.Editor;
using UnityEngine;

namespace LiveGameDev.ESS.Tests
{
    public class EconomyMetricsTests
    {
        [Test]
        public void ComputeGini_PerfectEquality_ReturnsZero()
        {
            float[] wealth = { 100f, 100f, 100f, 100f };
            float gini = EconomyMetrics.ComputeGini(wealth);
            Assert.AreEqual(0f, gini, 0.01f, "Perfect equality should have Gini = 0.");
        }

        [Test]
        public void ComputeGini_MaxInequality_ReturnsNearOne()
        {
            float[] wealth = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 100f };
            float gini = EconomyMetrics.ComputeGini(wealth);
            Assert.Greater(gini, 0.8f, "One player holding all wealth should have near-max Gini.");
        }

        [Test]
        public void ComputeGini_EmptyArray_ReturnsZero()
        {
            float[] wealth = { };
            float gini = EconomyMetrics.ComputeGini(wealth);
            Assert.AreEqual(0f, gini, "Empty array should return Gini = 0.");
        }

        [Test]
        public void ComputeGini_SinglePlayer_ReturnsZero()
        {
            float[] wealth = { 100f };
            float gini = EconomyMetrics.ComputeGini(wealth);
            Assert.AreEqual(0f, gini, "Single player should return Gini = 0.");
        }

        [Test]
        public void ComputeGini_AllZeroWealth_ReturnsZero()
        {
            float[] wealth = { 0f, 0f, 0f, 0f };
            float gini = EconomyMetrics.ComputeGini(wealth);
            Assert.AreEqual(0f, gini, "All zero wealth should return Gini = 0.");
        }

        [Test]
        public void Snapshot_CreatesValidDayMetrics()
        {
            var metrics = new EconomyMetrics();
            var players = new List<SimPlayerState>
            {
                new SimPlayerState(0, "Test", 100f)
            };
            var prices = new Dictionary<string, float> { ["Item"] = 10f };
            var ah = new AuctionHouseModel();
            var config = new SimConfig
            {
                TrackedItems = new[] { CreateItem("Item") },
                PlayerCount = 1
            };

            var dayMetrics = metrics.Snapshot(1, players, prices, ah, config);

            Assert.IsNotNull(dayMetrics);
            Assert.AreEqual(1, dayMetrics.Day);
            Assert.GreaterOrEqual(dayMetrics.GiniCoefficient, 0f);
        }

        private static ItemDefinition CreateItem(string name)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = name;
            item.BaseValue = 1f;
            item.TargetCirculationPerPlayer = 10f;
            return item;
        }
    }
}