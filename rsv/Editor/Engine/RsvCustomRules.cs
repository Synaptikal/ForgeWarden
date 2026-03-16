using System;
using System.Collections.Generic;
using LiveGameDev.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Custom validation rules support.
    /// Allows users to define custom validation logic for complex scenarios.
    /// </summary>
    public static class RsvCustomRules
    {
        private static readonly Dictionary<string, List<CustomValidationRule>> _rules = new Dictionary<string, List<CustomValidationRule>>();

        /// <summary>
        /// Registers a custom validation rule.
        /// </summary>
        /// <param name="ruleName">Unique rule name.</param>
        /// <param name="rule">Validation rule function.</param>
        /// <param name="description">Rule description.</param>
        public static void RegisterRule(string ruleName, Func<JToken, string, LGD_ValidationReport, ValidationStatus?> rule, string description = "")
        {
            var customRule = new CustomValidationRule
            {
                Name = ruleName,
                Rule = rule,
                Description = description,
                RegisteredAt = DateTime.UtcNow
            };

            if (!_rules.ContainsKey(ruleName))
            {
                _rules[ruleName] = new List<CustomValidationRule>();
            }

            _rules[ruleName].Add(customRule);

            Debug.Log($"[RSV] Registered custom rule: {ruleName}");
        }

        /// <summary>
        /// Registers a custom validation rule with parameters.
        /// </summary>
        /// <param name="ruleName">Unique rule name.</param>
        /// <param name="rule">Validation rule function with parameters.</param>
        /// <param name="parameters">Rule parameters.</param>
        /// <param name="description">Rule description.</param>
        public static void RegisterRuleWithParams(
            string ruleName,
            Func<JToken, string, LGD_ValidationReport, Dictionary<string, object>, ValidationStatus?> rule,
            Dictionary<string, object> parameters,
            string description = "")
        {
            var customRule = new CustomValidationRuleWithParams
            {
                Name = ruleName,
                RuleWithParams = rule,
                Parameters = parameters,
                Description = description,
                RegisteredAt = DateTime.UtcNow
            };

            if (!_rules.ContainsKey(ruleName))
            {
                _rules[ruleName] = new List<CustomValidationRule>();
            }

            _rules[ruleName].Add(customRule);

            Debug.Log($"[RSV] Registered custom rule with params: {ruleName}");
        }

        /// <summary>
        /// Applies a custom validation rule.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        /// <param name="token">JSON token to validate.</param>
        /// <param name="path">JSON path.</param>
        /// <param name="report">Validation report.</param>
        /// <returns>Validation status, or null if rule not found.</returns>
        public static ValidationStatus? ApplyRule(string ruleName, JToken token, string path, LGD_ValidationReport report)
        {
            if (!_rules.ContainsKey(ruleName) || _rules[ruleName].Count == 0)
                return null;

            var rule = _rules[ruleName][0];
            return rule.Rule(token, path, report);
        }

        /// <summary>
        /// Applies a custom validation rule with parameters.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        /// <param name="token">JSON token to validate.</param>
        /// <param name="path">JSON path.</param>
        /// <param name="report">Validation report.</param>
        /// <param name="parameters">Rule parameters.</param>
        /// <returns>Validation status, or null if rule not found.</returns>
        public static ValidationStatus? ApplyRuleWithParams(
            string ruleName,
            JToken token,
            string path,
            LGD_ValidationReport report,
            Dictionary<string, object> parameters)
        {
            if (!_rules.ContainsKey(ruleName) || _rules[ruleName].Count == 0)
                return null;

            var rule = _rules[ruleName][0] as CustomValidationRuleWithParams;
            if (rule == null)
                return null;

            return rule.RuleWithParams?.Invoke(token, path, report, parameters ?? rule.Parameters);
        }

        /// <summary>
        /// Gets a custom rule by name.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        /// <returns>Custom rule, or null if not found.</returns>
        public static CustomValidationRule GetRule(string ruleName)
        {
            if (!_rules.ContainsKey(ruleName) || _rules[ruleName].Count == 0)
                return null;

            return _rules[ruleName][0];
        }

        /// <summary>
        /// Gets all registered rules.
        /// </summary>
        /// <returns>List of all custom rules.</returns>
        public static List<CustomValidationRule> GetAllRules()
        {
            var allRules = new List<CustomValidationRule>();
            foreach (var kvp in _rules)
            {
                allRules.AddRange(kvp.Value);
            }
            return allRules;
        }

        /// <summary>
        /// Checks if a rule is registered.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        /// <returns>True if registered, false otherwise.</returns>
        public static bool HasRule(string ruleName)
        {
            return _rules.ContainsKey(ruleName) && _rules[ruleName].Count > 0;
        }

        /// <summary>
        /// Unregisters a custom rule.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        public static void UnregisterRule(string ruleName)
        {
            if (_rules.ContainsKey(ruleName))
            {
                _rules.Remove(ruleName);
                Debug.Log($"[RSV] Unregistered custom rule: {ruleName}");
            }
        }

        /// <summary>
        /// Clears all custom rules.
        /// </summary>
        public static void ClearAll()
        {
            _rules.Clear();
            Debug.Log("[RSV] Cleared all custom rules");
        }

        /// <summary>
        /// Registers built-in custom rules.
        /// </summary>
        public static void RegisterBuiltInRules()
        {
            // Email validation rule
            RegisterRule("email", (token, path, report) =>
            {
                if (token == null || token.Type != JTokenType.String)
                    return null;

                var email = token.ToString();
                if (!IsValidEmail(email))
                {
                    report.Add(ValidationStatus.Error, "CustomRule",
                        $"Field '{path}' is not a valid email address: {email}",
                        suggestedFix: "Provide a valid email address.");
                    return ValidationStatus.Error;
                }
                return ValidationStatus.Pass;
            }, "Validates email address format");

            // URL validation rule
            RegisterRule("url", (token, path, report) =>
            {
                if (token == null || token.Type != JTokenType.String)
                    return null;

                var url = token.ToString();
                if (!IsValidUrl(url))
                {
                    report.Add(ValidationStatus.Error, "CustomRule",
                        $"Field '{path}' is not a valid URL: {url}",
                        suggestedFix: "Provide a valid URL (e.g., https://example.com).");
                    return ValidationStatus.Error;
                }
                return ValidationStatus.Pass;
            }, "Validates URL format");

            // UUID validation rule
            RegisterRule("uuid", (token, path, report) =>
            {
                if (token == null || token.Type != JTokenType.String)
                    return null;

                var uuid = token.ToString();
                if (!IsValidUuid(uuid))
                {
                    report.Add(ValidationStatus.Error, "CustomRule",
                        $"Field '{path}' is not a valid UUID: {uuid}",
                        suggestedFix: "Provide a valid UUID (e.g., 550e8400-e29b-41d4-a716-446655440000).");
                    return ValidationStatus.Error;
                }
                return ValidationStatus.Pass;
            }, "Validates UUID format");

            // Regex validation rule
            RegisterRuleWithParams("regex", (token, path, report, parameters) =>
            {
                if (token == null || token.Type != JTokenType.String)
                    return null;

                if (!parameters.TryGetValue("pattern", out var patternObj) || patternObj == null)
                {
                    report.Add(ValidationStatus.Error, "CustomRule",
                        $"Field '{path}' regex rule requires 'pattern' parameter.");
                    return ValidationStatus.Error;
                }

                var pattern = patternObj.ToString();
                var value = token.ToString();

                try
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern, System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
                    {
                        report.Add(ValidationStatus.Error, "CustomRule",
                            $"Field '{path}' does not match pattern: {pattern}",
                            suggestedFix: $"Ensure the value matches the pattern: {pattern}");
                        return ValidationStatus.Error;
                    }
                }
                catch (Exception ex)
                {
                    report.Add(ValidationStatus.Error, "CustomRule",
                        $"Field '{path}' regex pattern is invalid: {ex.Message}");
                    return ValidationStatus.Error;
                }

                return ValidationStatus.Pass;
            }, new Dictionary<string, object>(), "Validates against a regex pattern");

            Debug.Log("[RSV] Registered built-in custom rules");
        }

        /// <summary>
        /// Validates email format.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates URL format.
        /// </summary>
        private static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Validates UUID format.
        /// </summary>
        private static bool IsValidUuid(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(uuid,
                @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
                System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2));
        }
    }

    /// <summary>
    /// Represents a custom validation rule.
    /// </summary>
    public class CustomValidationRule
    {
        public string Name { get; set; }
        public Func<JToken, string, LGD_ValidationReport, ValidationStatus?> Rule { get; set; }
        public string Description { get; set; }
        public DateTime RegisteredAt { get; set; }

        public override string ToString()
        {
            return $"{Name}: {Description}";
        }
    }

    /// <summary>
    /// Represents a custom validation rule with parameters.
    /// </summary>
    public class CustomValidationRuleWithParams : CustomValidationRule
    {
        public Func<JToken, string, LGD_ValidationReport, Dictionary<string, object>, ValidationStatus?> RuleWithParams { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public new Func<JToken, string, LGD_ValidationReport, ValidationStatus?> Rule
        {
            get => (token, path, report) => RuleWithParams?.Invoke(token, path, report, Parameters);
            set => throw new NotSupportedException("Use RuleWithParams instead.");
        }
    }
}