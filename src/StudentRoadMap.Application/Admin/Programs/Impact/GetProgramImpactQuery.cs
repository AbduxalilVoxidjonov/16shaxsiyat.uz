using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Programs.Impact;

/// <summary>
/// `GET /api/admin/programs/{id}/impact?action=…` — dastur ustidagi amal NECHTA MAKTABNI
/// havolasiz qoldirishini OLDINDAN aytadi (2026-09-03 jonli hodisasi: admin yagona dasturni
/// o'chirdi, hech qanday ogohlantirish ko'rmadi, barcha maktab havolasi jimgina o'lik bo'ldi).
///
/// Amalni TAQIQLAMAYDI — bu faqat o'qish so'rovi; tasdiq oynasi oqibatni ko'rsatadi, qaror
/// adminniki.
/// </summary>
public sealed record GetProgramImpactQuery(Guid ProgramId, string Action) : IRequest<Result<AdminProgramImpactDto>>;

/// <summary>Qo'llab-quvvatlanadigan amallar — mijoz shu qiymatlarni yuboradi.</summary>
public static class ProgramImpactActions
{
    /// <summary>`POST /toggle-active` — faol dasturni o'chirish.</summary>
    public const string Deactivate = "deactivate";

    /// <summary>`POST /archive` — arxivlash.</summary>
    public const string Archive = "archive";

    /// <summary>`PUT` orqali `Visibility: Public → Assigned`.</summary>
    public const string MakeAssigned = "makeAssigned";

    public static bool IsKnown(string? action) =>
        action is Deactivate or Archive or MakeAssigned;
}

/// <summary>
/// `AffectedSchoolCount` — amaldan KEYIN umuman dastursiz qoladigan FAOL maktablar soni
/// (hozir dasturi bor, keyin bo'lmaydi). `Schools` — birinchi
/// <see cref="SampleLimit"/> tasi; qolgani `AffectedSchoolCount - Schools.Count`.
/// </summary>
public sealed record AdminProgramImpactDto(
    string Action,
    int AffectedSchoolCount,
    IReadOnlyList<AdminImpactedSchoolDto> Schools)
{
    /// <summary>Tasdiq oynasida sanab o'tiladigan maktablar chegarasi.</summary>
    public const int SampleLimit = 20;
}

/// <summary>Amal natijasida dastursiz qoladigan bitta maktab.</summary>
public sealed record AdminImpactedSchoolDto(Guid Id, string Name);
