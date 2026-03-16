using System;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Represents a migration hint for upgrading a schema from one version to another.
    /// Used to document breaking changes and provide guidance for data migration.
    /// </summary>
    [Serializable]
    public class RsvMigrationHint
    {
        /// <summary>The target version this migration applies to (e.g., "2.0.0").</summary>
        [Tooltip("Target version for this migration (e.g., '2.0.0').")]
        public string TargetVersion;

        /// <summary>Human-readable description of what changed in this version.</summary>
        [Tooltip("Description of breaking changes and migration requirements.")]
        public string Description;

        /// <summary>Optional path to a migration script that can auto-migrate data.</summary>
        [Tooltip("Optional path to a C# script that can auto-migrate JSON data to this version.")]
        public string MigrationScriptPath;

        /// <summary>Whether this migration is required (breaking change) or optional.</summary>
        [Tooltip("Whether this migration is required (breaking change) or optional.")]
        public bool IsRequired;

        /// <summary>Date when this migration was created.</summary>
        [Tooltip("Date when this migration was created.")]
        public string CreatedDate;

        public RsvMigrationHint()
        {
            CreatedDate = DateTime.Now.ToString("yyyy-MM-dd");
        }

        public RsvMigrationHint(string targetVersion, string description, bool isRequired = true)
        {
            TargetVersion = targetVersion;
            Description = description;
            IsRequired = isRequired;
            CreatedDate = DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
}
