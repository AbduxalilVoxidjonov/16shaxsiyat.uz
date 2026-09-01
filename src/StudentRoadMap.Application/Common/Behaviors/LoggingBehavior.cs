using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StudentRoadMap.Application.Common.Behaviors;

/// <summary>
/// Har bir Command/Query uchun boshlanish/tugash va davomiylikni log qiladi. Xato bo'lsa
/// (istisno pastga otiladi — bu behavior uni yutmaydi) darajasi `Error`, aks holda `Information`.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("So'rov boshlandi: {RequestName}", requestName);

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            _logger.LogInformation(
                "So'rov tugadi: {RequestName} ({ElapsedMs} ms)", requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex, "So'rov xato bilan tugadi: {RequestName} ({ElapsedMs} ms)", requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
