using System.Text.Json;
using System.Text.Json.Serialization;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Application.Seeding;

/// <summary>
/// Test bankiga oid seed JSON'larini o'qish/validatsiya/domenga aylantirishning **DB'ga
/// bog'liq bo'lmagan** qismi (`prompts/04-katalog-va-seed-infratuzilma.md`). `Infrastructure`
/// dagi `DbSeeder` faylni o'qiydi (I/O) va matnni shu yerga uzatadi — shu sabab bu klass
/// EF/HTTP/fayl tizimiga tegmaydi, faqat sof funksiyalardan iborat va alohida sinaladi
/// (`tests/StudentRoadMap.Application.Tests/Seeding/`).
/// </summary>
public static class SeedDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Namunaviy so'rovnoma fayllari (`docs/18`) `VisibilityRule`/`VisibilityCondition`
    /// enum'larini (`Match`, `Operator`) JSON'da SATR sifatida saqlaydi (masalan `"Equals"`)
    /// — `JsonStringEnumConverter` shart. Test bankiga oid seed fayllarida (`ParseTestDefinition`)
    /// bunday enum-maydon yo'q, shu sabab alohida (blast radius kichik).
    /// </summary>
    private static readonly JsonSerializerOptions SurveyJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>JSON matnni o'qiydi va sxema bo'yicha validatsiya qiladi.</summary>
    /// <exception cref="SeedDataFormatException">JSON buzilgan yoki majburiy maydon yo'q/noto'g'ri.</exception>
    public static TestDefinitionSeedDto ParseTestDefinition(string json, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(sourceName);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new SeedDataFormatException(sourceName, "Seed fayli bo'sh.");
        }

        TestDefinitionSeedDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TestDefinitionSeedDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new SeedDataFormatException(sourceName, $"JSON formati noto'g'ri: {ex.Message}", ex);
        }

        if (dto is null)
        {
            throw new SeedDataFormatException(sourceName, "Seed fayli 'null' sifatida o'qildi.");
        }

        Validate(dto, sourceName);
        return dto;
    }

    /// <summary>
    /// Tasdiqlangan DTO'ni tizim metodikasi (`IsSystem = true`, `Kind = Standard`,
    /// `Status = Published`) sifatida domen agregatiga aylantiradi. `ScoringStrategyCode`
    /// har doim `dto.Code` dan olinadi.
    /// </summary>
    public static TestDefinition ToDomainSystemTestDefinition(
        TestDefinitionSeedDto dto,
        Guid testDefinitionId,
        Func<QuestionSeedDto, Guid> questionIdFactory,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(questionIdFactory);

        var questions = dto.Questions
            .OrderBy(q => q.Order)
            .Select(q => Question.Create(
                questionIdFactory(q),
                testDefinitionId,
                q.Code,
                q.Order,
                q.TextUz,
                Enum.Parse<QuestionType>(q.Type),
                q.Scale,
                q.Direction,
                q.Weight,
                q.IsRequired,
                isSystem: true,
                textRu: q.TextRu,
                textEn: q.TextEn))
            .ToList();

        return TestDefinition.CreateSystemPublished(
            testDefinitionId,
            dto.Code,
            dto.NameUz,
            dto.DescriptionUz,
            dto.DisplayOrder,
            dto.EstimatedMinutes,
            dto.ShuffleQuestions,
            dto.PageSize,
            scoringStrategyCode: dto.Code,
            questions,
            now);
    }

    /// <summary>
    /// Bazadagi mavjud tizim metodikasi bilan seed faylidagi savollarni `Code` bo'yicha
    /// solishtirib, `Scale`/`ScaleDirection`/`Weight` farqlarini topadi (BR-8 himoyasi,
    /// `CLAUDE.md` 9a-qoida). Yangi kod
    /// bilan kelgan savol (bazada yo'q) konflikt hisoblanmaydi — `DbSeeder` uni alohida
    /// ogohlantirish sifatida qayd etadi (tizim metodikasiga runtime'da savol qo'shilmaydi).
    /// </summary>
    public static IReadOnlyList<ScaleConflict> DetectScaleConflicts(TestDefinition existing, TestDefinitionSeedDto incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);

        var existingByCode = existing.Questions.ToDictionary(q => q.Code, StringComparer.Ordinal);
        var conflicts = new List<ScaleConflict>();

        foreach (var incomingQuestion in incoming.Questions)
        {
            if (!existingByCode.TryGetValue(incomingQuestion.Code, out var existingQuestion))
            {
                continue;
            }

            if (!string.Equals(existingQuestion.Scale, incomingQuestion.Scale, StringComparison.Ordinal)
                || existingQuestion.ScaleDirection != incomingQuestion.Direction
                || existingQuestion.Weight != incomingQuestion.Weight)
            {
                conflicts.Add(new ScaleConflict(
                    incomingQuestion.Code,
                    existingQuestion.Scale,
                    existingQuestion.ScaleDirection,
                    existingQuestion.Weight,
                    incomingQuestion.Scale,
                    incomingQuestion.Direction,
                    incomingQuestion.Weight));
            }
        }

        return conflicts;
    }

    /// <summary>
    /// `Infrastructure/Persistence/SeedData/surveys/*.json` matnini o'qiydi va sxema bo'yicha
    /// validatsiya qiladi (`docs/18` §7 — namunaviy tarmoqlanuvchi so'rovnoma).
    /// </summary>
    /// <exception cref="SeedDataFormatException">JSON buzilgan yoki majburiy maydon yo'q/noto'g'ri.</exception>
    public static SurveySeedDto ParseSurvey(string json, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(sourceName);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new SeedDataFormatException(sourceName, "Seed fayli bo'sh.");
        }

        SurveySeedDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<SurveySeedDto>(json, SurveyJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new SeedDataFormatException(sourceName, $"JSON formati noto'g'ri: {ex.Message}", ex);
        }

        if (dto is null)
        {
            throw new SeedDataFormatException(sourceName, "Seed fayli 'null' sifatida o'qildi.");
        }

        ValidateSurvey(dto, sourceName);
        return dto;
    }

    /// <summary>
    /// Tasdiqlangan DTO'ni `Kind = Custom`/`IsSystem = false` anketa sifatida domen agregatiga
    /// aylantiradi. `TestDefinition.Create` boshlang'ich holati doim `Draft` (`docs/18` §7:
    /// namunaviy so'rovnoma HECH QACHON avtomatik nashr qilinmaydi — superadmin tahrirlab
    /// o'zi nashr qiladi). Bo'limlar savollardan OLDIN qo'shiladi (savol `sectionCode` orqali
    /// bo'lim `Id`sini topadi).
    /// </summary>
    public static TestDefinition ToDomainCustomDraftSurvey(
        SurveySeedDto dto,
        Guid testDefinitionId,
        Func<SurveySectionSeedDto, Guid> sectionIdFactory,
        Func<SurveyQuestionSeedDto, Guid> questionIdFactory,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(sectionIdFactory);
        ArgumentNullException.ThrowIfNull(questionIdFactory);

        var scoringMode = Enum.Parse<TestScoringMode>(dto.ScoringMode, ignoreCase: true);

        var test = TestDefinition.Create(
            testDefinitionId,
            dto.Code,
            dto.NameUz,
            dto.DisplayOrder,
            dto.EstimatedMinutes,
            scoringStrategyCode: scoringMode == TestScoringMode.Scored ? "SUM" : null,
            now,
            kind: TestKind.Custom,
            isSystem: false,
            pageSize: dto.PageSize,
            shuffleQuestions: dto.ShuffleQuestions,
            descriptionUz: dto.DescriptionUz,
            scoringMode: scoringMode);

        foreach (var sectionDto in dto.Sections.OrderBy(s => s.DisplayOrder))
        {
            var section = QuestionSection.Create(
                sectionIdFactory(sectionDto),
                testDefinitionId,
                sectionDto.Code,
                sectionDto.TitleUz,
                sectionDto.DisplayOrder,
                sectionDto.DescriptionUz,
                sectionDto.Visibility);

            test.AddSection(section, now);
        }

        foreach (var questionDto in dto.Questions.OrderBy(q => q.Order))
        {
            Guid? sectionId = null;
            if (questionDto.SectionCode is { Length: > 0 } sectionCode)
            {
                // `ValidateSurvey` allaqachon har bir 'sectionCode' e'lon qilingan bo'limga
                // ishora qilishini tekshirgan — bu yerdagi `FirstOrDefault` shu sabab hech
                // qachon `null` qaytarmasligi kerak (himoya sifatida qoldirilgan).
                var section = test.Sections.FirstOrDefault(s => s.Code == sectionCode)
                    ?? throw new InvalidOperationException($"'{questionDto.Code}' savolining '{sectionCode}' bo'limi topilmadi — ValidateSurvey buni oldindan ushlashi kerak edi.");
                sectionId = section.Id;
            }

            var question = Question.Create(
                questionIdFactory(questionDto),
                testDefinitionId,
                questionDto.Code,
                questionDto.Order,
                questionDto.TextUz,
                Enum.Parse<QuestionType>(questionDto.Type),
                questionDto.Scale,
                questionDto.Direction,
                questionDto.Weight,
                isRequired: questionDto.IsRequired ?? true,
                isSystem: false,
                sectionId: sectionId,
                visibilityRule: questionDto.Visibility,
                placeholder: questionDto.Placeholder,
                inputPattern: questionDto.InputPattern,
                maxLength: questionDto.MaxLength,
                minSelections: questionDto.MinSelections,
                maxSelections: questionDto.MaxSelections);

            foreach (var optionDto in questionDto.Options ?? [])
            {
                question.AddOption(AnswerOption.Create(Guid.NewGuid(), question.Id, optionDto.TextUz, optionDto.Value, optionDto.DisplayOrder));
            }

            test.AddQuestion(question, now);
        }

        return test;
    }

    private static void ValidateSurvey(SurveySeedDto dto, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            throw new SeedDataFormatException(sourceName, "'code' maydoni bo'sh bo'lishi mumkin emas.");
        }

        if (string.IsNullOrWhiteSpace(dto.NameUz))
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': 'nameUz' maydoni bo'sh bo'lishi mumkin emas.");
        }

        if (dto.PageSize <= 0)
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': 'pageSize' musbat bo'lishi kerak.");
        }

        if (dto.EstimatedMinutes <= 0)
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': 'estimatedMinutes' musbat bo'lishi kerak.");
        }

        if (!Enum.TryParse<TestScoringMode>(dto.ScoringMode, ignoreCase: true, out _))
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': 'scoringMode' qiymati noto'g'ri: '{dto.ScoringMode}'.");
        }

        if (dto.Questions.Count == 0)
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': savollar ro'yxati bo'sh.");
        }

        var sectionCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in dto.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Code))
            {
                throw new SeedDataFormatException(sourceName, $"'{dto.Code}': kod ko'rsatilmagan bo'lim bor.");
            }

            if (!sectionCodes.Add(section.Code))
            {
                throw new SeedDataFormatException(sourceName, $"'{dto.Code}': takrorlangan bo'lim kodi '{section.Code}'.");
            }

            if (string.IsNullOrWhiteSpace(section.TitleUz))
            {
                throw new SeedDataFormatException(sourceName, $"'{dto.Code}'/'{section.Code}': bo'lim sarlavhasi bo'sh.");
            }
        }

        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var question in dto.Questions)
        {
            ValidateSurveyQuestion(dto.Code, question, seenCodes, sectionCodes, sourceName);
        }
    }

    private static void ValidateSurveyQuestion(string testCode, SurveyQuestionSeedDto question, HashSet<string> seenCodes, HashSet<string> sectionCodes, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(question.Code))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}': kod ko'rsatilmagan savol bor.");
        }

        if (!seenCodes.Add(question.Code))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}': takrorlangan savol kodi '{question.Code}'.");
        }

        if (string.IsNullOrWhiteSpace(question.TextUz))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': savol matni bo'sh.");
        }

        if (string.IsNullOrWhiteSpace(question.Scale))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': 'scale' ko'rsatilmagan.");
        }

        if (question.Direction is not (1 or -1))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': 'direction' faqat +1/-1 bo'lishi mumkin (berilgan: {question.Direction}).");
        }

        if (question.Weight <= 0)
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': 'weight' musbat bo'lishi kerak.");
        }

        if (!Enum.TryParse<QuestionType>(question.Type, ignoreCase: false, out _))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': 'type' qiymati noto'g'ri: '{question.Type}'.");
        }

        if (question.SectionCode is { Length: > 0 } sectionCode && !sectionCodes.Contains(sectionCode))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': '{sectionCode}' kodli bo'lim topilmadi — bo'limlar savoldan OLDIN e'lon qilinishi shart.");
        }
    }

    private static void Validate(TestDefinitionSeedDto dto, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            throw new SeedDataFormatException(sourceName, "'code' maydoni bo'sh bo'lishi mumkin emas.");
        }

        if (string.IsNullOrWhiteSpace(dto.NameUz))
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': 'nameUz' maydoni bo'sh bo'lishi mumkin emas.");
        }

        if (dto.PageSize <= 0)
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': 'pageSize' musbat bo'lishi kerak.");
        }

        if (dto.EstimatedMinutes <= 0)
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': 'estimatedMinutes' musbat bo'lishi kerak.");
        }

        if (dto.Questions.Count == 0)
        {
            throw new SeedDataFormatException(sourceName, $"'{dto.Code}': savollar ro'yxati bo'sh.");
        }

        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var question in dto.Questions)
        {
            ValidateQuestion(dto.Code, question, seenCodes, sourceName);
        }
    }

    private static void ValidateQuestion(string testCode, QuestionSeedDto question, HashSet<string> seenCodes, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(question.Code))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}': kod ko'rsatilmagan savol bor.");
        }

        if (!seenCodes.Add(question.Code))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}': takrorlangan savol kodi '{question.Code}'.");
        }

        if (string.IsNullOrWhiteSpace(question.TextUz))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': savol matni bo'sh.");
        }

        if (string.IsNullOrWhiteSpace(question.Scale))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': 'scale' ko'rsatilmagan.");
        }

        if (question.Direction is not (1 or -1))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': 'direction' faqat +1/-1 bo'lishi mumkin (berilgan: {question.Direction}).");
        }

        if (question.Weight <= 0)
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': 'weight' musbat bo'lishi kerak.");
        }

        if (!Enum.TryParse<QuestionType>(question.Type, ignoreCase: false, out _))
        {
            throw new SeedDataFormatException(sourceName, $"'{testCode}'/'{question.Code}': 'type' qiymati noto'g'ri: '{question.Type}'.");
        }
    }
}
