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

    /// <summary>
    /// Sessiya bog'langan dastur (`docs/06` 8-bo'lim, 2026-09-02 qaror, `prompts/34` A5-band).
    /// Majburiy — `Assessment.Create` dasturni talab qiladi. Ikki bosqichli migratsiya
    /// (`CLAUDE.md` 7-qoida) yakunlandi: birinchi migratsiya (`AddAssessmentPrograms`) ustunni
    /// nullable qo'shdi, `DbSeeder.SeedSystemProgramAsync` mavjud sessiyalarni `PERSONALITY_PROFILE`
    /// tizim dasturiga bog'ladi, ikkinchi migratsiya (`RequireAssessmentProgramId`) `NOT NULL`
    /// qildi.
    /// </summary>
    public Guid ProgramId { get; private set; }

    /// <summary>
    /// ⚠️ ESKI (ochiq matnli) sessiya tokeni. Ikki bosqichli migratsiya (`CLAUDE.md` 7-qoida)
    /// ning BIRINCHI bosqichida hali saqlanadi — hozirgi `SessionTokenAuthenticationHandler`
    /// aynan shu ustun bo'yicha qidiradi. IKKINCHI bosqichda (keyingi agent: autentifikatsiya
    /// handleri va `StartSession` rezyume yo'li <see cref="SessionTokenHash"/> ga o'tgach)
    /// ustun alohida migratsiya bilan O'CHIRILADI. YANGI kod bu maydonni ISHLATMASIN.
    /// </summary>
    public string SessionToken { get; private set; } = null!;

    /// <summary>
    /// Sessiya tokenining SHA-256 xeshi (<see cref="TokenHash"/>) — refresh tokenlar bilan
    /// bir xil himoya darajasi. Ilgari sessiya tokeni bazada OCHIQ saqlanardi: DB nusxasi
    /// sizib chiqsa barcha faol sessiyalar bevosita ochilardi.
    ///
    /// `Create`/`RotateSessionToken` xom tokendan O'ZI hisoblaydi — chaqiruvchi qatlam
    /// ikkalasini sinxron ushlab turishi shart emas, ya'ni "xesh yozilmay qolgan" holat
    /// domen darajasida imkonsiz.
    /// </summary>
    public string SessionTokenHash { get; private set; } = null!;

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
        Guid programId,
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
        SessionTokenHash = TokenHash.Compute(sessionToken);
        LanguageCode = languageCode;
        ProgramId = programId;
        Status = AssessmentStatus.Draft;
        StartedAt = startedAt;
        ExpiresAt = expiresAt;
        IpHash = ipHash;
        UserAgent = userAgent;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Yangi sessiya — `Draft` holatida yaratiladi, `AssessmentStartedEvent` ko'taradi.
    /// `programId` majburiy (`docs/06` 8-bo'lim, 2026-09-02 qaror) — sessiyaga faqat shu
    /// dasturning testlari qo'shiladi (`prompts/34` A5-band).
    /// </summary>
    public static Assessment Create(
        Guid id,
        Guid studentId,
        Guid schoolId,
        string sessionToken,
        string languageCode,
        Guid programId,
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

        var assessment = new Assessment(id, studentId, schoolId, sessionToken, languageCode, programId, startedAt, expiresAt, ipHash, userAgent, now);
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

    /// <summary>
    /// Bitta test blokini yakunlaydi (butun sessiyani emas) — `TestCompletedEvent` ko'taradi.
    /// `requiredQuestionIds` — shu test blokidagi MAJBURIY savollar ID'lari (Application
    /// qatlami `Catalog.Question.IsRequired`dan hisoblab beradi, `Assessment` Catalog'ga
    /// bog'liq emas — faqat ID ro'yxati) — `AssessmentTest.Complete` shularning barchasi
    /// javoblanganini tekshiradi (QA tuzatmasi: ixtiyoriy savol javobsiz qolishi mumkin).
    /// </summary>
    public void CompleteTest(Guid testDefinitionId, IReadOnlyCollection<Guid> requiredQuestionIds, DateTimeOffset now)
    {
        if (Status != AssessmentStatus.InProgress)
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatida testni yakunlab bo'lmaydi.");
        }

        var test = FindTestOrThrow(testDefinitionId);
        test.Complete(now, requiredQuestionIds);
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

    /// <summary>
    /// Sessiya tokenini almashtiradi (rotatsiya). Xesh saqlanadigan bo'lgach xom tokenni
    /// bazadan QAYTA O'QIB bo'lmaydi — shu sabab mavjud sessiyani davom ettirish
    /// ("resumed") oqimi eski tokenni qaytara olmaydi va YANGI token berishi kerak.
    /// Bu ayni paytda xavfsizroq ham: har rezyumeda token yangilanadi, eskisi o'ladi.
    /// Yakunlangan/tashlab ketilgan sessiyaga yangi token berilmaydi.
    /// </summary>
    public void RotateSessionToken(string newSessionToken, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newSessionToken))
        {
            throw new ArgumentException("Sessiya tokeni bo'sh bo'lishi mumkin emas.", nameof(newSessionToken));
        }

        if (Status is not (AssessmentStatus.Draft or AssessmentStatus.InProgress))
        {
            throw new DomainException("ASSESSMENT_INVALID_TRANSITION", $"Sessiya '{Status}' holatida yangi token ololmaydi.");
        }

        SessionToken = newSessionToken;
        SessionTokenHash = TokenHash.Compute(newSessionToken);
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
