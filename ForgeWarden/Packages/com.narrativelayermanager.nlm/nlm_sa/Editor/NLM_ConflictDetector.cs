using System.Collections.Generic;
using UnityEngine;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Detects conflicts and issues in narrative bindings and layers.
    /// </summary>
    /// <remarks>
    /// Analyzes bindings against a layer to find:
    /// - Unreachable fallback rules
    /// - Missing variable references
    /// - Overlapping rule overrides
    /// </remarks>
    public static class NLM_ConflictDetector
    {
        /// <summary>
        /// Represents a single conflict or issue found during analysis.
        /// </summary>
        public class ConflictEntry
        {
            /// <summary>Severity level of this conflict</summary>
            public NLM_Status Severity;

            /// <summary>Type/category of conflict</summary>
            public string Type;

            /// <summary>Human-readable description</summary>
            public string Message;

            /// <summary>The affected GameObject</summary>
            public GameObject AffectedObject;
        }

        /// <summary>
        /// Analyzes a narrative layer for conflicts across all bindings.
        /// </summary>
        /// <param name="layer">The layer to analyze</param>
        /// <param name="bindings">The bindings to check against the layer</param>
        /// <returns>List of detected conflicts</returns>
        /// <remarks>
        /// Checks for:
        /// - UnreachableFallback: Rules with no conditions that block later rules
        /// - MissingVariable: Conditions referencing variables not in any beat
        /// - OverlappingRules: Multiple rules applying the same override type
        /// </remarks>
        public static List<ConflictEntry> Analyze(
            NarrativeLayerDefinition layer,
            List<NarrativeObjectBinding> bindings)
        {
            var conflicts = new List<ConflictEntry>();
            if (layer?.Beats == null || bindings == null) return conflicts;

            // Build known variable universe from all beats
            var knownVars = new HashSet<string>();
            foreach (var beat in layer.Beats)
                if (beat?.State?.Variables != null)
                    foreach (var v in beat.State.Variables)
                        if (!string.IsNullOrWhiteSpace(v.Name))
                            knownVars.Add(v.Name);

            foreach (var binding in bindings)
            {
                if (binding?.Rules == null) continue;

                CheckUnreachableFallback(binding, conflicts);
                CheckMissingVariables(binding, knownVars, layer.name, conflicts);
                CheckOverlappingRules(binding, layer, conflicts);
            }

            return conflicts;
        }

        #region Private Check Methods

        private static void CheckUnreachableFallback(
            NarrativeObjectBinding binding,
            List<ConflictEntry> conflicts)
        {
            for (int i = 0; i < binding.Rules.Count - 1; i++)
            {
                var r = binding.Rules[i];
                if (r.Condition.IsEmpty && !r.FallThrough)
                    conflicts.Add(new ConflictEntry
                    {
                        Severity = NLM_Status.Warning,
                        Type = "UnreachableFallback",
                        Message = $"'{binding.gameObject.name}': Rule '{r.RuleName}' has no " +
                                 $"conditions (always true) and blocks the {binding.Rules.Count - i - 1} rule(s) below it.",
                        AffectedObject = binding.gameObject
                    });
            }
        }

        private static void CheckMissingVariables(
            NarrativeObjectBinding binding,
            HashSet<string> knownVars,
            string layerName,
            List<ConflictEntry> conflicts)
        {
            foreach (var rule in binding.Rules)
                foreach (var cond in rule.Condition?.Conditions ?? new List<NarrativeCondition>())
                    if (!string.IsNullOrEmpty(cond.VariableName) && !knownVars.Contains(cond.VariableName))
                        conflicts.Add(new ConflictEntry
                        {
                            Severity = NLM_Status.Warning,
                            Type = "MissingVariable",
                            Message = $"'{binding.gameObject.name}': Condition references variable " +
                                     $"'{cond.VariableName}' not found in any beat of layer '{layerName}'.",
                            AffectedObject = binding.gameObject
                        });
        }

        private static void CheckOverlappingRules(
            NarrativeObjectBinding binding,
            NarrativeLayerDefinition layer,
            List<ConflictEntry> conflicts)
        {
            foreach (var beat in layer.Beats)
            {
                if (beat?.State == null) continue;
                var triggered = NLM_Evaluator.ResolveBinding(binding, beat.State);
                if (triggered.Count <= 1) continue;

                var seen = new Dictionary<OverrideType, string>();
                foreach (var rule in triggered)
                    foreach (var ovr in rule.Overrides)
                    {
                        if (seen.TryGetValue(ovr.Type, out string first))
                            conflicts.Add(new ConflictEntry
                            {
                                Severity = NLM_Status.Warning,
                                Type = "OverlappingRules",
                                Message = $"'{binding.gameObject.name}' at beat '{beat.BeatName}': " +
                                         $"Rules '{first}' and '{rule.RuleName}' both apply {ovr.Type}. Last wins silently.",
                                AffectedObject = binding.gameObject
                            });
                        else seen[ovr.Type] = rule.RuleName;
                    }
            }
        }

        #endregion
    }
}
