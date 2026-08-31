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

    public Guid SchoolId { get; private set; }

    public string FullName { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public DateOnly BirthDate { get; private set; }

    public Gender Gender { get; private set; }

    public int Grade { get; private set; }

    public string? ClassLetter { get; private set; }

    public PhoneNumber Phone { get; private set; } = null!;

    public PhoneNumber? ParentPhone { get; private set; }

    public string? Email { get; private set; }

    public DateTimeOffset ConsentGivenAt { get; private set; }

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
        DateOnly birthDate,
        Gender gender,
        int grade,
        string? classLetter,
        PhoneNumber phone,
        PhoneNumber? parentPhone,
        string? email,
        DateTimeOffset consentGivenAt,
        DateTimeOffset now)
        : base(id)
    {
        SchoolId = schoolId;
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
        CompletedAssessmentCount = 0;
        NeedsAttention = false;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Student Create(
        Guid id,
        Guid schoolId,
        string fullName,
        DateOnly birthDate,
        Gender gender,
        int grade,
        PhoneNumber phone,
        DateTimeOffset consentGivenAt,
        DateTimeOffset now,
        string? classLetter = null,
        PhoneNumber? parentPhone = null,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("F.I.Sh. bo'sh bo'lishi mumkin emas.", nameof(fullName));
        }

        if (grade is < MinGrade or > MaxGrade)
        {
            throw new ArgumentOutOfRangeException(nameof(grade), $"Sinf {MinGrade}..{MaxGrade} oralig'ida bo'lishi kerak.");
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
            now);
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
