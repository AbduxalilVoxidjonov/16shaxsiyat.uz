using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Security;

namespace StudentRoadMap.Infrastructure.Tests.Jobs.Testing;

/// <summary>
/// `Jobs` testlari uchun umumiy minimal (lekin FK'lari HAQIQIY) entity yaratish — `analysis_jobs`/
/// `ai_analyses` jadvallari `assessments`ga FK bilan bog'langan, shu sabab SQLite'da ham
/// haqiqiy `School`/`Student`/`AssessmentProgram`/`Assessment` qatorlari kerak.
/// </summary>
internal static class TestEntityFactory
{
    /// <summary>Ismi PII-sizib ketish tekshiruviga (`AiResponseValidator`) tasodifan urib qolmasligi uchun oddiy so'zlardan qochilgan.</summary>
    public static (School School, Student Student, AssessmentProgram Program) CreateSchoolStudentProgram(DateTimeOffset now, string studentFullName = "Alisher Karimov")
    {
        var school = School.Create(
            Guid.NewGuid(), "Test maktabi", "Toshkent", "Chilonzor",
            SchoolSlug.Create($"test-maktabi-{Guid.NewGuid():N}").Value,
            $"access-{Guid.NewGuid():N}", new EntryCodeGenerator().Generate(), now);

        var student = Student.Create(
            Guid.NewGuid(), school.Id, studentFullName, new DateOnly(2010, 5, 20), Gender.Male, 9,
            PhoneNumber.Create("901234567").Value, now, now);

        var program = AssessmentProgram.Create(Guid.NewGuid(), $"PROG-{Guid.NewGuid():N}"[..12], "Test dasturi", now);

        return (school, student, program);
    }

    /// <summary>`Draft` holatida — faqat FK yaxlitligi kerak bo'lgan testlar uchun (masalan `AnalysisJobQueue`).</summary>
    public static Assessment CreateDraftAssessment(AppDbContext context, DateTimeOffset now)
    {
        var (school, student, program) = CreateSchoolStudentProgram(now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, $"tok-{Guid.NewGuid():N}", "uz", program.Id, now, now.AddDays(7), now);

        context.Add(school);
        context.Add(student);
        context.Add(program);
        context.Add(assessment);

        return assessment;
    }
}
