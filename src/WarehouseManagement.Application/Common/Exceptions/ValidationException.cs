using FluentValidation.Results;

namespace WarehouseManagement.Application.Common.Exceptions;

/// <summary>
/// Exception thrown by the MediatR ValidationBehavior when request validation rules fail.
/// Captures and groups all property-level validation errors.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(
                failureGroup => failureGroup.Key,
                failureGroup => failureGroup.Distinct().ToArray()
            );
    }
}
