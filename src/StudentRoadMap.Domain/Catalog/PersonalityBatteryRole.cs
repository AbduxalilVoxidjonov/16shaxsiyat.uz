namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Anketaning shaxsiyat batareyasi ICHIDAGI roli — "bu natija NIMANI tashiydi?" degan savolning
/// yagona domen javobi (`PersonalityBattery.RoleOf`). Batareyaning O'ZI bormi degan savolga
/// <see cref="PersonalityBattery.Includes(TestKind, TestScoringMode)"/> javob beradi; bu enum
/// esa batareya ichidagi TO'RT xil natijani bir-biridan ajratadi.
///
/// <para>
/// <b>Nega kerak.</b> `Includes` "batareyami?" degan HA/YO'Q savolga javob beradi, lekin
/// `GetStudentResultQueryHandler` (shaxsiyat tipi va Holland kodi) hamda
/// `CompleteSessionCommandHandler` (`MaturityIndex` = BIG5 + ACTIVITY, `StudentSnapshot`)
/// bundan ko'proq narsani bilishi kerak: sessiyadagi bir nechta batareya natijasidan QAYSI BIRI
/// tip, qaysi biri kasb qiziqishi. Ilgari bu savolga metodika KODI (`TestResult.TestCode ==
/// "MBTI16"`) bilan javob berilardi — aynan `PersonalityBattery` bartaraf etgan xatoning o'zi
/// (`Custom` dastur boshqa kodli metodika ishlatsa yoki kod versiyalansa, jimgina noto'g'ri
/// natija; hech qanday xato ko'rinmaydi).
/// </para>
///
/// <para>
/// <b>Roli qayerdan aniqlanadi.</b> Natijaning MA'NOSINI uni hisoblagan ALGORITM belgilaydi,
/// anketa nomi/kodi emas. Domen bu algoritmni allaqachon `TestDefinition.ScoringStrategyCode`
/// bilan nomlaydi (`docs/04` §2.7: `MBTI16`|`BIG5`|`RIASEC`|`ACTIVITY`|`SUM`, `docs/03` §8:
/// `IScoringStrategy.StrategyCode`) — ya'ni YANGI sehrli satr kiritilmaydi, mavjud strategiya
/// registri qayta ishlatiladi. Kod (`Code`) — superadmin tahrirlaydigan yorliq; strategiya kodi
/// esa formulaning o'zi: `PERS-BAT-1` kodli anketa `MBTI16` strategiyasi bilan ballansa,
/// undan CHIQADIGAN narsa baribir shaxsiyat tipi.
/// </para>
/// </summary>
public enum PersonalityBatteryRole
{
    /// <summary>Batareyaga kirmaydi yoki roli tanilmagan (masalan `SUM` — superadmin anketasi).</summary>
    None = 0,

    /// <summary>Shaxsiyat tipi (`MBTI16` strategiyasi): `ResultCode` → `TypeCatalog` kaliti (`docs/03` §2.3).</summary>
    PersonalityType = 1,

    /// <summary>Shaxsiyat omillari (`BIG5` strategiyasi): `MaturityIndex` shu natijaga yoziladi (`docs/03` §3.3).</summary>
    Traits = 2,

    /// <summary>Kasb qiziqishlari (`RIASEC` strategiyasi): `ResultCode` → Holland kodi, `CareerMap` kaliti (`docs/03` §4.3).</summary>
    CareerInterest = 3,

    /// <summary>Aktivlik va motivatsiya (`ACTIVITY` strategiyasi): `ActivityIndex`, daraja va `NeedsAttention` manbai (`docs/03` §5).</summary>
    Activity = 4,
}
