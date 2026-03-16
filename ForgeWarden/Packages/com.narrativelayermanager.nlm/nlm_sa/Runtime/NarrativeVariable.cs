using System;
using UnityEngine;

namespace NarrativeLayerManager
{
    /// <summary>
    /// Defines the data types supported by narrative variables.
    /// </summary>
    public enum NarrativeVariableType { Bool, Int, Float, String }

    /// <summary>
    /// A single typed narrative variable stored in a <see cref="NarrativeStateDefinition"/>.
    /// Serializes as a discriminated union — only the field matching <see cref="Type"/> is meaningful.
    /// </summary>
    /// <remarks>
    /// Use this to store narrative state such as quest progress, chapter numbers, or player choices.
    /// Example: Chapter=3, QuestComplete=true, PlayerName="Hero"
    /// </remarks>
    [Serializable]
    public class NarrativeVariable
    {
        [Tooltip("Unique key. Referenced by NarrativeConditions and PropertyOverrides.")]
        public string Name;

        [Tooltip("The data type of this variable.")]
        public NarrativeVariableType Type = NarrativeVariableType.Bool;

        [Tooltip("Value when Type is Bool")]
        public bool BoolValue;

        [Tooltip("Value when Type is Int")]
        public int IntValue;

        [Tooltip("Value when Type is Float")]
        public float FloatValue;

        [Tooltip("Value when Type is String")]
        public string StringValue;

        /// <summary>
        /// Gets the value of this variable as a boxed object based on its Type.
        /// </summary>
        /// <returns>The value as object, or null if type is unrecognized</returns>
        public object GetValue() => Type switch
        {
            NarrativeVariableType.Bool => (object)BoolValue,
            NarrativeVariableType.Int => IntValue,
            NarrativeVariableType.Float => FloatValue,
            NarrativeVariableType.String => StringValue,
            _ => null
        };

        /// <summary>
        /// Parses a string value and sets the appropriate typed field based on Type.
        /// </summary>
        /// <param name="raw">The string to parse</param>
        /// <remarks>
        /// Bool parsing is case-insensitive. Float parsing uses invariant culture.
        /// </remarks>
        public void SetFromString(string raw)
        {
            switch (Type)
            {
                case NarrativeVariableType.Bool:
                    BoolValue = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case NarrativeVariableType.Int:
                    IntValue = int.TryParse(raw, out int i) ? i : 0;
                    break;
                case NarrativeVariableType.Float:
                    FloatValue = float.TryParse(raw,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float f) ? f : 0f;
                    break;
                case NarrativeVariableType.String:
                    StringValue = raw;
                    break;
            }
        }

        /// <summary>
        /// Creates a deep copy of this variable.
        /// </summary>
        /// <returns>A new NarrativeVariable with the same values</returns>
        public NarrativeVariable Clone() => new NarrativeVariable
        {
            Name = Name,
            Type = Type,
            BoolValue = BoolValue,
            IntValue = IntValue,
            FloatValue = FloatValue,
            StringValue = StringValue
        };

        /// <summary>
        /// Returns a string representation of this variable.
        /// </summary>
        /// <returns>Formatted string: "Name (Type) = Value"</returns>
        public override string ToString() => $"{Name} ({Type}) = {GetValue()}";
    }
}
