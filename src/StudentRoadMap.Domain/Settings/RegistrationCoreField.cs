using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Domain.Settings;

/// <summary>
/// Bitta "asosiy" (barcha dasturlarda mavjud bo'lgan) ro'yxatdan o'tish maydonining sozlamasi —
/// `RegistrationFormSettings` (2026-09-11, egasining talabi: sozlama GLOBAL, `Sozlamalar`
/// sahifasidan boshqariladi). Maydonning o'zi (masalan `birthDate`) qattiq kodlangan —
/// faqat holati (<see cref="Requirement"/>) va matni (<see cref="LabelUz"/>/<see cref="PlaceholderUz"/>)
/// hamda tartibi (<see cref="Order"/>) tahrirlanadi.
/// </summary>
public sealed record RegistrationCoreField(
    RegistrationFieldRequirement Requirement,
    string LabelUz,
    string? PlaceholderUz,
    int Order);
