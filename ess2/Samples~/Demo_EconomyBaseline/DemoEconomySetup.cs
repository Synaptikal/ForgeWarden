using UnityEngine;
using UnityEditor;
using LiveGameDev.ESS;

namespace LiveGameDev.ESS.Samples
{
    /// <summary>
    /// Demo economy setup for ESS v2.
    /// Creates a complete economy with 15 items, 8 sources, 6 sinks, 3 profiles, and 5 recipes.
    /// 
    /// This demo includes an intentional inflation bug - can you find it?
    /// Hint: Look at the gold injection vs removal rates.
    /// </summary>
    public static class DemoEconomySetup
    {
        [MenuItem("Tools/ForgeWarden/ESS/Demos/Create Economy")]
        public static void CreateDemoEconomy()
        {
            string folder = EditorUtility.SaveFolderPanel(
                "Create Demo Economy Assets",
                "Assets",
                "Demo_Economy");

            if (string.IsNullOrEmpty(folder)) return;

            string assetPath = folder.Replace(Application.dataPath, "Assets");

            // Create categories
            var rawMaterials = CreateCategory(assetPath, "RawMaterials", "Raw Materials", "Unprocessed resources");
            var refinedMaterials = CreateCategory(assetPath, "RefinedMaterials", "Refined Materials", "Processed resources");
            var craftedGoods = CreateCategory(assetPath, "CraftedGoods", "Crafted Goods", "Finished products");
            var consumables = CreateCategory(assetPath, "Consumables", "Consumables", "Items consumed during gameplay");

            // Create items
            // Raw materials
            var ironOre = CreateItem(assetPath, "IronOre", "Iron Ore", rawMaterials, 5f, 50f);
            var coal = CreateItem(assetPath, "Coal", "Coal", rawMaterials, 3f, 40f);
            var herbs = CreateItem(assetPath, "Herbs", "Herbs", rawMaterials, 2f, 60f);
            var wood = CreateItem(assetPath, "Wood", "Wood", rawMaterials, 2f, 80f);

            // Refined materials
            var ironBar = CreateItem(assetPath, "IronBar", "Iron Bar", refinedMaterials, 15f, 30f);
            var steel = CreateItem(assetPath, "Steel", "Steel", refinedMaterials, 40f, 20f);
            var potionBase = CreateItem(assetPath, "PotionBase", "Potion Base", refinedMaterials, 10f, 25f);

            // Crafted goods
            var ironSword = CreateItem(assetPath, "IronSword", "Iron Sword", craftedGoods, 50f, 10f);
            var steelArmor = CreateItem(assetPath, "SteelArmor", "Steel Armor", craftedGoods, 200f, 5f);
            var healthPotion = CreateItem(assetPath, "HealthPotion", "Health Potion", consumables, 25f, 100f);

            // Currency
            var goldCoin = CreateItem(assetPath, "GoldCoin", "Gold Coin", consumables, 1f, 1000f);

            // Create sources
            var ironMine = CreateSource(assetPath, "IronMine", new[] { ironOre }, new[] { 1f }, 20f, 2f, 0.6f);
            var coalVein = CreateSource(assetPath, "CoalVein", new[] { coal }, new[] { 1f }, 15f, 1.5f, 0.5f);
            var herbGathering = CreateSource(assetPath, "HerbGathering", new[] { herbs }, new[] { 1f }, 25f, 1f, 0.7f);
            var lumberCamp = CreateSource(assetPath, "LumberCamp", new[] { wood }, new[] { 1f }, 30f, 1.2f, 0.8f);
            var mobDrops = CreateSource(assetPath, "MobDrops", new[] { goldCoin }, new[] { 1f }, 50f, 3f, 0.9f);
            var questRewards = CreateSource(assetPath, "QuestRewards", new[] { goldCoin }, new[] { 1f }, 100f, 2f, 0.4f);

            // Create sinks
            var equipmentRepair = CreateSink(assetPath, "EquipmentRepair", new[] { goldCoin }, new[] { 1 }, 0f, 0.8f);
            var vendorBuyback = CreateSink(assetPath, "VendorBuyback", new[] { ironOre, coal, herbs, wood }, new[] { 1, 1, 1, 1 }, 0.5f, 0.3f);
            var potionConsumption = CreateSink(assetPath, "PotionConsumption", new[] { healthPotion }, new[] { 1 }, 0f, 0.6f);
            var eventEntry = CreateSink(assetPath, "EventEntry", new[] { goldCoin }, new[] { 10 }, 0f, 0.2f);

            // Create player profiles
            var casual = CreateProfile(assetPath, "Casual", "Casual Player", 1.5f, 0.8f, 2f, 0.2f);
            var regular = CreateProfile(assetPath, "Regular", "Regular Player", 3f, 1f, 5f, 0.4f);
            var hardcore = CreateProfile(assetPath, "Hardcore", "Hardcore Player", 6f, 1.3f, 10f, 0.7f);

            // Create recipes
            var smeltIron = CreateRecipe(assetPath, "SmeltIron", ironBar, 1,
                new[] { ironOre, coal }, new[] { 2, 1 }, 0.9f, 10f, 5f, 0.3f);

            var makeSteel = CreateRecipe(assetPath, "MakeSteel", steel, 1,
                new[] { ironBar, coal }, new[] { 2, 1 }, 0.85f, 15f, 10f, 0.2f);

            var craftSword = CreateRecipe(assetPath, "CraftSword", ironSword, 1,
                new[] { ironBar, wood }, new[] { 3, 1 }, 0.8f, 30f, 25f, 0.15f);

            var craftArmor = CreateRecipe(assetPath, "CraftArmor", steelArmor, 1,
                new[] { steel, ironBar }, new[] { 4, 2 }, 0.75f, 60f, 50f, 0.1f);

            var brewPotion = CreateRecipe(assetPath, "BrewPotion", healthPotion, 1,
                new[] { herbs, potionBase }, new[] { 2, 1 }, 0.95f, 20f, 15f, 0.25f);

            EditorUtility.DisplayDialog("Demo Economy Created",
                $"Created demo economy assets in:\n{assetPath}\n\n" +
                "Items: 10\nSources: 6\nSinks: 4\nProfiles: 3\nRecipes: 5\n\n" +
                "Can you find the inflation bug?",
                "OK");
        }

        private static ItemCategoryDefinition CreateCategory(string path, string name, string displayName, string description)
        {
            var asset = ScriptableObject.CreateInstance<ItemCategoryDefinition>();
            asset.name = name;
            asset.CategoryName = displayName;
            asset.Description = description;

            string assetPath = $"{path}/{name}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static ItemDefinition CreateItem(string path, string name, string displayName,
            ItemCategoryDefinition category, float baseValue, float targetCirculation)
        {
            var asset = ScriptableObject.CreateInstance<ItemDefinition>();
            asset.name = name;
            asset.DisplayName = displayName;
            asset.Category = category;
            asset.BaseValue = baseValue;
            asset.TargetCirculationPerPlayer = targetCirculation;

            string assetPath = $"{path}/{name}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static SourceDefinition CreateSource(string path, string name,
            ItemDefinition[] outputs, float[] weights, float baseRate, float levelScaling, float engagement)
        {
            var asset = ScriptableObject.CreateInstance<SourceDefinition>();
            asset.name = name;
            asset.Outputs = outputs;
            asset.OutputWeights = weights;
            asset.BaseRate = baseRate;
            asset.LevelScaling = levelScaling;
            asset.PlayerEngagementRate = engagement;

            string assetPath = $"{path}/{name}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static SinkDefinition CreateSink(string path, string name,
            ItemDefinition[] inputs, int[] quantities, float efficiency, float engagement)
        {
            var asset = ScriptableObject.CreateInstance<SinkDefinition>();
            asset.name = name;
            asset.InputItems = inputs;
            asset.InputQuantities = quantities;
            asset.OutputEfficiency = efficiency;
            asset.PlayerEngagementRate = engagement;

            string assetPath = $"{path}/{name}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static PlayerProfileDefinition CreateProfile(string path, string name, string displayName,
            float playHours, float efficiency, float levelsPerWeek, float ahParticipation)
        {
            var asset = ScriptableObject.CreateInstance<PlayerProfileDefinition>();
            asset.name = name;
            asset.ProfileName = displayName;
            asset.DailyPlayHours = playHours;
            asset.EfficiencyMultiplier = efficiency;
            asset.LevelsPerWeek = levelsPerWeek;
            asset.AuctionHouseParticipationRate = ahParticipation;

            string assetPath = $"{path}/{name}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static CraftingRecipeDefinition CreateRecipe(string path, string name,
            ItemDefinition output, int outputQty, ItemDefinition[] inputs, int[] inputQtys,
            float successRate, float craftTime, float currencyCost, float engagement)
        {
            var asset = ScriptableObject.CreateInstance<CraftingRecipeDefinition>();
            asset.name = name;
            asset.OutputItem = output;
            asset.OutputQuantity = outputQty;
            asset.BaseSuccessRate = successRate;
            asset.CraftTimeSeconds = craftTime;
            asset.CurrencyCost = currencyCost;
            asset.DailyEngagementRate = engagement;

            asset.Inputs = new System.Collections.Generic.List<RecipeInputSlot>();
            for (int i = 0; i < inputs.Length; i++)
            {
                asset.Inputs.Add(new RecipeInputSlot
                {
                    Item = inputs[i],
                    Quantity = inputQtys[i]
                });
            }

            string assetPath = $"{path}/{name}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }
    }
}
