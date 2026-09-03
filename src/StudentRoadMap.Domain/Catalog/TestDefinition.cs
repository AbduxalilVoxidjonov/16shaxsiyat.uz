using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Anketa (metodika) ta'rifi — agregat ildizi. Tizim metodikalari (`IsSystem = true`) seed'dan
/// keladi va qulflangan: savol qo'shilmaydi/o'chirilmaydi, shkalasi o'zgarmaydi (BR-8).
/// Nashr oqimi: `Draft → Published → Archived` (ADR-15, `docs/04` 2.7-bo'lim).
/// </summary>
public sealed class TestDefinition : AggregateRoot
{
    private readonly List<Question> _questions = [];
    private readonly List<TestScale> _scales = [];

    public string Code { get; private set; } = null!;

    public string NameUz { get; private set; } = null!;

    public string? DescriptionUz { get; private set; }

    public int Version { get; private set; }

    public int DisplayOrder { get; private set; }

    /// <summary>Savollar soni — `_questions` kolleksiyasidan hisoblanadi.</summary>
    public int QuestionCount => _questions.Count;

    public int EstimatedMinutes { get; private set; }

    public bool ShuffleQuestions { get; private set; }

    public int PageSize { get; private set; }

    public bool IsActive { get; private set; }

    public TestKind Kind { get; private set; }

    /// <summary>Seed'dan kelgan tizim metodikasi — o'chirilmaydi, qulflangan (BR-8).</summary>
    public bool IsSystem { get; private set; }

    /// <summary>`Survey` (`ScoringMode`) rejimida `null` — ballanmaydi, strategiya ishlatilmaydi
    /// (`docs/06` 8-bo'lim, 2026-09-02 qaror, `prompts/34` A4-band).</summary>
    public string? ScoringStrategyCode { get; private set; }

    /// <summary>`Scored` — ballanadigan anketa; `Survey` — oddiy so'rovnoma (`ScoringStrategyCode` ishlatilmaydi).</summary>
    public TestScoringMode ScoringMode { get; private set; }

    /// <summary>
    /// Ushbu anketa "shaxsiyat batareyasi"ga (ilmiy metodikalar) kiradimi — mezon va uning
    /// asosi `PersonalityBattery` sinfida (kod satri bo'yicha EMAS).
    /// </summary>
    public bool IsPersonalityBattery => PersonalityBattery.Includes(Kind, ScoringMode);

    public TestDefinitionStatus Status { get; private set; }

    public Guid? CreatedByAdminUserId { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    /// <summary>Faqat `Custom` testlarda to'ldiriladi — tizim metodikasida bo'sh (`docs/07` §3.4).</summary>
    public IReadOnlyCollection<TestScale> Scales => _scales.AsReadOnly();

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private TestDefinition()
    {
    }

    private TestDefinition(
        Guid id,
        string code,
        string nameUz,
        string? descriptionUz,
        int displayOrder,
        int estimatedMinutes,
        bool shuffleQuestions,
        int pageSize,
        TestKind kind,
        bool isSystem,
        string? scoringStrategyCode,
        TestScoringMode scoringMode,
        Guid? createdByAdminUserId,
        DateTimeOffset now)
        : base(id)
    {
        Code = code;
        NameUz = nameUz;
        DescriptionUz = descriptionUz;
        Version = 1;
        DisplayOrder = displayOrder;
        EstimatedMinutes = estimatedMinutes;
        ShuffleQuestions = shuffleQuestions;
        PageSize = pageSize;
        IsActive = true;
        Kind = kind;
        IsSystem = isSystem;
        ScoringStrategyCode = scoringStrategyCode;
        ScoringMode = scoringMode;
        Status = TestDefinitionStatus.Draft;
        CreatedByAdminUserId = createdByAdminUserId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static TestDefinition Create(
        Guid id,
        string code,
        string nameUz,
        int displayOrder,
        int estimatedMinutes,
        string? scoringStrategyCode,
        DateTimeOffset now,
        TestKind kind = TestKind.Custom,
        bool isSystem = false,
        int pageSize = 10,
        bool shuffleQuestions = false,
        string? descriptionUz = null,
        Guid? createdByAdminUserId = null,
        TestScoringMode scoringMode = TestScoringMode.Scored)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Anketa kodi bo'sh bo'lishi mumkin emas.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Anketa nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        // `Survey` (`docs/06` 8-bo'lim, 2026-09-02 qaror) — `ScoringStrategyCode` ishlatilmaydi,
        // shu sabab bo'sh bo'lishi mumkin. `Scored` uchun hamon majburiy (mavjud xatti-harakat).
        if (scoringMode == TestScoringMode.Scored && string.IsNullOrWhiteSpace(scoringStrategyCode))
        {
            throw new ArgumentException("Scoring strategiyasi kodi bo'sh bo'lishi mumkin emas.", nameof(scoringStrategyCode));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Sahifa hajmi musbat bo'lishi kerak.");
        }

        var effectiveScoringStrategyCode = scoringMode == TestScoringMode.Survey ? null : scoringStrategyCode;

        return new TestDefinition(id, code, nameUz, descriptionUz, displayOrder, estimatedMinutes, shuffleQuestions, pageSize, kind, isSystem, effectiveScoringStrategyCode, scoringMode, createdByAdminUserId, now);
    }

    /// <summary>Yangi savol qo'shadi. Tizim metodikasida taqiqlangan (BR-8).</summary>
    public void AddQuestion(Question question, DateTimeOffset now)
    {
        if (IsSystem)
        {
            throw new DomainException("SYSTEM_TEST_LOCKED", "Tizim metodikasiga yangi savol qo'shib bo'lmaydi.");
        }

        if (question.TestDefinitionId != Id)
        {
            throw new ArgumentException("Savol boshqa anketaga tegishli.", nameof(question));
        }

        if (_questions.Any(q => q.Code == question.Code))
        {
            throw new DomainException("QUESTION_CODE_DUPLICATE", "Bu kod bilan savol allaqachon mavjud.");
        }

        _questions.Add(question);
        BumpVersionIfPublished();
        UpdatedAt = now;
    }

    /// <summary>Savolni olib tashlaydi. Tizim metodikasida taqiqlangan (BR-8).</summary>
    public void RemoveQuestion(Guid questionId, DateTimeOffset now)
    {
        if (IsSystem)
        {
            throw new DomainException("SYSTEM_TEST_LOCKED", "Tizim metodikasidan savol o'chirib bo'lmaydi.");
        }

        var removed = _questions.RemoveAll(q => q.Id == questionId) > 0;
        if (!removed)
        {
            return;
        }

        BumpVersionIfPublished();
        UpdatedAt = now;
    }

    /// <summary>Yangi shkala qo'shadi. Tizim metodikasida taqiqlangan (BR-8, `docs/07` §3.4: "Shkalalar — faqat Custom").</summary>
    public void AddScale(TestScale scale, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(scale);

        if (IsSystem)
        {
            throw new DomainException("SYSTEM_TEST_LOCKED", "Tizim metodikasiga yangi shkala qo'shib bo'lmaydi.");
        }

        if (scale.TestDefinitionId != Id)
        {
            throw new ArgumentException("Shkala boshqa anketaga tegishli.", nameof(scale));
        }

        if (_scales.Any(s => s.Code == scale.Code))
        {
            throw new DomainException("SCALE_CODE_DUPLICATE", "Bu kod bilan shkala allaqachon mavjud.");
        }

        _scales.Add(scale);
        BumpVersionIfPublished();
        UpdatedAt = now;
    }

    /// <summary>
    /// Shkalani olib tashlaydi. Tizim metodikasida taqiqlangan (BR-8); shkalada savollar bo'lsa
    /// `SCALE_IN_USE` (`docs/07` §3.4: "DELETE .../scales/{scaleId} (savollari bo'lsa 409)").
    /// </summary>
    public void RemoveScale(Guid scaleId, DateTimeOffset now)
    {
        if (IsSystem)
        {
            throw new DomainException("SYSTEM_TEST_LOCKED", "Tizim metodikasidan shkala o'chirib bo'lmaydi.");
        }

        var scale = _scales.FirstOrDefault(s => s.Id == scaleId);
        if (scale is null)
        {
            return;
        }

        if (_questions.Any(q => q.Scale == scale.Code))
        {
            throw new DomainException("SCALE_IN_USE", "Bu shkalada savollar bor — avval savollarni boshqa shkalaga o'tkazing yoki o'chiring.");
        }

        _scales.Remove(scale);
        BumpVersionIfPublished();
        UpdatedAt = now;
    }

    /// <summary>`Draft ──▶ Published`: kamida bitta faol savol bo'lishi shart (`TEST_NOT_PUBLISHABLE`).</summary>
    public void Publish(DateTimeOffset now)
    {
        if (Status != TestDefinitionStatus.Draft)
        {
            throw new DomainException("TEST_DEFINITION_INVALID_TRANSITION", $"Anketa '{Status}' holatidan 'Published' ga o'ta olmaydi.");
        }

        if (!_questions.Any(q => q.IsActive))
        {
            throw new DomainException("TEST_NOT_PUBLISHABLE", "Kamida bitta faol savol bo'lmasa anketani nashr qilib bo'lmaydi.");
        }

        Status = TestDefinitionStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Yangi sessiyalarga kirish/kirmasligini boshqaradi (BR-10) — `docs/07` §3.4 "toggle-active".</summary>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>BR-10: faqat YANGI sessiyalarga ta'sir qiladi, boshlangan sessiyalar oxirigacha davom etadi (Application/`AssessmentTest` darajasida — bu yerda faqat bayroq).</summary>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>`Draft`/`Published` ──▶ `Archived`.</summary>
    public void Archive(DateTimeOffset now)
    {
        if (Status is not (TestDefinitionStatus.Draft or TestDefinitionStatus.Published))
        {
            throw new DomainException("TEST_DEFINITION_INVALID_TRANSITION", $"Anketa '{Status}' holatidan 'Archived' ga o'ta olmaydi.");
        }

        Status = TestDefinitionStatus.Archived;
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>
    /// Anketaning yangi (tahrirlanadigan) nusxasini yaratadi — har doim `Custom`/`IsSystem=false`/`Draft`,
    /// tizim metodikasini tahrirlash kerak bo'lganda ishlatiladi.
    /// </summary>
    public TestDefinition Duplicate(Guid newId, string newCode, DateTimeOffset now, Guid? createdByAdminUserId = null)
    {
        var copy = Create(
            newId,
            newCode,
            NameUz,
            DisplayOrder,
            EstimatedMinutes,
            ScoringStrategyCode,
            now,
            kind: TestKind.Custom,
            isSystem: false,
            pageSize: PageSize,
            shuffleQuestions: ShuffleQuestions,
            descriptionUz: DescriptionUz,
            createdByAdminUserId: createdByAdminUserId);

        foreach (var question in _questions)
        {
            var questionCopy = Question.Create(
                Guid.NewGuid(),
                newId,
                question.Code,
                question.DisplayOrder,
                question.TextUz,
                question.QuestionType,
                question.Scale,
                question.ScaleDirection,
                question.Weight,
                question.IsRequired,
                isSystem: false,
                textRu: question.TextRu,
                textEn: question.TextEn);

            foreach (var option in question.Options)
            {
                questionCopy.AddOption(AnswerOption.Create(Guid.NewGuid(), questionCopy.Id, option.TextUz, option.Value, option.DisplayOrder, option.Scale));
            }

            copy._questions.Add(questionCopy);
        }

        foreach (var scale in _scales)
        {
            copy._scales.Add(TestScale.Create(
                Guid.NewGuid(),
                newId,
                scale.Code,
                scale.NameUz,
                scale.DisplayOrder,
                scale.InterpretationBands,
                scale.DescriptionUz));
        }

        return copy;
    }

    /// <summary>
    /// Seed infratuzilmasi uchun: tizim metodikasini (`IsSystem = true`) savollari bilan birga
    /// to'g'ridan-to'g'ri **nashr qilingan** holatda materiallashtiradi. `AddQuestion` tizim
    /// metodikasida taqiqlangani uchun (BR-8) bu — savollarni bir martalik joylashtirishning
    /// yagona yo'li; keyingi qayta seedlashda faqat matn/tartib yangilanadi
    /// (`Question.UpdateText`/`UpdateOrder`), shkala esa `SeedDataLoader.DetectScaleConflicts`
    /// bilan himoyalanadi.
    /// </summary>
    public static TestDefinition CreateSystemPublished(
        Guid id,
        string code,
        string nameUz,
        string? descriptionUz,
        int displayOrder,
        int estimatedMinutes,
        bool shuffleQuestions,
        int pageSize,
        string scoringStrategyCode,
        IReadOnlyList<Question> questions,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(questions);

        if (questions.Count == 0)
        {
            throw new ArgumentException("Tizim metodikasida kamida bitta savol bo'lishi kerak.", nameof(questions));
        }

        var testDefinition = new TestDefinition(
            id,
            code,
            nameUz,
            descriptionUz,
            displayOrder,
            estimatedMinutes,
            shuffleQuestions,
            pageSize,
            TestKind.Standard,
            isSystem: true,
            scoringStrategyCode,
            TestScoringMode.Scored,
            createdByAdminUserId: null,
            now);

        foreach (var question in questions)
        {
            if (question.TestDefinitionId != id)
            {
                throw new ArgumentException("Savol boshqa anketaga tegishli.", nameof(questions));
            }

            if (!question.IsSystem)
            {
                throw new ArgumentException("Tizim metodikasi faqat 'IsSystem = true' savollarni qabul qiladi.", nameof(questions));
            }

            testDefinition._questions.Add(question);
        }

        testDefinition.Status = TestDefinitionStatus.Published;
        testDefinition.PublishedAt = now;

        return testDefinition;
    }

    /// <summary>
    /// Seed qayta ishga tushirilganda matn/tartibga oid metadatani yangilaydi — `Scale`/`Kind`/
    /// `IsSystem`/`Status`ga tegmaydi (BR-8 shkala qulfi shu metoddan tashqarida saqlanadi).
    /// </summary>
    public void UpdateMetadata(
        string nameUz,
        string? descriptionUz,
        int displayOrder,
        int estimatedMinutes,
        bool shuffleQuestions,
        int pageSize,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Anketa nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Sahifa hajmi musbat bo'lishi kerak.");
        }

        NameUz = nameUz;
        DescriptionUz = descriptionUz;
        DisplayOrder = displayOrder;
        EstimatedMinutes = estimatedMinutes;
        ShuffleQuestions = shuffleQuestions;
        PageSize = pageSize;
        UpdatedAt = now;
    }

    private void BumpVersionIfPublished()
    {
        // Nashr qilingan anketaga savol qo'shilsa/olib tashlansa versiya oshadi (docs/04 2.7, BR-9) —
        // `TestVersionBumpedEvent` P02 doirasiga kiritilmagan (prompt 8-band ro'yxatida yo'q).
        if (Status == TestDefinitionStatus.Published)
        {
            Version++;
        }
    }
}
