using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.ESS
{
    /// <summary>
    /// Defines an item source (e.g., mob drops, quest rewards, gathering nodes).
    /// Sources inject items into the economy at configurable rates.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Economy/Source Definition",
        fileName = "NewSource")]
    public class SourceDefinition : LGD_BaseDefinition
    {
        [Header("Outputs")]
        [Tooltip("Items produced by this source.")]
        public ItemDefinition[] Outputs;

        [Tooltip("Relative weight for each output (normalized internally).")]
        public float[] OutputWeights;

        [Header("Rate")]
        [Tooltip("Base items per hour at level 0.")]
        [Min(0.1f)] public float BaseRate = 10f;

        [Tooltip("Rate multiplier at max level (1.0 = no scaling).")]
        [Min(0f)] public float LevelScaling = 1.5f;

        [Header("Engagement")]
        [Tooltip("Fraction of players who engage with this source per day (0-1).")]
        [Range(0f, 1f)] public float PlayerEngagementRate = 0.5f;

        /// <summary>
        /// Calculate output rate at a given normalized player level (0-1).
        /// </summary>
        public float GetRateAtLevel(float normalizedLevel)
        {
            return BaseRate * (1f + LevelScaling * normalizedLevel);
        }

        /// <inheritdoc/>
        public override ValidationStatus Validate(LGD_ValidationReport report)
        {
            if (Outputs == null || Outputs.Length == 0)
                report.Add(ValidationStatus.Error, "Source",
                    $"Source '{name}': No outputs defined.", name);
            else
            {
                for (int i = 0; i < Outputs.Length; i++)
                {
                    if (Outputs[i] == null)
                        report.Add(ValidationStatus.Error, "Source",
                            $"Source '{name}': Output at index {i} is null.", name);
                }
            }

            if (BaseRate <= 0f)
                report.Add(ValidationStatus.Error, "Source",
                    $"Source '{name}': BaseRate must be > 0.", name);

            if (PlayerEngagementRate < 0f || PlayerEngagementRate > 1f)
                report.Add(ValidationStatus.Error, "Source",
                    $"Source '{name}': PlayerEngagementRate must be 0-1.", name);

            return report.OverallStatus;
        }
    }
}