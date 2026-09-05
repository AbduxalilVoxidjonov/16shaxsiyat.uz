namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Sir bo'lmagan, konfiguratsiyadan olinadigan ilova sozlamalari (`docs/06-arxitektura.md`
/// 7-bo'lim). Sirlar (`Jwt:Key`, `Security:EncryptionKey` va h.k.) bu orqali berilmaydi —
/// ular alohida, maxsus servislar (`IEncryptionService` va h.k.) orqali o'qiladi.
/// </summary>
public interface IAppSettings
{
    /// <summary>O'quvchi sessiya tokenining amal qilish muddati, kunlarda (`docs/08` 4-bo'lim: 7 kun).</summary>
    int SessionLifetimeDays { get; }

    /// <summary>
    /// GLOBAL "natija ko'rsatilsinmi" bayrog'i (`App:ShowResultToStudent`). **P47dan buyon bu
    /// yagona qaror nuqtasi EMAS, balki avariya rubilnigi (kill-switch)** — haqiqiy qaror
    /// makon darajasida (`School.ShowResultToStudent`), ikkalasi `ShowResultPolicy` da
    /// **VA** (`&amp;&amp;`) bilan birlashtiriladi.
    /// <para>
    /// Standart qiymat — **`true`** (ilgari `false`). Sabab: makon bayrog'i maktab uchun
    /// standart `false` (natija psixolog orqali beriladi — avvalgi xatti-harakat SAQLANADI),
    /// ommaviy makon uchun esa `true`. Global bayroq yopiq qolsa ommaviy kabinet qutidan
    /// chiqishi bilan ishlamas edi. `App:ShowResultToStudent=false` — huquqiy talab yoki
    /// insident holatida BUTUN tizimni bir env o'zgaruvchisi bilan yopish uchun.
    /// </para>
    /// </summary>
    bool ShowResultToStudent { get; }

    /// <summary>
    /// Superadmin refresh tokenining amal qilish muddati, kunlarda (`docs/08` 2-bo'lim,
    /// `docs/06` 7-bo'lim: `Jwt:RefreshTokenDays`, standart 14). Sir emas — `Jwt:Key`dan farqli
    /// o'laroq bu shu interfeys orqali beriladi.
    /// </summary>
    int RefreshTokenDays { get; }

    /// <summary>
    /// O'quvchi sessiyani yakunlaganda AI tahlili AVTOMATIK navbatga qo'yiladimi
    /// (`Ai:AutoAnalyzeOnCompletion`, env: `Ai__AutoAnalyzeOnCompletion`). Standart qiymat —
    /// **`false`** (`docs/06` 8-bo'lim, 2026-09-03 loyiha EGASI qarori): har tahlil AI xarajati,
    /// shu sabab egasi qaysi o'quvchi tahlil qilinishini admin panelidagi "AI tahlil qilish"
    /// tugmasi (`POST /api/admin/assessments/{id}/rerun-analysis`) orqali O'ZI tanlaydi.
    /// <para>
    /// ⚠️ Bayroq FAQAT avtomatik oqimga (`CompleteSessionCommandHandler`) tegishli — qo'lda
    /// ishga tushirish (`RerunAnalysisCommandHandler`) undan MUSTAQIL, har doim ishlaydi.
    /// Bayroq YOQILGANDA navbatga qo'yish baribir `IPostCommitActions` orqali, tranzaksiya
    /// commit bo'lgandan KEYIN bajariladi (P18-R1/P18-R2 o'zgarishsiz).
    /// </para>
    /// </summary>
    bool AutoAnalyzeOnCompletion { get; }

    /// <summary>
    /// Ommaviy frontend bazaviy manzili — maktab havolasi shu asosda quriladi:
    /// `{PublicWebBaseUrl}/t/{slug}?k={accessToken}` (`docs/08-auth-va-xavfsizlik.md` 3-bo'lim,
    /// `P14`). `App:FrontendUrl` konfiguratsiyasi bilan bir xil manba (`Program.cs`da CORS uchun
    /// ham ishlatiladi) — standart `https://16shaxsiyat.uz`.
    /// </summary>
    string PublicWebBaseUrl { get; }
}
