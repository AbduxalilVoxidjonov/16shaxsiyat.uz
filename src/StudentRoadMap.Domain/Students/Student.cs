using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Students;

/// <summary>
/// O'quvchi — agregat ildizi. Snapshot maydonlari admin ro'yxatida JOIN'siz tez ko'rsatish
/// uchun denormalizatsiya qilingan (`docs/04` 2.2-bo'lim, ADR-11).
/// </summary>
public sealed class Student : AggregateRoot
{
    public const int MinGrade = 1;
    public const int MaxGrade = 11;

    /// <summary>
    /// "Sinf yo'q" — maktabda o'qimaydigan ommaviy foydalanuvchi (talaba, kattalar) uchun
    /// sentinel qiymat. `Grade` ni nullable qilish 21 ta ishchi kod faylini (admin filtr,
    /// eksport, dashboard guruhlash) qayta yozishni talab qilardi; nol esa `1..11` oralig'iga
    /// hech qachon tushmaydi va `ck_students_grade` cheklovi `0..11` ga kengaytirildi.
    /// </summary>
    public const int NoGrade = 0;

    /// <summary>
    /// Ro'yxatdan o'tish uchun ruxsat etilgan yosh oralig'i. Ilgari `StartSessionCommandValidator`
    /// 6–20 ni talab qilardi — ya'ni kattalar umuman kira olmasdi. Ommaviy oqim ochilgach
    /// yuqori chegara 99 ga ko'tarildi (quyi chegara o'zgarmadi: 6 yoshdan kichik bola
    /// metodikalarning o'qish darajasiga mos kelmaydi). Validator (keyingi qatlam) AYNAN
    /// shu ikki konstantaga tayanadi — sehrli raqam takrorlanmasin.
    /// </summary>
    public const int MinAge = 6;

    public const int MaxAge = 99;

    public Guid SchoolId { get; private set; }

    /// <summary>
    /// Ommaviy foydalanuvchi akkaunti (`PublicUsers.PublicUser`) bilan bog'lanish — NULLABLE.
    /// Maktab havolasi oqimida har doim `null` (u yerda akkaunt tushunchasi yo'q va oqim
    /// O'ZGARMAYDI), ommaviy makonda esa har doim to'ldiriladi.
    /// </summary>
    public Guid? PublicUserId { get; private set; }

    /// <summary>
    /// Ro'yxatdan o'tishsiz dastur (`AssessmentProgram.RegistrationMode.None`, P52) orqali
    /// yaratilgan anonim yozuv — shaxs ma'lumoti YO'Q (`Student.CreateAnonymous`). Bunday
    /// yozuvda <see cref="BirthDate"/> va <see cref="Phone"/> DOIM `null`.
    /// </summary>
    public bool IsAnonymous { get; private set; }

    public string FullName { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    /// <summary>
    /// Anonim o'quvchida DOIM `null` (`IsAnonymous`, P52). Anonim BO'LMAGAN o'quvchida ham
    /// `null` bo'lishi mumkin — agar dastur `RegistrationFields.BirthDate`ni `Optional`/`Hidden`
    /// qilgan bo'lsa (P52 kengaytmasi, `docs/18` §9.5).
    /// </summary>
    public DateOnly? BirthDate { get; private set; }

    public Gender Gender { get; private set; }

    public int Grade { get; private set; }

    public string? ClassLetter { get; private set; }

    /// <summary>
    /// Anonim o'quvchida DOIM `null` (`IsAnonymous`, P52). Anonim BO'LMAGAN o'quvchida ham
    /// `null` bo'lishi mumkin — `RegistrationFields.Phone` `Optional`/`Hidden` bo'lsa (P52
    /// kengaytmasi, `docs/18` §9.5).
    /// </summary>
    public PhoneNumber? Phone { get; private set; }

    public PhoneNumber? ParentPhone { get; private set; }

    public string? Email { get; private set; }

    /// <summary>
    /// Rozilik berilgan vaqt. Topshiriqdagi `ConsentAcceptedAt` uchun YANGI ustun qo'shilmadi —
    /// bu maydon aynan o'sha ma'noni bildiradi (`docs/08` 5-bo'lim: "rozilik vaqti
    /// `consent_given_at` da qayd etiladi") va ikkita bir xil ma'noli ustun chalkashlik
    /// manbai bo'lardi.
    /// </summary>
    public DateTimeOffset ConsentGivenAt { get; private set; }

    /// <summary>
    /// Qabul qilingan rozilik matnining versiyasi (masalan `2026-09-v1`). Rozilik matni
    /// o'zgarganda kim qaysi tahrirga rozi bo'lganini isbotlash uchun — versiyasiz "rozilik
    /// bor" yozuvining huquqiy qiymati past. Eski yozuvlarda `null` (matn hali
    /// versiyalanmagan davr).
    /// </summary>
    public string? ConsentVersion { get; private set; }

    /// <summary>
    /// Voyaga yetmagan foydalanuvchi uchun ota-ona/vasiy roziligi olinganmi.
    ///
    /// Nima uchun `Assessment` da emas, `Student` da: rozilik SESSIYAGA emas, SHAXSGA
    /// tegishli — bir foydalanuvchi yillar davomida bir necha marta test topshiradi, rozilik
    /// esa bir marta beriladi (va `ConsentGivenAt`/`ConsentVersion` allaqachon shu yerda).
    /// Maktab oqimida `false` bo'lib qoladi — u yerda rozilik maktab bilan tuzilgan
    /// shartnoma orqali keladi (`docs/08` 5-bo'lim).
    /// </summary>
    public bool ParentalConsent { get; private set; }

    // --- Snapshot (StudentSnapshot, docs/04 2.2) ---
    public string? LastPersonalityType { get; private set; }

    public double? LastMaturityIndex { get; private set; }

    public double? LastActivityIndex { get; private set; }

    public ActivityLevel? LastActivityLevel { get; private set; }

    public string? LastHollandCode { get; private set; }

    public bool NeedsAttention { get; private set; }

    public DateTimeOffset? LastAssessmentAt { get; private set; }

    public int CompletedAssessmentCount { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private Student()
    {
    }

    private Student(
        Guid id,
        Guid schoolId,
        string fullName,
        DateOnly? birthDate,
        Gender gender,
        int grade,
        string? classLetter,
        PhoneNumber? phone,
        PhoneNumber? parentPhone,
        string? email,
        DateTimeOffset consentGivenAt,
        Guid? publicUserId,
        string? consentVersion,
        bool parentalConsent,
        bool isAnonymous,
        DateTimeOffset now)
        : base(id)
    {
        // P52 kengaytmasi (`RegistrationFields`, `docs/18` §9.5, egasining qarori): ilgari
        // (2026-09-11) bu yerda "anonim BO'LMAGAN o'quvchida tug'ilgan sana va telefon DOIM
        // to'ldirilgan" invarianti bor edi. Endi HAR MAYDON (`birthDate`/`phone` ham) dasturga
        // qarab `Optional`/`Hidden` bo'lishi mumkin — ya'ni anonim BO'LMAGAN (FISH bor)
        // o'quvchida ham ikkalasi `null` bo'lishi LEGITIM holat. Majburiylik endi Application
        // qatlamida (`StartSessionCommandHandler.ValidateRequiredIdentityFields`,
        // `AssessmentProgram.RegistrationFields`) tekshiriladi — domen bu yerda cheklamaydi.
        SchoolId = schoolId;
        PublicUserId = publicUserId;
        IsAnonymous = isAnonymous;
        FullName = fullName;
        NormalizedName = NameNormalizer.Normalize(fullName);
        BirthDate = birthDate;
        Gender = gender;
        Grade = grade;
        ClassLetter = classLetter;
        Phone = phone;
        ParentPhone = parentPhone;
        Email = email;
        ConsentGivenAt = consentGivenAt;
        ConsentVersion = consentVersion;
        ParentalConsent = parentalConsent;
        CompletedAssessmentCount = 0;
        NeedsAttention = false;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// <paramref name="birthDate"/>/<paramref name="phone"/> P52 kengaytmasi (`docs/18` §9.5)
    /// sabab NULLABLE — dastur bu maydonlarni `Optional`/`Hidden` qilgan bo'lishi mumkin.
    /// Majburiylik Application qatlamida tekshiriladi, bu yerda emas.
    /// </summary>
    public static Student Create(
        Guid id,
        Guid schoolId,
        string fullName,
        DateOnly? birthDate,
        Gender gender,
        int grade,
        PhoneNumber? phone,
        DateTimeOffset consentGivenAt,
        DateTimeOffset now,
        string? classLetter = null,
        PhoneNumber? parentPhone = null,
        string? email = null,
        Guid? publicUserId = null,
        string? consentVersion = null,
        bool parentalConsent = false)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("F.I.Sh. bo'sh bo'lishi mumkin emas.", nameof(fullName));
        }

        // `NoGrade` (0) — maktabda o'qimaydigan ommaviy foydalanuvchi; qolgan qiymatlar 1..11.
        if (grade != NoGrade && grade is < MinGrade or > MaxGrade)
        {
            throw new ArgumentOutOfRangeException(nameof(grade), $"Sinf {MinGrade}..{MaxGrade} oralig'ida yoki {NoGrade} (sinf yo'q) bo'lishi kerak.");
        }

        if (consentGivenAt == default)
        {
            throw new ArgumentException("Rozilik vaqti ko'rsatilmasa o'quvchi yaratilmaydi.", nameof(consentGivenAt));
        }

        return new Student(
            id,
            schoolId,
            fullName,
            birthDate,
            gender,
            grade,
            classLetter,
            phone,
            parentPhone,
            email,
            consentGivenAt,
            publicUserId,
            consentVersion,
            parentalConsent,
            isAnonymous: false,
            now);
    }

    /// <summary>
    /// P52 (`RegistrationMode.None` dastur, egasining 2026-09-11 qarori): registratsiya
    /// ekrani ko'rsatilmagan, shaxs ma'lumoti YO'Q. `FullName` — PII EMAS: "Anonim ishtirokchi"
    /// + o'quvchining o'z `id`sidan olingan qisqa (6 belgili) noyob qo'shimcha — admin
    /// ro'yxatida qatorlar bir-biridan ajralib turishi uchun (bir xil nom ostida bir nechta
    /// qator chalkashtirmasin). Qo'shimcha uchun alohida tasodifiylik manbai kerak emas: `id`
    /// (chaqiruvchi tomonidan `Guid.NewGuid()` bilan beriladi) allaqachon noyob.
    /// `Grade = NoGrade`, `Gender = Unspecified` — mavjud sentinel qiymatlar (yangisi
    /// o'ylab topilmadi).
    /// </summary>
    public static Student CreateAnonymous(Guid id, Guid schoolId, DateTimeOffset consentGivenAt, DateTimeOffset now)
    {
        if (consentGivenAt == default)
        {
            throw new ArgumentException("Rozilik vaqti ko'rsatilmasa o'quvchi yaratilmaydi.", nameof(consentGivenAt));
        }

        var suffix = id.ToString("N")[..6].ToUpperInvariant();
        var fullName = $"Anonim ishtirokchi #{suffix}";

        return new Student(
            id,
            schoolId,
            fullName,
            birthDate: null,
            Gender.Unspecified,
            NoGrade,
            classLetter: null,
            phone: null,
            parentPhone: null,
            email: null,
            consentGivenAt,
            publicUserId: null,
            consentVersion: null,
            parentalConsent: false,
            isAnonymous: true,
            now);
    }

    /// <summary>
    /// To'liq yoshni hisoblaydi (tug'ilgan kun o'tganini hisobga oladi). Vaqt PARAMETR bilan
    /// keladi — domen tizim soatiga murojaat qilmaydi (`CLAUDE.md` 2-qoida).
    /// </summary>
    public static int CalculateAge(DateOnly birthDate, DateOnly asOf)
    {
        var age = asOf.Year - birthDate.Year;
        if (asOf < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }

    /// <summary>Yosh <see cref="MinAge"/>..<see cref="MaxAge"/> oralig'idami — validator shu qoidaga tayanadi.</summary>
    public static bool IsAgeAllowed(DateOnly birthDate, DateOnly asOf)
    {
        var age = CalculateAge(birthDate, asOf);
        return age is >= MinAge and <= MaxAge;
    }

    /// <summary>
    /// Mavjud (maktab oqimida yaratilgan) o'quvchi yozuvini ommaviy akkauntga bog'laydi —
    /// "eski natijamni Telegram akkauntimga ulang" ssenariysi uchun. Allaqachon boshqa
    /// akkauntga bog'langan yozuv qayta bog'lanmaydi (ma'lumot o'g'irlashning oldini olish).
    /// </summary>
    public void LinkToPublicUser(Guid publicUserId, DateTimeOffset now)
    {
        if (publicUserId == Guid.Empty)
        {
            throw new ArgumentException("Ommaviy foydalanuvchi identifikatori bo'sh bo'lishi mumkin emas.", nameof(publicUserId));
        }

        if (PublicUserId is not null && PublicUserId != publicUserId)
        {
            throw new DomainException("STUDENT_ALREADY_LINKED", "Bu o'quvchi yozuvi boshqa akkauntga bog'langan.");
        }

        PublicUserId = publicUserId;
        UpdatedAt = now;
    }

    /// <summary>
    /// Ommaviy foydalanuvchi o'z anketasini tahrirlaydi (`POST /api/me/sessions` mavjud
    /// `Student` bilan chaqirilganda). Barcha qiymatlar TO'LIQ keladi — "kelmagan maydon
    /// bazadagidek qoladi" birlashtirish Application qatlamida (`request ?? mavjud`), domen
    /// qisman yangilashning noaniqligini (null = "o'zgarmasin"mi yoki "tozalansin"mi) ko'tarib
    /// yurmaydi. Qoidalar `Create` bilan bir xil: F.I.Sh. bo'sh emas, sinf `NoGrade` yoki
    /// `MinGrade..MaxGrade`. `NormalizedName` qayta hisoblanadi — admin qidiruvi yangi F.I.Sh.
    /// bilan ishlashi uchun. Rozilik maydonlari bu yerda TEGILMAYDI (`RecordConsent`).
    /// </summary>
    public void UpdateProfile(
        string fullName,
        DateOnly birthDate,
        Gender gender,
        int grade,
        PhoneNumber phone,
        string? email,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("F.I.Sh. bo'sh bo'lishi mumkin emas.", nameof(fullName));
        }

        if (grade != NoGrade && grade is < MinGrade or > MaxGrade)
        {
            throw new ArgumentOutOfRangeException(nameof(grade), $"Sinf {MinGrade}..{MaxGrade} oralig'ida yoki {NoGrade} (sinf yo'q) bo'lishi kerak.");
        }

        FullName = fullName;
        NormalizedName = NameNormalizer.Normalize(fullName);
        BirthDate = birthDate;
        Gender = gender;
        Grade = grade;
        Phone = phone;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        UpdatedAt = now;
    }

    /// <summary>Roziliknomani yangilaydi (matn versiyasi o'zgarganda qayta so'raladi).</summary>
    public void RecordConsent(DateTimeOffset consentGivenAt, string? consentVersion, bool parentalConsent, DateTimeOffset now)
    {
        if (consentGivenAt == default)
        {
            throw new ArgumentException("Rozilik vaqti ko'rsatilmasa rozilik yozilmaydi.", nameof(consentGivenAt));
        }

        ConsentGivenAt = consentGivenAt;
        ConsentVersion = consentVersion;
        ParentalConsent = parentalConsent;
        UpdatedAt = now;
    }

    /// <summary>
    /// Sessiya `Analyzed` holatiga o'tganda snapshot maydonlarini yangilaydi (`docs/04` 2.2, 3-bo'lim).
    /// </summary>
    public void UpdateSnapshot(
        string? lastPersonalityType,
        double? lastMaturityIndex,
        double? lastActivityIndex,
        ActivityLevel? lastActivityLevel,
        string? lastHollandCode,
        bool needsAttention,
        DateTimeOffset lastAssessmentAt,
        int completedAssessmentCount,
        DateTimeOffset now)
    {
        LastPersonalityType = lastPersonalityType;
        LastMaturityIndex = lastMaturityIndex;
        LastActivityIndex = lastActivityIndex;
        LastActivityLevel = lastActivityLevel;
        LastHollandCode = lastHollandCode;
        NeedsAttention = needsAttention;
        LastAssessmentAt = lastAssessmentAt;
        CompletedAssessmentCount = completedAssessmentCount;
        UpdatedAt = now;
    }

    /// <summary>
    /// Yumshoq o'chirish (`docs/05` DDL: `students.is_deleted` + `deleted_at`; `deleted_by`
    /// ustuni yo'q). "Ma'lumotimni o'chiring" so'rovi esa hard delete — bu Application/Infrastructure
    /// qatlamida bajariladi, bu yerda emas.
    /// </summary>
    public void MarkDeleted(DateTimeOffset now)
    {
        IsDeleted = true;
        DeletedAt = now;
        UpdatedAt = now;
    }
}
