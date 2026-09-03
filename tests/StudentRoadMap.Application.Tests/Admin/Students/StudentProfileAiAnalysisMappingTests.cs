using FluentAssertions;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Tests.Admin.Students;

/// <summary>
/// `AdminAiAnalysisDto.IsFallbackReport` — P28 koordinator ko'rsatmasi (2026-09-02).
/// <para>
/// Muammo: AI zanjirdagi HAMMA provayderda yiqilganda tizim shablon hisobot yozadi
/// (`AiAnalysis.CreateFallbackReport`, `docs/09` 11-bo'lim). Bayroq admin DTO'siga
/// chiqarilmasa — superadmin shablon matnni HAQIQIY AI tahlili deb o'qiydi. Shu sabab
/// `IsFallbackReport` (va `ErrorMessage`) DTO'da OCHIQ qaytariladi.
/// </para>
/// </summary>
public sealed class StudentProfileAiAnalysisMappingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildAiAnalysis_FallbackReport_SetsIsFallbackReportTrue()
    {
        var analysis = AiAnalysis.CreateFallbackReport(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AiProvider.Gemini,
            "v1.0",
            attemptNumber: 3,
            summary: "Shablon xulosa",
            personalityPortrait: "Shablon portret",
            strengthsJson: null,
            growthAreasJson: null,
            careerSuggestionsJson: null,
            teacherNotes: null,
            parentNotes: null,
            attentionFlagsJson: null,
            now: Now);

        var dto = StudentProfileMapping.BuildAiAnalysis(analysis);

        dto.Should().NotBeNull();
        dto!.IsFallbackReport.Should().BeTrue("admin shablon hisobotni haqiqiy AI tahlilidan ajrata olishi kerak");
        dto.Model.Should().Be("template");
    }

    [Fact]
    public void BuildAiAnalysis_RealAnalysis_IsFallbackReportFalse()
    {
        var analysis = AiAnalysis.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AiProvider.Gemini,
            "gemini-2.0-flash",
            "v1.0",
            attemptNumber: 1,
            now: Now);

        var dto = StudentProfileMapping.BuildAiAnalysis(analysis);

        dto.Should().NotBeNull();
        dto!.IsFallbackReport.Should().BeFalse();
    }

    [Fact]
    public void BuildAiAnalysis_FailedAnalysis_ExposesErrorMessage()
    {
        var analysis = AiAnalysis.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AiProvider.OpenAi,
            "gpt-4.1-mini",
            "v1.0",
            attemptNumber: 1,
            now: Now);
        analysis.Start();
        analysis.Fail("Provayder javob bermadi.", Now, durationMs: 120);

        var dto = StudentProfileMapping.BuildAiAnalysis(analysis);

        dto!.ErrorMessage.Should().Be("Provayder javob bermadi.");
    }

    /// <summary>
    /// `docs/09` 5-bo'lim sxemasidagi TO'LIQ javob — admin DTO'si uning hech bir bo'limini
    /// yo'qotmasligi kerak (ilgari `learningStyle`/`motivationProfile`/`activityAssessment`/
    /// `disclaimer`/`reliabilityNote` umuman chiqmasdi, `strengths`/`growthAreas`/
    /// `attentionFlags` esa tuzilmasini yo'qotardi).
    /// </summary>
    [Fact]
    public void BuildAiAnalysis_SchemaResponse_ExposesEveryDocs09Section()
    {
        var analysis = SucceededAnalysis(SchemaResponseJson, attentionFlagsJson: "[]");

        var dto = StudentProfileMapping.BuildAiAnalysis(analysis);

        dto.Should().NotBeNull();
        dto!.Summary.Should().Be("Umumiy xulosa.");
        dto.PersonalityPortrait.Should().Be("Shaxsiyat portreti.");

        dto.Strengths.Should().ContainSingle();
        dto.Strengths[0].Title.Should().Be("Tahliliy fikrlash");
        dto.Strengths[0].Description.Should().Be("Murakkab masalani qismlarga ajratadi.");
        dto.Strengths[0].Evidence.Should().Be("Vijdonlilik 77%.");

        dto.GrowthAreas.Should().ContainSingle();
        dto.GrowthAreas[0].Title.Should().Be("Jamoada ishlash");
        dto.GrowthAreas[0].ActionStep.Should().Be("Haftada bir marta guruh loyihasida qatnashsin.");

        dto.LearningStyle.Should().Be("Mustaqil, chuqur o'qish.");
        dto.MotivationProfile.Should().Be("Aniq maqsad motivatsiya beradi.");
        dto.ActivityAssessment.Should().Be("O'rtacha faol.");

        dto.CareerSuggestions.Should().ContainSingle();
        dto.CareerSuggestions[0].Field.Should().Be("Muhandislik");
        dto.CareerSuggestions[0].ExampleProfessions.Should().Equal("Dasturchi");
        dto.CareerSuggestions[0].NextSteps.Should().Equal("Kursga yozilsin", "Loyiha qilsin");

        dto.StudentRecommendations.Should().Equal("Kuniga 30 daqiqa o'qi.");
        dto.TeacherNotes.Should().Equal("Guruh ishiga jalb qiling.", "Savol berishga rag'batlantiring.");
        dto.ParentNotes.Should().Equal("Mustaqil vaqt bering.");

        dto.AttentionFlags.Should().ContainSingle();
        dto.AttentionFlags[0].Code.Should().Be("LOW_SOCIAL_ACTIVITY");
        dto.AttentionFlags[0].Severity.Should().Be("attention");

        dto.ReliabilityNote.Should().Be("Javoblar tez berilgan.");
        dto.Disclaimer.Should().Be("Bu tahlil tibbiy xulosa emas.");
        dto.IsModerated.Should().BeFalse();
    }

    /// <summary>
    /// `docs/09` 6-bo'lim, 3-band: moderatsiya belgisi FAQAT `AttentionFlags` USTUNIDA bo'ladi
    /// (`AnalysisOrchestrator.AppendModerationFlag`), `ResponseJson`da emas. Mazmun sxemadan
    /// o'qilganda ham u yo'qolmasligi va `IsModerated` orqali ochiq ko'rinishi kerak — aks holda
    /// post-filtrdan toza o'tmagan matn ekranga jimgina chiqib ketardi (`CLAUDE.md` 6-qoida).
    /// </summary>
    [Fact]
    public void BuildAiAnalysis_ModeratedResponse_KeepsModerationFlagAndSetsIsModerated()
    {
        var analysis = SucceededAnalysis(
            SchemaResponseJson,
            attentionFlagsJson: """["[attention] Ijtimoiy faollik past.","MODERATION_REQUIRED: taqiqlangan atama ikkinchi urinishda ham topildi."]""");

        var dto = StudentProfileMapping.BuildAiAnalysis(analysis);

        dto!.IsModerated.Should().BeTrue();
        dto.AttentionFlags.Should().Contain(f => f.Code == "MODERATION_REQUIRED" && f.Severity == "high");
        // Sxemadagi bayroq ham qoladi — biri ikkinchisini almashtirmaydi.
        dto.AttentionFlags.Should().Contain(f => f.Code == "LOW_SOCIAL_ACTIVITY");
    }

    /// <summary>
    /// Shablon hisobotda (`docs/09` 11-bo'lim) `ResponseJson` YO'Q — mazmun ustunlardan
    /// tiklanadi: satrli kuchli tomonlar `title`ga, `•` bilan yozilgan izohlar massivga.
    /// Sxemada bo'lmagan maydonlar `null` bo'lib qoladi (bo'sh satr emas).
    /// </summary>
    [Fact]
    public void BuildAiAnalysis_FallbackReport_RebuildsSectionsFromColumns()
    {
        var analysis = AiAnalysis.CreateFallbackReport(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AiProvider.Gemini,
            "v1.0",
            attemptNumber: 3,
            summary: "Shablon xulosa",
            personalityPortrait: "Shablon portret",
            strengthsJson: """["Tahliliy fikrlash","Mustaqillik"]""",
            growthAreasJson: """["Jamoada ishlash"]""",
            careerSuggestionsJson: """[{"field":"IT","why":"Qiziqishga mos","nextSteps":["Kurs"]}]""",
            teacherNotes: "• Birinchi izoh\n• Ikkinchi izoh",
            parentNotes: "Yagona izoh",
            attentionFlagsJson: """["Bu avtomatik shablon hisobot."]""",
            now: Now);

        var dto = StudentProfileMapping.BuildAiAnalysis(analysis);

        dto!.Strengths.Select(s => s.Title).Should().Equal("Tahliliy fikrlash", "Mustaqillik");
        dto.Strengths.Should().OnlyContain(s => s.Description == null && s.Evidence == null);
        dto.GrowthAreas.Select(g => g.Title).Should().Equal("Jamoada ishlash");
        dto.CareerSuggestions.Should().ContainSingle();
        dto.CareerSuggestions[0].ExampleProfessions.Should().BeEmpty();
        dto.TeacherNotes.Should().Equal("Birinchi izoh", "Ikkinchi izoh");
        dto.ParentNotes.Should().Equal("Yagona izoh");
        dto.AttentionFlags.Should().ContainSingle();
        dto.AttentionFlags[0].Code.Should().BeNull();
        dto.AttentionFlags[0].Severity.Should().Be("attention");
        dto.LearningStyle.Should().BeNull();
        dto.MotivationProfile.Should().BeNull();
        dto.ActivityAssessment.Should().BeNull();
        dto.Disclaimer.Should().BeNull();
        dto.ReliabilityNote.Should().BeNull();
        dto.IsModerated.Should().BeFalse();
    }

    /// <summary>Buzuq `ResponseJson` butun profilni yiqitmaydi — ustunlardagi nusxa ishlatiladi.</summary>
    [Fact]
    public void BuildAiAnalysis_CorruptResponseJson_FallsBackToColumns()
    {
        var analysis = SucceededAnalysis("{ buzuq json", attentionFlagsJson: "[]", strengthsJson: """["Ustundagi kuch"]""");

        var dto = StudentProfileMapping.BuildAiAnalysis(analysis);

        dto!.Strengths.Select(s => s.Title).Should().Equal("Ustundagi kuch");
        dto.Disclaimer.Should().BeNull();
    }

    [Fact]
    public void BuildAiAnalysis_Null_ReturnsNull()
    {
        StudentProfileMapping.BuildAiAnalysis(null).Should().BeNull();
    }

    private static AiAnalysis SucceededAnalysis(string responseJson, string attentionFlagsJson, string? strengthsJson = null)
    {
        var analysis = AiAnalysis.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AiProvider.Gemini,
            "gemini-2.0-flash",
            "v1.0",
            attemptNumber: 1,
            now: Now);
        analysis.Start();
        analysis.Succeed(
            responseJson,
            summary: "Umumiy xulosa.",
            personalityPortrait: "Shaxsiyat portreti.",
            now: Now,
            strengthsJson: strengthsJson,
            attentionFlagsJson: attentionFlagsJson);
        return analysis;
    }

    /// <summary>`docs/09` 5-bo'lim sxemasiga mos qisqartirilgan (lekin barcha maydonli) javob.</summary>
    private const string SchemaResponseJson = """
        {
          "summary": "Umumiy xulosa.",
          "personalityPortrait": "Shaxsiyat portreti.",
          "strengths": [
            { "title": "Tahliliy fikrlash", "description": "Murakkab masalani qismlarga ajratadi.", "evidence": "Vijdonlilik 77%." }
          ],
          "growthAreas": [
            { "title": "Jamoada ishlash", "description": "Guruh ishlarida chetda qolishi mumkin.", "actionStep": "Haftada bir marta guruh loyihasida qatnashsin." }
          ],
          "learningStyle": "Mustaqil, chuqur o'qish.",
          "motivationProfile": "Aniq maqsad motivatsiya beradi.",
          "activityAssessment": "O'rtacha faol.",
          "careerSuggestions": [
            { "field": "Muhandislik", "why": "Tizimli fikrlashga mos.", "exampleProfessions": ["Dasturchi"], "nextSteps": ["Kursga yozilsin", "Loyiha qilsin"] }
          ],
          "studentRecommendations": ["Kuniga 30 daqiqa o'qi."],
          "teacherNotes": ["Guruh ishiga jalb qiling.", "Savol berishga rag'batlantiring."],
          "parentNotes": ["Mustaqil vaqt bering."],
          "attentionFlags": [
            { "code": "LOW_SOCIAL_ACTIVITY", "message": "Ijtimoiy faollik past.", "severity": "attention" }
          ],
          "reliabilityNote": "Javoblar tez berilgan.",
          "disclaimer": "Bu tahlil tibbiy xulosa emas."
        }
        """;
}
