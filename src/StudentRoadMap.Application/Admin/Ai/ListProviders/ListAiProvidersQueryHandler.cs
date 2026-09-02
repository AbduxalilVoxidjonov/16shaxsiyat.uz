using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.ListProviders;

/// <summary>Read-only — `AsNoTracking`. Faqat DB'da MAVJUD (kamida bir marta `PUT` qilingan) provayderlar qaytadi.</summary>
internal sealed class ListAiProvidersQueryHandler : IRequestHandler<ListAiProvidersQuery, Result<IReadOnlyList<AdminAiProviderDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IEncryptionService _encryptionService;

    public ListAiProvidersQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IEncryptionService encryptionService)
    {
        _context = context;
        _executor = executor;
        _encryptionService = encryptionService;
    }

    public async Task<Result<IReadOnlyList<AdminAiProviderDto>>> Handle(ListAiProvidersQuery request, CancellationToken cancellationToken)
    {
        var configs = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AiProviderConfigs).OrderBy(c => c.FallbackOrder),
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AdminAiProviderDto> dtos = configs.Select(c => new AdminAiProviderDto(
            c.Provider,
            c.DisplayName,
            c.Model,
            c.BaseUrl,
            c.MaxOutputTokens,
            c.Temperature,
            c.IsDefault,
            c.IsActive,
            c.FallbackOrder,
            c.ApiKeyEncrypted is null ? null : ApiKeyMasker.Mask(_encryptionService.Decrypt(c.ApiKeyEncrypted)),
            c.LastCheckedAt,
            c.LastCheckStatus)).ToList();

        return Result.Success(dtos);
    }
}
