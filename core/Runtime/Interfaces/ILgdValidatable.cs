namespace LiveGameDev.Core
{
    /// <summary>
    /// Implement on any ScriptableObject or class that participates in
    /// suite-level validation passes (e.g., DataSchemaDefinition, ZoneDefinition).
    /// </summary>
    public interface ILgdValidatable
    {
        /// <summary>
        /// Validate this object's internal state.
        /// Append any issues to <paramref name="report"/> and return the highest severity found.
        /// </summary>
        ValidationStatus Validate(LGD_ValidationReport report);
    }
}
