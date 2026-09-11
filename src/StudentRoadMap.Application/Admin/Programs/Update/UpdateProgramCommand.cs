using MediatR;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Application.Common.Models;
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
    /// <summary>
    /// P52 kengaytmasi (2026-09-11, `docs/18` §9.5): `RegistrationMode` bilan bir xil TO'LIQ
    /// ALMASHTIRISH naqshi — `null` (yubormaslik) standart qiymatlarga qaytaradi. Batareya bor
    /// dasturda `birthDate`/`grade` boshqa qiymatga o'rnatilsa `400 REGISTRATION_FIELD_REQUIRED_FOR_BATTERY`
    /// (`AssessmentProgram.SetRegistrationFields`).
    /// </summary>
    RegistrationFieldsInput? RegistrationFields = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminProgramDetailDto>>;
