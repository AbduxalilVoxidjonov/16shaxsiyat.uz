using System.Diagnostics;
using FluentAssertions;
using StudentRoadMap.Application.Public.Common;

namespace StudentRoadMap.Application.Tests.Public.Common;

/// <summary>
/// `CachedInputPatternMatcher` — `docs/18` §4.2 ReDoS himoyasi. QA topilmasi (P52 yakuniy
/// qabul): bu klass ILGARI hech qanday testda qamrab olinmagan edi (`CLAUDE.md` 10-qoida —
/// "test yozilmagan biznes mantiq tugallanmagan hisoblanadi"), garchi u ommaviy so'rovnomaning
/// eng ehtimolli ReDoS yuzasi bo'lsa ham. Bu testlar ikki qatlamli himoyani ham qulflaydi:
/// (1) `NonBacktracking` klassik katastrofik-orqaga-qaytish naqshlarini (`^(a+)+$`) DFA bilan
/// zudlik bilan hal qiladi; (2) `NonBacktracking` qo'llab-quvvatlamaydigan konstruksiyalar
/// (masalan orqaga havola — `\1`) backtracking'ga tushadi, u esa 100ms timeout bilan
/// cheklangan — muvaffaqiyatsiz bo'lsa ham savol BLOKLANMAYDI (`IsMatch` → `true`).
/// </summary>
public sealed class CachedInputPatternMatcherTests
{
    [Fact]
    public void IsMatch_OddiyShablon_TogriIshlaydi()
    {
        CachedInputPatternMatcher.IsMatch("^\\+?998[0-9]{9}$", "+998901234567").Should().BeTrue();
        CachedInputPatternMatcher.IsMatch("^\\+?998[0-9]{9}$", "12345").Should().BeFalse();
    }

    [Fact]
    public void IsValidPattern_KompilyatsiyaQilinmaydiganShablon_FalseQaytaradi()
    {
        // Muvozanatsiz qavs — na `NonBacktracking`, na oddiy `Regex` kompilyatsiya qila oladi.
        CachedInputPatternMatcher.IsValidPattern("(unbalanced[").Should().BeFalse();
    }

    [Fact]
    public void IsValidPattern_OddiyShablon_TrueQaytaradi()
    {
        CachedInputPatternMatcher.IsValidPattern("^[a-z]+$").Should().BeTrue();
    }

    /// <summary>
    /// Klassik katastrofik-orqaga-qaytish naqshi (`^(a+)+$`) — `RegexOptions.NonBacktracking`
    /// bilan DFA orqali hisoblanadi, shu sabab uzun "yomon" kirish bilan ham DARHOL qaytadi
    /// (eksponensial portlash YO'Q). Bu — himoyaning BIRINCHI qatlami.
    /// </summary>
    [Fact]
    public void IsMatch_KatastrofikOrqagaQaytishNaqshi_NonBacktrackingBilanTezDarhalQaytadi()
    {
        var pattern = "^(a+)+$";
        var maliciousInput = new string('a', 40) + "!"; // mos kelmaydi — klassik ReDoS triggeri

        var stopwatch = Stopwatch.StartNew();
        var result = CachedInputPatternMatcher.IsMatch(pattern, maliciousInput);
        stopwatch.Stop();

        result.Should().BeFalse();
        // Chegara keng (5s, CI jitter'ga bardoshli) — DFA algoritmi baribir eksponensial
        // EMAS, shu sabab bu chegara amalda millisekundlarda bajariladi, faqat qattiq
        // yuklangan mashinada sun'iy yiqilishning oldini oladi.
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5_000, "NonBacktracking DFA — eksponensial portlash bo'lmasligi kerak");
    }

    /// <summary>
    /// `NonBacktracking` qo'llab-quvvatlamaydigan konstruksiya (orqaga havola, `\1`) —
    /// kompilyatsiya oddiy (backtracking) `Regex`ga tushadi. Yomon kirish bilan bu SEKIN
    /// ishlaydi, lekin 100ms timeout uni to'xtatadi VA savol BLOKLANMAYDI (`true` qaytadi) —
    /// himoyaning IKKINCHI qatlami (`docs/18` §4.2: "shablonni e'tiborsiz qoldirish").
    /// </summary>
    [Fact]
    public void IsMatch_OrqagaHavolaBilanKatastrofikNaqsh_TimeoutBilanBloklamayQaytadi()
    {
        var pattern = "^(a+)+\\1$"; // orqaga havola — NonBacktracking rad etadi, backtracking'ga tushadi
        var maliciousInput = new string('a', 32) + "!";

        var stopwatch = Stopwatch.StartNew();
        var result = CachedInputPatternMatcher.IsMatch(pattern, maliciousInput);
        stopwatch.Stop();

        // Savol bloklanmasin — muvaffaqiyatsiz/timeout bo'lgan shablon HAR DOIM `true`.
        result.Should().BeTrue();
        // Timeout ~100ms bilan cheklangan. Chegara ATAYLAB keng (10s) — .NET'ning
        // `Regex.MatchTimeout` tekshiruvi RASMAN "eng yaxshi urinish" (hujjatlarda: davriy
        // tekshiriladi, aniq kafolat YO'Q) va CI/parallel test yuklamasida bir necha soniya
        // kechikishi mumkin (QA kuzatuvi: shu mashinada muvaqqat yuklama ostida ba'zan soniyalar
        // ketdi). Himoya BUTUNLAY o'chirilsa (masalan `MatchTimeout` olib tashlansa) shu naqsh
        // ~30s da tabiiy tugaydi (mutatsiya sinovi bilan tasdiqlangan) — 10s chegara ikkalasini
        // ISHONCHLI farqlaydi, lekin CI jitter'idan yiqilmaydi.
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10_000, "100ms timeout ReDoS'ni to'xtatishi kerak");
    }

    [Fact]
    public void IsMatch_KompilyatsiyaQilinmaydiganShablon_HarDoimTrueQaytaradi()
    {
        // Shablon o'zi yaroqsiz bo'lsa ham (masalan admin xato kiritgan) — o'quvchi bloklanmaydi.
        CachedInputPatternMatcher.IsMatch("(unbalanced[", "har qanday matn").Should().BeTrue();
    }

    /// <summary>
    /// Bir xil shablon ikkinchi chaqiriqda kesh orqali qayta ishlatiladi — natija barqaror
    /// (funksional jihatdan kuzatilmaydi, lekin muhim regressiya: keshlash natijani
    /// o'zgartirmasligi kerak).
    /// </summary>
    [Fact]
    public void IsMatch_IkkinchiChaqiriq_KeshdanKeyinHamAynanBirXilNatija()
    {
        var pattern = "^[0-9]{3}$";

        CachedInputPatternMatcher.IsMatch(pattern, "123").Should().BeTrue();
        CachedInputPatternMatcher.IsMatch(pattern, "123").Should().BeTrue();
        CachedInputPatternMatcher.IsMatch(pattern, "abc").Should().BeFalse();
    }
}
