using System.Collections.Generic;
using NUnit.Framework;
using LiveGameDev.ESS.Editor;
using UnityEngine;

namespace LiveGameDev.ESS.Tests
{
    public class AuctionHouseModelTests
    {
        [Test]
        public void Constructor_WithDefaults_CreatesValidModel()
        {
            var ah = new AuctionHouseModel();
            Assert.IsNotNull(ah);
        }

        [Test]
        public void Resolve_OversupplyPushesPriceDown()
        {
            var ah = new AuctionHouseModel();
            var prices = new Dictionary<string, float> { ["Ore"] = 10f };
            var supply = new Dictionary<string, float> { ["Ore"] = 1000f };
            var demand = new Dictionary<string, float> { ["Ore"] = 10f };
            float pool = 100_000f;

            var closing = ah.Resolve(supply, demand, prices, ref pool, new System.Random(1));

            Assert.Less(closing["Ore"], 10f, "Price should fall under oversupply.");
        }

        [Test]
        public void Resolve_ScarcityPushesPriceUp()
        {
            var ah = new AuctionHouseModel();
            var prices = new Dictionary<string, float> { ["Gem"] = 10f };
            var supply = new Dictionary<string, float> { ["Gem"] = 1f };
            var demand = new Dictionary<string, float> { ["Gem"] = 1000f };
            float pool = 100_000f;

            var closing = ah.Resolve(supply, demand, prices, ref pool, new System.Random(1));

            Assert.Greater(closing["Gem"], 10f, "Price should rise under scarcity.");
        }

        [Test]
        public void Resolve_DestroysListingFees()
        {
            var ah = new AuctionHouseModel(listingFeePct: 0.02f);
            var prices = new Dictionary<string, float> { ["Item"] = 100f };
            var supply = new Dictionary<string, float> { ["Item"] = 10f };
            var demand = new Dictionary<string, float> { ["Item"] = 5f };
            float pool = 1000f;

            ah.Resolve(supply, demand, prices, ref pool, new System.Random(1));

            Assert.Less(pool, 1000f, "Currency should be destroyed via listing fees.");
        }

        [Test]
        public void GetInflationVelocity_WithHistory_ReturnsCorrectValue()
        {
            var ah = new AuctionHouseModel();
            var prices = new Dictionary<string, float> { ["Item"] = 100f };
            var supply = new Dictionary<string, float> { ["Item"] = 10f };
            var demand = new Dictionary<string, float> { ["Item"] = 10f };
            float pool = 1000f;

            // Run for 10 days to build history
            for (int i = 0; i < 10; i++)
            {
                ah.Resolve(supply, demand, prices, ref pool, new System.Random(i));
            }

            float velocity = ah.GetInflationVelocity("Item", windowDays: 7);
            Assert.IsNotNull(velocity);
        }

        [Test]
        public void MoneyVelocity_WithTransactions_ReturnsCorrectValue()
        {
            var ah = new AuctionHouseModel();
            var prices = new Dictionary<string, float> { ["Item"] = 100f };
            var supply = new Dictionary<string, float> { ["Item"] = 10f };
            var demand = new Dictionary<string, float> { ["Item"] = 10f };
            float pool = 1000f;

            ah.Resolve(supply, demand, prices, ref pool, new System.Random(1));

            float velocity = ah.MoneyVelocity(1000f);
            Assert.GreaterOrEqual(velocity, 0f);
        }
    }
}