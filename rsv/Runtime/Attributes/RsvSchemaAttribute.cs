using System;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Apply to a string field on a MonoBehaviour or ScriptableObject to register
    /// it for RSV validation whenever the asset is saved (Editor) or at runtime.
    /// Usage: [RsvSchema("AbilitySchema")] public string AbilityJson;
    ///
    /// Runtime Validation:
    /// - Call RsvRuntimeValidator.ValidateComponent(component) to validate all [RsvSchema] fields
    /// - Call RsvRuntimeValidator.ValidateScriptableObject(scriptableObject) for ScriptableObjects
    /// - The schema must be registered in RsvSchemaRegistry before validation
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class RsvSchemaAttribute : Attribute
    {
        /// <summary>The SchemaId of the DataSchemaDefinition to validate against.</summary>
        public string SchemaId { get; }

        /// <summary>
        /// Whether to automatically validate this field at runtime when the component awakes.
        /// Default is false to avoid performance overhead.
        /// </summary>
        public bool AutoValidateOnAwake { get; set; }

        /// <summary>
        /// Whether to log validation errors to the console.
        /// Default is true.
        /// </summary>
        public bool LogErrors { get; set; } = true;

        /// <summary>
        /// Whether to throw an exception on validation errors.
        /// Default is false.
        /// </summary>
        public bool ThrowOnError { get; set; } = false;

        public RsvSchemaAttribute(string schemaId)
        {
            SchemaId = schemaId;
        }

        /// <summary>
        /// Creates an attribute with automatic runtime validation enabled.
        /// </summary>
        public RsvSchemaAttribute(string schemaId, bool autoValidateOnAwake) : this(schemaId)
        {
            AutoValidateOnAwake = autoValidateOnAwake;
        }
    }
}
