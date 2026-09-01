using Microsoft.EntityFrameworkCore.Storage;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Persistence;

/// <summary>`IAppDbContextTransaction`ning EF Core `IDbContextTransaction` ustidagi o'rovchisi.</summary>
internal sealed class EfAppDbContextTransaction : IAppDbContextTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfAppDbContextTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
