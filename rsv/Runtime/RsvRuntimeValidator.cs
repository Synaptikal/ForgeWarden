using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Static API for runtime JSON validation.
    /// Provides lightweight validation without Editor dependencies.
    /// Uses struct-based results for zero allocations in the common pass case.
    /// </summary>
    public static class RsvRuntimeValidator
    {
        /// <summary>
        /// Validates a JSON string against a pre-compiled schema asset.
        /// </summary>
        /// <param name="schema">The compiled schema to validate against.</param>
        /// <param name="json">The JSON string to validate.</param>
        /// <returns>A validation result struct.</returns>
        public static RsvValidationResult Validate(RsvCompiledSchemaAsset schema, string json)
        {
            // Validate schema
            if (schema == null)
            {
                var result = RsvValidationResult.Create(RsvValidationStatus.Critical);
                result.AddEntry(RsvValidationStatus.Critical, "Setup", "Schema is null.");
                return result;
            }

            if (!schema.IsValid())
            {
                var result = RsvValidationResult.Create(RsvValidationStatus.Critical);
                result.AddEntry(RsvValidationStatus.Critical, "Setup", $"Schema '{schema.SchemaId}' is invalid.");
                return result;
            }

            // Validate JSON
            if (string.IsNullOrWhiteSpace(json))
            {
                var result = RsvValidationResult.Create(RsvValidationStatus.Error);
                result.AddEntry(RsvValidationStatus.Error, "Source", "JSON is null or empty.");
                return result;
            }

            // Pre-parse security: check raw JSON nesting depth before allocating a JToken tree.
            // This catches deeply-nested JSON even when the schema has shallow definitions.
            int rawDepth = MeasureJsonDepth(json);
            if (rawDepth > schema.MaxNestingDepth)
            {
                var result = RsvValidationResult.Create(RsvValidationStatus.Critical);
                result.AddEntry(RsvValidationStatus.Critical, "Security",
                    $"JSON nesting depth ({rawDepth}) exceeds maximum ({schema.MaxNestingDepth}).");
                return result;
            }

            // Parse JSON
            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch (JsonReaderException ex)
            {
                var result = RsvValidationResult.Create(RsvValidationStatus.Critical);
                result.AddEntry(RsvValidationStatus.Critical, "ParseError", $"Invalid JSON: {ex.Message}");
                return result;
            }

            // Validate against schema
            var validationResult = RsvValidationResult.Pass();
            ValidateToken(token, schema.RootNodes, "", schema, 0, ref validationResult);

            return validationResult;
        }

        /// <summary>
        /// Validates a JSON string against a schema by ID.
        /// </summary>
        /// <param name="schemaId">The ID of the schema to validate against.</param>
        /// <param name="json">The JSON string to validate.</param>
        /// <returns>A validation result struct.</returns>
        public static RsvValidationResult Validate(string schemaId, string json)
        {
            var schema = RsvSchemaRegistry.Get(schemaId);
            if (schema == null)
            {
                var result = RsvValidationResult.Create(RsvValidationStatus.Critical);
                result.AddEntry(RsvValidationStatus.Critical, "Setup", $"Schema not found: {schemaId}");
                return result;
            }

            return Validate(schema, json);
        }

        /// <summary>
        /// Validates all [RsvSchema] fields on a MonoBehaviour.
        /// </summary>
        /// <param name="component">The component to validate.</param>
        /// <returns>A validation result struct containing all validation results.</returns>
        public static RsvValidationResult ValidateComponent(MonoBehaviour component)
        {
            if (component == null)
            {
                var result = RsvValidationResult.Create(RsvValidationStatus.Critical);
                result.AddEntry(RsvValidationStatus.Critical, "Setup", "Component is null.");
                return result;
            }

            var validationResult = RsvValidationResult.Pass();
            var type = component.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<RsvSchemaAttribute>();
                if (attribute == null)
                    continue;

                if (field.FieldType != typeof(string))
                {
                    validationResult.AddEntry(RsvValidationStatus.Warning, "Attribute",
                        $"[RsvSchema] on non-string field '{field.Name}' is ignored.");
                    continue;
                }

                var json = field.GetValue(component) as string;
                if (string.IsNullOrWhiteSpace(json))
                {
                    validationResult.AddEntry(RsvValidationStatus.Warning, "Validation",
                        $"Field '{field.Name}' is null or empty.");
                    continue;
                }

                var fieldResult = Validate(attribute.SchemaId, json);
                validationResult.Merge(fieldResult);

                // Add path information
                foreach (var entry in fieldResult.GetEntries())
                {
                    var updatedEntry = entry;
                    updatedEntry.Path = $"{field.Name}.{updatedEntry.Path}";
                }
            }

            return validationResult;
        }

        /// <summary>
        /// Validates all [RsvSchema] fields on a ScriptableObject.
        /// </summary>
        /// <param name="scriptableObject">The ScriptableObject to validate.</param>
        /// <returns>A validation result struct containing all validation results.</returns>
        public static RsvValidationResult ValidateScriptableObject(ScriptableObject scriptableObject)
        {
            if (scriptableObject == null)
            {
                var result = RsvValidationResult.Create(RsvValidationStatus.Critical);
                result.AddEntry(RsvValidationStatus.Critical, "Setup", "ScriptableObject is null.");
                return result;
            }

            var validationResult = RsvValidationResult.Pass();
            var type = scriptableObject.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<RsvSchemaAttribute>();
                if (attribute == null)
                    continue;

                if (field.FieldType != typeof(string))
                {
                    validationResult.AddEntry(RsvValidationStatus.Warning, "Attribute",
                        $"[RsvSchema] on non-string field '{field.Name}' is ignored.");
                    continue;
                }

                var json = field.GetValue(scriptableObject) as string;
                if (string.IsNullOrWhiteSpace(json))
                {
                    validationResult.AddEntry(RsvValidationStatus.Warning, "Validation",
                        $"Field '{field.Name}' is null or empty.");
                    continue;
                }

                var fieldResult = Validate(attribute.SchemaId, json);
                validationResult.Merge(fieldResult);
            }

            return validationResult;
        }

        /// <summary>
        /// Validates a JToken against a list of schema nodes.
        /// </summary>
        private static void ValidateToken(
            JToken token,
            List<RsvCompiledNode> nodes,
            string path,
            RsvCompiledSchemaAsset schema,
            int depth,
            ref RsvValidationResult result)
        {
            // Check nesting depth
            if (depth > schema.MaxNestingDepth)
            {
                result.AddEntry(RsvValidationStatus.Critical, "Security",
                    $"Maximum nesting depth ({schema.MaxNestingDepth}) exceeded at path: {path}");
                return;
            }

            if (nodes == null || nodes.Count == 0)
                return;

            // Validate each node
            foreach (var node in nodes)
            {
                ValidateNode(token, node, path, schema, depth, ref result);
            }
        }

        /// <summary>
        /// Validates a single schema node against a JToken.
        /// </summary>
        private static void ValidateNode(
            JToken token,
            RsvCompiledNode node,
            string path,
            RsvCompiledSchemaAsset schema,
            int depth,
            ref RsvValidationResult result)
        {
            var nodePath = string.IsNullOrEmpty(path) ? node.Name : $"{path}.{node.Name}";
            var fieldToken = token.Type == JTokenType.Object ? token[node.Name] : null;

            // Check if field is required
            if (node.IsRequired && fieldToken == null)
            {
                result.AddEntry(RsvValidationStatus.Error, "MissingField",
                    $"Required field '{node.Name}' is missing.", nodePath);
                return;
            }

            // If field is not required and not present, skip validation
            if (fieldToken == null)
                return;

            // Validate type
            if (!ValidateType(fieldToken, node.FieldType, nodePath, ref result))
                return;

            // Validate based on type
            switch (node.FieldType)
            {
                case RsvFieldType.String:
                    ValidateString(fieldToken, node, nodePath, schema, ref result);
                    break;

                case RsvFieldType.Integer:
                    ValidateInteger(fieldToken, node, nodePath, ref result);
                    break;

                case RsvFieldType.Number:
                    ValidateNumber(fieldToken, node, nodePath, ref result);
                    break;

                case RsvFieldType.Boolean:
                    // Boolean type is already validated by ValidateType
                    break;

                case RsvFieldType.Object:
                    ValidateObject(fieldToken, node, nodePath, schema, depth, ref result);
                    break;

                case RsvFieldType.Array:
                    ValidateArray(fieldToken, node, nodePath, schema, depth, ref result);
                    break;

                case RsvFieldType.Null:
                    if (fieldToken.Type != JTokenType.Null)
                    {
                        result.AddEntry(RsvValidationStatus.Error, "TypeMismatch",
                            $"Field '{node.Name}' must be null.", nodePath);
                    }
                    break;
            }
        }

        /// <summary>
        /// Validates the type of a JToken.
        /// </summary>
        private static bool ValidateType(JToken token, RsvFieldType expectedType, string path, ref RsvValidationResult result)
        {
            bool isValid = expectedType switch
            {
                RsvFieldType.String => token.Type == JTokenType.String,
                RsvFieldType.Integer => token.Type == JTokenType.Integer,
                RsvFieldType.Number => token.Type == JTokenType.Integer || token.Type == JTokenType.Float,
                RsvFieldType.Boolean => token.Type == JTokenType.Boolean,
                RsvFieldType.Object => token.Type == JTokenType.Object,
                RsvFieldType.Array => token.Type == JTokenType.Array,
                RsvFieldType.Null => token.Type == JTokenType.Null,
                _ => true
            };

            if (!isValid)
            {
                result.AddEntry(RsvValidationStatus.Error, "TypeMismatch",
                    $"Expected type '{expectedType}' but got '{token.Type}'.", path);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates a string field.
        /// </summary>
        private static void ValidateString(JToken token, RsvCompiledNode node, string path, RsvCompiledSchemaAsset schema, ref RsvValidationResult result)
        {
            var value = token.Value<string>();

            // Check string length
            if (value != null)
            {
                if (value.Length > schema.MaxStringLength)
                {
                    result.AddEntry(RsvValidationStatus.Critical, "Security",
                        $"String exceeds maximum length ({schema.MaxStringLength}).", path);
                    return;
                }

                if (node.MinLength > 0 && value.Length < node.MinLength)
                {
                    result.AddEntry(RsvValidationStatus.Error, "LengthViolation",
                        $"String length ({value.Length}) is less than minimum ({node.MinLength}).", path);
                }

                if (node.MaxLength > 0 && value.Length > node.MaxLength)
                {
                    result.AddEntry(RsvValidationStatus.Error, "LengthViolation",
                        $"String length ({value.Length}) exceeds maximum ({node.MaxLength}).", path);
                }
            }

            // Check pattern
            if (!string.IsNullOrEmpty(node.Pattern))
            {
                try
                {
                    // Use MatchTimeout to prevent ReDoS attacks
                    var regex = new Regex(node.Pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
                    if (!regex.IsMatch(value))
                    {
                        result.AddEntry(RsvValidationStatus.Error, "PatternViolation",
                            $"String does not match pattern: {node.Pattern}", path);
                    }
                }
                catch (ArgumentException ex)
                {
                    result.AddEntry(RsvValidationStatus.Warning, "Configuration",
                        $"Invalid regex pattern: {ex.Message}", path);
                }
                catch (RegexMatchTimeoutException)
                {
                    result.AddEntry(RsvValidationStatus.Critical, "Security",
                        "Regex pattern matching timed out. Pattern may be too complex.", path);
                }
            }

            // Check enum values
            if (node.EnumValues != null && node.EnumValues.Length > 0)
            {
                bool isValid = false;
                foreach (var enumValue in node.EnumValues)
                {
                    if (value == enumValue)
                    {
                        isValid = true;
                        break;
                    }
                }

                if (!isValid)
                {
                    result.AddEntry(RsvValidationStatus.Error, "EnumViolation",
                        $"Value '{value}' is not in allowed enum values.", path);
                }
            }
        }

        /// <summary>
        /// Validates an integer field.
        /// </summary>
        private static void ValidateInteger(JToken token, RsvCompiledNode node, string path, ref RsvValidationResult result)
        {
            var value = token.Value<long>();

            if (node.HasMinMax)
            {
                if (value < node.Min)
                {
                    result.AddEntry(RsvValidationStatus.Error, "RangeViolation",
                        $"Value ({value}) is less than minimum ({node.Min}).", path);
                }

                if (value > node.Max)
                {
                    result.AddEntry(RsvValidationStatus.Error, "RangeViolation",
                        $"Value ({value}) exceeds maximum ({node.Max}).", path);
                }
            }
        }

        /// <summary>
        /// Validates a number field.
        /// </summary>
        private static void ValidateNumber(JToken token, RsvCompiledNode node, string path, ref RsvValidationResult result)
        {
            var value = token.Value<double>();

            if (node.HasMinMax)
            {
                if (value < node.Min)
                {
                    result.AddEntry(RsvValidationStatus.Error, "RangeViolation",
                        $"Value ({value}) is less than minimum ({node.Min}).", path);
                }

                if (value > node.Max)
                {
                    result.AddEntry(RsvValidationStatus.Error, "RangeViolation",
                        $"Value ({value}) exceeds maximum ({node.Max}).", path);
                }
            }
        }

        /// <summary>
        /// Validates an object field.
        /// </summary>
        private static void ValidateObject(JToken token, RsvCompiledNode node, string path, RsvCompiledSchemaAsset schema, int depth, ref RsvValidationResult result)
        {
            if (node.Children == null || node.Children.Count == 0)
                return;

            ValidateToken(token, node.Children, path, schema, depth + 1, ref result);
        }

        /// <summary>
        /// Validates an array field.
        /// </summary>
        private static void ValidateArray(JToken token, RsvCompiledNode node, string path, RsvCompiledSchemaAsset schema, int depth, ref RsvValidationResult result)
        {
            var array = token as JArray;
            if (array == null)
                return;

            // Check array length
            if (array.Count > schema.MaxArrayLength)
            {
                result.AddEntry(RsvValidationStatus.Critical, "Security",
                    $"Array exceeds maximum length ({schema.MaxArrayLength}).", path);
                return;
            }

            if (node.MinItems > 0 && array.Count < node.MinItems)
            {
                result.AddEntry(RsvValidationStatus.Error, "LengthViolation",
                    $"Array length ({array.Count}) is less than minimum ({node.MinItems}).", path);
            }

            if (node.MaxItems > 0 && array.Count > node.MaxItems)
            {
                result.AddEntry(RsvValidationStatus.Error, "LengthViolation",
                    $"Array length ({array.Count}) exceeds maximum ({node.MaxItems}).", path);
            }

            // Check unique items
            if (node.UniqueItems)
            {
                var seen = new HashSet<string>();
                foreach (var item in array)
                {
                    var itemStr = item.ToString();
                    if (seen.Contains(itemStr))
                    {
                        result.AddEntry(RsvValidationStatus.Error, "UniqueViolation",
                            $"Array contains duplicate items.", path);
                        break;
                    }
                    seen.Add(itemStr);
                }
            }

            // Validate array items
            if (node.Children != null && node.Children.Count > 0)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    var itemPath = $"{path}[{i}]";
                    ValidateToken(array[i], node.Children, itemPath, schema, depth + 1, ref result);
                }
            }
        }

        /// <summary>
        /// Counts the maximum JSON nesting depth in a raw JSON string.
        /// Correctly ignores { and [ characters inside string literals.
        /// Used for pre-parse security checks to catch deeply nested JSON
        /// before allocating a full JToken tree.
        /// </summary>
        private static int MeasureJsonDepth(string json)
        {
            int maxDepth = 0;
            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                if (c == '{' || c == '[')
                {
                    depth++;
                    if (depth > maxDepth) maxDepth = depth;
                }
                else if (c == '}' || c == ']')
                {
                    depth--;
                }
            }

            return maxDepth;
        }
    }
}