using System;
using System.Collections.Generic;
using System.Linq;
using LiveGameDev.Core;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Standardized result wrapper for validation operations.
    /// Provides consistent error handling and result reporting.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    public class RsvEditorValidationResult<T>
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Indicates whether the operation failed.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// The success value, if the operation succeeded.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// The error message, if the operation failed.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// The validation status of the result.
        /// </summary>
        public ValidationStatus Status { get; private set; }

        /// <summary>
        /// The validation report, if available.
        /// </summary>
        public LGD_ValidationReport Report { get; private set; }

        /// <summary>
        /// Additional error details.
        /// </summary>
        public Dictionary<string, object> ErrorDetails { get; private set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="value">The success value.</param>
        /// <param name="status">The validation status.</param>
        /// <param name="report">The validation report (optional).</param>
        /// <returns>A successful result.</returns>
        public static RsvEditorValidationResult<T> Success(T value, ValidationStatus status = ValidationStatus.Pass, LGD_ValidationReport report = null)
        {
            return new RsvEditorValidationResult<T>
            {
                IsSuccess = true,
                Value = value,
                Status = status,
                Report = report,
                ErrorMessage = null,
                ErrorDetails = null
            };
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="status">The validation status.</param>
        /// <param name="errorDetails">Additional error details (optional).</param>
        /// <returns>A failed result.</returns>
        public static RsvEditorValidationResult<T> Failure(string errorMessage, ValidationStatus status = ValidationStatus.Error, Dictionary<string, object> errorDetails = null)
        {
            return new RsvEditorValidationResult<T>
            {
                IsSuccess = false,
                Value = default,
                Status = status,
                Report = null,
                ErrorMessage = errorMessage,
                ErrorDetails = errorDetails
            };
        }

        /// <summary>
        /// Creates a failed result from an exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="status">The validation status.</param>
        /// <returns>A failed result.</returns>
        public static RsvEditorValidationResult<T> FromException(Exception exception, ValidationStatus status = ValidationStatus.Critical)
        {
            var sanitizedMessage = RsvErrorSanitizer.Sanitize(exception.Message);
            var errorDetails = new Dictionary<string, object>
            {
                { "ExceptionType", exception.GetType().Name },
                { "StackTrace", RsvErrorSanitizer.Sanitize(exception.StackTrace ?? string.Empty) }
            };

            return new RsvEditorValidationResult<T>
            {
                IsSuccess = false,
                Value = default,
                Status = status,
                Report = null,
                ErrorMessage = sanitizedMessage,
                ErrorDetails = errorDetails
            };
        }

        /// <summary>
        /// Maps the success value to a new type.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="mapper">The mapping function.</param>
        /// <returns>A new result with the mapped value.</returns>
        public RsvEditorValidationResult<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            if (IsSuccess)
            {
                try
                {
                    return RsvEditorValidationResult<TResult>.Success(mapper(Value), Status, Report);
                }
                catch (Exception ex)
                {
                    return RsvEditorValidationResult<TResult>.FromException(ex, ValidationStatus.Critical);
                }
            }
            else
            {
                return RsvEditorValidationResult<TResult>.Failure(ErrorMessage, Status, ErrorDetails);
            }
        }

        /// <summary>
        /// Binds the result to another result-producing function.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="binder">The binding function.</param>
        /// <returns>A new result from the binding function.</returns>
        public RsvEditorValidationResult<TResult> Bind<TResult>(Func<T, RsvEditorValidationResult<TResult>> binder)
        {
            if (IsSuccess)
            {
                try
                {
                    return binder(Value);
                }
                catch (Exception ex)
                {
                    return RsvEditorValidationResult<TResult>.FromException(ex, ValidationStatus.Critical);
                }
            }
            else
            {
                return RsvEditorValidationResult<TResult>.Failure(ErrorMessage, Status, ErrorDetails);
            }
        }

        /// <summary>
        /// Gets the value or a default value if the result failed.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The value or default.</returns>
        public T GetValueOrDefault(T defaultValue = default)
        {
            return IsSuccess ? Value : defaultValue;
        }

        /// <summary>
        /// Gets the value or throws an exception if the result failed.
        /// </summary>
        /// <returns>The value.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the result failed.</exception>
        public T GetValueOrThrow()
        {
            if (IsFailure)
            {
                throw new InvalidOperationException(ErrorMessage ?? "Validation failed");
            }
            return Value;
        }

        /// <summary>
        /// Executes an action if the result succeeded.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>This result for chaining.</returns>
        public RsvEditorValidationResult<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess)
            {
                try
                {
                    action(Value);
                }
                catch (Exception ex)
                {
                    // Log but don't change the result
                    Debug.LogWarning($"[RSV] Error in OnSuccess callback: {ex.Message}");
                }
            }
            return this;
        }

        /// <summary>
        /// Executes an action if the result failed.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>This result for chaining.</returns>
        public RsvEditorValidationResult<T> OnFailure(Action<string> action)
        {
            if (IsFailure)
            {
                try
                {
                    action(ErrorMessage);
                }
                catch (Exception ex)
                {
                    // Log but don't change the result
                    Debug.LogWarning($"[RSV] Error in OnFailure callback: {ex.Message}");
                }
            }
            return this;
        }

        /// <summary>
        /// Returns a string representation of the result.
        /// </summary>
        /// <returns>A string representation.</returns>
        public override string ToString()
        {
            if (IsSuccess)
            {
                return $"Success: {Status} - {Value}";
            }
            else
            {
                return $"Failure: {Status} - {ErrorMessage}";
            }
        }
    }

    /// <summary>
    /// Extension methods for RsvValidationResult.
    /// </summary>
    public static class RsvValidationResultExtensions
    {
        /// <summary>
        /// Converts a validation report to a result.
        /// </summary>
        /// <param name="report">The validation report.</param>
        /// <returns>A result indicating success or failure.</returns>
        public static RsvEditorValidationResult<bool> ToResult(this LGD_ValidationReport report)
        {
            if (report == null)
            {
                return RsvEditorValidationResult<bool>.Failure("Validation report is null", ValidationStatus.Critical);
            }

            if (report.HasCritical || report.HasErrors)
            {
                return RsvEditorValidationResult<bool>.Failure(
                    $"Validation failed with {report.Entries.Count} issues",
                    report.OverallStatus,
                    new Dictionary<string, object>
                    {
                        { "TotalCount",    report.Entries.Count },
                        { "CriticalCount", report.Entries.Where(e => e.Status == ValidationStatus.Critical).Count() },
                        { "ErrorCount",    report.Entries.Where(e => e.Status == ValidationStatus.Error).Count() }
                    }
                );
            }

            return RsvEditorValidationResult<bool>.Success(true, report.OverallStatus, report);
        }

        /// <summary>
        /// Combines multiple results into a single result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="results">The results to combine.</param>
        /// <returns>A combined result.</returns>
        public static RsvEditorValidationResult<T[]> Combine<T>(params RsvEditorValidationResult<T>[] results)
        {
            var values = new List<T>();
            var errors = new List<string>();
            var highestStatus = ValidationStatus.Pass;

            foreach (var result in results)
            {
                if (result.IsSuccess)
                {
                    values.Add(result.Value);
                    if (result.Status > highestStatus)
                    {
                        highestStatus = result.Status;
                    }
                }
                else
                {
                    errors.Add(result.ErrorMessage);
                    if (result.Status > highestStatus)
                    {
                        highestStatus = result.Status;
                    }
                }
            }

            if (errors.Count > 0)
            {
                return RsvEditorValidationResult<T[]>.Failure(
                    string.Join("; ", errors),
                    highestStatus,
                    new Dictionary<string, object>
                    {
                        { "ErrorCount", errors.Count },
                        { "SuccessCount", values.Count }
                    }
                );
            }

            return RsvEditorValidationResult<T[]>.Success(values.ToArray(), highestStatus);
        }
    }
}
