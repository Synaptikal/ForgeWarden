using System;
using System.Collections.Generic;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.ESS
{
    [Serializable]
    public class RecipeByProduct
    {
        [Tooltip("The item produced as a side effect.")]
        public ItemDefinition Item;

        [Tooltip("Units produced per craft attempt.")]
        [Min(1)] public int Quantity = 1;

        [Tooltip("Probability (0–1) this byproduct appears on each craft.")]
        [Range(0f, 1f)] public float Chance = 1f;
    }

    [Serializable]
    public class RecipeInputSlot
    {
        public ItemDefinition Item;

        [Tooltip("Units consumed per single craft attempt.")]
        [Min(1)] public int Quantity = 1;

        [Tooltip("If true the item is returned on failure.")]
        public bool ReturnedOnFailure = false;

        [Tooltip("Optional byproducts from processing this specific input (e.g. Iron Scrap from smelting).")]
        public List<RecipeByProduct> ByProducts = new();
    }

    /// <summary>
    /// Defines a single crafting step in the economy dependency graph.
    ///
    /// A recipe is a directed edge from input items → output item.
    /// Multiple recipes sharing an output item are valid (alternative routes).
    ///
    /// The simulator builds a DAG from all recipes in the project and processes
    /// them in topological order each day tick so that upstream items are available
    /// before downstream recipes run.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Economy/Crafting Recipe",
        fileName = "NewCraftingRecipe")]
    public class CraftingRecipeDefinition : LGD_BaseDefinition
    {
        [Header("Output")]
        [SerializeField] public ItemDefinition OutputItem;
        [SerializeField, Min(1)] public int OutputQuantity = 1;

        [Header("Inputs")]
        [SerializeField] public List<RecipeInputSlot> Inputs = new();

        [Header("Craft Parameters")]
        [Tooltip("0–1. Roll per craft attempt; failure still consumes inputs unless ReturnedOnFailure.")]
        [SerializeField, Range(0f, 1f)] public float BaseSuccessRate = 0.9f;

        [Tooltip("Player skill level (0–1) at which success rate is at full.")]
        [SerializeField, Range(0f, 1f)] public float MasteryLevel = 0.5f;

        [Tooltip("Time in seconds per craft attempt (used to calculate daily throughput).")]
        [SerializeField, Min(1f)] public float CraftTimeSeconds = 30f;

        [Tooltip("Fraction of active crafters that engage with this recipe per day (0–1).")]
        [SerializeField, Range(0f, 1f)] public float DailyEngagementRate = 0.1f;

        [Header("Economics")]
        [Tooltip("Currency cost per craft attempt (0 = free).")]
        [SerializeField, Min(0f)] public float CurrencyCost = 0f;

        [Tooltip("Currency received when the player vendors the output instead of using it (0 = no vendor).")]
        [SerializeField, Min(0f)] public float VendorPrice = 0f;

        // ── Derived helpers ───────────────────────────────────────

        /// <summary>
        /// Maximum craft attempts per player per day based on craft time and play hours.
        /// </summary>
        public int MaxAttemptsPerPlayer(float dailyPlayHours)
            => Mathf.FloorToInt(dailyPlayHours * 3600f / Mathf.Max(CraftTimeSeconds, 1f));

        /// <summary>
        /// Success rate at a given player skill level.
        /// Below MasteryLevel it scales linearly; at or above mastery it is always BaseSuccessRate.
        /// </summary>
        public float SuccessRateAtSkill(float normalizedSkill)
        {
            if (normalizedSkill >= MasteryLevel) return BaseSuccessRate;
            return BaseSuccessRate * (normalizedSkill / Mathf.Max(MasteryLevel, 0.01f));
        }

        /// <inheritdoc/>
        public override ValidationStatus Validate(LGD_ValidationReport report)
        {
            if (OutputItem == null)
                report.Add(ValidationStatus.Error, "Recipe", $"Recipe '{name}': OutputItem is null.", name);

            if (Inputs == null || Inputs.Count == 0)
                report.Add(ValidationStatus.Error, "Recipe", $"Recipe '{name}': No inputs defined.", name);
            else
            {
                foreach (var slot in Inputs)
                    if (slot.Item == null)
                        report.Add(ValidationStatus.Error, "Recipe",
                            $"Recipe '{name}': An input slot has a null item.", name);
            }

            if (CraftTimeSeconds <= 0f)
                report.Add(ValidationStatus.Error, "Recipe",
                    $"Recipe '{name}': CraftTimeSeconds must be > 0.", name);

            return report.OverallStatus;
        }
    }
}
