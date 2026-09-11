using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.Update;

/// <summary>
/// `PUT /api/admin/programs/{id}` — `prompts/34` E15-band. `Code`/`Kind`/`IsSystem` bu yerda
/// o'zgarmaydi. Tizim dasturida ham ruxsat etiladi (`TestDefinition.UpdateMetadata`ga o'xshab —
/// BR-8 ruhida faqat TARKIB (`AddTest`/`RemoveTest`/`ReorderTests`) qulflangan, metadata emas).
/// </summary>
public sealed record UpdateProgramCommand(
    Guid Id,
    string NameUz,
    string? DescriptionUz,
    int DisplayOrder,
    string Visibility,
    Guid AdminUserId,
    /// <summary>
    /// P52 (2026-09-11): `"Full"`/`"None"`. `None`ga o'zgartirishga urinishda dasturda
    /// shaxsiyat batareyasi bo'lsa `400 REGISTRATION_REQUIRED_FOR_BATTERY`
    /// (`AssessmentProgram.SetRegistrationMode`).
    /// </summary>
    string RegistrationMode = "Full",
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
