using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Persistence;

/// <summary>
/// `IAsyncQueryExecutor` ning EF Core bilan amalga oshirilishi — `Application` qatlami
/// `Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions` ga bevosita bog'lanmaydi
/// (PM qarori, `docs/06-arxitektura.md` 3-bo'limi).
/// </summary>
internal sealed class EfAsyncQueryExecutor : IAsyncQueryExecutor
{
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        query.ToListAsync(cancellationToken);

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        query.FirstOrDefaultAsync(cancellationToken);

    public Task<T> SingleAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        query.SingleAsync(cancellationToken);

    public Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        query.SingleOrDefaultAsync(cancellationToken);

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        query.CountAsync(cancellationToken);

    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        query.AnyAsync(cancellationToken);

    public Task<int> SumAsync(IQueryable<int> query, CancellationToken cancellationToken = default) =>
        query.SumAsync(cancellationToken);

    public Task<int> SumAsync<T>(IQueryable<T> query, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default) =>
        query.SumAsync(selector, cancellationToken);

    public Task<decimal> SumAsync<T>(IQueryable<T> query, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) =>
        query.SumAsync(selector, cancellationToken);
}
