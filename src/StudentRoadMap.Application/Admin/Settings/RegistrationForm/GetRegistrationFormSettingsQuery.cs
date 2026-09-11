using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Settings.RegistrationForm;

/// <summary>`GET /api/admin/settings/registration-form` — `docs/07` §3.8.</summary>
public sealed record GetRegistrationFormSettingsQuery : IRequest<Result<RegistrationFormDefinitionDto>>;
