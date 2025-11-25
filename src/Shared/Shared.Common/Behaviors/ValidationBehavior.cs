using FluentValidation;
using MediatR;

namespace Shared.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates requests using FluentValidation before processing.
/// </summary>
/// <remarks>
/// Excluded from code coverage: MediatR pipeline behavior.
/// Validation logic is tested via integration tests and validator tests.
/// </remarks>
/// <typeparam name="TRequest">The type of request to validate.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "MediatR pipeline behavior. Tested via integration tests.")]
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The collection of validators for the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
