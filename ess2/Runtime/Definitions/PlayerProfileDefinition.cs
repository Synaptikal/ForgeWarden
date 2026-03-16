using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.ESS
{
    /// <summary>
    /// Defines a player archetype (e.g., "Casual", "Hardcore", "Mixed").
    /// Used to model different player behaviors in the simulation.
    /// </summary>
    [CreateAssetMenu(
        menuName = "ForgeWarden/ESS/Player Profile Definition",
        fileName = "NewPlayerProfile")]
    public class PlayerProfileDefinition : LGD_BaseDefinition
    {
        [Header("Play Time")]
        [Tooltip("Average hours played per day.")]
        [Min(0.1f)] public float DailyPlayHours = 2f;

        [Tooltip("Efficiency multiplier (1.0 = average, >1.0 = more efficient).")]
        [Min(0.1f)] public float EfficiencyMultiplier = 1f;

        [Header("Progression")]
        [Tooltip("Levels gained per week (0-100 scale).")]
        [Range(0f, 100f)] public float LevelsPerWeek = 5f;

        [Header("Economy")]
        [Tooltip("Fraction of days this player participates in the auction house (0-1).")]
        [Range(0f, 1f)] public float AuctionHouseParticipationRate = 0.3f;

        [Header("Display")]
        [Tooltip("Display name for this profile.")]
        public string ProfileName;

        [Tooltip("Optional description for documentation.")]
        [TextArea(2, 4)]
        public new string Description;

        /// <inheritdoc/>
        public override ValidationStatus Validate(LGD_ValidationReport report)
        {
            if (DailyPlayHours <= 0f)
                report.Add(ValidationStatus.Error, "PlayerProfile",
                    $"Profile '{name}': DailyPlayHours must be > 0.", name);

            if (EfficiencyMultiplier <= 0f)
                report.Add(ValidationStatus.Error, "PlayerProfile",
                    $"Profile '{name}': EfficiencyMultiplier must be > 0.", name);

            if (LevelsPerWeek < 0f || LevelsPerWeek > 100f)
                report.Add(ValidationStatus.Error, "PlayerProfile",
                    $"Profile '{name}': LevelsPerWeek must be 0-100.", name);

            if (AuctionHouseParticipationRate < 0f || AuctionHouseParticipationRate > 1f)
                report.Add(ValidationStatus.Error, "PlayerProfile",
                    $"Profile '{name}': AuctionHouseParticipationRate must be 0-1.", name);

            return report.OverallStatus;
        }
    }
}