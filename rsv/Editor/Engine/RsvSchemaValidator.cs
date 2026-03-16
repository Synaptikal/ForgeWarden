using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Validates DataSchemaDefinition assets before compilation.
    /// Catches schema errors early and provides detailed error messages.
    /// </summary>
    public static class RsvSchemaValidator
    {
        /// <summary>
        /// Validates a schema definition.
        /// </summary>
        /// <param name="schema">The schema to validate.</param>
        /// <returns>Validation report with any errors or warnings.</returns>
        public static LGD_ValidationReport ValidateSchema(DataSchemaDefinition schema)
        {
            var report = new LGD_ValidationReport("SchemaValidator");

            if (schema == null)
            {
                report.Add(ValidationStatus.Critical, "Schema", "Schema is null.");
                return report;
            }

            // Validate basic schema properties
            ValidateBasicProperties(schema, report);

            // Validate field definitions
            ValidateFieldDefinitions(schema, report);

            // Validate field types
            ValidateFieldTypes(schema, report);

            // Validate field constraints
            ValidateFieldConstraints(schema, report);

            // Validate field references
            ValidateFieldReferences(schema, report);

            // Validate schema structure
            ValidateSchemaStructure(schema, report);

            return report;
        }

        /// <summary>
        /// Validates basic schema properties.
        /// </summary>
        private static void ValidateBasicProperties(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            // Validate SchemaId
            if (string.IsNullOrWhiteSpace(schema.SchemaId))
            {
                report.Add(ValidationStatus.Error, "SchemaId", "SchemaId is empty or null.",
                    suggestedFix: "Set a unique SchemaId for the schema.");
            }
            else if (schema.SchemaId.Contains(" "))
            {
                report.Add(ValidationStatus.Warning, "SchemaId", "SchemaId contains spaces.",
                    suggestedFix: "Use underscores or camelCase instead of spaces.");
            }

            // Validate Version
            if (string.IsNullOrWhiteSpace(schema.Version))
            {
                report.Add(ValidationStatus.Warning, "Version", "Version is empty or null.",
                    suggestedFix: "Set a version number (e.g., '1.0.0').");
            }

            // Validate Description
            if (string.IsNullOrWhiteSpace(schema.Description))
            {
                report.Add(ValidationStatus.Info, "Description", "Description is empty.",
                    suggestedFix: "Add a description to document the schema purpose.");
            }
        }

        /// <summary>
        /// Validates field definitions.
        /// </summary>
        private static void ValidateFieldDefinitions(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            if (schema.RootNodes == null || schema.RootNodes.Count == 0)
            {
                report.Add(ValidationStatus.Warning, "Fields", "Schema has no field definitions.",
                    suggestedFix: "Add at least one field to the schema.");
                return;
            }

            // Check for duplicate field names
            var fieldNames = new HashSet<string>();
            foreach (var node in schema.RootNodes)
            {
                if (string.IsNullOrWhiteSpace(node.Name))
                {
                    report.Add(ValidationStatus.Error, "Field", "Field has empty name.",
                        suggestedFix: "Set a unique name for the field.");
                    continue;
                }

                if (fieldNames.Contains(node.Name))
                {
                    report.Add(ValidationStatus.Error, "Field", $"Duplicate field name: '{node.Name}'.",
                        suggestedFix: "Rename one of the fields to have a unique name.");
                }
                else
                {
                    fieldNames.Add(node.Name);
                }
            }
        }

        /// <summary>
        /// Validates field types.
        /// </summary>
        private static void ValidateFieldTypes(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            if (schema.RootNodes == null)
                return;

            foreach (var node in schema.RootNodes)
            {
                if (node == null)
                {
                    report.Add(ValidationStatus.Error, "Field", "Schema contains a null field definition.",
                        suggestedFix: "Remove the null field definition or provide a valid field.");
                    continue;
                }

                if (node.Constraint == null)
                {
                    report.Add(ValidationStatus.Error, "Field", $"Field '{node.Name}' has no constraint defined.",
                        suggestedFix: "Add a constraint to the field definition.");
                    continue;
                }

                var fieldType = node.Constraint.FieldType.ToString().ToLower();
                if (string.IsNullOrWhiteSpace(fieldType))
                {
                    report.Add(ValidationStatus.Error, "Field", $"Field '{node.Name}' has no type specified.",
                        suggestedFix: "Set a type for the field (e.g., 'string', 'int', 'float', 'bool').");
                    continue;
                }

                // Validate type is supported
                if (!IsSupportedType(fieldType))
                {
                    report.Add(ValidationStatus.Warning, "Field", $"Field '{node.Name}' has unsupported type: '{fieldType}'.",
                        suggestedFix: "Use a supported type: string, int, float, bool, array, object, enum.");
                }
            }
        }

        /// <summary>
        /// Validates field constraints.
        /// </summary>
        private static void ValidateFieldConstraints(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            if (schema.RootNodes == null)
                return;

            foreach (var node in schema.RootNodes)
            {
                if (node == null || node.Constraint == null)
                    continue;

                var c = node.Constraint;
                var fieldType = c.FieldType.ToString().ToLower();

                // Validate numeric constraints
                if (fieldType == "integer" || fieldType == "number")
                {
                    if (c.HasMinMax && c.Min > c.Max)
                    {
                        report.Add(ValidationStatus.Error, "Field",
                            $"Field '{node.Name}' has Min ({c.Min}) greater than Max ({c.Max}).",
                            suggestedFix: "Swap Min and Max values.");
                    }
                }

                // Validate enum constraints
                if (c.EnumValues != null && c.EnumValues.Length > RsvConfiguration.MaxEnumValues)
                {
                    report.Add(ValidationStatus.Warning, "Field",
                        $"Field '{node.Name}' has {c.EnumValues.Length} enum values, exceeding recommended limit of {RsvConfiguration.MaxEnumValues}.",
                        suggestedFix: "Consider reducing the number of enum values or using a different validation approach.");
                }
            }
        }

        /// <summary>
        /// Validates field references.
        /// </summary>
        private static void ValidateFieldReferences(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            if (schema.RootNodes == null)
                return;

            foreach (var node in schema.RootNodes)
            {
                if (node == null || node.Constraint == null)
                    continue;

                var c = node.Constraint;

                // Validate required fields
                if (c.IsRequired && !string.IsNullOrEmpty(c.DefaultValue))
                {
                    report.Add(ValidationStatus.Warning, "Field",
                        $"Field '{node.Name}' is required but has a default value.",
                        suggestedFix: "Either remove the default value or make the field optional.");
                }

                // Validate enum values
                if (c.EnumValues != null && c.EnumValues.Length == 0)
                {
                    report.Add(ValidationStatus.Error, "Field",
                        $"Field '{node.Name}' has enum constraint but no enum values defined.",
                        suggestedFix: "Add enum values to the field or remove the enum constraint.");
                }

                // Validate default value matches type
                if (!string.IsNullOrEmpty(c.DefaultValue))
                {
                    if (!ValidateDefaultValue(node))
                    {
                        report.Add(ValidationStatus.Warning, "Field",
                            $"Field '{node.Name}' default value may not match type '{c.FieldType}'.",
                            suggestedFix: "Ensure the default value matches the field type.");
                    }
                }
            }
        }

        /// <summary>
        /// Validates schema structure.
        /// </summary>
        private static void ValidateSchemaStructure(DataSchemaDefinition schema, LGD_ValidationReport report)
        {
            if (schema.RootNodes == null)
                return;

            // Check for circular references
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in schema.RootNodes)
            {
                if (node == null || node.Constraint == null)
                    continue;

                if (node.Constraint.FieldType == RsvFieldType.Object && !string.IsNullOrWhiteSpace(node.Constraint.RefId))
                {
                    CheckCircularReference(schema, node.Constraint.RefId, visited, recursionStack, report);
                }
            }
        }

        /// <summary>
        /// Checks for circular references in object types.
        /// </summary>
        private static void CheckCircularReference(
            DataSchemaDefinition schema,
            string objectType,
            HashSet<string> visited,
            HashSet<string> recursionStack,
            LGD_ValidationReport report)
        {
            if (recursionStack.Contains(objectType))
            {
                report.Add(ValidationStatus.Error, "Schema",
                    $"Circular reference detected: {string.Join(" -> ", recursionStack)} -> {objectType}",
                    suggestedFix: "Restructure the schema to avoid circular references.");
                return;
            }

            if (visited.Contains(objectType))
                return;

            visited.Add(objectType);
            recursionStack.Add(objectType);

            // Find the referenced schema
            var allSchemas = LGD_AssetUtility.FindAllAssetsOfType<DataSchemaDefinition>();
            var referencedSchema = allSchemas.FirstOrDefault(s => s.SchemaId == objectType);

            if (referencedSchema != null && referencedSchema.RootNodes != null)
            {
                foreach (var node in referencedSchema.RootNodes)
                {
                    if (node.Constraint.FieldType == RsvFieldType.Object && !string.IsNullOrWhiteSpace(node.Constraint.RefId))
                    {
                        CheckCircularReference(schema, node.Constraint.RefId, visited, recursionStack, report);
                    }
                }
            }

            recursionStack.Remove(objectType);
        }

        /// <summary>
        /// Checks if a type is supported.
        /// </summary>
        private static bool IsSupportedType(string type)
        {
            var supportedTypes = new[] { "string", "int", "float", "bool", "array", "object", "enum" };
            return supportedTypes.Contains(type.ToLower());
        }

        /// <summary>
        /// Validates that the default value matches the field type.
        /// </summary>
        private static bool ValidateDefaultValue(RsvSchemaNode node)
        {
            // This is a simplified check - in a real implementation, you'd do more thorough validation
            try
            {
                var defaultValue = node.Constraint.DefaultValue;
                var fieldType = node.Constraint.FieldType;

                switch (fieldType)
                {
                    case RsvFieldType.Integer:
                        return int.TryParse(defaultValue, out _);
                    case RsvFieldType.Number:
                        return float.TryParse(defaultValue, out _);
                    case RsvFieldType.Boolean:
                        return bool.TryParse(defaultValue, out _);
                    case RsvFieldType.String:
                        return true; // Any string is valid
                    default:
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates all schemas in the project.
        /// </summary>
        /// <returns>Array of validation reports.</returns>
        public static LGD_ValidationReport[] ValidateAllSchemas()
        {
            var schemas = LGD_AssetUtility.FindAllAssetsOfType<DataSchemaDefinition>();
            var reports = new LGD_ValidationReport[schemas.Length];

            for (int i = 0; i < schemas.Length; i++)
            {
                reports[i] = ValidateSchema(schemas[i]);
            }

            return reports;
        }
    }
}