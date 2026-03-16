using System;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.ZDHG
{
    /// <summary>
    /// Defines a named zone region in world space used by the heatmap generator.
    /// Supports AABB bounds or a custom polygon outline.
    /// </summary>
    [CreateAssetMenu(menuName = "ForgeWarden/ZDHG/Zone Definition", fileName = "NewZone")]
    public class ZoneDefinition : LGD_BaseDefinition
    {
        [Tooltip("Unique ID used in heatmap reports and exports.")]
        [SerializeField] public string ZoneId;

        [Tooltip("Axis-aligned bounding box for this zone (world space).")]
        [SerializeField] public Bounds ZoneBounds;

        [Tooltip("Optional custom polygon outline (world-space XZ points). Used when UseCustomPolygon is true.")]
        [SerializeField] public Vector3[] CustomPolygon = Array.Empty<Vector3>();

        [Tooltip("Use CustomPolygon instead of ZoneBounds for containment checks.")]
        [SerializeField] public bool UseCustomPolygon = false;

        [Tooltip("Target minimum density score for this zone. Below this = desert alert.")]
        [SerializeField] public float TargetDensityMin = 0.1f;

        [Tooltip("Target maximum density score for this zone. Above this = overcrowding alert.")]
        [SerializeField] public float TargetDensityMax = 1.0f;

        [Tooltip("Tags relevant to density scoring in this zone.")]
        [SerializeField] public string[] RelevantTags = Array.Empty<string>();

        /// <inheritdoc/>
        public override ValidationStatus Validate(LGD_ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(ZoneId))
                report.Add(ValidationStatus.Error, "ZoneDefinition", "ZoneId is empty.", name);

            if (!UseCustomPolygon && ZoneBounds.size == Vector3.zero)
                report.Add(ValidationStatus.Warning, "ZoneDefinition",
                    $"Zone '{ZoneId}' has zero-size bounds.", name);

            if (UseCustomPolygon && (CustomPolygon == null || CustomPolygon.Length < 3))
                report.Add(ValidationStatus.Error, "ZoneDefinition",
                    $"Zone '{ZoneId}': UseCustomPolygon is true but polygon has fewer than 3 points.", name);

            if (TargetDensityMin >= TargetDensityMax)
                report.Add(ValidationStatus.Error, "ZoneDefinition",
                    $"Zone '{ZoneId}': TargetDensityMin must be less than TargetDensityMax.", name);

            return report.OverallStatus;
        }

        /// <summary>Returns true if worldPoint falls within this zone (XZ plane only).</summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            if (!UseCustomPolygon)
                return ZoneBounds.Contains(new Vector3(worldPoint.x, ZoneBounds.center.y, worldPoint.z));

            return IsPointInPolygon(worldPoint, CustomPolygon);
        }

        private static bool IsPointInPolygon(Vector3 point, Vector3[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if ((polygon[i].z > point.z) != (polygon[j].z > point.z) &&
                    point.x < (polygon[j].x - polygon[i].x) *
                    (point.z - polygon[i].z) /
                    (polygon[j].z - polygon[i].z) + polygon[i].x)
                    inside = !inside;
                j = i;
            }
            return inside;
        }
    }
}
