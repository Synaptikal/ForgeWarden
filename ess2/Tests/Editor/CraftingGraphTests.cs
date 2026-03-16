using System.Collections.Generic;
using NUnit.Framework;
using LiveGameDev.Core;
using LiveGameDev.ESS;
using LiveGameDev.ESS.Editor;
using UnityEngine;

namespace LiveGameDev.ESS.Tests
{
    public class CraftingGraphTests
    {
        private static ItemDefinition CreateItem(string name)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.name = name; i.BaseValue = 1f; i.TargetCirculationPerPlayer = 10f;
            return i;
        }

        private static CraftingRecipeDefinition CreateRecipe(string outputName, params string[] inputNames)
        {
            var r  = ScriptableObject.CreateInstance<CraftingRecipeDefinition>();
            r.name = $"Recipe_{outputName}";
            r.OutputItem = CreateItem(outputName);
            r.OutputQuantity = 1;
            r.Inputs = new List<RecipeInputSlot>();
            foreach (var n in inputNames)
                r.Inputs.Add(new RecipeInputSlot { Item = CreateItem(n), Quantity = 1 });
            return r;
        }

        [Test]
        public void LinearChain_TopologicalOrder_Correct()
        {
            // Iron Ore → Iron Ingot → Iron Sword
            var recipes = new List<CraftingRecipeDefinition>
            {
                CreateRecipe("IronSword",  "IronIngot"),
                CreateRecipe("IronIngot",  "IronOre"),    // listed second intentionally
            };
            var graph  = new CraftingGraph(recipes);
            var report = new LGD_ValidationReport("Test");
            graph.Build(report);

            Assert.AreEqual(2, graph.TopologicalOrder.Count);
            // IronIngot must come before IronSword
            int ingotIdx = graph.TopologicalOrder.FindIndex(r => r.OutputItem.name == "IronIngot");
            int swordIdx = graph.TopologicalOrder.FindIndex(r => r.OutputItem.name == "IronSword");
            Assert.Less(ingotIdx, swordIdx, "IronIngot recipe must precede IronSword in topological order.");
        }

        [Test]
        public void DiamondDAG_NoCycleDetected()
        {
            //        Ore
            //       /               //    Ingot  Coal
            //       \   /
            //       Alloy
            var recipes = new List<CraftingRecipeDefinition>
            {
                CreateRecipe("Ingot",  "Ore"),
                CreateRecipe("Alloy",  "Ingot", "Coal"),
            };
            var graph  = new CraftingGraph(recipes);
            var report = new LGD_ValidationReport("Test");
            var status = graph.Build(report);
            Assert.AreEqual(ValidationStatus.Pass, status);
            Assert.AreEqual(2, graph.TopologicalOrder.Count);
        }

        [Test]
        public void CycleDetected_ReturnsCritical()
        {
            // A needs B, B needs A — classic cycle
            var a = ScriptableObject.CreateInstance<CraftingRecipeDefinition>();
            a.name = "Recipe_A";
            a.OutputItem = CreateItem("ItemA");
            var slotA = new RecipeInputSlot { Item = CreateItem("ItemB"), Quantity = 1 };
            a.Inputs = new List<RecipeInputSlot> { slotA };

            var b = ScriptableObject.CreateInstance<CraftingRecipeDefinition>();
            b.name = "Recipe_B";
            b.OutputItem = slotA.Item; // ItemB
            b.Inputs = new List<RecipeInputSlot>
                { new RecipeInputSlot { Item = a.OutputItem, Quantity = 1 } }; // needs ItemA

            var graph  = new CraftingGraph(new List<CraftingRecipeDefinition> { a, b });
            var report = new LGD_ValidationReport("Test");
            var status = graph.Build(report);

            Assert.AreEqual(ValidationStatus.Critical, status);
            Assert.IsTrue(report.HasCritical, "Cycle should produce Critical entry in report.");
        }

        [Test]
        public void SuccessRateAtSkill_ScalesWithLevel()
        {
            var recipe = ScriptableObject.CreateInstance<CraftingRecipeDefinition>();
            recipe.BaseSuccessRate = 0.9f;
            recipe.MasteryLevel    = 0.5f;

            Assert.AreEqual(0.9f, recipe.SuccessRateAtSkill(0.5f), 0.001f, "At mastery level should be base rate.");
            Assert.AreEqual(0.9f, recipe.SuccessRateAtSkill(1.0f), 0.001f, "Above mastery should still be base rate.");
            Assert.Less(recipe.SuccessRateAtSkill(0.25f), 0.9f, "Below mastery should reduce success rate.");
            Assert.AreEqual(0f, recipe.SuccessRateAtSkill(0f), 0.001f, "Zero skill = zero success rate.");
        }

        [Test]
        public void GiniCoefficient_PerfectEquality_IsZero()
        {
            float[] wealth = { 100f, 100f, 100f, 100f };
            float gini = EconomyMetrics.ComputeGini(wealth);
            Assert.AreEqual(0f, gini, 0.01f);
        }

        [Test]
        public void GiniCoefficient_MaxInequality_NearOne()
        {
            float[] wealth = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 100f };
            float gini = EconomyMetrics.ComputeGini(wealth);
            Assert.Greater(gini, 0.8f, "One player holds all wealth → near-max Gini.");
        }

        [Test]
        public void AHModel_OversupplyPushesDown()
        {
            var ah     = new AuctionHouseModel();
            var prices = new Dictionary<string, float> { ["Ore"] = 10f };
            var supply = new Dictionary<string, float> { ["Ore"] = 1000f }; // massive oversupply
            var demand = new Dictionary<string, float> { ["Ore"] = 10f };
            float pool = 100_000f;

            var closing = ah.Resolve(supply, demand, prices, ref pool, new System.Random(1));
            Assert.Less(closing["Ore"], 10f, "Price should fall under oversupply.");
        }

        [Test]
        public void AHModel_ScarcityPushesUp()
        {
            var ah     = new AuctionHouseModel();
            var prices = new Dictionary<string, float> { ["Gem"] = 10f };
            var supply = new Dictionary<string, float> { ["Gem"] = 1f };
            var demand = new Dictionary<string, float> { ["Gem"] = 1000f }; // massive demand
            float pool = 100_000f;

            var closing = ah.Resolve(supply, demand, prices, ref pool, new System.Random(1));
            Assert.Greater(closing["Gem"], 10f, "Price should rise under scarcity.");
        }
    }
}
