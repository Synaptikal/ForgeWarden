using System.Collections.Generic;
using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// A complete snapshot of all narrative variables at a specific story beat.
    /// </summary>
    /// <remarks>
    /// Create via: Assets > Create > Narrative Layer Manager > State Definition
    /// 
    /// This ScriptableObject stores the values of all narrative variables at a specific
    /// moment in your story. Use it to define quest states, chapter progress, or any
    /// narrative condition that affects your scene.
    /// </remarks>
    [CreateAssetMenu(
        menuName = "Narrative Layer Manager/State Definition",
        fileName = "NewNarrativeState")]
    public class NarrativeStateDefinition : ScriptableObject
    {
        [Tooltip("Human-readable label shown in the NLM beat inspector.")]
        public string DisplayName;

        [Tooltip("Description of what this state represents in the narrative.")]
        [TextArea(1, 3)]
        public string Description;

        [Tooltip("All narrative variables at this story beat.")]
        public List<NarrativeVariable> Variables = new();

        /// <summary>
        /// Finds a variable by name in this state.
        /// </summary>
        /// <param name="varName">The name of the variable to find</param>
        /// <returns>The NarrativeVariable if found, null otherwise</returns>
        public NarrativeVariable GetVariable(string varName)
        {
            foreach (var v in Variables)
                if (v.Name == varName) return v;
            return null;
        }

        /// <summary>
        /// Gets a boolean variable value with fallback.
        /// </summary>
        /// <param name="n">Variable name</param>
        /// <param name="fallback">Value to return if variable not found or wrong type</param>
        /// <returns>The variable value or fallback</returns>
        public bool GetBool(string n, bool fallback = false) => GetVariable(n) is { Type: NarrativeVariableType.Bool } v ? v.BoolValue : fallback;

        /// <summary>
        /// Gets an integer variable value with fallback.
        /// </summary>
        /// <param name="n">Variable name</param>
        /// <param name="fallback">Value to return if variable not found or wrong type</param>
        /// <returns>The variable value or fallback</returns>
        public int GetInt(string n, int fallback = 0) => GetVariable(n) is { Type: NarrativeVariableType.Int } v ? v.IntValue : fallback;

        /// <summary>
        /// Gets a float variable value with fallback.
        /// </summary>
        /// <param name="n">Variable name</param>
        /// <param name="fallback">Value to return if variable not found or wrong type</param>
        /// <returns>The variable value or fallback</returns>
        public float GetFloat(string n, float fallback = 0f) => GetVariable(n) is { Type: NarrativeVariableType.Float } v ? v.FloatValue : fallback;

        /// <summary>
        /// Gets a string variable value with fallback.
        /// </summary>
        /// <param name="n">Variable name</param>
        /// <param name="fallback">Value to return if variable not found or wrong type</param>
        /// <returns>The variable value or fallback</returns>
        public string GetString(string n, string fallback = "") => GetVariable(n) is { Type: NarrativeVariableType.String } v ? v.StringValue : fallback;

        /// <summary>
        /// Creates a deep clone of this state definition.
        /// </summary>
        /// <returns>A new instance with copied variable values</returns>
        /// <remarks>
        /// Used by the preview applicator to record baseline state before modifications.
        /// </remarks>
        public NarrativeStateDefinition DeepClone()
        {
            var c = CreateInstance<NarrativeStateDefinition>();
            c.name = name + "_Clone";
            c.DisplayName = DisplayName;
            c.Description = Description;
            c.Variables = new List<NarrativeVariable>();
            foreach (var v in Variables) c.Variables.Add(v.Clone());
            return c;
        }

        /// <summary>
        /// Validates this state definition and returns a report of any issues.
        /// </summary>
        /// <returns>A validation report containing errors, warnings, or pass status</returns>
        /// <remarks>
        /// Checks for:
        /// - Empty variable names
        /// - Duplicate variable names
        /// </remarks>
        public NLM_ValidationReport Validate()
        {
            var report = new NLM_ValidationReport("State");
            var seen = new HashSet<string>();
            foreach (var v in Variables)
            {
                if (string.IsNullOrWhiteSpace(v.Name))
                    report.Add(NLM_Status.Warning, "Variable", $"State '{name}': A variable has an empty name.");
                else if (!seen.Add(v.Name))
                    report.Add(NLM_Status.Error, "Variable", $"State '{name}': Duplicate variable name '{v.Name}'.");
            }
            if (!report.HasErrors && !report.HasWarnings)
                report.Add(NLM_Status.Pass, "State", $"State '{name}' is valid ({Variables.Count} variables).");
            return report;
        }
    }
}
