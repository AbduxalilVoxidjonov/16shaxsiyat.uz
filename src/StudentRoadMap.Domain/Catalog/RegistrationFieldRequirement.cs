namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Bitta ro'yxatdan o'tish maydonining (`RegistrationFields`) holati — P52 kengaytmasi
/// (`docs/18-tarmoqlanuvchi-sorovnoma.md` §9.5, egasining qarori): har bir maydon HAR DASTURDA
/// alohida yashirilishi, ixtiyoriy yoki majburiy qilinishi mumkin.
/// </summary>
public enum RegistrationFieldRequirement
{
    /// <summary>Ekranda umuman ko'rsatilmaydi. Mijozdan qiymat kelsa ham e'tiborsiz qoldiriladi (saqlanmaydi).</summary>
    Hidden = 1,

    /// <summary>Ekranda ko'rsatiladi, lekin bo'sh qoldirish mumkin.</summary>
    Optional = 2,

    /// <summary>Ekranda ko'rsatiladi va bo'sh bo'lsa `400 VALIDATION_ERROR`.</summary>
    Required = 3,
}
