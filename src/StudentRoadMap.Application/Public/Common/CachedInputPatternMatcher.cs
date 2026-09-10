using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// `ShortText`/`Phone` savollarining `InputPattern`ini ReDoS'dan himoyalangan holda tekshiradi
/// (`docs/18` §4.2). Har bir shablon `RegexOptions.NonBacktracking` bilan 100 ms timeout'da
/// kompilyatsiya qilinadi; `NonBacktracking` qo'llab-quvvatlamaydigan konstruksiya
/// (`RegexParseException`/`NotSupportedException`) bo'lsa — backtracking + xuddi shu timeout
/// bilan qayta urinadi, u ham muvaffaqiyatsiz bo'lsa shablon E'TIBORSIZ qoldiriladi (savol
/// bloklanmaydi — noto'g'ri sozlangan admin shabloni o'quvchini to'sib qo'ymasligi uchun).
///
/// Kompilyatsiya natijasi statik `ConcurrentDictionary`da keshlanadi — har so'rovda qayta
/// kompilyatsiya qilinmaydi. Hajm chegaralangan (`MaxCachedPatterns`): admin tomonidan
/// kiritiladigan aniq shablonlar soni tabiiy ravishda kichik, lekin nazariy poyga holatida
/// xotira cheksiz o'smasin uchun.
/// </summary>
public static class CachedInputPatternMatcher
{
    private const int MaxCachedPatterns = 500;

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly ConcurrentDictionary<string, Regex?> Cache = new(StringComparer.Ordinal);

    /// <summary>
    /// `pattern` `input`ga TO'LIQ mos keladimi (`^...$` semantikasi — `Regex.IsMatch` qisman
    /// moslikni ham qabul qiladi, shu sabab chaqiruvchi shablonni `^`/`$` bilan bergan bo'lishi
    /// kutiladi, `docs/18` standart shablonlari — `Phone` — shunday). Shablon kompilyatsiya
    /// qilinmasa (ikkala urinish ham muvaffaqiyatsiz) yoki timeout bo'lsa — `true` (bloklamaydi).
    /// </summary>
    public static bool IsMatch(string pattern, string input)
    {
        var regex = GetOrCompile(pattern);
        if (regex is null)
        {
            return true;
        }

        try
        {
            return regex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            // Kompilyatsiya vaqtida timeout berilgan bo'lsa ham, nazariy jihatdan `IsMatch`
            // o'zi ham timeout otishi mumkin (ReDoS himoyasining ikkinchi qatlami) — savolni
            // bloklamaslik ustuvor (`docs/18` §4.2: "shablonni e'tiborsiz qoldirish").
            return true;
        }
    }

    /// <summary>
    /// Shablon kompilyatsiya qilinadimi (`docs/18` §5 `INPUT_PATTERN_INVALID`) — admin katalogida
    /// savol saqlashda ishlatiladi, xuddi shu keshdan va qoidadan foydalanadi (ikkinchi mustaqil
    /// regex-mantiq paydo bo'lmasligi uchun, `docs/18` §5: "Ommaviy oqimdagi `CachedInputPatternMatcher`
    /// bilan bir xil qoidaga tayan").
    /// </summary>
    public static bool IsValidPattern(string pattern) => GetOrCompile(pattern) is not null;

    private static Regex? GetOrCompile(string pattern)
    {
        if (Cache.TryGetValue(pattern, out var cached))
        {
            return cached;
        }

        var compiled = Compile(pattern);

        if (Cache.Count < MaxCachedPatterns)
        {
            Cache.TryAdd(pattern, compiled);
        }

        return compiled;
    }

    private static Regex? Compile(string pattern)
    {
        try
        {
            return new Regex(pattern, RegexOptions.NonBacktracking, MatchTimeout);
        }
        catch (Exception ex) when (ex is NotSupportedException or RegexParseException)
        {
            try
            {
                return new Regex(pattern, RegexOptions.None, MatchTimeout);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
