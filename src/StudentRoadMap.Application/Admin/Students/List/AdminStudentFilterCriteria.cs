using StudentRoadMap.Application.Admin.Students.Export;

namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// O'quvchilar ro'yxati va eksporti uchun UMUMIY filtr mezonlari (`docs/07` 3.2-bo'lim).
/// `ListStudentsQuery` va `ExportStudentsQuery` ikkalasi ham shu shaklga keltiriladi va
/// `AdminStudentFilterBuilder.Apply` FAQAT shuni qabul qiladi — filtr maydoni qo'shilsa
/// (masalan `Gender`/`AgeMin`/`AgeMax`, 2026-09-07) uni ikkala query'da ham ko'rsatish
/// kompilyator tomonidan majburlanadi (`prompts/27` MAXSUS DIQQAT #1: ro'yxat va eksport
/// filtri hech qachon ajralib ketmasin).
/// </summary>
internal sealed record AdminStudentFilterCriteria(
    Guid? SchoolId,
    int? Grade,
    string? Status,
    bool? NeedsAttention,
    string? PersonalityType,
    string? ActivityLevel,
    string? Gender,
    int? AgeMin,
    int? AgeMax,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search)
{
    public static AdminStudentFilterCriteria Of(ListStudentsQuery query) => new(
        query.SchoolId,
        query.Grade,
        query.Status,
        query.NeedsAttention,
        query.PersonalityType,
        query.ActivityLevel,
        query.Gender,
        query.AgeMin,
        query.AgeMax,
        query.From,
        query.To,
        query.Search);

    public static AdminStudentFilterCriteria Of(ExportStudentsQuery query) => new(
        query.SchoolId,
        query.Grade,
        query.Status,
        query.NeedsAttention,
        query.PersonalityType,
        query.ActivityLevel,
        query.Gender,
        query.AgeMin,
        query.AgeMax,
        query.From,
        query.To,
        query.Search);
}
