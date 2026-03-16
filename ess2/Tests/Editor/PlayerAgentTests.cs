using System.Collections.Generic;
using NUnit.Framework;
using LiveGameDev.ESS;
using LiveGameDev.ESS.Editor;
using UnityEngine;

namespace LiveGameDev.ESS.Tests
{
    public class PlayerAgentTests
    {
        [Test]
        public void SimPlayerState_Constructor_CreatesValidState()
        {
            var player = new SimPlayerState(0, "TestProfile", 100f);

            Assert.AreEqual(0, player.PlayerId);
            Assert.AreEqual("TestProfile", player.ArcheType);
            Assert.AreEqual(100f, player.Currency);
            Assert.AreEqual(0f, player.NormalizedLevel);
        }

        [Test]
        public void SimPlayerState_AddItem_IncreasesQuantity()
        {
            var player = new SimPlayerState(0, "Test", 100f);

            player.AddItem("ItemA", 5);
            Assert.AreEqual(5, player.GetQuantity("ItemA"));

            player.AddItem("ItemA", 3);
            Assert.AreEqual(8, player.GetQuantity("ItemA"));
        }

        [Test]
        public void SimPlayerState_TryConsumeItems_WithSufficientQuantity_ReturnsTrue()
        {
            var player = new SimPlayerState(0, "Test", 100f);
            player.AddItem("ItemA", 10);

            bool result = player.TryConsumeItems("ItemA", 5);

            Assert.IsTrue(result);
            Assert.AreEqual(5, player.GetQuantity("ItemA"));
        }

        [Test]
        public void SimPlayerState_TryConsumeItems_WithInsufficientQuantity_ReturnsFalse()
        {
            var player = new SimPlayerState(0, "Test", 100f);
            player.AddItem("ItemA", 3);

            bool result = player.TryConsumeItems("ItemA", 5);

            Assert.IsFalse(result);
            Assert.AreEqual(3, player.GetQuantity("ItemA"));
        }

        [Test]
        public void SimPlayerState_TryConsumeItems_RemovesItemWhenQuantityZero()
        {
            var player = new SimPlayerState(0, "Test", 100f);
            player.AddItem("ItemA", 5);

            player.TryConsumeItems("ItemA", 5);

            Assert.AreEqual(0, player.GetQuantity("ItemA"));
            Assert.IsFalse(player.Inventory.ContainsKey("ItemA"));
        }

        [Test]
        public void SimPlayerState_GetCraftingSkill_ReturnsZeroForUnknownItem()
        {
            var player = new SimPlayerState(0, "Test", 100f);

            float skill = player.GetCraftingSkill("UnknownItem");

            Assert.AreEqual(0f, skill);
        }

        [Test]
        public void SimPlayerState_RaiseCraftingSkill_IncreasesSkill()
        {
            var player = new SimPlayerState(0, "Test", 100f);

            player.RaiseCraftingSkill("ItemA", 0.1f);
            Assert.AreEqual(0.1f, player.GetCraftingSkill("ItemA"));

            player.RaiseCraftingSkill("ItemA", 0.2f);
            Assert.AreEqual(0.3f, player.GetCraftingSkill("ItemA"));
        }

        [Test]
        public void SimPlayerState_RaiseCraftingSkill_ClampsToOne()
        {
            var player = new SimPlayerState(0, "Test", 100f);

            player.RaiseCraftingSkill("ItemA", 0.5f);
            player.RaiseCraftingSkill("ItemA", 0.6f);

            Assert.AreEqual(1f, player.GetCraftingSkill("ItemA"));
        }

        [Test]
        public void SimPlayerState_TotalInventoryValue_IncludesCurrency()
        {
            var player = new SimPlayerState(0, "Test", 100f);
            var prices = new Dictionary<string, float> { ["ItemA"] = 10f };

            float value = player.TotalInventoryValue(prices);

            Assert.AreEqual(100f, value);
        }

        [Test]
        public void SimPlayerState_TotalInventoryValue_IncludesInventory()
        {
            var player = new SimPlayerState(0, "Test", 50f);
            player.AddItem("ItemA", 5);
            var prices = new Dictionary<string, float> { ["ItemA"] = 10f };

            float value = player.TotalInventoryValue(prices);

            Assert.AreEqual(100f, value); // 50 currency + 5 * 10
        }
    }
}