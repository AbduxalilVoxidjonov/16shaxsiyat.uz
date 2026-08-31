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

    public string ScoringStrategyCode { get; private set; } = null!;

    public TestDefinitionStatus Status { get; private set; }

    public Guid? CreatedByAdminUserId { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

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
        string scoringStrategyCode,
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
        string scoringStrategyCode,
        DateTimeOffset now,
        TestKind kind = TestKind.Custom,
        bool isSystem = false,
        int pageSize = 10,
        bool shuffleQuestions = false,
        string? descriptionUz = null,
        Guid? createdByAdminUserId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Anketa kodi bo'sh bo'lishi mumkin emas.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Anketa nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        if (string.IsNullOrWhiteSpace(scoringStrategyCode))
        {
            throw new ArgumentException("Scoring strategiyasi kodi bo'sh bo'lishi mumkin emas.", nameof(scoringStrategyCode));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Sahifa hajmi musbat bo'lishi kerak.");
        }

        return new TestDefinition(id, code, nameUz, descriptionUz, displayOrder, estimatedMinutes, shuffleQuestions, pageSize, kind, isSystem, scoringStrategyCode, createdByAdminUserId, now);
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
