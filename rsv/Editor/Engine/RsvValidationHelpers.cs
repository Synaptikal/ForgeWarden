using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.Core;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Shared validation utilities and helper methods.
    /// Reduces code duplication across the RSV validation system.
    /// </summary>
    internal static class RsvValidationHelpers
    {
        /// <summary>
        /// Validates that a string is not null or whitespace.
        /// </summary>
        /// <param name="value">The string to validate.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <param name="suggestedFix">Optional suggested fix message.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateRequiredString(string value, string fieldName, LGD_ValidationReport report, string suggestedFix = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} is required but is null or empty.",
                    suggestedFix: suggestedFix ?? $"Provide a valid {fieldName}.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a value is not null.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <param name="suggestedFix">Optional suggested fix message.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateRequired<T>(T value, string fieldName, LGD_ValidationReport report, string suggestedFix = null) where T : class
        {
            if (value == null)
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} is required but is null.",
                    suggestedFix: suggestedFix ?? $"Provide a valid {fieldName}.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a numeric value is within a specified range.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateRange(double value, double min, double max, string fieldName, LGD_ValidationReport report)
        {
            if (value < min)
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} value {value} is below minimum {min}.",
                    suggestedFix: $"Use a value >= {min}.");
                return false;
            }

            if (value > max)
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} value {value} exceeds maximum {max}.",
                    suggestedFix: $"Use a value <= {max}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a value is in a set of allowed values.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="allowedValues">The set of allowed values.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <param name="caseSensitive">Whether the comparison should be case-sensitive.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateEnum(string value, string[] allowedValues, string fieldName, LGD_ValidationReport report, bool caseSensitive = false)
        {
            if (allowedValues == null || allowedValues.Length == 0)
                return true;

            var comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            var isAllowed = Array.Exists(allowedValues, v => v.Equals(value, comparison));

            if (!isAllowed)
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} value '{value}' is not in allowed values [{string.Join(", ", allowedValues)}].",
                    suggestedFix: $"Use one of the allowed values: {string.Join(", ", allowedValues)}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a collection is not null or empty.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to validate.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <param name="suggestedFix">Optional suggested fix message.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateNotEmpty<T>(IEnumerable<T> collection, string fieldName, LGD_ValidationReport report, string suggestedFix = null)
        {
            if (collection == null || !collection.Any())
            {
                report.Add(ValidationStatus.Warning, fieldName,
                    $"{fieldName} is null or empty.",
                    suggestedFix: suggestedFix ?? $"Add items to {fieldName}.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a collection does not exceed a maximum size.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to validate.</param>
        /// <param name="maxSize">The maximum allowed size.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateMaxSize<T>(IEnumerable<T> collection, int maxSize, string fieldName, LGD_ValidationReport report)
        {
            if (collection == null)
                return true;

            var count = collection.Count();
            if (count > maxSize)
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} has {count} items, which exceeds the maximum of {maxSize}.",
                    suggestedFix: $"Reduce {fieldName} to {maxSize} or fewer items.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a string does not contain spaces.
        /// </summary>
        /// <param name="value">The string to validate.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateNoSpaces(string value, string fieldName, LGD_ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (value.Contains(" "))
            {
                report.Add(ValidationStatus.Warning, fieldName,
                    $"{fieldName} contains spaces.",
                    suggestedFix: "Use underscores or camelCase instead of spaces.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a string matches a specific pattern.
        /// </summary>
        /// <param name="value">The string to validate.</param>
        /// <param name="pattern">The regex pattern to match.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <param name="patternDescription">Human-readable description of the pattern.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidatePattern(string value, string pattern, string fieldName, LGD_ValidationReport report, string patternDescription = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern, System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} value '{value}' does not match the required pattern.",
                    suggestedFix: patternDescription ?? $"Ensure {fieldName} matches the pattern: {pattern}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a file path is valid and the file exists.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <param name="checkExists">Whether to check if the file exists.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateFilePath(string filePath, string fieldName, LGD_ValidationReport report, bool checkExists = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} is null or empty.",
                    suggestedFix: "Provide a valid file path.");
                return false;
            }

            try
            {
                var path = filePath.Trim();
                var invalidChars = System.IO.Path.GetInvalidPathChars();
                if (path.IndexOfAny(invalidChars) >= 0)
                {
                    report.Add(ValidationStatus.Error, fieldName,
                        $"{fieldName} contains invalid characters: {filePath}",
                        suggestedFix: "Remove invalid characters from the file path.");
                    return false;
                }

                if (checkExists && !System.IO.File.Exists(path))
                {
                    report.Add(ValidationStatus.Error, fieldName,
                        $"{fieldName} does not exist: {filePath}",
                        suggestedFix: "Ensure the file exists at the specified path.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} is invalid: {ex.Message}",
                    suggestedFix: "Provide a valid file path.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a URL is well-formed.
        /// </summary>
        /// <param name="url">The URL to validate.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateUrl(string url, string fieldName, LGD_ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} is null or empty.",
                    suggestedFix: "Provide a valid URL.");
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} is not a valid HTTP/HTTPS URL: {url}",
                    suggestedFix: "Provide a valid HTTP or HTTPS URL.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for duplicate values in a collection.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to check.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <param name="valueSelector">Function to select the value to compare for duplicates.</param>
        /// <returns>True if no duplicates found, false otherwise.</returns>
        public static bool ValidateNoDuplicates<T>(IEnumerable<T> collection, string fieldName, LGD_ValidationReport report, Func<T, string> valueSelector = null)
        {
            if (collection == null)
                return true;

            var selector = valueSelector ?? (item => item?.ToString());
            var duplicates = collection
                .GroupBy(selector)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} contains duplicate values: {string.Join(", ", duplicates)}",
                    suggestedFix: "Remove duplicate values or rename them to be unique.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a value is of a specific type.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <param name="report">The validation report to add errors to.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidateType(object value, Type expectedType, string fieldName, LGD_ValidationReport report)
        {
            if (value == null)
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} is null.",
                    suggestedFix: $"Provide a valid {expectedType.Name} value.");
                return false;
            }

            if (!expectedType.IsAssignableFrom(value.GetType()))
            {
                report.Add(ValidationStatus.Error, fieldName,
                    $"{fieldName} is of type {value.GetType().Name}, but expected {expectedType.Name}.",
                    suggestedFix: $"Provide a value of type {expectedType.Name}.");
                return false;
            }

            return true;
        }
    }
}