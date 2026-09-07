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
///
/// <para>
/// **Faqat maktab o'quvchilari** (egasining talabi, 2026-09-07): ommaviy makon (`SchoolKind.PublicSpace`)
/// foydalanuvchilari bu ro'yxatga va eksportga HECH QACHON kirmaydi — ular o'z bo'limida
/// (`GET /api/admin/public-space/users`, `/admin/ommaviy`) ko'rinadi. Ilgari `?source=`
/// parametri bilan ixtiyoriy ajratilardi; endi chiqarib tashlash SHARTSIZ, shu sabab
/// `source` parametri o'quvchilar endpointlaridan olib tashlandi (sessiyalar/panelda qoladi).
/// </para>
/// </summary>
internal static class AdminStudentFilterBuilder
{
    /// <param name="publicSpaceId">
    /// Ommaviy makonning `Id`si (`AdminSourceFilter.FindPublicSpaceIdAsync`). `null` — makon
    /// bazada yo'q (seed bajarilmagan), chiqarib tashlash uchun hech narsa yo'q.
    /// </param>
    /// <param name="today">
    /// Yosh filtri uchun bugungi sana (UTC, `IDateTime.UtcNow` dan) — `StudentAgeRange` izohi.
    /// </param>
    public static IQueryable<Student> Apply(
        IAppDbContext context,
        IQueryable<Student> query,
        AdminStudentFilterCriteria criteria,
        Guid? publicSpaceId,
        DateOnly today)
    {
        // Ommaviy makon — DOIM chiqarib tashlanadi (sinf izohiga qarang). `school_id <> @id`
        // `ix_students_school_*` indekslari bilan mos, JOIN/EXISTS kerak emas.
        if (publicSpaceId.HasValue)
        {
            var spaceId = publicSpaceId.Value;
            query = query.Where(s => s.SchoolId != spaceId);
        }

        if (criteria.SchoolId.HasValue)
        {
            var schoolId = criteria.SchoolId.Value;
            query = query.Where(s => s.SchoolId == schoolId);
        }

        if (criteria.Grade.HasValue)
        {
            var grade = criteria.Grade.Value;
            query = query.Where(s => s.Grade == grade);
        }

        if (criteria.NeedsAttention.HasValue)
        {
            var needsAttention = criteria.NeedsAttention.Value;
            query = query.Where(s => s.NeedsAttention == needsAttention);
        }

        if (!string.IsNullOrWhiteSpace(criteria.PersonalityType))
        {
            var trimmedPersonalityType = criteria.PersonalityType.Trim();
            query = query.Where(s => s.LastPersonalityType == trimmedPersonalityType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ActivityLevel) && Enum.TryParse<ActivityLevel>(criteria.ActivityLevel, ignoreCase: true, out var activityLevelValue))
        {
            query = query.Where(s => s.LastActivityLevel == activityLevelValue);
        }

        // Jins — `Male`/`Female` (validator `Unspecified`ni qabul qilmaydi, `ListStudentsQueryValidator`).
        if (!string.IsNullOrWhiteSpace(criteria.Gender) && Enum.TryParse<Gender>(criteria.Gender, ignoreCase: true, out var genderValue))
        {
            query = query.Where(s => s.Gender == genderValue);
        }

        // Yosh → `BirthDate` oralig'i (`StudentAgeRange` — formulalar va chegara holatlari o'sha yerda).
        if (criteria.AgeMin.HasValue)
        {
            var latestBirthDate = StudentAgeRange.LatestBirthDateInclusive(criteria.AgeMin.Value, today);
            query = query.Where(s => s.BirthDate <= latestBirthDate);
        }

        if (criteria.AgeMax.HasValue)
        {
            var earliestBirthDateExclusive = StudentAgeRange.EarliestBirthDateExclusive(criteria.AgeMax.Value, today);
            query = query.Where(s => s.BirthDate > earliestBirthDateExclusive);
        }

        if (criteria.From.HasValue)
        {
            var from = criteria.From.Value;
            query = query.Where(s => s.LastAssessmentAt >= from);
        }

        if (criteria.To.HasValue)
        {
            var to = criteria.To.Value;
            query = query.Where(s => s.LastAssessmentAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            // `docs/05` 2-bo'lim `ix_students_name_trgm` — `ListSchoolsQueryHandler`dagi bilan bir xil sabab.
            var term = criteria.Search.Trim();
            query = query.Where(s => s.FullName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status) && Enum.TryParse<AssessmentStatus>(criteria.Status, ignoreCase: true, out var statusValue))
        {
            // Sahifalash/saralashdan OLDIN qo'llanadi — filtr faol bo'lganda umumiy sonni ham
            // to'g'ri hisoblash uchun (`ix_assessments_status_started(status, started_at desc)`
            // indeksidan foydalanadigan `EXISTS`).
            query = query.Where(s => context.Assessments.Any(a => a.StudentId == s.Id && a.Status == statusValue));
        }

        return query;
    }
}
