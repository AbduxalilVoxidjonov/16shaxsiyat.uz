using FluentValidation;
using MediatR;
using ValidationException = StudentRoadMap.Application.Common.Exceptions.ValidationException;

namespace StudentRoadMap.Application.Common.Behaviors;

/// <summary>
/// Har bir so'rov uchun ro'yxatdan o'tgan `IValidator&lt;TRequest&gt;`larni ishga tushiradi
/// (`docs/06-arxitektura.md` 4-bo'lim: "Validator — FluentValidation, pipeline behavior orqali
/// avtomatik ishlaydi"). Xato topilsa handler chaqirilmaydi — `ValidationException` otiladi.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
