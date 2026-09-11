using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Settings;

/// <summary>
/// Ro'yxatdan o'tish formasining GLOBAL sozlamasi — bitta qator (singleton, <see cref="SingletonId"/>
/// bilan qulflangan), 2026-09-11 (egasining talabi, `docs/18` §9.6): superadmin "Sozlamalar"
/// sahifasidan maydon qo'shishi/o'chirishi, matnini tahrirlashi, majburiy/ixtiyoriy/yashirin
/// qilishi mumkin. `AssessmentProgram.RegistrationFields` (har dasturda alohida, 2026-09-11
/// ertalab qo'shilgan) O'RNIGA keladi — ikki joyda bir xil sozlama chalkashlik keltirib
/// chiqargani uchun (`RegistrationFormDefinition.cs` izohiga qarang).
/// </summary>
public sealed class RegistrationFormSettings : Entity
{
    /// <summary>Yagona qatorning qat'iy identifikatori — `docs/05` da shu bilan `UNIQUE`/tekshiruv.</summary>
    public static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-00005EFF1735");

    public RegistrationFormDefinition Definition { get; private set; } = null!;

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedByAdminUserId { get; private set; }

    /// <summary>EF Core uchun parametrsiz konstruktor.</summary>
    private RegistrationFormSettings()
    {
    }

    private RegistrationFormSettings(RegistrationFormDefinition definition, DateTimeOffset now, Guid? updatedByAdminUserId)
        : base(SingletonId)
    {
        Definition = definition;
        UpdatedAt = now;
        UpdatedByAdminUserId = updatedByAdminUserId;
    }

    /// <summary>Birinchi marta `PUT` qilinganda yaratiladi — `GetRegistrationFormSettingsQueryHandler` DB'da yozuv topmasa `RegistrationFormDefinition.Default`ni to'g'ridan-to'g'ri qaytaradi, yozuv YARATMAYDI.</summary>
    public static RegistrationFormSettings Create(RegistrationFormDefinition definition, DateTimeOffset now, Guid? updatedByAdminUserId) =>
        new(definition, now, updatedByAdminUserId);

    public void UpdateDefinition(RegistrationFormDefinition definition, DateTimeOffset now, Guid? updatedByAdminUserId)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
        UpdatedAt = now;
        UpdatedByAdminUserId = updatedByAdminUserId;
    }
}
