using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.GetUsage;

/// <summary>
/// `docs/09-ai-analiz-moduli.md` 9-bo'lim. Zaxira shablon hisobotlar (`IsFallbackReport = true`)
/// HISOBGA OLINMAYDI — ular haqiqiy AI chaqiruvi emas, token/xarajat sarflamagan.
/// </summary>
internal sealed class GetAiUsageQueryHandler : IRequestHandler<GetAiUsageQuery, Result<AdminAiUsageDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetAiUsageQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<AdminAiUsageDto>> Handle(GetAiUsageQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AsNoTracking(_context.AiAnalyses).Where(a => !a.IsFallbackReport);

        if (request.From is { } from)
        {
            query = query.Where(a => a.CreatedAt >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(a => a.CreatedAt <= to);
        }

        var analyses = await _executor.ToListAsync(query, cancellationToken).ConfigureAwait(false);

        var byProvider = analyses
            .GroupBy(a => a.Provider)
            .Select(g => new AdminAiUsageByProviderDto(
                g.Key,
                g.Count(),
                g.Sum(a => a.InputTokens ?? 0),
                g.Sum(a => a.OutputTokens ?? 0),
                g.Any(a => a.EstimatedCostUsd.HasValue) ? g.Sum(a => a.EstimatedCostUsd ?? 0) : null))
            .OrderBy(dto => dto.Provider)
            .ToList();

        var dto = new AdminAiUsageDto(
            analyses.Count,
            analyses.Sum(a => a.InputTokens ?? 0),
            analyses.Sum(a => a.OutputTokens ?? 0),
            analyses.Any(a => a.EstimatedCostUsd.HasValue) ? analyses.Sum(a => a.EstimatedCostUsd ?? 0) : null,
            byProvider);

        return Result.Success(dto);
    }
}
