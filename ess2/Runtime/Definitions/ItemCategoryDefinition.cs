using UnityEngine;
using LiveGameDev.Core;

namespace LiveGameDev.ESS
{
    /// <summary>
    /// Defines a category for grouping items (e.g., "Raw Materials", "Crafted Goods", "Consumables").
    /// Replaces hardcoded enums with extensible ScriptableObject-based categories.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Economy/Item Category Definition",
        fileName = "NewItemCategory")]
    public class ItemCategoryDefinition : LGD_BaseDefinition
    {
        [Tooltip("Display name for this category.")]
        public string CategoryName;

        [Tooltip("Optional description for documentation.")]
        [TextArea(2, 4)]
        public string Description;

        /// <inheritdoc/>
        public override ValidationStatus Validate(LGD_ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
                report.Add(ValidationStatus.Error, "ItemCategory",
                    $"ItemCategory '{name}' has no CategoryName.", name);

            return report.OverallStatus;
        }
    }
}