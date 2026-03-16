using System;
using System.Collections.Generic;
using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// Comparison operators available for narrative conditions.
    /// </summary>
    public enum ConditionOperator
    {
        /// <summary>Values are equal</summary>
        Equals,
        /// <summary>Values are not equal</summary>
        NotEquals,
        /// <summary>Left value is greater than right</summary>
        GreaterThan,
        /// <summary>Left value is less than right</summary>
        LessThan,
        /// <summary>Left value is greater than or equal to right</summary>
        GreaterOrEqual,
        /// <summary>Left value is less than or equal to right</summary>
        LessOrEqual,
        /// <summary>Boolean variable is true</summary>
        IsTrue,
        /// <summary>Boolean variable is false or undefined</summary>
        IsFalse
    }

    /// <summary>
    /// Logical operators for chaining multiple conditions together.
    /// </summary>
    public enum ConditionLogic { And, Or }

    /// <summary>
    /// A single condition that compares a narrative variable against a target value.
    /// </summary>
    /// <remarks>
    /// Conditions are evaluated by <see cref="NLM_Evaluator"/> against a <see cref="NarrativeStateDefinition"/>.
    /// Example: Chapter >= 3, Phase == "PostWar", QuestComplete == true
    /// </remarks>
    [Serializable]
    public class NarrativeCondition
    {
        [Tooltip("Must match a NarrativeVariable.Name in the evaluated state.")]
        public string VariableName;

        [Tooltip("How to compare the variable against the value.")]
        public ConditionOperator Operator = ConditionOperator.Equals;

        [Tooltip("Comparison value. Serialized as string, parsed to the variable's type at evaluation.")]
        public string Value;

        [Tooltip("How this condition chains with the next one in the group.")]
        public ConditionLogic LogicToNext = ConditionLogic.And;
    }

    /// <summary>
    /// A group of conditions that are evaluated together as a single logical expression.
    /// </summary>
    /// <remarks>
    /// An empty condition group always evaluates to true, making it useful for fallback rules.
    /// Conditions are evaluated in order, with LogicToNext determining how they chain together.
    /// </remarks>
    [Serializable]
    public class NarrativeConditionGroup
    {
        [Tooltip("The conditions in this group, evaluated in order.")]
        public List<NarrativeCondition> Conditions = new();

        /// <summary>
        /// Returns true if this group has no conditions.
        /// An empty group always evaluates to true (unconditional fallback rule).
        /// </summary>
        public bool IsEmpty => Conditions == null || Conditions.Count == 0;
    }
}
