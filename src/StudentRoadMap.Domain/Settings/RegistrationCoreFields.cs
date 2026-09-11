namespace StudentRoadMap.Domain.Settings;

/// <summary>
/// Ro'yxatdan o'tish formasining SakKIz qattiq kodlangan asosiy maydoni — `RegistrationFormSettings`.
/// <see cref="FullName"/> alohida diqqatga loyiq: uning <see cref="RegistrationCoreField.Requirement"/>
/// HAR DOIM `Required` bo'lishi shart (ism kerak bo'lmasa `AssessmentProgram.RegistrationMode = None`
/// bor — ikkita mustaqil "ism shart emas" mexanizmi chalkashlik keltirib chiqarardi,
/// `RegistrationFields.cs` izohiga qarang). Yorlig'i/placeholder'i esa tahrirlanadi.
/// </summary>
public sealed record RegistrationCoreFields(
    RegistrationCoreField FullName,
    RegistrationCoreField BirthDate,
    RegistrationCoreField Gender,
    RegistrationCoreField Grade,
    RegistrationCoreField ClassLetter,
    RegistrationCoreField Phone,
    RegistrationCoreField ParentPhone,
    RegistrationCoreField Email)
{
    /// <summary>
    /// Custom maydon kodlari shu nomlar bilan to'qnashmasligi kerak (`RegistrationFormDefinition.Validate`).
    /// </summary>
    public static readonly IReadOnlyCollection<string> ReservedCodes =
    [
        "fullName", "birthDate", "gender", "grade", "classLetter", "phone", "parentPhone", "email",
    ];
}
