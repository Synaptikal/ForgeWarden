using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Mutable runtime state for one simulated player instance.
    ///
    /// ESS creates one SimPlayerState per player (up to 10,000).
    /// For memory efficiency at large counts, use SimConfig.PlayerCount
    /// with a population sample size (default 500 representative players,
    /// results scaled to full count).
    /// </summary>
    public class SimPlayerState
    {
        // ── Identity ──────────────────────────────────────────────
        public int    PlayerId   { get; }
        public string ArcheType  { get; } // PlayerProfileDefinition.name

        // ── Progression ───────────────────────────────────────────
        /// <summary>Normalized level: 0 = new character, 1 = max level.</summary>
        public float NormalizedLevel { get; set; }

        /// <summary>Normalized crafting skill per recipe output category (keyed by ItemDefinition.name).</summary>
        public Dictionary<string, float> CraftingSkills { get; } = new();

        // ── Wealth ────────────────────────────────────────────────
        /// <summary>Current gold/currency held.</summary>
        public float Currency { get; set; }

        // ── Inventory ─────────────────────────────────────────────
        /// <summary>Current item quantities, keyed by ItemDefinition.name.</summary>
        public Dictionary<string, int> Inventory { get; } = new();

        // ── Auction House ─────────────────────────────────────────
        /// <summary>Items currently listed on the AH (name → quantity listed).</summary>
        public Dictionary<string, int> AhListings { get; } = new();

        // ── Behavior state ────────────────────────────────────────
        /// <summary>
        /// Tracks consecutive days spent farming a specific item (overfarming pressure).
        /// </summary>
        public Dictionary<string, int> ConsecutiveFarmDays { get; } = new();

        /// <summary>Days since last active play session (churn pressure).</summary>
        public int DaysSinceActive { get; set; }

        // ── Statistics ────────────────────────────────────────────
        public int TotalItemsCrafted    { get; set; }
        public int TotalItemsSoldOnAH   { get; set; }
        public float TotalCurrencyEarned { get; set; }
        public float TotalCurrencySpent  { get; set; }

        public SimPlayerState(int id, string archType, float startCurrency)
        {
            PlayerId   = id;
            ArcheType  = archType;
            Currency   = startCurrency;
        }

        // ── Inventory helpers ─────────────────────────────────────

        public int  GetQuantity(string itemName)
            => Inventory.TryGetValue(itemName, out var q) ? q : 0;

        public void AddItem(string itemName, int quantity)
        {
            Inventory.TryGetValue(itemName, out int existing);
            Inventory[itemName] = existing + quantity;
        }

        public bool TryConsumeItems(string itemName, int quantity)
        {
            if (GetQuantity(itemName) < quantity) return false;
            Inventory[itemName] -= quantity;
            if (Inventory[itemName] == 0) Inventory.Remove(itemName);
            return true;
        }

        public bool HasAllInputs(List<RecipeInputSlot> inputs)
        {
            foreach (var slot in inputs)
                if (slot.Item == null || GetQuantity(slot.Item.name) < slot.Quantity)
                    return false;
            return true;
        }

        public float GetCraftingSkill(string itemName)
            => CraftingSkills.TryGetValue(itemName, out var s) ? s : 0f;

        public void RaiseCraftingSkill(string itemName, float amount)
        {
            CraftingSkills.TryGetValue(itemName, out var s);
            CraftingSkills[itemName] = Mathf.Min(1f, s + amount);
        }

        public float TotalInventoryValue(Dictionary<string, float> currentPrices)
        {
            float total = Currency;
            foreach (var kvp in Inventory)
                if (currentPrices.TryGetValue(kvp.Key, out float price))
                    total += kvp.Value * price;
            return total;
        }
    }
}
