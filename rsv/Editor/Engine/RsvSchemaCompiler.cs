using System.Collections.Generic;
using LiveGameDev.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Compiles a DataSchemaDefinition into a form ready for validation,
    /// then evaluates JToken trees against it.
    /// </summary>
    public static class RsvSchemaCompiler
    {
        // Use configuration for max nesting depth

        /// <summary>Compile a DataSchemaDefinition for repeated validation use.</summary>
        public static CompiledSchema Compile(DataSchemaDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[RSV] Cannot compile null schema definition.");
                return new CompiledSchema(new List<RsvSchemaNode>());
            }

            // Validate schema before compilation
            var validationReport = RsvSchemaValidator.ValidateSchema(definition);

            if (validationReport.HasErrors || validationReport.HasCritical)
            {
                Debug.LogError($"[RSV] Schema '{definition.SchemaId}' has validation errors:");
                foreach (var entry in validationReport.Entries)
                {
                    if (entry.Status == ValidationStatus.Error || entry.Status == ValidationStatus.Critical)
                    {
                        Debug.LogError($"  [{entry.Status}] {entry.Category}: {entry.Message}");
                    }
                }
            }

            var rootNodes = definition.RootNodes ?? new List<RsvSchemaNode>();
            return new CompiledSchema(rootNodes);
        }

        /// <summary>
        /// Evaluate a single JToken against a schema node and all its children.
        /// Appends entries to the report; returns the highest severity found.
        /// </summary>
        public static void EvaluateNode(
            JToken token,
            RsvSchemaNode node,
            string path,
            LGD_ValidationReport report,
            int depth = 0)
        {
            // Null check for node
            if (node == null)
            {
                report.Add(ValidationStatus.Critical, "NullNode",
                    "Encountered null schema node during validation.",
                    suggestedFix: "Check schema definition for null nodes.");
                return;
            }

            // Null check for constraint
            if (node.Constraint == null)
            {
                report.Add(ValidationStatus.Critical, "NullConstraint",
                    $"Schema node '{node.Name}' has no constraint defined.",
                    suggestedFix: "Add a constraint to the schema node.");
                return;
            }

            // Check nesting depth to prevent stack overflow and DoS attacks
            if (depth > RsvConfiguration.MaxNestingDepth)
            {
                report.Add(ValidationStatus.Critical, "DepthLimitExceeded",
                    $"Validation depth exceeded maximum of {RsvConfiguration.MaxNestingDepth} levels at path '{path}'.",
                    suggestedFix: "Simplify your JSON structure or increase the depth limit in RsvConfiguration.");
                return;
            }

            var fullPath = string.IsNullOrEmpty(path) ? node.Name : $"{path}.{node.Name}";
            node.FullPath = fullPath;
            var c = node.Constraint;

            // ── Required check ────────────────────────────────────
            if (token == null || token.Type == JTokenType.Null)
            {
                if (c.IsRequired)
                    report.Add(ValidationStatus.Error, "MissingField",
                        $"Required field '{fullPath}' is missing or null.",
                        suggestedFix: $"Add \"{node.Name}\" to the JSON payload.");
                return; // no further checks on a missing value
            }

            // ── Type check ────────────────────────────────────────
            if (!IsTokenTypeMatch(token, c.FieldType))
            {
                report.Add(ValidationStatus.Error, "TypeMismatch",
                    $"Field '{fullPath}': expected {c.FieldType}, got {token.Type}.",
                    suggestedFix: $"Change the value type to {c.FieldType}.");
                return; // skip range/enum on wrong type
            }

            // ── Range check (numeric) ─────────────────────────────
            if (c.HasMinMax &&
                (c.FieldType == RsvFieldType.Integer || c.FieldType == RsvFieldType.Number))
            {
                var numVal = token.Value<double>();
                if (numVal < c.Min)
                    report.Add(ValidationStatus.Error, "RangeViolation",
                        $"Field '{fullPath}': value {numVal} is below minimum {c.Min}.",
                        suggestedFix: $"Use a value >= {c.Min}.");
                else if (numVal > c.Max)
                    report.Add(ValidationStatus.Error, "RangeViolation",
                        $"Field '{fullPath}': value {numVal} exceeds maximum {c.Max}.",
                        suggestedFix: $"Use a value <= {c.Max}.");
            }

            // ── Enum check ────────────────────────────────────────
            if (c.EnumValues != null && c.EnumValues.Length > 0)
            {
                var strVal = token.ToString();
                var allowed = System.Array.Exists(c.EnumValues,
                    e => e.Equals(strVal, System.StringComparison.OrdinalIgnoreCase));
                if (!allowed)
                    report.Add(ValidationStatus.Error, "EnumViolation",
                        $"Field '{fullPath}': \"{strVal}\" is not in allowed values [{string.Join(", ", c.EnumValues)}].",
                        suggestedFix: $"Use one of: {string.Join(", ", c.EnumValues)}");
            }

            // ── Recurse into children (Object) ────────────────────
            if (c.FieldType == RsvFieldType.Object && node.Children?.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    var childToken = token[child.Name];
                    EvaluateNode(childToken, child, fullPath, report, depth + 1);
                }
            }

            // ── Recurse into array items ──────────────────────────
            if (c.FieldType == RsvFieldType.Array && token is JArray arr && node.Children?.Count > 0)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    foreach (var child in node.Children)
                    {
                        var childToken = arr[i][child.Name];
                        EvaluateNode(childToken, child, $"{fullPath}[{i}]", report, depth + 1);
                    }
                }
            }
        }

        private static bool IsTokenTypeMatch(JToken token, RsvFieldType expected)
            => expected switch
            {
                RsvFieldType.String  => token.Type == JTokenType.String,
                RsvFieldType.Integer => token.Type == JTokenType.Integer,
                RsvFieldType.Number  => token.Type is JTokenType.Float or JTokenType.Integer,
                RsvFieldType.Boolean => token.Type == JTokenType.Boolean,
                RsvFieldType.Object  => token.Type == JTokenType.Object,
                RsvFieldType.Array   => token.Type == JTokenType.Array,
                _                    => false
            };
    }

    /// <summary>A compiled, ready-to-validate representation of a DataSchemaDefinition.</summary>
    public class CompiledSchema
    {
        public List<RsvSchemaNode> Nodes { get; }
        public CompiledSchema(List<RsvSchemaNode> nodes) => Nodes = nodes;
    }
}
