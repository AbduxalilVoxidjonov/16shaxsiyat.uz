using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// `ListStudentsQuery` filtr qurilishi (`docs/07` 3.2-bo'lim parametrlari) — `ListStudentsQueryHandler`
/// va `ExportStudentsQueryHandler` (`Admin/Students/Export`, `prompts/27`) IKKALASI ham shu bitta
/// metodni chaqiradi. Ataylab shu yerga (o'zi `internal`, `List` papkasi) chiqarilgan — eksport
/// ro'yxat sahifasida ko'rilgan filtr bilan AYNAN bir xil bo'lishi shart (`prompts/27` MAXSUS
/// DIQQAT #1: "filtr ikki joyda ajralib ketsa, hisobotlar jimgina bir-biriga mos kelmay
/// qoladi"). Faqat FILTR (`Where`) — sahifalash/saralash chaqiruvchida qoladi (eksportda
/// sahifalash yo'q, `ListStudentsQueryHandler`da esa mavjud saralash/sahifalash o'zgarmaydi).
/// `source`/`publicSpaceId` — MANBA filtri (maktab / ommaviy makon), xuddi shu sababdan shu
/// yerda: eksport ro'yxatdagi manba tanlovini ham AYNAN takrorlashi shart.
/// </summary>
internal static class AdminStudentFilterBuilder
{
    public static IQueryable<Student> Apply(
        IAppDbContext context,
        IQueryable<Student> query,
        Guid? schoolId,
        int? grade,
        string? status,
        bool? needsAttention,
        string? personalityType,
        string? activityLevel,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? search,
        string? source,
        Guid? publicSpaceId)
    {
        // MANBA (`AdminSourceFilter`) — maktab oqimi yoki ommaviy makon. `schoolId` bilan
        // BIRGA ishlatilsa ikkalasi ham qo'llanadi (mantiqan VA): ommaviy makon ID'si bilan
        // `source=school` bo'sh natija beradi — bu TO'G'RI, chunki so'rov o'zi ziddiyatli.
        //
        // Makon bazada bo'lmasa (seed bajarilmagan) `Guid.Empty` sentinel sifatida ishlatiladi:
        // hech bir `Student.SchoolId` unga teng emas, ya'ni `source=public` bo'sh, `source=school`
        // esa HAMMASINI qaytaradi — ikkalasi ham haqiqatga mos.
        if (source is not null)
        {
            var spaceId = publicSpaceId ?? Guid.Empty;
            query = source == AdminSourceFilter.Public
                ? query.Where(s => s.SchoolId == spaceId)
                : query.Where(s => s.SchoolId != spaceId);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(s => s.SchoolId == schoolId.Value);
        }

        if (grade.HasValue)
        {
            query = query.Where(s => s.Grade == grade.Value);
        }

        if (needsAttention.HasValue)
        {
            query = query.Where(s => s.NeedsAttention == needsAttention.Value);
        }

        if (!string.IsNullOrWhiteSpace(personalityType))
        {
            var trimmedPersonalityType = personalityType.Trim();
            query = query.Where(s => s.LastPersonalityType == trimmedPersonalityType);
        }

        if (!string.IsNullOrWhiteSpace(activityLevel) && Enum.TryParse<ActivityLevel>(activityLevel, ignoreCase: true, out var activityLevelValue))
        {
            query = query.Where(s => s.LastActivityLevel == activityLevelValue);
        }

        if (from.HasValue)
        {
            query = query.Where(s => s.LastAssessmentAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.LastAssessmentAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // `docs/05` 2-bo'lim `ix_students_name_trgm` — `ListSchoolsQueryHandler`dagi bilan bir xil sabab.
            var term = search.Trim();
            query = query.Where(s => s.FullName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AssessmentStatus>(status, ignoreCase: true, out var statusValue))
        {
            // Sahifalash/saralashdan OLDIN qo'llanadi — filtr faol bo'lganda umumiy sonni ham
            // to'g'ri hisoblash uchun (`ix_assessments_status_started(status, started_at desc)`
            // indeksidan foydalanadigan `EXISTS`).
            query = query.Where(s => context.Assessments.Any(a => a.StudentId == s.Id && a.Status == statusValue));
        }

        return query;
    }
}
