using StudentRoadMap.Application.Admin.Settings.RegistrationForm;

namespace StudentRoadMap.Api.Contracts.Admin.Settings;

/// <summary>
/// `PUT /api/admin/settings/registration-form` so'rov tanasi — `docs/07` §3.8. `GET` javobi
/// bilan AYNAN BIR XIL shakl (round-trip): mijoz `GET`dan olgan obyektni tahrirlab, xuddi
/// shuni qaytarib yuborishi mumkin. TO'LIQ almashtirish semantikasi — `registrationFields`
/// PUT bilan bir xil uslub.
/// </summary>
public sealed record UpdateRegistrationFormSettingsRequest(
    RegistrationCoreFieldsDto CoreFields,
    IReadOnlyList<RegistrationCustomFieldDto> CustomFields)
{
    public UpdateRegistrationFormSettingsCommand ToCommand(Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(new RegistrationFormDefinitionDto(CoreFields, CustomFields), adminUserId, ipAddress, userAgent);
}
