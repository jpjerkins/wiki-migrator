using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Text;

namespace WikiMigrator.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

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
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            _logger.LogWarning("Validation failures for {RequestType}: {Errors}", 
                typeof(TRequest).Name,
                string.Join(", ", failures.Select(f => f.ErrorMessage)));
            
            throw new ValidationException(failures);
        }

        return await next();
    }
}

public class ValidationException : Exception
{
    public IReadOnlyList<FluentValidation.Results.ValidationFailure> Errors { get; }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors.ToImmutableList();
    }

    private static string BuildMessage(IEnumerable<FluentValidation.Results.ValidationFailure> errors)
    {
        var sb = new StringBuilder("Validation failed: ");
        foreach (var error in errors)
        {
            sb.AppendLine($"  - {error.PropertyName}: {error.ErrorMessage}");
        }
        return sb.ToString();
    }
}
