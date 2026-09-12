using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Settings;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// GLOBAL ro'yxatdan o'tish formasi sozlamasini (`RegistrationFormSettings`) yechish — P52
/// 2-to'lqin (2026-09-12, egasining qarori, `docs/18` §9.6.2): `AssessmentProgram.RegistrationFields`
/// (§9.5, eskirgan) O'RNIGA yagona manba. Maktab oqimi (`StartSessionCommandHandler`) va
/// kirish ekrani (`GetSchoolInfoQueryHandler`) shu yerdan foydalanadi.
///
/// <para>
/// <b>Dastur darajasidagi ustunlik (hisoblanadi, saqlanmaydi):</b> dasturda shaxsiyat batareyasi
/// bo'lsa (`PersonalityBattery.Includes`) `birthDate`/`grade` GLOBAL sozlamada `Optional`/
/// `Hidden` bo'lsa ham natijada `Required`ga ko'tariladi — ball normalari yosh/sinfga tayanadi
/// (`RegistrationMode`/`AddTest` qulflari bilan bir xil ruh, `AssessmentProgram.cs`ga qarang).
/// `gender` bu qoidaga KIRMAYDI (hech qachon majburiy qilinmagan, mavjud xatti-harakat,
/// `RegistrationFields.SatisfiesPersonalityBatteryInvariant`dagi bilan bir xil).
/// </para>
///
/// <para>
/// <b>Telegram/kabinet oqimi bu ustunlikni ISHLATMAYDI</b> (`StartPublicSessionCommandHandler`/
/// `UpdateStudentProfileCommandHandler`) — atayin: u yerda profil so'ralishi dastur
/// tanlanishidan OLDIN sodir bo'ladi (bir marta so'rash naqshi), shu sabab dastur asosidagi
/// ustunlikni hisoblash uchun tartib butunlay o'zgartirilishi kerak bo'lardi — bu P52 2-to'lqin
/// doirasiga kirmaydi (PM'ga qaytariladi, hisobotga qarang).
/// </para>
/// </summary>
internal static class RegistrationFormResolver
{
    /// <summary>DB'da yozuv bo'lmasa (hali hech kim `PUT` qilmagan) `RegistrationFormDefinition.Default`.</summary>
    public static async Task<RegistrationFormDefinition> GetGlobalDefinitionAsync(
        IAppDbContext context, IAsyncQueryExecutor executor, CancellationToken cancellationToken)
    {
        var settings = await executor.FirstOrDefaultAsync(
            context.AsNoTracking(context.RegistrationFormSettings), cancellationToken).ConfigureAwait(false);

        return settings?.Definition ?? RegistrationFormDefinition.Default;
    }

    /// <summary>Dastur uchun MAVJUD testlar (`ProgramTestCatalog` — sessiya/kirish ekrani bilan bir xil manba) asosida ilmiy batareya bormi.</summary>
    public static async Task<bool> ProgramHasPersonalityBatteryAsync(
        IAppDbContext context, IAsyncQueryExecutor executor, Guid programId, CancellationToken cancellationToken)
    {
        var items = await ProgramTestCatalog.GetTestsAsync(context, executor, programId, cancellationToken).ConfigureAwait(false);
        return items.Any(i => PersonalityBattery.Includes(i.Kind, i.ScoringMode));
    }

    /// <summary>
    /// Dastur ustunligini qo'llagan holda yakuniy ta'rifni qaytaradi. `RegistrationFormDefinition.Create`
    /// ORQALI EMAS (u FullName qulfini qayta tekshiradi, lekin bu yerda kerak emas) — invariant
    /// allaqachon saqlangan qiymatda tekshirilgan, faqat ikkita maydon ko'tariladi (`with`).
    /// </summary>
    public static RegistrationFormDefinition ApplyProgramOverride(RegistrationFormDefinition definition, bool hasPersonalityBattery)
    {
        if (!hasPersonalityBattery)
        {
            return definition;
        }

        var core = definition.CoreFields;
        if (core.BirthDate.Requirement == RegistrationFieldRequirement.Required
            && core.Grade.Requirement == RegistrationFieldRequirement.Required)
        {
            return definition;
        }

        var newCore = core with
        {
            BirthDate = core.BirthDate.Requirement == RegistrationFieldRequirement.Required
                ? core.BirthDate
                : core.BirthDate with { Requirement = RegistrationFieldRequirement.Required },
            Grade = core.Grade.Requirement == RegistrationFieldRequirement.Required
                ? core.Grade
                : core.Grade with { Requirement = RegistrationFieldRequirement.Required },
        };

        return definition with { CoreFields = newCore };
    }

    /// <summary>`GetGlobalDefinitionAsync` + `ProgramHasPersonalityBatteryAsync` + `ApplyProgramOverride` — birgalikda, dastur tanlangan oqimlar uchun qulaylik.</summary>
    public static async Task<RegistrationFormDefinition> GetEffectiveDefinitionForProgramAsync(
        IAppDbContext context, IAsyncQueryExecutor executor, Guid programId, CancellationToken cancellationToken)
    {
        var global = await GetGlobalDefinitionAsync(context, executor, cancellationToken).ConfigureAwait(false);
        var hasBattery = await ProgramHasPersonalityBatteryAsync(context, executor, programId, cancellationToken).ConfigureAwait(false);

        return ApplyProgramOverride(global, hasBattery);
    }
}
