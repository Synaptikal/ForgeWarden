using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Validation rule inheritance and composition.
    /// Allows rules to be inherited from parent schemas and composed together.
    /// </summary>
    public static class RsvRuleComposition
    {
        private const int MAX_RULE_SETS = 100;
        private static readonly Dictionary<string, List<ComposableRule>> _ruleSets = new Dictionary<string, List<ComposableRule>>();
        private static readonly Dictionary<string, string> _inheritanceMap = new Dictionary<string, string>();

        /// <summary>
        /// Creates a rule set.
        /// </summary>
        /// <param name="ruleSetName">Unique rule set name.</param>
        public static void CreateRuleSet(string ruleSetName)
        {
            if (_ruleSets.Count >= MAX_RULE_SETS)
            {
                var oldestKey = _ruleSets.Keys.First();
                _ruleSets.Remove(oldestKey);
            }

            if (!_ruleSets.ContainsKey(ruleSetName))
            {
                _ruleSets[ruleSetName] = new List<ComposableRule>();
                Debug.Log($"[RSV] Created rule set: {ruleSetName}");
            }
        }

        /// <summary>
        /// Adds a rule to a rule set.
        /// </summary>
        /// <param name="ruleSetName">Rule set name.</param>
        /// <param name="rule">Composable rule.</param>
        public static void AddRuleToSet(string ruleSetName, ComposableRule rule)
        {
            if (!_ruleSets.ContainsKey(ruleSetName))
            {
                CreateRuleSet(ruleSetName);
            }

            _ruleSets[ruleSetName].Add(rule);
            Debug.Log($"[RSV] Added rule '{rule.Name}' to rule set '{ruleSetName}'");
        }

        /// <summary>
        /// Gets all rules in a rule set.
        /// </summary>
        /// <param name="ruleSetName">Rule set name.</param>
        /// <returns>List of rules, or empty list if not found.</returns>
        public static List<ComposableRule> GetRuleSet(string ruleSetName)
        {
            if (!_ruleSets.ContainsKey(ruleSetName))
                return new List<ComposableRule>();

            return new List<ComposableRule>(_ruleSets[ruleSetName]);
        }

        /// <summary>
        /// Applies all rules in a rule set.
        /// </summary>
        /// <param name="ruleSetName">Rule set name.</param>
        /// <param name="token">JSON token to validate.</param>
        /// <param name="path">JSON path.</param>
        /// <param name="report">Validation report.</param>
        /// <returns>Highest validation status found.</returns>
        public static ValidationStatus ApplyRuleSet(string ruleSetName, JToken token, string path, LGD_ValidationReport report)
        {
            var rules = GetRuleSet(ruleSetName);
            var highestStatus = ValidationStatus.Pass;

            foreach (var rule in rules)
            {
                var status = rule.Validate(token, path, report);
                if (status.HasValue && status.Value > highestStatus)
                {
                    highestStatus = status.Value;
                }
            }

            return highestStatus;
        }

        /// <summary>
        /// Sets up inheritance between rule sets.
        /// </summary>
        /// <param name="childRuleSet">Child rule set name.</param>
        /// <param name="parentRuleSet">Parent rule set name.</param>
        public static void SetInheritance(string childRuleSet, string parentRuleSet)
        {
            _inheritanceMap[childRuleSet] = parentRuleSet;
            Debug.Log($"[RSV] Set inheritance: {childRuleSet} -> {parentRuleSet}");
        }

        /// <summary>
        /// Gets all rules in a rule set including inherited rules.
        /// </summary>
        /// <param name="ruleSetName">Rule set name.</param>
        /// <returns>List of all rules including inherited.</returns>
        public static List<ComposableRule> GetRuleSetWithInheritance(string ruleSetName)
        {
            var allRules = new List<ComposableRule>();
            var visited = new HashSet<string>();

            GetRuleSetWithInheritanceRecursive(ruleSetName, allRules, visited);

            return allRules;
        }

        /// <summary>
        /// Recursively gets rules with inheritance.
        /// </summary>
        private static void GetRuleSetWithInheritanceRecursive(
            string ruleSetName,
            List<ComposableRule> allRules,
            HashSet<string> visited)
        {
            if (visited.Contains(ruleSetName))
                return;

            visited.Add(ruleSetName);

            // Add rules from this set
            if (_ruleSets.ContainsKey(ruleSetName))
            {
                allRules.AddRange(_ruleSets[ruleSetName]);
            }

            // Add rules from parent set
            if (_inheritanceMap.ContainsKey(ruleSetName))
            {
                var parentSet = _inheritanceMap[ruleSetName];
                GetRuleSetWithInheritanceRecursive(parentSet, allRules, visited);
            }
        }

        /// <summary>
        /// Composes multiple rule sets into a single rule set.
        /// </summary>
        /// <param name="newRuleSetName">New rule set name.</param>
        /// <param name="ruleSetNames">Rule set names to compose.</param>
        public static void ComposeRuleSets(string newRuleSetName, params string[] ruleSetNames)
        {
            CreateRuleSet(newRuleSetName);

            foreach (var ruleSetName in ruleSetNames)
            {
                var rules = GetRuleSetWithInheritance(ruleSetName);
                foreach (var rule in rules)
                {
                    AddRuleToSet(newRuleSetName, rule);
                }
            }

            Debug.Log($"[RSV] Composed rule set '{newRuleSetName}' from {ruleSetNames.Length} rule sets");
        }

        /// <summary>
        /// Creates a composable rule from a validation function.
        /// </summary>
        /// <param name="name">Rule name.</param>
        /// <param name="validation">Validation function.</param>
        /// <param name="description">Rule description.</param>
        /// <returns>Composable rule.</returns>
        public static ComposableRule CreateRule(
            string name,
            Func<JToken, string, LGD_ValidationReport, ValidationStatus?> validation,
            string description = "")
        {
            return new ComposableRule
            {
                Name = name,
                Validation = validation,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a composable rule with parameters.
        /// </summary>
        /// <param name="name">Rule name.</param>
        /// <param name="validation">Validation function with parameters.</param>
        /// <param name="parameters">Rule parameters.</param>
        /// <param name="description">Rule description.</param>
        /// <returns>Composable rule with parameters.</returns>
        public static ComposableRuleWithParams CreateRuleWithParams(
            string name,
            Func<JToken, string, LGD_ValidationReport, Dictionary<string, object>, ValidationStatus?> validation,
            Dictionary<string, object> parameters,
            string description = "")
        {
            return new ComposableRuleWithParams
            {
                Name = name,
                ValidationWithParams = validation,
                Parameters = parameters,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a composable rule from a custom rule.
        /// </summary>
        /// <param name="customRule">Custom validation rule.</param>
        /// <returns>Composable rule.</returns>
        public static ComposableRule CreateFromCustomRule(CustomValidationRule customRule)
        {
            return new ComposableRule
            {
                Name = customRule.Name,
                Validation = customRule.Rule,
                Description = customRule.Description,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a composable rule from a conditional rule.
        /// </summary>
        /// <param name="conditionalRule">Conditional validation rule.</param>
        /// <returns>Composable rule.</returns>
        public static ComposableRule CreateFromConditionalRule(ConditionalRule conditionalRule)
        {
            return new ComposableRule
            {
                Name = conditionalRule.Name,
                Validation = (token, path, report) =>
                {
                    if (!conditionalRule.Condition(token))
                        return null;

                    return conditionalRule.Validation(token, path, report);
                },
                Description = conditionalRule.Description,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Removes a rule from a rule set.
        /// </summary>
        /// <param name="ruleSetName">Rule set name.</param>
        /// <param name="ruleName">Rule name.</param>
        public static void RemoveRuleFromSet(string ruleSetName, string ruleName)
        {
            if (_ruleSets.ContainsKey(ruleSetName))
            {
                var rule = _ruleSets[ruleSetName].FirstOrDefault(r => r.Name == ruleName);
                if (rule != null)
                {
                    _ruleSets[ruleSetName].Remove(rule);
                    Debug.Log($"[RSV] Removed rule '{ruleName}' from rule set '{ruleSetName}'");
                }
            }
        }

        /// <summary>
        /// Clears a rule set.
        /// </summary>
        /// <param name="ruleSetName">Rule set name.</param>
        public static void ClearRuleSet(string ruleSetName)
        {
            if (_ruleSets.ContainsKey(ruleSetName))
            {
                _ruleSets[ruleSetName].Clear();
                Debug.Log($"[RSV] Cleared rule set '{ruleSetName}'");
            }
        }

        /// <summary>
        /// Deletes a rule set.
        /// </summary>
        /// <param name="ruleSetName">Rule set name.</param>
        public static void DeleteRuleSet(string ruleSetName)
        {
            if (_ruleSets.ContainsKey(ruleSetName))
            {
                _ruleSets.Remove(ruleSetName);
                Debug.Log($"[RSV] Deleted rule set '{ruleSetName}'");
            }

            if (_inheritanceMap.ContainsKey(ruleSetName))
            {
                _inheritanceMap.Remove(ruleSetName);
            }
        }

        /// <summary>
        /// Gets all rule set names.
        /// </summary>
        /// <returns>List of rule set names.</returns>
        public static List<string> GetAllRuleSetNames()
        {
            return new List<string>(_ruleSets.Keys);
        }

        /// <summary>
        /// Clears all rule sets and inheritance.
        /// </summary>
        public static void ClearAll()
        {
            _ruleSets.Clear();
            _inheritanceMap.Clear();
            Debug.Log("[RSV] Cleared all rule sets and inheritance");
        }

        /// <summary>
        /// Auto-creates rule sets from schemas.
        /// </summary>
        public static void AutoCreateRuleSetsFromSchemas()
        {
            var schemas = LGD_AssetUtility.FindAllAssetsOfType<DataSchemaDefinition>();

            foreach (var schema in schemas)
            {
                if (string.IsNullOrWhiteSpace(schema.SchemaId))
                    continue;

                var ruleSetName = schema.SchemaId;
                CreateRuleSet(ruleSetName);

                // Create rules from schema fields
                if (schema.RootNodes != null)
                {
                    foreach (var field in schema.RootNodes)
                    {
                        var rule = CreateRule(
                            $"field_{field.Name}",
                            (token, path, report) =>
                            {
                                // This is a placeholder - actual implementation would validate the field
                                return ValidationStatus.Pass;
                            },
                            $"Validates field '{field.Name}'"
                        );

                        AddRuleToSet(ruleSetName, rule);
                    }
                }
            }

            Debug.Log($"[RSV] Auto-created rule sets from {schemas.Length} schemas");
        }
    }

    /// <summary>
    /// Represents a composable validation rule.
    /// </summary>
    public class ComposableRule
    {
        public string Name { get; set; }
        public Func<JToken, string, LGD_ValidationReport, ValidationStatus?> Validation { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Validates a token against this rule.
        /// </summary>
        public virtual ValidationStatus? Validate(JToken token, string path, LGD_ValidationReport report)
        {
            return Validation?.Invoke(token, path, report);
        }

        public override string ToString()
        {
            return $"{Name}: {Description}";
        }
    }

    /// <summary>
    /// Represents a composable validation rule with parameters.
    /// </summary>
    public class ComposableRuleWithParams : ComposableRule
    {
        public Func<JToken, string, LGD_ValidationReport, Dictionary<string, object>, ValidationStatus?> ValidationWithParams { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public override ValidationStatus? Validate(JToken token, string path, LGD_ValidationReport report)
        {
            return ValidationWithParams?.Invoke(token, path, report, Parameters);
        }
    }
}