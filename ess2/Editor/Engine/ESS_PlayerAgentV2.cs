using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.ESS;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Simulates one player's full daily decision loop.
    ///
    /// Decision priority (evaluated top to bottom each day):
    ///   1. Basic needs (repair gear, buy consumables if near-empty)
    ///   2. Crafting queue (if player has ingredients for a profitable recipe)
    ///   3. Auction House sells (list items > threshold quantity)
    ///   4. Auction House buys (if item cheaper than self-farming cost)
    ///   5. Farming (fill inventory gaps using available sources)
    ///
    /// Each decision is bounded by DailyPlayHours.
    /// This model produces emergent hoarding, market cornering, and
    /// supply shocks from player-level behavior without hardcoding them.
    /// </summary>
    internal static class ESS_PlayerAgentV2
    {
        // Thresholds
        private const float HoardThreshold        = 3f;  // list if holding > 3× daily farming output
        private const float BuyVsFarmCostRatio     = 0.7f; // buy from AH if price < 70% of self-farm cost
        private const float SkillGainPerCraftBatch = 0.001f; // skill gain per craft attempt
        private const float ConsumableReserve      = 5f;   // player always tries to hold at least 5 consumables
        private const float MinCraftMargin         = 0.1f;  // only craft if output value > inputs value + 10%

        /// <summary>
        /// Execute one full day for a single player.
        ///
        /// Mutates player state (inventory, currency, skills, AH listings).
        /// Populates supplyListings and demandWanted aggregation dicts for
        /// the AuctionHouseModel to resolve at end-of-day.
        /// </summary>
        internal static void ExecuteDay(
            SimPlayerState player,
            PlayerProfileDefinition profile,
            SimConfig config,
            CraftingGraph graph,
            Dictionary<string, float> currentPrices,
            Dictionary<string, float> supplyListings,  // OUT: units listed on AH
            Dictionary<string, float> demandWanted,    // OUT: units wanted from AH
            System.Random rng)
        {
            float remainingHours = profile.DailyPlayHours;
            if (remainingHours <= 0f) return;

            // Update player level
            float levelGainPerDay = profile.LevelsPerWeek / 7f / 100f; // 100 max levels
            player.NormalizedLevel = Mathf.Min(1f, player.NormalizedLevel + levelGainPerDay);

            // ── 1. Basic needs ─────────────────────────────────────────────
            // Repair cost: proportional to level and daily hours (gear degrades)
            float repairCost = player.NormalizedLevel * profile.DailyPlayHours * 0.5f;
            player.Currency = Mathf.Max(0f, player.Currency - repairCost);

            // Buy consumables if below reserve
            foreach (var item in config.TrackedItems)
            {
                if (item?.Category == null) continue;
                if (!item.Category.CategoryName.Contains("Consumable")) continue;
                int held = player.GetQuantity(item.name);
                if (held >= ConsumableReserve) continue;
                int need = Mathf.CeilToInt(ConsumableReserve - held);
                currentPrices.TryGetValue(item.name, out float price);
                if (price <= 0f) continue;
                float cost = need * price;
                if (player.Currency >= cost)
                {
                    player.Currency -= cost;
                    player.AddItem(item.name, need);
                    player.TotalCurrencySpent += cost;
                    Accumulate(demandWanted, item.name, need);
                }
            }

            // ── 2. Crafting ────────────────────────────────────────────────
            // Try recipes in topological order — upstream items processed first
            foreach (var recipe in graph.TopologicalOrder)
            {
                if (remainingHours <= 0f) break;
                if (recipe?.OutputItem == null || recipe.Inputs == null) continue;

                // Is it profitable? Output value > sum of input values + margin
                currentPrices.TryGetValue(recipe.OutputItem.name, out float outPrice);
                float inputCost = 0f;
                foreach (var slot in recipe.Inputs)
                {
                    if (slot.Item == null) continue;
                    currentPrices.TryGetValue(slot.Item.name, out float inPrice);
                    inputCost += inPrice * slot.Quantity;
                }
                float outputValue = outPrice * recipe.OutputQuantity;
                bool  isProfitable = outputValue > inputCost * (1f + MinCraftMargin);

                if (!isProfitable) continue;

                // Do we have all inputs?
                if (!player.HasAllInputs(recipe.Inputs)) continue;

                // How many crafts can we do in remaining time?
                int maxAttempts = recipe.MaxAttemptsPerPlayer(remainingHours);
                if (maxAttempts <= 0) continue;

                float skill       = player.GetCraftingSkill(recipe.OutputItem.name);
                float successRate = recipe.SuccessRateAtSkill(skill);
                int   attempts    = Mathf.Max(1, maxAttempts / 2); // conservative: use half budget

                // Consume inputs
                bool consumed = true;
                for (int i = 0; i < attempts && consumed; i++)
                {
                    consumed = true;
                    foreach (var slot in recipe.Inputs)
                    {
                        if (slot.Item == null) continue;
                        if (!player.TryConsumeItems(slot.Item.name, slot.Quantity))
                        { consumed = false; break; }
                    }
                    if (!consumed) break;

                    // Pay crafting currency cost
                    if (recipe.CurrencyCost > 0f)
                    {
                        if (player.Currency < recipe.CurrencyCost) { consumed = false; break; }
                        player.Currency -= recipe.CurrencyCost;
                        player.TotalCurrencySpent += recipe.CurrencyCost;
                    }

                    // Roll success
                    bool success = rng.NextDouble() < successRate;
                    if (success)
                    {
                        player.AddItem(recipe.OutputItem.name, recipe.OutputQuantity);
                        player.TotalItemsCrafted += recipe.OutputQuantity;
                    }
                    else
                    {
                        // Return input items where flagged ReturnedOnFailure
                        foreach (var slot in recipe.Inputs)
                            if (slot.ReturnedOnFailure && slot.Item != null)
                                player.AddItem(slot.Item.name, slot.Quantity);
                    }

                    // Skill gain on every attempt
                    player.RaiseCraftingSkill(recipe.OutputItem.name, SkillGainPerCraftBatch);

                    // Byproducts
                    foreach (var slot in recipe.Inputs)
                    {
                        if (slot.ByProducts == null) continue;
                        foreach (var bp in slot.ByProducts)
                        {
                            if (bp?.Item == null) continue;
                            if (rng.NextDouble() < bp.Chance)
                                player.AddItem(bp.Item.name, bp.Quantity);
                        }
                    }
                }

                // Time consumed (rough)
                remainingHours -= attempts * recipe.CraftTimeSeconds / 3600f;
            }

            // ── 3. AH sells ────────────────────────────────────────────────
            foreach (var item in config.TrackedItems)
            {
                if (item == null) continue;
                int held = player.GetQuantity(item.name);
                if (held <= 0) continue;

                // Compute daily farming yield for this item (rough)
                float dailyFarmYield = EstimateDailyFarm(item, profile, config, player.NormalizedLevel);
                float listThreshold  = dailyFarmYield * HoardThreshold;

                if (held > listThreshold && rng.NextDouble() < profile.AuctionHouseParticipationRate)
                {
                    int toList = Mathf.FloorToInt(held - dailyFarmYield); // keep 1 day's supply
                    if (toList <= 0) continue;
                    toList = Mathf.Min(toList, held);
                    player.TryConsumeItems(item.name, toList);
                    player.AhListings.TryGetValue(item.name, out int existing);
                    player.AhListings[item.name] = existing + toList;
                    player.TotalItemsSoldOnAH   += toList;
                    Accumulate(supplyListings, item.name, toList);
                }
            }

            // ── 4. AH buys ─────────────────────────────────────────────────
            foreach (var item in config.TrackedItems)
            {
                if (item == null) continue;
                currentPrices.TryGetValue(item.name, out float price);
                if (price <= 0f) continue;

                float farmCost = EstimateFarmCost(item, profile, config, player.NormalizedLevel);
                if (farmCost <= 0f) continue;

                // Buy from AH if cheaper than farming
                if (price < farmCost * BuyVsFarmCostRatio)
                {
                    int alreadyHeld  = player.GetQuantity(item.name);
                    float targetHold = EstimateDailyFarm(item, profile, config, player.NormalizedLevel) * 2f;
                    int   toBuy      = Mathf.Max(0, Mathf.FloorToInt(targetHold - alreadyHeld));
                    if (toBuy <= 0) continue;

                    float totalCost = toBuy * price;
                    if (player.Currency < totalCost)
                        toBuy = Mathf.FloorToInt(player.Currency / price);
                    if (toBuy <= 0) continue;

                    player.Currency -= toBuy * price;
                    player.TotalCurrencySpent += toBuy * price;
                    player.AddItem(item.name, toBuy);
                    Accumulate(demandWanted, item.name, toBuy);
                }
            }

            // ── 5. Farming ─────────────────────────────────────────────────
            if (remainingHours > 0f)
            {
                foreach (var source in config.Sources)
                {
                    if (source?.Outputs == null) continue;
                    float rate = source.GetRateAtLevel(player.NormalizedLevel) *
                                 remainingHours * profile.EfficiencyMultiplier;

                    for (int i = 0; i < source.Outputs.Length; i++)
                    {
                        var item = source.Outputs[i];
                        if (item == null) continue;
                        float weight  = i < source.OutputWeights.Length ? source.OutputWeights[i] : 1f;
                        float jitter  = 0.85f + (float)rng.NextDouble() * 0.3f;
                        int   gained  = Mathf.FloorToInt(rate * weight * jitter);
                        if (gained <= 0) continue;
                        player.AddItem(item.name, gained);
                        player.ConsecutiveFarmDays.TryGetValue(item.name, out int streak);
                        player.ConsecutiveFarmDays[item.name] = streak + 1;
                    }
                }

                // Earn currency from farming (mob drops / quest gold)
                float goldPerHour = 5f * profile.EfficiencyMultiplier * (1f + player.NormalizedLevel);
                float jitterGold  = 0.9f + (float)rng.NextDouble() * 0.2f;
                float earned      = goldPerHour * remainingHours * jitterGold;
                player.Currency          += earned;
                player.TotalCurrencyEarned += earned;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static float EstimateDailyFarm(
            ItemDefinition item,
            PlayerProfileDefinition profile,
            SimConfig config,
            float normalizedLevel)
        {
            float total = 0f;
            foreach (var source in config.Sources)
            {
                if (source?.Outputs == null) continue;
                for (int i = 0; i < source.Outputs.Length; i++)
                    if (source.Outputs[i] == item)
                    {
                        float w = i < source.OutputWeights.Length ? source.OutputWeights[i] : 1f;
                        total  += source.GetRateAtLevel(normalizedLevel) * profile.DailyPlayHours * w;
                    }
            }
            return total;
        }

        private static float EstimateFarmCost(
            ItemDefinition item,
            PlayerProfileDefinition profile,
            SimConfig config,
            float normalizedLevel)
        {
            float yield = EstimateDailyFarm(item, profile, config, normalizedLevel);
            if (yield <= 0f) return float.MaxValue;
            float goldPerHour = 5f * profile.EfficiencyMultiplier * (1f + normalizedLevel);
            float goldPerItem = goldPerHour * profile.DailyPlayHours / yield;
            return goldPerItem;
        }

        private static void Accumulate(Dictionary<string, float> dict, string key, float value)
        {
            dict.TryGetValue(key, out float existing);
            dict[key] = existing + value;
        }
    }
}
