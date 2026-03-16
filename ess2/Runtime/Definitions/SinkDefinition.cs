using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.ESS
{
    /// <summary>
    /// Defines an item sink (e.g., crafting, vendor sell-back, event entry).
    /// Sinks remove items from the economy at configurable rates.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Economy/Sink Definition",
        fileName = "NewSink")]
    public class SinkDefinition : LGD_BaseDefinition
    {
        [Header("Inputs")]
        [Tooltip("Items consumed by this sink.")]
        public ItemDefinition[] InputItems;

        [Tooltip("Quantity of each input consumed per engagement.")]
        public int[] InputQuantities;

        [Header("Output")]
        [Tooltip("Efficiency of currency return (0 = no return, 1 = full value).")]
        [Range(0f, 1f)] public float OutputEfficiency = 0.5f;

        [Header("Engagement")]
        [Tooltip("Fraction of players who engage with this sink per day (0-1).")]
        [Range(0f, 1f)] public float PlayerEngagementRate = 0.3f;

        /// <inheritdoc/>
        public override ValidationStatus Validate(LGD_ValidationReport report)
        {
            if (InputItems == null || InputItems.Length == 0)
                report.Add(ValidationStatus.Error, "Sink",
                    $"Sink '{name}': No input items defined.", name);
            else
            {
                for (int i = 0; i < InputItems.Length; i++)
                {
                    if (InputItems[i] == null)
                        report.Add(ValidationStatus.Error, "Sink",
                            $"Sink '{name}': Input at index {i} is null.", name);

                    if (InputQuantities != null && i < InputQuantities.Length && InputQuantities[i] <= 0)
                        report.Add(ValidationStatus.Error, "Sink",
                            $"Sink '{name}': InputQuantity at index {i} must be > 0.", name);
                }
            }

            if (OutputEfficiency < 0f || OutputEfficiency > 1f)
                report.Add(ValidationStatus.Error, "Sink",
                    $"Sink '{name}': OutputEfficiency must be 0-1.", name);

            if (PlayerEngagementRate < 0f || PlayerEngagementRate > 1f)
                report.Add(ValidationStatus.Error, "Sink",
                    $"Sink '{name}': PlayerEngagementRate must be 0-1.", name);

            return report.OverallStatus;
        }
    }
}