using System;
using System.Collections.Generic;
using LiveGameDev.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Conditional validation logic.
    /// Allows validation rules to be applied based on conditions.
    /// </summary>
    public static class RsvConditionalValidation
    {
        private static readonly Dictionary<string, ConditionalRule> _conditionalRules = new Dictionary<string, ConditionalRule>();

        /// <summary>
        /// Registers a conditional validation rule.
        /// </summary>
        /// <param name="ruleName">Unique rule name.</param>
        /// <param name="condition">Condition function.</param>
        /// <param name="validation">Validation function.</param>
        /// <param name="description">Rule description.</param>
        public static void RegisterConditionalRule(
            string ruleName,
            Func<JToken, bool> condition,
            Func<JToken, string, LGD_ValidationReport, ValidationStatus?> validation,
            string description = "")
        {
            var rule = new ConditionalRule
            {
                Name = ruleName,
                Condition = condition,
                Validation = validation,
                Description = description,
                RegisteredAt = DateTime.UtcNow
            };

            _conditionalRules[ruleName] = rule;

            Debug.Log($"[RSV] Registered conditional rule: {ruleName}");
        }

        /// <summary>
        /// Applies a conditional validation rule.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        /// <param name="token">JSON token to validate.</param>
        /// <param name="path">JSON path.</param>
        /// <param name="report">Validation report.</param>
        /// <returns>Validation status, or null if condition not met or rule not found.</returns>
        public static ValidationStatus? ApplyConditionalRule(string ruleName, JToken token, string path, LGD_ValidationReport report)
        {
            if (!_conditionalRules.ContainsKey(ruleName))
                return null;

            var rule = _conditionalRules[ruleName];

            // Check condition
            if (!rule.Condition(token))
            {
                return null; // Condition not met, skip validation
            }

            // Apply validation
            return rule.Validation(token, path, report);
        }

        /// <summary>
        /// Gets a conditional rule by name.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        /// <returns>Conditional rule, or null if not found.</returns>
        public static ConditionalRule GetConditionalRule(string ruleName)
        {
            return _conditionalRules.TryGetValue(ruleName, out var rule) ? rule : null;
        }

        /// <summary>
        /// Gets all conditional rules.
        /// </summary>
        /// <returns>List of all conditional rules.</returns>
        public static List<ConditionalRule> GetAllConditionalRules()
        {
            return new List<ConditionalRule>(_conditionalRules.Values);
        }

        /// <summary>
        /// Checks if a conditional rule is registered.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        /// <returns>True if registered, false otherwise.</returns>
        public static bool HasConditionalRule(string ruleName)
        {
            return _conditionalRules.ContainsKey(ruleName);
        }

        /// <summary>
        /// Unregisters a conditional rule.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        public static void UnregisterConditionalRule(string ruleName)
        {
            if (_conditionalRules.ContainsKey(ruleName))
            {
                _conditionalRules.Remove(ruleName);
                Debug.Log($"[RSV] Unregistered conditional rule: {ruleName}");
            }
        }

        /// <summary>
        /// Clears all conditional rules.
        /// </summary>
        public static void ClearAll()
        {
            _conditionalRules.Clear();
            Debug.Log("[RSV] Cleared all conditional rules");
        }

        /// <summary>
        /// Registers built-in conditional rules.
        /// </summary>
        public static void RegisterBuiltInRules()
        {
            // Required if another field has a specific value
            RegisterConditionalRule("requiredIf", (token) =>
            {
                // This is a placeholder - actual implementation would check the condition
                return true;
            }, (token, path, report) =>
            {
                // This is a placeholder - actual implementation would validate
                return ValidationStatus.Pass;
            }, "Field is required if another field has a specific value");

            // Validate only if field exists
            RegisterConditionalRule("validateIfExists", (token) =>
            {
                return token != null && token.Type != JTokenType.Null;
            }, (token, path, report) =>
            {
                // This is a placeholder - actual implementation would validate
                return ValidationStatus.Pass;
            }, "Validate field only if it exists");

            // Validate only if field is not empty
            RegisterConditionalRule("validateIfNotEmpty", (token) =>
            {
                if (token == null || token.Type == JTokenType.Null)
                    return false;

                if (token.Type == JTokenType.String)
                    return !string.IsNullOrWhiteSpace(token.ToString());

                if (token.Type == JTokenType.Array)
                    return token.HasValues;

                if (token.Type == JTokenType.Object)
                    return token.HasValues;

                return true;
            }, (token, path, report) =>
            {
                // This is a placeholder - actual implementation would validate
                return ValidationStatus.Pass;
            }, "Validate field only if it's not empty");

            Debug.Log("[RSV] Registered built-in conditional rules");
        }

        /// <summary>
        /// Creates a condition based on field value.
        /// </summary>
        /// <param name="fieldName">Field name to check.</param>
        /// <param name="expectedValue">Expected value.</param>
        /// <returns>Condition function.</returns>
        public static Func<JToken, bool> CreateFieldValueCondition(string fieldName, object expectedValue)
        {
            return (token) =>
            {
                if (token == null || token.Type != JTokenType.Object)
                    return false;

                var field = token[fieldName];
                if (field == null)
                    return false;

                var actualValue = field.Value<object>();
                return Equals(actualValue, expectedValue);
            };
        }

        /// <summary>
        /// Creates a condition based on field existence.
        /// </summary>
        /// <param name="fieldName">Field name to check.</param>
        /// <returns>Condition function.</returns>
        public static Func<JToken, bool> CreateFieldExistsCondition(string fieldName)
        {
            return (token) =>
            {
                if (token == null || token.Type != JTokenType.Object)
                    return false;

                return token[fieldName] != null;
            };
        }

        /// <summary>
        /// Creates a condition based on field type.
        /// </summary>
        /// <param name="fieldName">Field name to check.</param>
        /// <param name="expectedType">Expected type.</param>
        /// <returns>Condition function.</returns>
        public static Func<JToken, bool> CreateFieldTypeCondition(string fieldName, JTokenType expectedType)
        {
            return (token) =>
            {
                if (token == null || token.Type != JTokenType.Object)
                    return false;

                var field = token[fieldName];
                if (field == null)
                    return false;

                return field.Type == expectedType;
            };
        }

        /// <summary>
        /// Creates a condition based on field value range.
        /// </summary>
        /// <param name="fieldName">Field name to check.</param>
        /// <param name="minValue">Minimum value (inclusive).</param>
        /// <param name="maxValue">Maximum value (inclusive).</param>
        /// <returns>Condition function.</returns>
        public static Func<JToken, bool> CreateFieldValueRangeCondition(string fieldName, double minValue, double maxValue)
        {
            return (token) =>
            {
                if (token == null || token.Type != JTokenType.Object)
                    return false;

                var field = token[fieldName];
                if (field == null)
                    return false;

                if (field.Type != JTokenType.Integer && field.Type != JTokenType.Float)
                    return false;

                var value = field.Value<double>();
                return value >= minValue && value <= maxValue;
            };
        }

        /// <summary>
        /// Creates a condition based on field value length.
        /// </summary>
        /// <param name="fieldName">Field name to check.</param>
        /// <param name="minLength">Minimum length (inclusive).</param>
        /// <param name="maxLength">Maximum length (inclusive).</param>
        /// <returns>Condition function.</returns>
        public static Func<JToken, bool> CreateFieldLengthCondition(string fieldName, int minLength, int maxLength)
        {
            return (token) =>
            {
                if (token == null || token.Type != JTokenType.Object)
                    return false;

                var field = token[fieldName];
                if (field == null)
                    return false;

                if (field.Type == JTokenType.String)
                {
                    var length = field.ToString().Length;
                    return length >= minLength && length <= maxLength;
                }

                if (field.Type == JTokenType.Array)
                {
                    var length = ((JArray)field).Count;
                    return length >= minLength && length <= maxLength;
                }

                return false;
            };
        }

        /// <summary>
        /// Creates a condition based on field value matching a pattern.
        /// </summary>
        /// <param name="fieldName">Field name to check.</param>
        /// <param name="pattern">Regex pattern.</param>
        /// <returns>Condition function.</returns>
        public static Func<JToken, bool> CreateFieldPatternCondition(string fieldName, string pattern)
        {
            return (token) =>
            {
                if (token == null || token.Type != JTokenType.Object)
                    return false;

                var field = token[fieldName];
                if (field == null || field.Type != JTokenType.String)
                    return false;

                var value = field.ToString();
                return System.Text.RegularExpressions.Regex.IsMatch(value, pattern, System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2));
            };
        }
    }

    /// <summary>
    /// Represents a conditional validation rule.
    /// </summary>
    public class ConditionalRule
    {
        public string Name { get; set; }
        public Func<JToken, bool> Condition { get; set; }
        public Func<JToken, string, LGD_ValidationReport, ValidationStatus?> Validation { get; set; }
        public string Description { get; set; }
        public DateTime RegisteredAt { get; set; }

        public override string ToString()
        {
            return $"{Name}: {Description}";
        }
    }
}