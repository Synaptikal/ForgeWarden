using System;
using System.Collections.Generic;

namespace NarrativeLayerManager
{
    /// <summary>
    /// Pure evaluation engine for narrative conditions and binding rules.
    /// </summary>
    /// <remarks>
    /// No UnityEngine or Editor dependencies — fully unit-testable in isolation.
    /// 
    /// <para>Key methods:</para>
    /// <list type="bullet">
    /// <item><see cref="EvaluateGroup"/> — Evaluates if a condition group is satisfied by a state</item>
    /// <item><see cref="ResolveBinding"/> — Gets ordered list of triggered rules for a binding</item>
    /// </list>
    /// </remarks>
    public static class NLM_Evaluator
    {
        #region Public API

        /// <summary>
        /// Evaluates a condition group against a narrative state.
        /// </summary>
        /// <param name="group">The condition group to evaluate</param>
        /// <param name="state">The narrative state to evaluate against</param>
        /// <returns>True if the condition group is satisfied</returns>
        /// <remarks>
        /// An empty condition group always returns true (useful for fallback rules).
        /// Conditions are evaluated in order with short-circuit logic (AND/OR).
        /// </remarks>
        public static bool EvaluateGroup(NarrativeConditionGroup group, NarrativeStateDefinition state)
        {
            if (group == null || group.IsEmpty) return true;

            bool result = EvaluateSingle(group.Conditions[0], state);
            for (int i = 1; i < group.Conditions.Count; i++)
            {
                bool next = EvaluateSingle(group.Conditions[i], state);
                result = group.Conditions[i - 1].LogicToNext == ConditionLogic.And
                    ? result && next
                    : result || next;
            }
            return result;
        }

        /// <summary>
        /// Resolves which rules from a binding are triggered by the given state.
        /// </summary>
        /// <param name="binding">The object binding containing rules</param>
        /// <param name="state">The narrative state to evaluate against</param>
        /// <returns>Ordered list of triggered rules (respects FallThrough)</returns>
        /// <remarks>
        /// Rules are evaluated top-to-bottom. First match wins unless FallThrough is true.
        /// </remarks>
        public static List<NarrativeObjectBinding.BindingRule> ResolveBinding(
            NarrativeObjectBinding binding, NarrativeStateDefinition state)
        {
            var triggered = new List<NarrativeObjectBinding.BindingRule>();
            if (binding?.Rules == null) return triggered;
            foreach (var rule in binding.Rules)
            {
                if (!EvaluateGroup(rule.Condition, state)) continue;
                triggered.Add(rule);
                if (!rule.FallThrough) break;
            }
            return triggered;
        }

        #endregion

        #region Private Evaluation

        private static bool EvaluateSingle(NarrativeCondition cond, NarrativeStateDefinition state)
        {
            if (cond == null) return true;

            var variable = state?.GetVariable(cond.VariableName);

            if (cond.Operator == ConditionOperator.IsTrue)
                return variable?.Type == NarrativeVariableType.Bool && variable.BoolValue;

            if (cond.Operator == ConditionOperator.IsFalse)
                return variable == null
                    || variable.Type != NarrativeVariableType.Bool
                    || !variable.BoolValue;

            if (variable == null)
                return cond.Operator == ConditionOperator.NotEquals;

            try
            {
                return variable.Type switch
                {
                    NarrativeVariableType.Bool => CompareBool(variable.BoolValue, cond.Value, cond.Operator),
                    NarrativeVariableType.Int => CompareInt(variable.IntValue, cond.Value, cond.Operator),
                    NarrativeVariableType.Float => CompareFloat(variable.FloatValue, cond.Value, cond.Operator),
                    NarrativeVariableType.String => CompareString(variable.StringValue, cond.Value, cond.Operator),
                    _ => false
                };
            }
            catch { return false; }
        }

        private static bool CompareBool(bool actual, string raw, ConditionOperator op)
        {
            bool target = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
            return op switch
            {
                ConditionOperator.Equals => actual == target,
                ConditionOperator.NotEquals => actual != target,
                _ => false
            };
        }

        private static bool CompareInt(int actual, string raw, ConditionOperator op)
        {
            if (!int.TryParse(raw, out int t)) return false;
            return op switch
            {
                ConditionOperator.Equals => actual == t,
                ConditionOperator.NotEquals => actual != t,
                ConditionOperator.GreaterThan => actual > t,
                ConditionOperator.LessThan => actual < t,
                ConditionOperator.GreaterOrEqual => actual >= t,
                ConditionOperator.LessOrEqual => actual <= t,
                _ => false
            };
        }

        private static bool CompareFloat(float actual, string raw, ConditionOperator op)
        {
            if (!float.TryParse(raw,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float t)) return false;
            return op switch
            {
                ConditionOperator.Equals => MathF.Abs(actual - t) < 0.0001f,
                ConditionOperator.NotEquals => MathF.Abs(actual - t) >= 0.0001f,
                ConditionOperator.GreaterThan => actual > t,
                ConditionOperator.LessThan => actual < t,
                ConditionOperator.GreaterOrEqual => actual >= t,
                ConditionOperator.LessOrEqual => actual <= t,
                _ => false
            };
        }

        private static bool CompareString(string actual, string target, ConditionOperator op)
        {
            bool eq = string.Equals(actual, target, StringComparison.OrdinalIgnoreCase);
            return op switch
            {
                ConditionOperator.Equals => eq,
                ConditionOperator.NotEquals => !eq,
                ConditionOperator.GreaterThan => string.Compare(actual, target, StringComparison.OrdinalIgnoreCase) > 0,
                ConditionOperator.LessThan => string.Compare(actual, target, StringComparison.OrdinalIgnoreCase) < 0,
                ConditionOperator.GreaterOrEqual => string.Compare(actual, target, StringComparison.OrdinalIgnoreCase) >= 0,
                ConditionOperator.LessOrEqual => string.Compare(actual, target, StringComparison.OrdinalIgnoreCase) <= 0,
                _ => false
            };
        }

        #endregion
    }
}
