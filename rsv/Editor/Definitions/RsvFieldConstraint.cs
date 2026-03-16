using System;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Defines the type constraints and validation rules for a single schema field node.
    /// </summary>
    [Serializable]
    public class RsvFieldConstraint
    {
        [Tooltip("Whether this field must be present in the JSON payload.")]
        [SerializeField] public bool IsRequired = true;

        [Tooltip("Expected JSON type for this field.")]
        [SerializeField] public RsvFieldType FieldType = RsvFieldType.String;

        [Tooltip("Minimum allowed value (for Integer and Number types).")]
        [SerializeField] public double Min = 0;

        [Tooltip("Maximum allowed value (for Integer and Number types).")]
        [SerializeField] public double Max = double.MaxValue;

        [Tooltip("Whether Min/Max constraints are enforced.")]
        [SerializeField] public bool HasMinMax = false;

        [Tooltip("If set, the field value must be one of these strings/values.")]
        [SerializeField] public string[] EnumValues;

        [Tooltip("Optional reference ID to a shared field group within the same schema.")]
        [SerializeField] public string RefId;

        [Tooltip("Human-readable description of this field (for documentation / example generation).")]
        [SerializeField] public string Description;

        [Tooltip("Example default value used in example JSON generation only. Not enforced at runtime.")]
        [SerializeField] public string DefaultValue;

        [Header("String Constraints")]
        [Tooltip("Minimum string length (for String type).")]
        [SerializeField] public int MinLength;

        [Tooltip("Maximum string length (for String type).")]
        [SerializeField] public int MaxLength;

        [Tooltip("Regular expression pattern for string validation (optional).")]
        [SerializeField] public string Pattern;

        [Header("Array Constraints")]
        [Tooltip("Minimum number of items in array (for Array type).")]
        [SerializeField] public int MinItems;

        [Tooltip("Maximum number of items in array (for Array type).")]
        [SerializeField] public int MaxItems;

        [Tooltip("Whether all array items must be unique (for Array type).")]
        [SerializeField] public bool UniqueItems;
    }
}
