using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Settings.RegistrationForm;

/// <summary>
/// `PUT /api/admin/settings/registration-form` — `docs/07` §3.8. TO'LIQ almashtirish
/// (`registrationFields` PUT bilan bir xil semantika, `RegistrationFieldsInput` naqshi FARQI:
/// bu yerda HAR bir maydon TO'LIQ berilishi shart — qisman `null` bilan "standart" to'ldirilmaydi,
/// chunki `customFields` ro'yxati o'zi ham har `PUT`da to'liq almashadi).
/// </summary>
public sealed record UpdateRegistrationFormSettingsCommand(
    RegistrationFormDefinitionDto Definition,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<RegistrationFormDefinitionDto>>;
