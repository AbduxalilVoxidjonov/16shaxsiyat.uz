using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.LinkHealth;

/// <summary>
/// `GET /api/admin/schools/link-health` — butun tizim bo'yicha "nechta maktab havolasi
/// ishlamaydi" signali. Boshqaruv panelidagi banner shundan oziqlanadi (2026-09-03: admin
/// dasturni o'chirganda hech qanday belgi ko'rmaganligi uchun qo'shildi).
/// </summary>
public sealed record GetSchoolsLinkHealthQuery : IRequest<Result<AdminSchoolsLinkHealthDto>>;

/// <summary>
/// `ActiveSchoolCount` — FAOL maktablar soni (o'chirilgan maktab havolasi ATAYIN ishlamaydi,
/// `410 SCHOOL_INACTIVE` — u yolg'on ogohlantirish bermasligi uchun hisobga olinmaydi).
/// `BrokenSchoolCount` — shulardan havolasi ishlamaydiganlari (`LinkHealth.Status != "Ok"`).
/// `Schools` — birinchi <see cref="AdminSchoolsLinkHealthDto.SampleLimit"/> tasi (nomi bilan);
/// qolgani `BrokenSchoolCount - Schools.Count`.
/// </summary>
public sealed record AdminSchoolsLinkHealthDto(
    int ActiveSchoolCount,
    int BrokenSchoolCount,
    IReadOnlyList<AdminBrokenSchoolLinkDto> Schools)
{
    /// <summary>Namunada ko'rsatiladigan maktablar chegarasi — javob cheksiz o'smasligi uchun.</summary>
    public const int SampleLimit = 10;
}

/// <summary>Havolasi ishlamaydigan bitta maktab — sabab bilan.</summary>
public sealed record AdminBrokenSchoolLinkDto(Guid Id, string Name, AdminSchoolLinkHealthDto LinkHealth);
