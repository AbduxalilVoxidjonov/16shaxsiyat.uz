namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// `IQueryable&lt;T&gt;` ustida asinxron materializatsiya — `Application` qatlami EF Core'ning
/// `ToListAsync`/`FirstOrDefaultAsync` kabi kengaytma metodlariga bevosita bog'lanmasligi uchun
/// (PM qarori, `docs/06-arxitektura.md` 3-bo'limi). `Infrastructure` qatlamida EF Core bilan
/// amalga oshiriladi (`EfAsyncQueryExecutor`).
/// </summary>
public interface IAsyncQueryExecutor
{
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    Task<T> SingleAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    Task<int> SumAsync(IQueryable<int> query, CancellationToken cancellationToken = default);

    Task<int> SumAsync<T>(IQueryable<T> query, System.Linq.Expressions.Expression<Func<T, int>> selector, CancellationToken cancellationToken = default);

    Task<decimal> SumAsync<T>(IQueryable<T> query, System.Linq.Expressions.Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);
}
