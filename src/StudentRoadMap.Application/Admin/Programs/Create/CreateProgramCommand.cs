using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.Create;

/// <summary>
/// `POST /api/admin/programs` — `prompts/34` E15-band (minimal admin API). Har doim
/// `Kind = Custom`/`Status = Draft` — tizim dasturi (`Kind = System`) faqat seed orqali
/// yaratiladi (`DbSeeder.SeedSystemProgramAsync`).
/// </summary>
public sealed record CreateProgramCommand(
    string Code,
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    string Visibility,
    Guid AdminUserId,
    /// <summary>P52 (2026-09-11): `"Full"`/`"None"`. Yangi dasturda hali test yo'q, shu sabab batareya invarianti bu yerda buzila olmaydi.</summary>
    string RegistrationMode = "Full",
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
