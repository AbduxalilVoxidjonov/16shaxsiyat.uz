using System.Text;

namespace StudentRoadMap.Domain.Schools;

/// <summary>
/// **Maktab kodi** (`School.EntryCode`) — o'quvchi `/kirish` sahifasida "Maktab uchun" yo'lidan
/// kiritadigan 8 belgili sir. Kod maktabni ANIQLAYDI va havola bilan bir xil huquq beradi:
/// `POST /api/public/schools/resolve-code` uni `{ slug, accessToken }` ga aylantiradi, keyin
/// brauzer mavjud `/t/{slug}?k=` oqimiga o'tadi (`docs/08` 3a-bo'lim).
///
/// **`School.AccessCode` bilan ADASHTIRILMASIN:** u — ixtiyoriy, admin qo'lda kiritadigan
/// 6 raqamli "sinf kodi" (`docs/02` FR-1.5), maktab havolasi orqali kirgan o'quvchidan
/// anketada qo'shimcha tekshiruv sifatida so'raladi; unikal emas va maktabni aniqlamaydi.
///
/// **Format:** 8 belgi, faqat <see cref="Alphabet"/> dan (katta lotin harflar + raqamlar,
/// chalkash `0 O 1 I L` chiqarib tashlangan — kod doskaga yoziladi, telefon orqali aytiladi).
/// Ko'rsatishda `XXXX-XXXX` (defis FAQAT vizual, bazada yo'q). Kiritishda kichik harf, defis
/// va bo'shliq qabul qilinadi — <see cref="Normalize"/> ularni yechadi.
///
/// **Generatsiya BU YERDA EMAS** — `Infrastructure.Security.EntryCodeGenerator`
/// (`RandomNumberGenerator`). Sabab: `AccessToken` bilan bir xil naqsh (`ITokenGenerator`) —
/// tasodifiylik yon ta'sir, domen esa deterministik va sinovda bashorat qilinadigan qoladi
/// (`CLAUDE.md` 2-qoida ruhi). Domen faqat FORMATni biladi va tekshiradi.
/// </summary>
public static class SchoolEntryCode
{
    /// <summary>Kod uzunligi (defissiz).</summary>
    public const int Length = 8;

    /// <summary>
    /// 31 belgi: `A–Z` dan `I`, `L`, `O` chiqarilgan (23 harf) + `2–9` (8 raqam). `0`/`1` yo'q —
    /// `O`/`I`/`L` bilan chalkashadi. Entropiya: 31^8 ≈ 8.5·10^11.
    /// </summary>
    public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <summary>
    /// Foydalanuvchi kiritgan matnni saqlash shakliga keltiradi: katta harf, defis/bo'shliq
    /// olib tashlanadi. Natija yaroqsiz bo'lsa (uzunlik ≠ 8 yoki alifbodan tashqari belgi)
    /// `null` — chaqiruvchi buni "kod topilmadi" deb ko'rsatadi (format xatosi bilan
    /// "bunday kod yo'q" ATAYLAB farqlanmaydi — enumeration'ga yo'l qo'ymaslik uchun).
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var builder = new StringBuilder(Length);
        foreach (var ch in input)
        {
            if (ch is '-' or ' ' or '\t' or '‑' or '–' or '—')
            {
                continue;
            }

            builder.Append(char.ToUpperInvariant(ch));

            if (builder.Length > Length)
            {
                return null;
            }
        }

        var normalized = builder.ToString();
        return IsValid(normalized) ? normalized : null;
    }

    /// <summary>Saqlash shakli to'g'rimi: aynan 8 belgi, hammasi <see cref="Alphabet"/> dan.</summary>
    public static bool IsValid(string? code)
    {
        if (code is null || code.Length != Length)
        {
            return false;
        }

        foreach (var ch in code)
        {
            if (Alphabet.IndexOf(ch, StringComparison.Ordinal) < 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>`ABCD2345` → `ABCD-2345`. Yaroqsiz qiymat o'zgarishsiz qaytariladi (ko'rsatish uchun zararsiz).</summary>
    public static string Format(string code) =>
        IsValid(code) ? $"{code[..4]}-{code[4..]}" : code;
}
