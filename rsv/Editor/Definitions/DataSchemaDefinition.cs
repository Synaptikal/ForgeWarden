using System;
using System.Collections.Generic;
using System.Text;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// ScriptableObject representing the expected structure of a JSON payload.
    /// Author schemas in the RSV window or import from standard JSON Schema Draft 7 / 2020-12.
    /// </summary>
    [CreateAssetMenu(
        menuName = "ForgeWarden/RSV/Data Schema Definition",
        fileName = "NewSchema")]
    public class DataSchemaDefinition : LGD_BaseDefinition
    {
        [Tooltip("Unique identifier for this schema. Referenced by JsonSourceBinding and [RsvSchema] attribute.")]
        [SerializeField] public string SchemaId;

        [Tooltip("Semantic version of this schema (e.g. 1.0.0). Used for migration warnings.")]
        [SerializeField] public string Version = "1.0.0";

        [Tooltip("Migration hints for upgrading from previous versions.")]
        [SerializeField] public List<RsvMigrationHint> MigrationHints = new();

        [SerializeField] public List<RsvSchemaNode> RootNodes = new();

        /// <inheritdoc/>
        public override ValidationStatus Validate(LGD_ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(SchemaId))
                report.Add(ValidationStatus.Error, "Schema", "SchemaId is empty.", name);

            if (string.IsNullOrWhiteSpace(Version))
                report.Add(ValidationStatus.Warning, "Schema", "Version is empty.", name);

            if (RootNodes == null || RootNodes.Count == 0)
                report.Add(ValidationStatus.Warning, "Schema", "Schema has no root nodes defined.", name);

            ValidateNodes(RootNodes, report);
            return report.OverallStatus;
        }

        private void ValidateNodes(List<RsvSchemaNode> nodes, LGD_ValidationReport report)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Name))
                    report.Add(ValidationStatus.Error, "Schema", "A schema node has an empty name.", name);

                var c = node.Constraint;
                if (c.HasMinMax && c.Min > c.Max)
                    report.Add(ValidationStatus.Error, "Constraint",
                        $"Field '{node.Name}': Min ({c.Min}) is greater than Max ({c.Max}).", name);

                if (c.EnumValues?.Length == 1)
                    report.Add(ValidationStatus.Warning, "Constraint",
                        $"Field '{node.Name}': Enum with only 1 value — is this intentional?", name);

                ValidateNodes(node.Children, report);
            }
        }

        /// <summary>
        /// Generate a minimal valid JSON string from this schema for documentation / playground use.
        /// </summary>
        public string GenerateExampleJson()
        {
            var sb = new StringBuilder();
            AppendNodes(RootNodes, sb, 0);
            return sb.ToString();
        }

        private void AppendNodes(List<RsvSchemaNode> nodes, StringBuilder sb, int depth)
        {
            var indent  = new string(' ', depth * 2);
            var indent1 = new string(' ', (depth + 1) * 2);
            sb.AppendLine(indent + "{");
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var comma = i < nodes.Count - 1 ? "," : "";
                var value = GetExampleValue(node);
                sb.AppendLine($"{indent1}\"{node.Name}\": {value}{comma}");
            }
            sb.AppendLine(indent + "}");
        }

        private string GetExampleValue(RsvSchemaNode node)
        {
            if (!string.IsNullOrEmpty(node.Constraint?.DefaultValue))
            {
                if (node.Constraint.FieldType == RsvFieldType.String)
                {
                    var stringVal = node.Constraint.DefaultValue ?? "string";
                    var escaped = stringVal
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
                    return $"\"{escaped}\"";
                }
                return node.Constraint.DefaultValue;
            }
            return node.Constraint?.FieldType switch
            {
                RsvFieldType.String  => "\"example\"",
                RsvFieldType.Integer => "0",
                RsvFieldType.Number  => "0.0",
                RsvFieldType.Boolean => "false",
                RsvFieldType.Object  => "{}",
                RsvFieldType.Array   => "[]",
                _                    => "null"
            };
        }
    }
}
