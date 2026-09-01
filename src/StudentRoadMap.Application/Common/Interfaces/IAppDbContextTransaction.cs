namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Aniq (explicit) DB tranzaksiyasi abstraksiyasi — EF Core'ning `IDbContextTransaction`
/// turini `Application` qatlamiga sizdirmaslik uchun (`docs/06-arxitektura.md` 3-bo'lim).
/// `TransactionBehavior` shu orqali Command'larni bitta tranzaksiyaga o'raydi.
/// </summary>
public interface IAppDbContextTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
