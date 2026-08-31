using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Events;

namespace StudentRoadMap.Domain.Assessments;

/// <summary>
/// Test topshirish sessiyasi — asosiy agregat ildizi (`docs/04` 2.3-bo'lim).
///
/// Holat mashinasi (`docs/04` 2.3):
/// <code>
/// Draft ──StartTest──▶ InProgress ──Complete──▶ Completed
///   │                      │                        │
///   │                      │                        ├─MarkAnalyzing──▶ Analyzing
///   │                      │                        │                    │
///   │                      ▼                         │                    ├─MarkAnalyzed──▶ Analyzed
///   └──────────────▶ Abandoned ◀─────────────────────┘                    └─MarkAnalysisFailed─▶ AnalysisFailed
///        MarkAbandoned        MarkAbandoned
/// </code>
/// </summary>
public sealed class Assessment : AggregateRoot
{
    private readonly List<AssessmentTest> _tests = [];

    public Guid StudentId { get; private set; }

    public Guid SchoolId { get; private set; }

    public string SessionToken { get; private set; } = null!;

    public AssessmentStatus Status { get; private set; }

    public string LanguageCode { get; private set; } = null!;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public double? ReliabilityScore { get; private set; }

    public ReliabilityFlag? ReliabilityFlag { get; private set; }

    public string? IpHash { get; private set; }

    public string? UserAgent { get; private set; }

    public int? TotalDurationSeconds { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<AssessmentTest> Tests => _tests.AsReadOnly();

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private Assessment()
    {
    }

    private Assessment(
        Guid id,
        Guid studentId,
        Guid schoolId,
        string sessionToken,
        string languageCode,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt,
        string? ipHash,
        string? userAgent,
        DateTimeOffset now)
        : base(id)
    {
        StudentId = studentId;
        SchoolId = schoolId;
        SessionToken = sessionToken;
        LanguageCode = languageCode;
        Status = AssessmentStatus.Draft;
        StartedAt = startedAt;
        ExpiresAt = expiresAt;
        IpHash = ipHash;
        UserAgent = userAgent;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Yangi sessiya — `Draft` holatida yaratiladi, `AssessmentStartedEvent` ko'taradi.</summary>
    public static Assessment Create(
        Guid id,
        Guid studentId,
        Guid schoolId,
        string sessionToken,
        string languageCode,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        string? ipHash = null,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw new ArgumentException("Sessiya tokeni bo'sh bo'lishi mumkin emas.", nameof(sessionToken));
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("Til kodi bo'sh bo'lishi mumkin emas.", nameof(languageCode));
        }

        if (expiresAt <= startedAt)
        {
            throw new ArgumentException("Amal qilish muddati boshlanish vaqtidan keyin bo'lishi kerak.", nameof(expiresAt));
        }

        var assessment = new Assessment(id, studentId, schoolId, sessionToken, languageCode, startedAt, expiresAt, ipHash, userAgent, now);
        assessment.RaiseDomainEvent(new AssessmentStartedEvent(id, studentId, schoolId, now));
        return assessment;
    }

    /// <summary>
    /// Sessiya yaratilganda test bloklari (`AssessmentTest`) biriktiriladi — faqat `Draft`
    /// holatida ruxsat etiladi.
    /// </summary>
    public void AddTest(AssessmentTest test)
    {
        if (Status != AssessmentStatus.Draft)
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", "Test bloklari faqat 'Draft' holatida biriktiriladi.");
        }

        if (_tests.Any(t => t.TestDefinitionId == test.TestDefinitionId))
        {
            throw new DomainException("ASSESSMENT_TEST_DUPLICATE", "Bu test bloki allaqachon biriktirilgan.");
        }

        _tests.Add(test);
    }

    /// <summary>`Draft ──▶ InProgress`: birinchi test boshlanganda. Keyingi testlar uchun idempotent.</summary>
    public void StartTest(Guid testDefinitionId, DateTimeOffset now)
    {
        var test = FindTestOrThrow(testDefinitionId);

        switch (Status)
        {
            case AssessmentStatus.Draft:
                Status = AssessmentStatus.InProgress;
                break;
            case AssessmentStatus.InProgress:
                break; // keyingi test — holat allaqachon to'g'ri
            default:
                throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatida yangi test boshlay olmaydi.");
        }

        test.Start(now);
        UpdatedAt = now;
    }

    /// <summary>Bitta test blokini yakunlaydi (butun sessiyani emas) — `TestCompletedEvent` ko'taradi.</summary>
    public void CompleteTest(Guid testDefinitionId, DateTimeOffset now)
    {
        if (Status != AssessmentStatus.InProgress)
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatida testni yakunlab bo'lmaydi.");
        }

        var test = FindTestOrThrow(testDefinitionId);
        test.Complete(now);
        UpdatedAt = now;

        RaiseDomainEvent(new TestCompletedEvent(Id, test.Id, testDefinitionId, now));
    }

    /// <summary>`InProgress ──▶ Completed`: barcha test bloklari yakunlanganda ("allTestsDone").</summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status != AssessmentStatus.InProgress)
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatidan 'Completed' ga o'ta olmaydi.");
        }

        if (_tests.Count == 0 || _tests.Any(t => t.Status != TestStatus.Completed))
        {
            throw new DomainException("ASSESSMENT_TESTS_NOT_DONE", "Barcha test bloklari yakunlanmasdan sessiyani yopib bo'lmaydi.");
        }

        Status = AssessmentStatus.Completed;
        CompletedAt = now;
        TotalDurationSeconds = (int)(now - StartedAt).TotalSeconds;
        UpdatedAt = now;

        RaiseDomainEvent(new AssessmentCompletedEvent(Id, StudentId, SchoolId, now));
    }

    /// <summary>
    /// `Completed`/`Analyzed`/`AnalysisFailed` ──▶ `Analyzing`: AI navbatga qo'yilganda.
    /// Uchta boshlang'ich holatdan ruxsat etiladi, chunki `docs/06-arxitektura.md`
    /// Application/Assessments/RerunAnalysis use-case'i superadminga tahlilni qayta
    /// ishga tushirishga imkon beradi: birinchi tahlil uchun `Completed` ("enqueue"),
    /// qayta tahlil uchun `Analyzed` (masalan, yangi AI provayder bilan), qayta urinish
    /// uchun `AnalysisFailed`. `Analyzed`/`AnalysisFailed` dan qaytadan boshlanganda
    /// `CompletedAt`, `TotalDurationSeconds` va boshqa yakuniy maydonlar o'zgarmaydi —
    /// faqat `Status` va `UpdatedAt` yangilanadi.
    /// </summary>
    public void MarkAnalyzing(DateTimeOffset now)
    {
        if (Status is not (AssessmentStatus.Completed or AssessmentStatus.Analyzed or AssessmentStatus.AnalysisFailed))
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatidan 'Analyzing' ga o'ta olmaydi.");
        }

        Status = AssessmentStatus.Analyzing;
        UpdatedAt = now;
    }

    /// <summary>`Analyzing ──▶ Analyzed`: AI muvaffaqiyatli javob berganda ("ok").</summary>
    public void MarkAnalyzed(DateTimeOffset now)
    {
        if (Status != AssessmentStatus.Analyzing)
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatidan 'Analyzed' ga o'ta olmaydi.");
        }

        Status = AssessmentStatus.Analyzed;
        UpdatedAt = now;
    }

    /// <summary>`Analyzing ──▶ AnalysisFailed`: 3 urinishdan keyin ham AI javob bermaganda ("fail").</summary>
    public void MarkAnalysisFailed(DateTimeOffset now)
    {
        if (Status != AssessmentStatus.Analyzing)
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatidan 'AnalysisFailed' ga o'ta olmaydi.");
        }

        Status = AssessmentStatus.AnalysisFailed;
        UpdatedAt = now;
    }

    /// <summary>
    /// `Draft`/`InProgress` ──▶ `Abandoned`: `ExpiresAt` o'tganda, hali yakunlanmagan sessiya
    /// tashlab ketilgan deb belgilanadi ("expired").
    /// </summary>
    public void MarkAbandoned(DateTimeOffset now)
    {
        if (Status is not (AssessmentStatus.Draft or AssessmentStatus.InProgress))
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatidan 'Abandoned' ga o'ta olmaydi.");
        }

        Status = AssessmentStatus.Abandoned;
        UpdatedAt = now;
    }

    /// <summary>Ishonchlilik tahlili natijasini yozadi (sessiya `Completed` bo'lgach hisoblanadi).</summary>
    public void SetReliability(double score, ReliabilityFlag flag, DateTimeOffset now)
    {
        ReliabilityScore = score;
        ReliabilityFlag = flag;
        UpdatedAt = now;
    }

    public void MarkDeleted(DateTimeOffset now)
    {
        IsDeleted = true;
        UpdatedAt = now;
    }

    private AssessmentTest FindTestOrThrow(Guid testDefinitionId)
    {
        var test = _tests.FirstOrDefault(t => t.TestDefinitionId == testDefinitionId);
        if (test is null)
        {
            throw new DomainException("ASSESSMENT_TEST_NOT_FOUND", "Ko'rsatilgan test bloki ushbu sessiyaga biriktirilmagan.");
        }

        return test;
    }
}
