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

    /// <summary>
    /// P52 (2026-09-11 qaror) — `RegistrationMode.cs` izohiga qarang. Qat'iy invariant:
    /// dasturda shaxsiyat batareyasi bo'lsa bu maydon DOIM `Full`.
    /// </summary>
    public RegistrationMode RegistrationMode { get; private set; }

    /// <summary>
    /// P52 kengaytmasi (`RegistrationFields.cs` izohiga qarang, `docs/18` §9.5): har bir
    /// ro'yxatdan o'tish maydonining (F.I.Sh. bundan mustasno) holati. `null` — "standart
    /// qiymatlar ishlatilsin" (<see cref="ResolveRegistrationFields"/>), bazada `NULL` (jsonb).
    /// `RegistrationMode = None` bo'lganda bu maydon UMUMAN ishlatilmaydi (registratsiya
    /// ekrani ko'rsatilmaydi) — saqlansa ham zarari yo'q, validatsiyada e'tiborga olinmaydi.
    /// </summary>
    public RegistrationFields? RegistrationFields { get; private set; }

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

    /// <summary>
    /// **2026-09-23 egasi qarori** (`docs/18` §9.7, `docs/06` ADR): admin UI'dan "dastur"
    /// tushunchasi olib tashlandi — biriktirish (ommaviy / maktablar) endi TEST ichida qilinadi.
    /// Dastur jadvali ICHKI biriktirish qatlami bo'lib qoladi: har bir `TestDefinition` uchun
    /// ko'pi bilan BITTA "test dasturi" (1:1, tarkibida faqat shu test) bo'ladi, uni tizim
    /// avtomatik yaratadi va boshqaradi. Bu ustun o'sha testni ko'rsatadi (`NULL` — eski/tizim
    /// dasturi, admin UI'da ko'rinmaydi, sessiya tarixi uchun saqlanadi). Unikal —
    /// `ux_assessment_programs_owner_test` (qisman, `NOT NULL` qatorlar uchun).
    /// </summary>
    public Guid? OwnerTestDefinitionId { get; private set; }

    /// <summary>Test dasturi (<see cref="OwnerTestDefinitionId"/> bor) — tarkibi, nomi va holati testdan boshqariladi.</summary>
    public bool IsTestProgram => OwnerTestDefinitionId is not null;

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
        RegistrationMode registrationMode,
        RegistrationFields? registrationFields,
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
        RegistrationMode = registrationMode;
        RegistrationFields = registrationFields;
        Status = ProgramStatus.Draft;
        IsActive = true;
        IsSystem = isSystem;
        CreatedByAdminUserId = createdByAdminUserId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Yangi (bo'sh, `Draft`) dastur — hali test biriktirilmagan, shu sabab
    /// <paramref name="registrationMode"/> va <paramref name="registrationFields"/> bu bosqichda
    /// hech qanday batareya invariantini buza olmaydi (tekshiruv `SetRegistrationMode`/
    /// `SetRegistrationFields`/`Publish`da, test biriktirilgach ishlaydi).
    /// </summary>
    /// <param name="registrationFields">
    /// P52 kengaytmasi: `null` — standart qiymatlar (<see cref="RegistrationFields.Default"/>).
    /// </param>
    public static AssessmentProgram Create(
        Guid id,
        string code,
        string nameUz,
        DateTimeOffset now,
        int displayOrder = 1,
        ProgramKind kind = ProgramKind.Custom,
        ProgramVisibility visibility = ProgramVisibility.Assigned,
        RegistrationMode registrationMode = RegistrationMode.Full,
        RegistrationFields? registrationFields = null,
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

        return new AssessmentProgram(
            id, code, nameUz, descriptionUz, displayOrder, kind, visibility, registrationMode, registrationFields,
            isSystem: false, createdByAdminUserId, now);
    }

    /// <summary>
    /// Test dasturi (2026-09-23) — AYNAN bitta testli, <see cref="OwnerTestDefinitionId"/> shu
    /// testga bog'langan. Boshlang'ich holat: `Draft`, `Assigned` (hech qayerda ko'rinmaydi),
    /// `RegistrationMode.Full`. Nashr holati keyin <see cref="SyncStateWithTest"/> bilan testga
    /// ergashtiriladi; nom/tavsif/tartib — <see cref="SyncDetailsFromTest"/>.
    /// </summary>
    public static AssessmentProgram CreateForTest(
        Guid id,
        Guid testDefinitionId,
        string code,
        string nameUz,
        string? descriptionUz,
        int displayOrder,
        DateTimeOffset now,
        Guid? createdByAdminUserId = null)
    {
        if (testDefinitionId == Guid.Empty)
        {
            throw new ArgumentException("Test identifikatori bo'sh bo'lishi mumkin emas.", nameof(testDefinitionId));
        }

        var program = Create(
            id, code, nameUz, now, displayOrder, ProgramKind.Custom, ProgramVisibility.Assigned,
            RegistrationMode.Full, registrationFields: null, descriptionUz, createdByAdminUserId);

        program.OwnerTestDefinitionId = testDefinitionId;
        program._tests.Add(ProgramTest.Create(Guid.NewGuid(), id, testDefinitionId, 1));

        return program;
    }

    /// <summary>
    /// Test dasturining kodi/nomi/tavsifi/tartibini testdan ko'chiradi (test nomi o'zgarganda
    /// ommaviy oqimda — landing, kabinet — ham yangi nom ko'rinsin). Faqat test dasturida.
    /// O'zgarish bo'lsa `true`.
    /// </summary>
    public bool SyncDetailsFromTest(string code, string nameUz, string? descriptionUz, int displayOrder, DateTimeOffset now)
    {
        GuardTestProgram();

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Dastur kodi bo'sh bo'lishi mumkin emas.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(nameUz))
        {
            throw new ArgumentException("Dastur nomi bo'sh bo'lishi mumkin emas.", nameof(nameUz));
        }

        if (Code == code && NameUz == nameUz && DescriptionUz == descriptionUz && DisplayOrder == displayOrder)
        {
            return false;
        }

        Code = code;
        NameUz = nameUz;
        DescriptionUz = descriptionUz;
        DisplayOrder = displayOrder;
        UpdatedAt = now;
        return true;
    }

    /// <summary>
    /// Test dasturining nashr holatini testning holatiga ergashtiradi — dastur o'quvchiga
    /// FAQAT test o'zi ochiq bo'lganda (`Published` + `IsActive`) ko'rinadi. Aks holda
    /// ommaviy landing'da testsiz (bo'sh) dastur chiqib qolardi (`ProgramTestCatalog` nashr
    /// qilinmagan testni tushirib qoldiradi, `ProgramAvailability` esa faqat dasturga qaraydi).
    ///
    /// | Test | Dastur |
    /// |---|---|
    /// | `Draft` | `Draft` o'zgarmaydi; nashr qilingan bo'lsa — `Paused` |
    /// | `Published` + faol | `Active` (`Publish` / `Restore`+`Activate` / `Activate`) |
    /// | `Published` + nofaol | `Paused` |
    /// | `Archived` | `Archived` |
    ///
    /// Faqat mavjud domen o'tishlaridan foydalanadi — batareya invarianti (`Publish`) ham
    /// avtomatik tekshiriladi. O'zgarish bo'lsa `true`.
    /// </summary>
    public bool SyncStateWithTest(TestDefinitionStatus testStatus, bool testIsActive, bool hasPersonalityBattery, DateTimeOffset now)
    {
        GuardTestProgram();

        var before = State;

        switch (testStatus)
        {
            case TestDefinitionStatus.Archived:
                if (Status != ProgramStatus.Archived)
                {
                    Archive(now);
                }

                break;

            case TestDefinitionStatus.Published:
                if (Status == ProgramStatus.Draft)
                {
                    Publish(now, hasPersonalityBattery);
                }
                else if (Status == ProgramStatus.Archived)
                {
                    Restore(now);
                }

                if (testIsActive && !IsActive)
                {
                    Activate(now);
                }
                else if (!testIsActive && IsActive)
                {
                    Deactivate(now);
                }

                break;

            default:
                // `Draft` test: dastur hali nashr qilinmagan bo'lsa shunday qoladi; nashr
                // qilingan bo'lsa orqaga `Draft`ga o'tish yo'q — to'xtatiladi (`Paused`).
                if (Status == ProgramStatus.Published && IsActive)
                {
                    Deactivate(now);
                }

                break;
        }

        return before != State;
    }

    /// <summary>
    /// Test dasturini "ommaviy" (`Public` — barcha maktab va ommaviy kabinet) yoki
    /// "biriktirilgan" (`Assigned` — faqat `school_programs`) qiladi.
    /// </summary>
    public bool SetPublic(bool isPublic, DateTimeOffset now)
    {
        var target = isPublic ? ProgramVisibility.Public : ProgramVisibility.Assigned;
        if (Visibility == target)
        {
            return false;
        }

        SetVisibility(target, now);
        return true;
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

        // Tizim dasturi HAR DOIM ilmiy batareyani o'z ichiga oladi — `RegistrationMode` bu
        // yerda parametr sifatida ochilmaydi, DOIM `Full` (invariant: batareya bor dastur
        // registratsiyasiz bo'lolmaydi, `RegistrationMode.cs` izohi). `RegistrationFields` ham
        // `null` (standart) — standart to'plamda `BirthDate`/`Grade` allaqachon `Required`,
        // ya'ni batareya invariantini avtomatik qanoatlantiradi.
        var program = new AssessmentProgram(
            id, code, nameUz, descriptionUz, displayOrder, ProgramKind.System, ProgramVisibility.Public,
            RegistrationMode.Full, registrationFields: null, isSystem: true, createdByAdminUserId: null, now);

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

    /// <summary>
    /// Dasturga anketa biriktiradi. Tizim dasturida taqiqlangan (BR-8 ruhida).
    ///
    /// <paramref name="isPersonalityBatteryTest"/> — biriktirilayotgan ANKETANING o'zi ilmiy
    /// shaxsiyat batareyasiga kirimi (`Domain.Catalog.PersonalityBattery.Includes`, chaqiruvchi
    /// — `AddProgramTestCommandHandler` — `TestDefinition.Kind`/`ScoringMode`dan hisoblab beradi:
    /// domen agregatining o'zi `TestDefinition`ga to'g'ridan-to'g'ri murojaat qila olmaydi).
    /// **P52 xato tuzatmasi (2026-09-11, kod ko'rigi):** ilgari `AddTest` bu haqda hech narsa
    /// bilmas edi — batareyasiz `Custom` dastur nashr qilinib, `RegistrationMode`/`RegistrationFields`
    /// keyin bo'shashtirilib (guard o'tadi, chunki o'sha payt batareya YO'Q), so'ng shu metod orqali
    /// MBTI16/BIG5/RIASEC/ACTIVITY biriktirilsa ikkala nazorat nuqtasi (`SetRegistrationMode`/
    /// `SetRegistrationFields`, `Publish`) CHETLAB O'TILARDI — natija `Age: 0`/`Grade: 0` bilan
    /// jimgina buzilardi. Endi bu — UCHINCHI nazorat nuqtasi: batareya anketasi biriktirilayotgan
    /// PAYTNING O'ZIDA joriy `RegistrationMode`/`RegistrationFields` tekshiriladi.
    /// </summary>
    public void AddTest(Guid testDefinitionId, int displayOrder, bool isPersonalityBatteryTest, DateTimeOffset now)
    {
        GuardNotLocked();

        if (_tests.Any(t => t.TestDefinitionId == testDefinitionId))
        {
            throw new DomainException("PROGRAM_TEST_DUPLICATE", "Bu anketa allaqachon dasturga biriktirilgan.");
        }

        if (isPersonalityBatteryTest)
        {
            GuardRegistrationModeAllowsBatteryAttachment();
            GuardRegistrationFieldsAllowBatteryAttachment();
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
    /// <summary>
    /// <paramref name="hasPersonalityBattery"/> — chaqiruvchi (`PublishProgramCommandHandler`)
    /// biriktirilgan testlarni (`TestDefinition.Kind`/`ScoringMode`) yuklab,
    /// `Domain.Catalog.PersonalityBattery.ContainedIn` bilan hisoblab beradi: domenning o'zi
    /// `TestDefinition` agregatlariga to'g'ridan-to'g'ri murojaat qila olmaydi (faqat
    /// `ProgramTest.TestDefinitionId` saqlaydi), shu sabab tayyor bayroq PARAMETR sifatida
    /// keladi — `RegistrationMode.cs` invarianti IKKINCHI nazorat nuqtasi (birinchisi —
    /// <see cref="SetRegistrationMode"/>).
    /// </summary>
    public void Publish(DateTimeOffset now, bool hasPersonalityBattery)
    {
        if (Status != ProgramStatus.Draft)
        {
            throw new DomainException("PROGRAM_INVALID_TRANSITION", $"Dastur '{Status}' holatidan 'Published' ga o'ta olmaydi.");
        }

        if (_tests.Count == 0)
        {
            throw new DomainException("PROGRAM_NOT_PUBLISHABLE", "Kamida bitta test biriktirilmasa dasturni nashr qilib bo'lmaydi.");
        }

        GuardRegistrationModeAllowsBattery(hasPersonalityBattery);
        GuardRegistrationFieldsAllowBattery(ResolveRegistrationFields(), hasPersonalityBattery);

        Status = ProgramStatus.Published;
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>
    /// Ro'yxatdan o'tish rejimini o'zgartiradi — BIRINCHI nazorat nuqtasi (ikkinchisi —
    /// <see cref="Publish"/>). <paramref name="hasPersonalityBattery"/> chaqiruvchi tomonidan
    /// hisoblanadi (<see cref="Publish"/> izohiga qarang).
    /// </summary>
    public void SetRegistrationMode(RegistrationMode registrationMode, bool hasPersonalityBattery, DateTimeOffset now)
    {
        if (registrationMode == RegistrationMode.None && hasPersonalityBattery)
        {
            throw new DomainException(
                "REGISTRATION_REQUIRED_FOR_BATTERY",
                "Shaxsiyat batareyasi bo'lgan dasturda ro'yxatdan o'tish o'chirilishi mumkin emas.");
        }

        RegistrationMode = registrationMode;
        UpdatedAt = now;
    }

    /// <summary>
    /// Bazadagi `null` (standart) holatini haqiqiy qiymatlarga yechadi — <see cref="RegistrationFields"/>
    /// o'rnatilmagan bo'lsa <see cref="RegistrationFields.Default"/> qaytadi.
    /// </summary>
    public RegistrationFields ResolveRegistrationFields() => RegistrationFields ?? RegistrationFields.Default;

    /// <summary>
    /// Ro'yxatdan o'tish maydonlari sozlamasini o'zgartiradi — BIRINCHI nazorat nuqtasi
    /// (ikkinchisi — <see cref="Publish"/>), `SetRegistrationMode` bilan bir xil naqsh.
    /// <paramref name="registrationFields"/> `null` — "standart qiymatlarga qaytarish"
    /// (<see cref="RegistrationFields.Default"/>).
    /// </summary>
    public void SetRegistrationFields(RegistrationFields? registrationFields, bool hasPersonalityBattery, DateTimeOffset now)
    {
        var effective = registrationFields ?? RegistrationFields.Default;
        GuardRegistrationFieldsAllowBattery(effective, hasPersonalityBattery);

        RegistrationFields = registrationFields;
        UpdatedAt = now;
    }

    private static void GuardRegistrationFieldsAllowBattery(RegistrationFields fields, bool hasPersonalityBattery)
    {
        if (hasPersonalityBattery && !fields.SatisfiesPersonalityBatteryInvariant())
        {
            throw new DomainException(
                "REGISTRATION_FIELD_REQUIRED_FOR_BATTERY",
                "Shaxsiyat batareyasi bo'lgan dasturda tug'ilgan sana va sinf maydonlari majburiy bo'lishi shart.");
        }
    }

    private void GuardRegistrationModeAllowsBattery(bool hasPersonalityBattery)
    {
        if (RegistrationMode == RegistrationMode.None && hasPersonalityBattery)
        {
            throw new DomainException(
                "REGISTRATION_REQUIRED_FOR_BATTERY",
                "Shaxsiyat batareyasi bo'lgan dasturni ro'yxatdan o'tishsiz nashr qilib bo'lmaydi.");
        }
    }

    /// <summary>
    /// `AddTest`ning UCHINCHI nazorat nuqtasi (`GuardRegistrationModeAllowsBattery`/
    /// `GuardRegistrationFieldsAllowBattery`dan farqli, bu yerda `hasPersonalityBattery` doim
    /// `true` — chaqiruvchi allaqachon "biriktirilayotgan anketa batareyaga kiradi" deb bilgan).
    /// Xato xabari aniq amal ko'rsatadi — admin nima qilishi kerakligini bilsin.
    /// </summary>
    private void GuardRegistrationModeAllowsBatteryAttachment()
    {
        if (RegistrationMode == RegistrationMode.None)
        {
            throw new DomainException(
                "REGISTRATION_REQUIRED_FOR_BATTERY",
                "Bu anketa ilmiy shaxsiyat batareyasiga kiradi. Avval dastur sozlamasida " +
                "ro'yxatdan o'tishni yoqing (\"Ro'yxatdan o'tish rejimi\" — \"To'liq\"), so'ng bu anketani biriktiring.");
        }
    }

    /// <summary>`GuardRegistrationModeAllowsBatteryAttachment` bilan bir xil naqsh, maydonlar uchun.</summary>
    private void GuardRegistrationFieldsAllowBatteryAttachment()
    {
        if (!ResolveRegistrationFields().SatisfiesPersonalityBatteryInvariant())
        {
            throw new DomainException(
                "REGISTRATION_FIELD_REQUIRED_FOR_BATTERY",
                "Bu anketa ilmiy shaxsiyat batareyasiga kiradi. Avval dastur sozlamasida " +
                "tug'ilgan sana va sinfni \"Majburiy\" qiling, so'ng bu anketani biriktiring.");
        }
    }

    /// <summary>
    /// `Draft`/`Published` ──▶ `Archived`. Faqat `Status`/`IsActive` o'zgaradi: dasturning
    /// tarkibi (`Tests`) ham, maktab biriktirishlari (`school_programs`, alohida agregat —
    /// `SchoolProgram`) ham SAQLANIB QOLADI (tekshirildi 2026-09-06:
    /// `ArchiveProgramCommandHandler` `SchoolPrograms`ga tegmaydi; `SchoolProgramConfiguration`
    /// dagi `Cascade` faqat dastur QATORI o'chirilganda ishlaydi, arxivlashda emas).
    /// Shu sabab arxiv — "o'quvchiga ko'rinmaydi" degani, "aloqalar uzildi" degani emas;
    /// <see cref="Restore"/> nega `Paused` ga qaytarishini ham aynan bu belgilaydi.
    /// </summary>
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

    /// <summary>
    /// `Archived` ──▶ `Paused` (`Published + IsActive = false`) — arxivdan tiklash (egasining
    /// 2026-09-06 so'rovi: asosiy `PERSONALITY_PROFILE` dasturi arxivda qolib ketgan, uni
    /// faqat nusxa olib "tiklash" mumkin edi).
    ///
    /// **Nega `Paused`, `Active` EMAS:** `Archive()` maktab biriktirishlarini
    /// (`school_programs`) O'CHIRMAYDI, saqlab qoladi (yuqoridagi izoh). Bir bosishda `Active`
    /// ga tiklash dasturni o'sha maktablar uchun DARHOL jonli qilib qo'yardi — admin buni
    /// kutmagan bo'lishi mumkin (dastur arxivga tarkibi eskirgani uchun tushgan bo'lsa-chi?).
    /// Tiklash va faollashtirish — IKKI alohida qaror: avval `Restore()` (dastur yana
    /// boshqariladigan holatga qaytadi, lekin o'quvchi ko'rmaydi), keyin admin tarkib va
    /// biriktirishlarni ko'rib chiqib ANIQ `Activate()` bosadi. `Publish()`dagi "nashr =
    /// ochish qarori" mantiqi bu yerga ko'chmaydi: nashr — yangi dasturni ochish, tiklash —
    /// eski dasturni qaytarish, undagi mavjud aloqalar bilan.
    ///
    /// Tarkib (`Tests`) arxivda saqlangani uchun `Published` ga qaytishda kamida bitta test
    /// bo'lishi (`PROGRAM_NOT_PUBLISHABLE` sharti) qayta TEKSHIRILMAYDI: `Draft` dan
    /// arxivlangan bo'sh dastur tiklansa `Paused` bo'ladi va bo'sh `Published` dastur bo'lib
    /// qoladi — bu holat allaqachon mumkin (`RemoveTest` nashrdan keyin ham ishlaydi) va
    /// ommaviy oqim uni `ProgramsWithoutTests` deb to'g'ri ko'rsatadi.
    /// </summary>
    public void Restore(DateTimeOffset now)
    {
        if (Status != ProgramStatus.Archived)
        {
            throw new DomainException("PROGRAM_INVALID_TRANSITION", $"Dastur '{State}' holatidan 'Paused' ga o'ta olmaydi.");
        }

        Status = ProgramStatus.Published;
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
    /// mazmunan mos kelmaydi (qoralama hali nashr qilinmagan, arxivlangan dastur esa avval
    /// <see cref="Restore"/> orqali `Paused` ga qaytarilishi kerak).
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
    /// qaytadi. Bu — dasturni FAOLLASHTIRISH ham, TIKLASH ham emas (tiklash — aniq admin
    /// amali, <see cref="Restore"/>), aksincha: `Archived` holatini ma'lumot darajasida ham
    /// haqiqiy qilish.
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

        if (IsTestProgram)
        {
            // Test dasturi AYNAN bitta testli (1:1) — tarkibi testning o'zidan kelib chiqadi.
            throw new DomainException("TEST_PROGRAM_LOCKED", "Test dasturining tarkibi o'zgartirilmaydi — u faqat o'z testidan iborat.");
        }
    }

    private void GuardTestProgram()
    {
        if (!IsTestProgram)
        {
            throw new DomainException("TEST_PROGRAM_REQUIRED", "Bu amal faqat test dasturi uchun.");
        }
    }
}
