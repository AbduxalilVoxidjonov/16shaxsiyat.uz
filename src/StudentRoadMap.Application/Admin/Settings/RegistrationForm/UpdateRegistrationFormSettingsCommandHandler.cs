using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using RegistrationFormSettingsEntity = StudentRoadMap.Domain.Settings.RegistrationFormSettings;

namespace StudentRoadMap.Application.Admin.Settings.RegistrationForm;

/// <summary>
/// `docs/07` §3.8. Upsert (`RegistrationFormSettings` yozuvi birinchi `PUT`da yaratiladi) —
/// `UpdateAiProviderCommandHandler` bilan bir xil naqsh. `RegistrationFormDefinition.Create`
/// (Domain) `REGISTRATION_FORM_*` `DomainException`larini bevosita ko'taradi — global middleware
/// ushlaydi (`CreateTestSectionCommandHandler` uslubi). `InputPattern` kompilyatsiya tekshiruvi
/// BU YERDA (handler darajasida) — `CreateTestQuestionCommandHandler` bilan bir xil qoida
/// (`CachedInputPatternMatcher`, ikkinchi mustaqil regex-mantiq paydo bo'lmasin).
/// </summary>
internal sealed class UpdateRegistrationFormSettingsCommandHandler : IRequestHandler<UpdateRegistrationFormSettingsCommand, Result<RegistrationFormDefinitionDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public UpdateRegistrationFormSettingsCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<RegistrationFormDefinitionDto>> Handle(UpdateRegistrationFormSettingsCommand request, CancellationToken cancellationToken)
    {
        foreach (var field in request.Definition.CustomFields)
        {
            if (field.InputPattern is { Length: > 0 } pattern && !CachedInputPatternMatcher.IsValidPattern(pattern))
            {
                return Result.Failure<RegistrationFormDefinitionDto>(new Error(
                    ProblemCodes.InputPatternInvalid,
                    $"'{field.Code}' maydonining InputPattern shabloni kompilyatsiya qilinmadi."));
            }
        }

        // `RegistrationFormDefinition.Create` — REGISTRATION_FORM_* DomainException'larini
        // (fullName qulfi, kod formati/takrori, tanlov soni/takrori) bevosita ko'taradi.
        var definition = RegistrationFormSettingsMapping.ToDomain(request.Definition);

        var now = _dateTime.UtcNow;

        var settings = await _executor.FirstOrDefaultAsync(
            _context.RegistrationFormSettings,
            cancellationToken).ConfigureAwait(false);

        if (settings is null)
        {
            settings = RegistrationFormSettingsEntity.Create(definition, now, request.AdminUserId);
            _context.Add(settings);
        }
        else
        {
            settings.UpdateDefinition(definition, now, request.AdminUserId);
        }

        _context.Add(AuditLog.Create(
            AuditActions.RegistrationFormSettingsUpdated,
            now,
            request.AdminUserId,
            entityType: "RegistrationFormSettings",
            entityId: settings.Id,
            afterJson: AuditSnapshot.Serialize(new
            {
                CoreFieldCount = 8,
                CustomFieldCount = definition.CustomFields.Count,
                CustomFieldCodes = definition.CustomFields.Select(f => f.Code).ToList(),
            }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(RegistrationFormSettingsMapping.ToDto(definition));
    }
}
