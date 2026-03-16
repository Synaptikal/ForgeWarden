using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.ESS
{
    /// <summary>
    /// Defines an item in the economy simulation.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Economy/Item Definition",
        fileName = "NewItem")]
    public class ItemDefinition : LGD_BaseDefinition
    {
        [Header("Category")]
        [SerializeField] public ItemCategoryDefinition Category;

        [Header("Economics")]
        [Tooltip("Base value for price calculations and vendor sell-back.")]
        [Min(0.01f)] public float BaseValue = 1f;

        [Tooltip("Target quantity in circulation per player (used for supply ratio calculations).")]
        [Min(0.1f)] public float TargetCirculationPerPlayer = 10f;

        [Header("Display")]
        [Tooltip("Display name for this item.")]
        public new string DisplayName;

        [Tooltip("Optional icon for UI display.")]
        public Sprite Icon;

        /// <inheritdoc/>
        public override ValidationStatus Validate(LGD_ValidationReport report)
        {
            if (Category == null)
                report.Add(ValidationStatus.Error, "Item",
                    $"Item '{name}' has no Category assigned.", name);

            if (BaseValue <= 0f)
                report.Add(ValidationStatus.Error, "Item",
                    $"Item '{name}': BaseValue must be > 0.", name);

            if (TargetCirculationPerPlayer <= 0f)
                report.Add(ValidationStatus.Error, "Item",
                    $"Item '{name}': TargetCirculationPerPlayer must be > 0.", name);

            return report.OverallStatus;
        }
    }
}