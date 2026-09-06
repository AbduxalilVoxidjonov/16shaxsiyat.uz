using System.Linq.Expressions;

namespace StudentRoadMap.Domain.Catalog;

/// <summary>
/// Dasturning TASHQARIGA (admin API va UI'ga) ko'rinadigan YAGONA holati — `Status`
/// (`ProgramStatus`) va `IsActive` juftligidan HOSILA (2026-09-06 qarori, egasining
/// topshirig'i).
///
/// **Muammo:** ilgari ro'yxatda ikkita mustaqil ustun bor edi va bitta dastur bir vaqtda
/// "Arxiv" ham, "Faol" ham bo'lib ko'rinardi — chunki `Activate()` holatni umuman
/// tekshirmasdi va arxivlangan dasturni `IsActive = true` qilib qo'yish mumkin edi
/// (egasining bazasida AYNAN shu qator bor edi: `status = 3 AND is_active = true`).
///
/// **Yechim:** ikkala maydon bazada QOLADI (`Application/Public` va `SchoolLinkHealth`
/// mezoni ularga tayanadi), lekin tashqariga faqat shu bitta enum beriladi. Ikki maydonli
/// ziddiyat endi mumkin emas: `Archive()` `IsActive = false` qiladi, `Publish()`
/// `IsActive = true` qiladi, `Activate()`/`Deactivate()` esa faqat `Published` holatida
/// ishlaydi.
/// </summary>
public enum ProgramState
{
    /// <summary>`Status == Draft` — hali nashr qilinmagan (`IsActive` bu holatda ma'nosiz).</summary>
    Draft = 1,

    /// <summary>`Status == Published && IsActive` — o'quvchi shu dasturni tanlay oladi.</summary>
    Active = 2,

    /// <summary>`Status == Published && !IsActive` — nashr qilingan, lekin vaqtincha to'xtatilgan.</summary>
    Paused = 3,

    /// <summary>
    /// `Status == Archived` — o'quvchiga ko'rinmaydi (`IsActive` e'tiborga olinmaydi). Yagona
    /// chiqish yo'li — `AssessmentProgram.Restore()` ──▶ `Paused` (2026-09-06: arxiv endi
    /// yakuniy EMAS, lekin tiklash to'g'ridan-to'g'ri `Active` ga OLIB CHIQMAYDI).
    /// </summary>
    Archived = 4,
}

/// <summary>
/// Hosila holatning YAGONA hisoblanadigan joyi. Boshqa hech bir qatlam (`Application`,
/// `Api`, frontend) `Status`/`IsActive` juftligini qaytadan talqin QILMAYDI — aks holda
/// mezon ikkiga bo'linib, yana ziddiyat paydo bo'ladi (`SchoolAssignmentPanel`ning
/// 2026-09-03 dagi saboqi: "panel yolg'on aytmasligi uchun mezon bitta bo'lishi shart").
/// </summary>
public static class ProgramStateRules
{
    /// <summary>
    /// (`Status`, `IsActive`) ──▶ `ProgramState`. Har qanday juftlik uchun aniqlangan,
    /// shu jumladan bazada qolib ketishi mumkin bo'lgan eski ziddiyatli qator uchun ham
    /// (`Archived + IsActive` ──▶ `Archived`): `Status` ustuvor — arxivdagi dastur `IsActive`
    /// bayrog'idan qat'i nazar o'quvchiga ko'rinmaydi.
    /// </summary>
    public static ProgramState Resolve(ProgramStatus status, bool isActive) => status switch
    {
        ProgramStatus.Draft => ProgramState.Draft,
        ProgramStatus.Published => isActive ? ProgramState.Active : ProgramState.Paused,
        ProgramStatus.Archived => ProgramState.Archived,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Noma'lum dastur nashr holati."),
    };

    /// <summary>
    /// `ProgramState` ──▶ DB darajasida bajariladigan predikat (ro'yxat filtri uchun).
    /// <see cref="Resolve"/> ning teskarisi: xotirada filtrlash TAQIQLANGAN (`docs/06` §8),
    /// shu sabab hisoblanuvchi xossani `Where` ichida ishlatib bo'lmaydi va shart ikkinchi
    /// marta, ifoda daraxti shaklida yoziladi. Ikkalasining mos kelishi test bilan
    /// qulflangan (`ProgramStateRulesTests`).
    /// </summary>
    public static Expression<Func<AssessmentProgram, bool>> Filter(ProgramState state) => state switch
    {
        ProgramState.Draft => p => p.Status == ProgramStatus.Draft,
        ProgramState.Active => p => p.Status == ProgramStatus.Published && p.IsActive,
        ProgramState.Paused => p => p.Status == ProgramStatus.Published && !p.IsActive,
        ProgramState.Archived => p => p.Status == ProgramStatus.Archived,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Noma'lum dastur holati."),
    };

    /// <summary>
    /// Query parametridan (`?state=Paused`) o'qish — registr farqlamaydi. Raqamli qiymat
    /// (`?state=3`) ATAYLAB rad etiladi: API shartnomasida holat NOM bilan uzatiladi
    /// (`docs/07` 4-bo'lim), `Enum.TryParse` esa raqamni ham qabul qilib yuboradi va
    /// mavjud bo'lmagan qiymatni ham enum'ga aylantirib qo'yishi mumkin.
    /// </summary>
    public static bool TryParse(string? value, out ProgramState state)
    {
        state = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        return !int.TryParse(trimmed, out _)
            && Enum.TryParse(trimmed, ignoreCase: true, out state)
            && Enum.IsDefined(state);
    }
}
