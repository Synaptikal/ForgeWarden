using LiveGameDev.Core;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Error codes for RSV validation errors.
    /// Used for programmatic error handling and automated responses.
    /// </summary>
    public enum RsvErrorCode
    {
        // Setup errors (100-199)
        SchemaNull = 100,
        BindingNull = 101,
        SchemaNotAssigned = 102,
        InvalidSchemaId = 103,

        // Source errors (200-299)
        JsonTextNullOrEmpty = 200,
        FileNotFound = 201,
        FileTooLarge = 202,
        StreamingAssetsFileNotFound = 203,
        ResourcesAssetNotFound = 204,
        InvalidUrlFormat = 205,
        HttpsRequired = 206,
        HttpRequestFailed = 207,
        HttpRequestTimeout = 208,
        ResponseTooLarge = 209,

        // Parse errors (300-399)
        ParseError = 300,
        InvalidJsonSyntax = 301,

        // Validation errors (400-499)
        MissingField = 400,
        TypeMismatch = 401,
        RangeViolation = 402,
        EnumViolation = 403,
        DepthLimitExceeded = 404,
        RequiredFieldNull = 405,

        // Schema errors (500-599)
        SchemaValidationError = 500,
        MinGreaterThanMax = 501,
        EmptySchemaId = 502,
        EmptyVersion = 503,
        NoRootNodes = 504,
        EmptyNodeName = 505,

        // Migration errors (600-699)
        MigrationScriptNotFound = 600,
        MigrationScriptInvalid = 601,
        MigrationScriptExecutionFailed = 602,
        DuplicateMigrationVersion = 603,
        MigrationHintsNotOrdered = 604,

        // Unknown error
        Unknown = 999
    }

    /// <summary>
    /// Extension methods for RsvErrorCode.
    /// </summary>
    public static class RsvErrorCodeExtensions
    {
        /// <summary>
        /// Gets a human-readable description for the error code.
        /// </summary>
        public static string GetDescription(this RsvErrorCode code)
        {
            return code switch
            {
                RsvErrorCode.SchemaNull => "Schema is null",
                RsvErrorCode.BindingNull => "Binding is null",
                RsvErrorCode.SchemaNotAssigned => "No schema assigned to binding",
                RsvErrorCode.InvalidSchemaId => "Invalid schema ID",
                RsvErrorCode.JsonTextNullOrEmpty => "JSON text is null or empty",
                RsvErrorCode.FileNotFound => "File not found",
                RsvErrorCode.FileTooLarge => "File exceeds size limit",
                RsvErrorCode.StreamingAssetsFileNotFound => "StreamingAssets file not found",
                RsvErrorCode.ResourcesAssetNotFound => "Resources asset not found",
                RsvErrorCode.InvalidUrlFormat => "Invalid URL format",
                RsvErrorCode.HttpsRequired => "HTTPS is required for remote URLs",
                RsvErrorCode.HttpRequestFailed => "HTTP request failed",
                RsvErrorCode.HttpRequestTimeout => "HTTP request timed out",
                RsvErrorCode.ResponseTooLarge => "Response exceeds size limit",
                RsvErrorCode.ParseError => "JSON parse error",
                RsvErrorCode.InvalidJsonSyntax => "Invalid JSON syntax",
                RsvErrorCode.MissingField => "Required field is missing",
                RsvErrorCode.TypeMismatch => "Field type mismatch",
                RsvErrorCode.RangeViolation => "Value out of allowed range",
                RsvErrorCode.EnumViolation => "Value not in allowed enum values",
                RsvErrorCode.DepthLimitExceeded => "Nesting depth exceeded maximum",
                RsvErrorCode.RequiredFieldNull => "Required field is null",
                RsvErrorCode.SchemaValidationError => "Schema validation error",
                RsvErrorCode.MinGreaterThanMax => "Minimum value greater than maximum",
                RsvErrorCode.EmptySchemaId => "Schema ID is empty",
                RsvErrorCode.EmptyVersion => "Schema version is empty",
                RsvErrorCode.NoRootNodes => "Schema has no root nodes",
                RsvErrorCode.EmptyNodeName => "Schema node has empty name",
                RsvErrorCode.MigrationScriptNotFound => "Migration script not found",
                RsvErrorCode.MigrationScriptInvalid => "Migration script is invalid",
                RsvErrorCode.MigrationScriptExecutionFailed => "Migration script execution failed",
                RsvErrorCode.DuplicateMigrationVersion => "Duplicate migration version",
                RsvErrorCode.MigrationHintsNotOrdered => "Migration hints not in ascending order",
                RsvErrorCode.Unknown => "Unknown error",
                _ => "Unknown error"
            };
        }

        /// <summary>
        /// Gets the severity level for the error code.
        /// </summary>
        public static ValidationStatus GetSeverity(this RsvErrorCode code)
        {
            return code switch
            {
                RsvErrorCode.SchemaNull => ValidationStatus.Error,
                RsvErrorCode.BindingNull => ValidationStatus.Error,
                RsvErrorCode.SchemaNotAssigned => ValidationStatus.Warning,
                RsvErrorCode.InvalidSchemaId => ValidationStatus.Error,
                RsvErrorCode.JsonTextNullOrEmpty => ValidationStatus.Error,
                RsvErrorCode.FileNotFound => ValidationStatus.Error,
                RsvErrorCode.FileTooLarge => ValidationStatus.Error,
                RsvErrorCode.StreamingAssetsFileNotFound => ValidationStatus.Error,
                RsvErrorCode.ResourcesAssetNotFound => ValidationStatus.Error,
                RsvErrorCode.InvalidUrlFormat => ValidationStatus.Error,
                RsvErrorCode.HttpsRequired => ValidationStatus.Critical,
                RsvErrorCode.HttpRequestFailed => ValidationStatus.Error,
                RsvErrorCode.HttpRequestTimeout => ValidationStatus.Error,
                RsvErrorCode.ResponseTooLarge => ValidationStatus.Error,
                RsvErrorCode.ParseError => ValidationStatus.Critical,
                RsvErrorCode.InvalidJsonSyntax => ValidationStatus.Critical,
                RsvErrorCode.MissingField => ValidationStatus.Error,
                RsvErrorCode.TypeMismatch => ValidationStatus.Error,
                RsvErrorCode.RangeViolation => ValidationStatus.Error,
                RsvErrorCode.EnumViolation => ValidationStatus.Error,
                RsvErrorCode.DepthLimitExceeded => ValidationStatus.Critical,
                RsvErrorCode.RequiredFieldNull => ValidationStatus.Error,
                RsvErrorCode.SchemaValidationError => ValidationStatus.Error,
                RsvErrorCode.MinGreaterThanMax => ValidationStatus.Error,
                RsvErrorCode.EmptySchemaId => ValidationStatus.Error,
                RsvErrorCode.EmptyVersion => ValidationStatus.Warning,
                RsvErrorCode.NoRootNodes => ValidationStatus.Warning,
                RsvErrorCode.EmptyNodeName => ValidationStatus.Error,
                RsvErrorCode.MigrationScriptNotFound => ValidationStatus.Warning,
                RsvErrorCode.MigrationScriptInvalid => ValidationStatus.Warning,
                RsvErrorCode.MigrationScriptExecutionFailed => ValidationStatus.Error,
                RsvErrorCode.DuplicateMigrationVersion => ValidationStatus.Warning,
                RsvErrorCode.MigrationHintsNotOrdered => ValidationStatus.Warning,
                RsvErrorCode.Unknown => ValidationStatus.Error,
                _ => ValidationStatus.Error
            };
        }
    }
}
