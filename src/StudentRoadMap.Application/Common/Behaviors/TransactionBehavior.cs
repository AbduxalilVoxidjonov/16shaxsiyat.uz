using MediatR;
using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Application.Common.Behaviors;

/// <summary>
/// Command'larni bitta DB tranzaksiyasiga o'raydi (`docs/06-arxitektura.md` 4-bo'lim:
/// "Command'lar `TransactionBehavior` ichida bajariladi"). Query'lar READ-ONLY bo'lgani
/// uchun (`AsNoTracking`) tranzaksiya kerak emas — nomlash konvensiyasiga tayanib
/// (`*Command`/`*Query`, `docs/06` 4-bo'limidagi namuna) faqat Command'lar o'raladi.
///
/// Handler ichida bir nechta `SaveChangesAsync`/xom SQL chaqiruvi (masalan, kunlik hisoblagichni
/// atomik oshirish va keyin yozuvlarni saqlash) shu tranzaksiya doirasida — muvaffaqiyatsizlikda
/// hammasi birga qaytariladi (rollback).
/// </summary>
internal sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAppDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IAppDbContext context, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        if (!requestName.EndsWith("Command", StringComparison.Ordinal))
        {
            return await next().ConfigureAwait(false);
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await next().ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tranzaksiya bekor qilindi: {RequestName}", requestName);
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
