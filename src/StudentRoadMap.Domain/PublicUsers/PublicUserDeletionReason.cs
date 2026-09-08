namespace StudentRoadMap.Domain.PublicUsers;

/// <summary>
/// "Ma'lumotimni o'chiring" so'ralganda foydalanuvchi tanlagan sabab (`DELETE /api/me`,
/// egasining 2026-09-08 qarori: o'chirishdan oldin sabab so'raladi va u superadminga
/// ko'rinadigan holda saqlanadi). Qiymatlar `smallint` sifatida saqlanadi — raqamlar
/// `docs/05-database-schema.md` 3-bo'limi bilan bir xil, o'zgarmas.
/// </summary>
public enum PublicUserDeletionReason
{
    /// <summary>Endi kerak emas.</summary>
    NoLongerNeeded = 1,

    /// <summary>Natijalar foydali bo'lmadi.</summary>
    NotUseful = 2,

    /// <summary>Ma'lumotlarim saqlanishini xohlamayman.</summary>
    PrivacyConcern = 3,

    /// <summary>Xato bilan ro'yxatdan o'tganman.</summary>
    CreatedByMistake = 4,

    /// <summary>Boshqa sabab — erkin matnli izoh (`PublicUser.DeletionComment`) MAJBURIY.</summary>
    Other = 5,
}
