using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Pre-compiled schema asset for runtime validation.
    /// Created by the Editor's RsvSchemaCompiler and used by RsvRuntimeValidator.
    /// This asset contains all the validation logic in a serialized form that can be
    /// loaded at runtime without Editor dependencies.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Live Game Dev/Runtime Schema/Compiled Schema Asset",
        fileName = "NewCompiledSchema")]
    public class RsvCompiledSchemaAsset : ScriptableObject
    {
        [Header("Schema Metadata")]
        [Tooltip("Unique identifier for this schema. Must match the original DataSchemaDefinition.SchemaId.")]
        public string SchemaId;

        [Tooltip("Semantic version of this schema (e.g. 1.0.0).")]
        public string Version;

        [Tooltip("Description of what this schema validates.")]
        [TextArea(2, 4)]
        public string Description;

        [Header("Validation Configuration")]
        [Tooltip("Maximum nesting depth allowed for JSON objects. Prevents stack overflow attacks.")]
        [Min(1)]
        public int MaxNestingDepth = 100;

        [Tooltip("Maximum string length allowed. Prevents OOM attacks.")]
        [Min(0)]
        public int MaxStringLength = 1000000;

        [Tooltip("Maximum array length allowed. Prevents OOM attacks.")]
        [Min(0)]
        public int MaxArrayLength = 10000;

        [Header("Compiled Schema Nodes")]
        [Tooltip("Root-level validation nodes. These define the expected structure of the JSON.")]
        public List<RsvCompiledNode> RootNodes;

        /// <summary>
        /// Validates that this compiled schema asset is properly configured.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(SchemaId))
                return false;

            if (string.IsNullOrWhiteSpace(Version))
                return false;

            if (RootNodes == null || RootNodes.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a node by name from the root nodes.
        /// </summary>
        public RsvCompiledNode GetRootNode(string name)
        {
            if (RootNodes == null)
                return null;

            foreach (var node in RootNodes)
            {
                if (node.Name == name)
                    return node;
            }

            return null;
        }
    }

    /// <summary>
    /// A compiled validation node that represents a field in the JSON schema.
    /// This is a serialized form of the validation logic that can be executed at runtime.
    /// </summary>
    [Serializable]
    public class RsvCompiledNode
    {
        [Tooltip("Name of the field in the JSON object.")]
        public string Name;

        [Tooltip("Expected type of the field.")]
        public RsvFieldType FieldType;

        [Tooltip("Whether this field is required to be present.")]
        public bool IsRequired;

        [Tooltip("Default value to use if the field is missing (optional).")]
        public string DefaultValue;

        [Tooltip("Description of what this field represents.")]
        [TextArea(1, 3)]
        public string Description;

        [Header("Numeric Constraints")]
        [Tooltip("Whether this field has min/max value constraints.")]
        public bool HasMinMax;

        [Tooltip("Minimum allowed value (for Integer and Number types).")]
        public double Min;

        [Tooltip("Maximum allowed value (for Integer and Number types).")]
        public double Max;

        [Header("String Constraints")]
        [Tooltip("Minimum string length.")]
        [Min(0)]
        public int MinLength;

        [Tooltip("Maximum string length.")]
        [Min(0)]
        public int MaxLength;

        [Tooltip("Regular expression pattern for string validation (optional).")]
        public string Pattern;

        [Header("Enum Constraints")]
        [Tooltip("Allowed enum values (if this field is an enum).")]
        public string[] EnumValues;

        [Header("Array Constraints")]
        [Tooltip("Minimum number of items in array.")]
        [Min(0)]
        public int MinItems;

        [Tooltip("Maximum number of items in array.")]
        [Min(0)]
        public int MaxItems;

        [Tooltip("Whether all array items must be unique.")]
        public bool UniqueItems;

        [Header("Child Nodes")]
        [Tooltip("Child nodes for Object and Array types.")]
        public List<RsvCompiledNode> Children;

        /// <summary>
        /// Validates that this node is properly configured.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return false;

            if (HasMinMax && Min > Max)
                return false;

            if (MinLength < 0 || MaxLength < 0)
                return false;

            if (MinItems < 0 || MaxItems < 0)
                return false;

            if (MinItems > MaxItems)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Field types supported by RSV validation.
    /// </summary>
    public enum RsvFieldType
    {
        String,
        Integer,
        Number,
        Boolean,
        Object,
        Array,
        Null
    }
}