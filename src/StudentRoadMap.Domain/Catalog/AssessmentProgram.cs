using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Dastur — nomlangan, tartiblangan test to'plami (`docs/06-arxitektura.md` 8-bo'lim,
/// 2026-09-02 qaror; `prompts/34` A1-band). O'quvchi kirishda mavjud dasturlardan bittasini
/// tanlaydi (bitta bo'lsa avtomatik) — sessiyaga faqat shu dasturning testlari qo'shiladi.
///
/// Tizim dasturi (`IsSystem = true`, seed'dan keladi — mavjud 4 metodikani birlashtiruvchi
/// `PERSONALITY_PROFILE`) qulflangan: tarkib (`AddTest`/`RemoveTest`/`ReorderTests`)
/// o'zgartirilmaydi (BR-8 ruhida, xato kodi `SYSTEM_PROGRAM_LOCKED`). Superadmin `Custom`
/// dasturlarida hammasi ochiq (`CLAUDE.md` 9a-qoida).
/// </summary>
public sealed class AssessmentProgram : AggregateRoot
{
    private readonly List<ProgramTest> _tests = [];

    public string Code { get; private set; } = null!;

    public string NameUz { get; private set; } = null!;

    public string? DescriptionUz { get; private set; }

    public ProgramKind Kind { get; private set; }

    public ProgramVisibility Visibility { get; private set; }

    public ProgramStatus Status { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Tashqariga (admin API va UI'ga) beriladigan YAGONA holat — `Status` va `IsActive`
    /// dan HOSILA (`ProgramStateRules.Resolve`). EF Core buni ustunga bog'lamaydi: faqat
    /// o'quvchi (setter'siz, zaxira maydonsiz) xossalar konvensiya bo'yicha xaritalanmaydi.
    ///
    /// Ikkita maydon bazada saqlanadi, chunki ommaviy oqim va havola sog'ligi mezoni
    /// (`ProgramAvailability`, `SchoolLinkHealthEvaluator`) ularga tayanadi; lekin
    /// "Arxiv + Faol" kabi ziddiyatli JUFTLIK endi ko'rsatilmaydi — bu yerda `Status`
    /// ustuvor.
    /// </summary>
    public ProgramState State => ProgramStateRules.Resolve(Status, IsActive);

    public int DisplayOrder { get; private set; }

    /// <summary>Seed'dan kelgan tizim dasturi — tarkibi qulflangan (BR-8 ruhida).</summary>
    public bool IsSystem { get; private set; }

    public Guid? CreatedByAdminUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<ProgramTest> Tests => _tests.AsReadOnly();

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private AssessmentProgram()
    {
    }

    private AssessmentProgram(
        Guid id,
        string code,
        string nameUz,
        string? descriptionUz,
        int displayOrder,
        ProgramKind kind,
        ProgramVisibility visibility,
        bool isSystem,
        Guid? createdByAdminUserId,
        DateTimeOffset now)
        : base(id)
    {
        Code = code;
        NameUz = nameUz;
        DescriptionUz = descriptionUz;
        DisplayOrder = displayOrder;
        Kind = kind;
        Visibility = visibility;
        Status = ProgramStatus.Draft;
        IsActive = true;
        IsSystem = isSystem;
        CreatedByAdminUserId = createdByAdminUserId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static AssessmentProgram Create(
        Guid id,
        string code,
        string nameUz,
        DateTimeOffset now,
        int displayOrder = 1,
        ProgramKind kind = ProgramKind.Custom,
        ProgramVisibility visibility = ProgramVisibility.Assigned,
        string? descriptionUz = null,
        Guid? createdByAdminUserId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Dastur kodi bo'sh bo'lishi mumkin emas.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Dastur nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        return new AssessmentProgram(id, code, nameUz, descriptionUz, displayOrder, kind, visibility, isSystem: false, createdByAdminUserId, now);
    }

    /// <summary>
    /// Seed infratuzilmasi uchun: tizim dasturini (`IsSystem = true`) testlari bilan birga
    /// to'g'ridan-to'g'ri nashr qilingan holatda materiallashtiradi — `TestDefinition.CreateSystemPublished`
    /// naqshiga o'xshash. `AddTest` tizim dasturida taqiqlangani uchun (BR-8 ruhida) bu —
    /// tarkibni bir martalik joylashtirishning yagona yo'li.
    /// </summary>
    public static AssessmentProgram CreateSystemPublished(
        Guid id,
        string code,
        string nameUz,
        string? descriptionUz,
        int displayOrder,
        IReadOnlyList<(Guid TestDefinitionId, int DisplayOrder)> tests,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(tests);

        if (tests.Count == 0)
        {
            throw new ArgumentException("Tizim dasturida kamida bitta test bo'lishi kerak.", nameof(tests));
        }

        var program = new AssessmentProgram(
            id, code, nameUz, descriptionUz, displayOrder, ProgramKind.System, ProgramVisibility.Public,
            isSystem: true, createdByAdminUserId: null, now);

        foreach (var (testDefinitionId, testDisplayOrder) in tests)
        {
            program._tests.Add(ProgramTest.Create(Guid.NewGuid(), id, testDefinitionId, testDisplayOrder));
        }

        program.Status = ProgramStatus.Published;

        return program;
    }

    /// <summary>
    /// Seed infratuzilmasi uchun (`DbSeeder.SeedSystemProgramAsync`): mavjud tizim dasturiga
    /// (`IsSystem = true`) HALI BOG'LANMAGAN test(lar)ni qo'shadi — `AddTest`dan farqli, BR-8
    /// qulfini CHETLAB O'TADI. Bu faqat bitta legitim stsenariy uchun: birinchi deploy'da
    /// migratsiya (`RequireAssessmentProgramId`) dasturni test banki hali seed qilinmagan
    /// paytda (0 ta test bilan) yaratishi mumkin — keyinroq `--seed` haqiqiy test banki (4 tizim
    /// metodikasi) yozilgach, shu metod orqali ULARNI (va faqat ULARNI — chaqiruvchi tanlagan
    /// ro'yxatni) bog'laydi. Admin API (`AddProgramTestCommandHandler`) BU METODNI CHAQIRMAYDI —
    /// u oddiy `AddTest`dan foydalanadi, shu sabab tizim dasturi tarkibi runtime'da admin
    /// tomonidan o'zgartirilmaydi (BR-8 buzilmaydi). Faqat `IsSystem = true` dasturda ishlaydi.
    /// </summary>
    public IReadOnlyList<ProgramTest> EnsureSystemTestsAttached(IReadOnlyList<(Guid TestDefinitionId, int DisplayOrder)> tests, DateTimeOffset now)
    {
        if (!IsSystem)
        {
            throw new DomainException("SYSTEM_PROGRAM_LOCKED", "Bu metod faqat tizim dasturlari uchun.");
        }

        ArgumentNullException.ThrowIfNull(tests);

        var alreadyLinked = _tests.Select(t => t.TestDefinitionId).ToHashSet();
        var newlyAdded = new List<ProgramTest>();

        foreach (var (testDefinitionId, displayOrder) in tests)
        {
            if (alreadyLinked.Contains(testDefinitionId))
            {
                continue;
            }

            var programTest = ProgramTest.Create(Guid.NewGuid(), Id, testDefinitionId, displayOrder);
            _tests.Add(programTest);
            newlyAdded.Add(programTest);
        }

        if (newlyAdded.Count > 0)
        {
            UpdatedAt = now;
        }

        return newlyAdded;
    }

    /// <summary>Dasturga anketa biriktiradi. Tizim dasturida taqiqlangan (BR-8 ruhida).</summary>
    public void AddTest(Guid testDefinitionId, int displayOrder, DateTimeOffset now)
    {
        GuardNotLocked();

        if (_tests.Any(t => t.TestDefinitionId == testDefinitionId))
        {
            throw new DomainException("PROGRAM_TEST_DUPLICATE", "Bu anketa allaqachon dasturga biriktirilgan.");
        }

        _tests.Add(ProgramTest.Create(Guid.NewGuid(), Id, testDefinitionId, displayOrder));
        UpdatedAt = now;
    }

    /// <summary>Dasturdan anketani olib tashlaydi. Tizim dasturida taqiqlangan (BR-8 ruhida).</summary>
    public void RemoveTest(Guid testDefinitionId, DateTimeOffset now)
    {
        GuardNotLocked();

        var removed = _tests.RemoveAll(t => t.TestDefinitionId == testDefinitionId) > 0;
        if (!removed)
        {
            return;
        }

        UpdatedAt = now;
    }

    /// <summary>
    /// Dastur ichidagi testlar tartibini to'liq qayta belgilaydi — `orderedTestDefinitionIds`
    /// mavjud tarkib bilan AYNAN bir xil to'plam bo'lishi shart (`PROGRAM_TEST_NOT_FOUND` aks holda).
    /// Tizim dasturida taqiqlangan (BR-8 ruhida).
    /// </summary>
    public void ReorderTests(IReadOnlyList<Guid> orderedTestDefinitionIds, DateTimeOffset now)
    {
        GuardNotLocked();
        ArgumentNullException.ThrowIfNull(orderedTestDefinitionIds);

        if (orderedTestDefinitionIds.Count != _tests.Count || orderedTestDefinitionIds.Distinct().Count() != _tests.Count)
        {
            throw new DomainException("PROGRAM_TEST_REORDER_MISMATCH", "Tartiblash ro'yxati dastur tarkibi bilan bir xil bo'lishi kerak.");
        }

        var byTestDefinitionId = _tests.ToDictionary(t => t.TestDefinitionId);

        for (var i = 0; i < orderedTestDefinitionIds.Count; i++)
        {
            if (!byTestDefinitionId.TryGetValue(orderedTestDefinitionIds[i], out var programTest))
            {
                throw new DomainException("PROGRAM_TEST_NOT_FOUND", "Ko'rsatilgan anketa ushbu dasturga biriktirilmagan.");
            }

            programTest.UpdateOrder(i + 1);
        }

        UpdatedAt = now;
    }

    /// <summary>
    /// `Draft ──▶ Published`: kamida bitta test biriktirilgan bo'lishi shart.
    ///
    /// **`IsActive` nima bo'ladi:** ATAYLAB `true` qilib ANIQ o'rnatiladi, ya'ni nashrdan
    /// keyin dastur `Active` holatida bo'ladi. Sabab: "nashr qilish" — adminning dasturni
    /// o'quvchilarga OCHISH qarori; uni nashr qilib, keyin alohida "faollashtirish" bosishni
    /// talab qilish ikkita maydonli eski chalkashlikni qaytaradi. Bu yerda konstruktordagi
    /// `IsActive = true` boshlang'ich qiymatiga TAYANMAYMIZ: `Draft` holatida `IsActive`
    /// ma'nosiz (`ProgramState.Draft` uni umuman o'qimaydi), shu sabab u qanday qolganidan
    /// qat'i nazar, nashr natijasi DOIM aniq — `Published + IsActive = true`.
    /// </summary>
    public void Publish(DateTimeOffset now)
    {
        if (Status != ProgramStatus.Draft)
        {
            throw new DomainException("PROGRAM_INVALID_TRANSITION", $"Dastur '{Status}' holatidan 'Published' ga o'ta olmaydi.");
        }

        if (_tests.Count == 0)
        {
            throw new DomainException("PROGRAM_NOT_PUBLISHABLE", "Kamida bitta test biriktirilmasa dasturni nashr qilib bo'lmaydi.");
        }

        Status = ProgramStatus.Published;
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>`Draft`/`Published` ──▶ `Archived`.</summary>
    public void Archive(DateTimeOffset now)
    {
        if (Status is not (ProgramStatus.Draft or ProgramStatus.Published))
        {
            throw new DomainException("PROGRAM_INVALID_TRANSITION", $"Dastur '{Status}' holatidan 'Archived' ga o'ta olmaydi.");
        }

        Status = ProgramStatus.Archived;
        IsActive = false;
        UpdatedAt = now;
    }

    public void SetVisibility(ProgramVisibility visibility, DateTimeOffset now)
    {
        Visibility = visibility;
        UpdatedAt = now;
    }

    /// <summary>
    /// `Paused ──▶ Active` (`Published` doirasida). FAQAT nashr qilingan dasturda ishlaydi:
    /// qoralamani ham, arxivlangan dasturni ham "faollashtirib" bo'lmaydi — bu holatlar
    /// mazmunan mos kelmaydi (qoralama hali nashr qilinmagan, arxiv esa yakuniy holat).
    ///
    /// Aynan shu qo'riqchining yo'qligi egasi ko'rgan xatoni tug'dirgan edi: arxivlangan
    /// dastur `IsActive = true` bo'lib qolib, ro'yxatda bir vaqtda "Arxiv" ham, "Faol" ham
    /// bo'lib ko'rinardi.
    /// </summary>
    public void Activate(DateTimeOffset now)
    {
        GuardPublishedForActivation(nameof(ProgramState.Active));

        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>
    /// `Active ──▶ Paused` (`Published` doirasida). `Activate` bilan bir xil qo'riqchi:
    /// nashr qilinmagan yoki arxivlangan dasturni "to'xtatib" bo'lmaydi.
    /// </summary>
    public void Deactivate(DateTimeOffset now)
    {
        GuardPublishedForActivation(nameof(ProgramState.Paused));

        IsActive = false;
        UpdatedAt = now;
    }

    public void UpdateDetails(string nameUz, string? descriptionUz, int displayOrder, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Dastur nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        NameUz = nameUz;
        DescriptionUz = descriptionUz;
        DisplayOrder = displayOrder;
        UpdatedAt = now;
    }

    /// <summary>
    /// **Ma'lumot tuzatmasi (2026-09-06), seed bosqichi uchun.** Arxivlangan dasturda
    /// `IsActive` DOIM `false` bo'lishi kerak — bu invariantni `Archive()` ta'minlaydi,
    /// lekin qo'riqchisiz `Activate()` mavjud bo'lgan davrda buzilgan qatorlar bazada
    /// qolib ketgan (egasining bazasidagi `PERSONALITY_PROFILE`: `status = 3`,
    /// `is_active = true`).
    ///
    /// IDEMPOTENT: qator allaqachon to'g'ri bo'lsa hech narsa o'zgarmaydi va `false`
    /// qaytadi. Bu — dasturni FAOLLASHTIRISH emas, aksincha: yakuniy `Archived` holatini
    /// ma'lumot darajasida ham haqiqiy qilish.
    /// </summary>
    public bool ReconcileArchivedInactive(DateTimeOffset now)
    {
        if (Status != ProgramStatus.Archived || !IsActive)
        {
            return false;
        }

        IsActive = false;
        UpdatedAt = now;
        return true;
    }

    private void GuardPublishedForActivation(string targetState)
    {
        if (Status != ProgramStatus.Published)
        {
            throw new DomainException("PROGRAM_INVALID_TRANSITION", $"Dastur '{State}' holatidan '{targetState}' ga o'ta olmaydi.");
        }
    }

    private void GuardNotLocked()
    {
        if (IsSystem)
        {
            throw new DomainException("SYSTEM_PROGRAM_LOCKED", "Tizim dasturining tarkibini o'zgartirib bo'lmaydi.");
        }
    }
}
