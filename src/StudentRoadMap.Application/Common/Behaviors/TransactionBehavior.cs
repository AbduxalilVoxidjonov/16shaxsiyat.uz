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
///
/// ⚠️ P18-R1 (`prompts/18-ai-navbat-va-orkestratsiya.md`, MAJBURIY): tranzaksiya MUVAFFAQIYATLI
/// commit bo'lgandan KEYIN <see cref="IPostCommitActions.RunAsync"/> chaqiriladi — handler'lar
/// (masalan `CompleteSessionCommandHandler`) AI navbatiga qo'yishni to'g'ridan-to'g'ri emas,
/// `IPostCommitActions.Enqueue(...)` orqali kechiktiradi. Sabab: `AppDbContext.SaveChangesAsync`
/// domen hodisalarini `SaveChanges` ICHIDA (ya'ni bu tranzaksiya hali commit bo'lmasdan turib)
/// publish qiladi — fon ishchisi navbatga to'g'ridan-to'g'ri/darhol qo'yilgan vazifani olib,
/// hali commit qilinmagan yozuvni o'qishga urinishi (yoki rollback bo'lganda mavjud bo'lmagan
/// sessiya uchun vazifa navbatda qolib ketishi) mumkin edi — klassik poyga holati.
/// Rollback (istisno) holatida `RunAsync` UMUMAN chaqirilmaydi — ro'yxat shunchaki DI scope
/// bilan birga yo'q qilinadi (P18-R2 testi shuni tasdiqlaydi).
/// </summary>
internal sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAppDbContext _context;
    private readonly IPostCommitActions _postCommitActions;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IAppDbContext context, IPostCommitActions postCommitActions, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _postCommitActions = postCommitActions;
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

        TResponse response;
        try
        {
            response = await next().ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tranzaksiya bekor qilindi: {RequestName}", requestName);
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }

        // P18-R1: FAQAT shu yerda, commit MUVAFFAQIYATLI bo'lgandan KEYIN — va yuqoridagi
        // try/catch'dan TASHQARIDA ataylab: tranzaksiya ALLAQACHON commit qilingan, shu sabab
        // bu yerda istisno chiqsa uni "rollback" qilishga urinish (EF Core'da allaqachon
        // tugatilgan tranzaksiyani rollback qilish o'zi `InvalidOperationException` otadi va
        // asl xatoni yashiradi) noto'g'ri bo'lardi — istisno shunchaki yuqoriga otiladi.
        await _postCommitActions.RunAsync(cancellationToken).ConfigureAwait(false);

        return response;
    }
}
