using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Settings;

namespace StudentRoadMap.Application.Admin.Settings.RegistrationForm;

/// <summary>
/// Read-only — `AsNoTracking`. DB'da yozuv bo'lmasa (sozlama hali hech kim tomonidan
/// saqlanmagan) `RegistrationFormDefinition.Default` qaytadi — mijozga HAR DOIM TO'LIQ shakl
/// beriladi, `null` emas (`docs/18` §9.6).
/// </summary>
internal sealed class GetRegistrationFormSettingsQueryHandler : IRequestHandler<GetRegistrationFormSettingsQuery, Result<RegistrationFormDefinitionDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetRegistrationFormSettingsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<RegistrationFormDefinitionDto>> Handle(GetRegistrationFormSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.RegistrationFormSettings),
            cancellationToken).ConfigureAwait(false);

        var definition = settings?.Definition ?? RegistrationFormDefinition.Default;

        return Result.Success(RegistrationFormSettingsMapping.ToDto(definition));
    }
}
