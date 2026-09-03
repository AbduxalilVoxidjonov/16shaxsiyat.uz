using System.Globalization;
using System.Text;

namespace StudentRoadMap.Infrastructure.Excel;

/// <summary>
/// Varaq nomlari va ustun sarlavhalari — YOZUVCHI va O'QUVCHI uchun YAGONA manba. Ikkalasi
/// alohida ro'yxat ishlatsa, eksport bilan import vaqt o'tib jimgina uzilib ketardi; aylanma
/// test (`CatalogExcelRoundTripTests`) buni ushlaydi, lekin bitta manba uni umuman
/// yuzaga keltirmaydi.
///
/// <para>
/// Matnlar o'zbekcha — faylni superadmin ochadi (`CLAUDE.md` 1-qoida).
/// </para>
/// </summary>
internal static class CatalogExcelSchema
{
    public const string TestSheet = "Anketa";
    public const string ScalesSheet = "Shkalalar";
    public const string BandsSheet = "Oraliqlar";
    public const string QuestionsSheet = "Savollar";
    public const string InstructionsSheet = "Ko'rsatma";

    // --- `Anketa` ---
    public const string TestCode = "Kod";
    public const string TestName = "Nomi";
    public const string TestDescription = "Tavsifi";
    public const string TestMinutes = "Taxminiy daqiqa";
    public const string TestPageSize = "Sahifa hajmi";
    public const string TestScoringMode = "Ballash rejimi";

    // --- `Shkalalar` ---
    public const string ScaleCode = "Shkala kodi";
    public const string ScaleName = "Nomi";
    public const string ScaleDescription = "Tavsifi";

    // --- `Oraliqlar` ---
    public const string BandFrom = "Dan";
    public const string BandTo = "Gacha";
    public const string BandLabel = "Yorliq";

    // --- `Savollar` ---
    public const string QuestionCode = "Savol kodi";
    public const string QuestionOrder = "Tartib";
    public const string QuestionText = "Matn";
    public const string QuestionScale = "Shkala";
    public const string QuestionDirection = "Yo'nalish";
    public const string QuestionWeight = "Og'irlik";
    public const string QuestionRequired = "Majburiy";

    /// <summary>
    /// IXTIYORIY ustun (`Likert5` yoki `Likert7`, `docs/03` §6.1). Egasining ustun ro'yxatida
    /// yo'q, shu sabab OXIRIDA turadi va bo'sh/yo'q bo'lsa `Likert5` deb o'qiladi — ya'ni
    /// ro'yxatdagi yettita ustun bilan yozilgan fayl ham ishlaydi. Ustun kerak, chunki usiz
    /// `Likert7` anketa eksport→import aylanmasida JIMGINA `Likert5`ga aylanib, ballash
    /// formulasini buzardi.
    /// </summary>
    public const string QuestionType = "Javob turi";

    public static readonly string[] TestColumns =
        [TestCode, TestName, TestDescription, TestMinutes, TestPageSize, TestScoringMode];

    public static readonly string[] ScaleColumns =
        [ScaleCode, ScaleName, ScaleDescription];

    public static readonly string[] BandColumns =
        [ScaleCode, BandFrom, BandTo, BandLabel];

    public static readonly string[] QuestionColumns =
        [QuestionCode, QuestionOrder, QuestionText, QuestionScale, QuestionDirection, QuestionWeight, QuestionRequired, QuestionType];

    public const string YesLabel = "Ha";
    public const string NoLabel = "Yo'q";

    /// <summary>
    /// Sarlavhani solishtirish uchun normallashtiradi: kichik harf, chekka bo'shliqlarsiz,
    /// ichki bo'shliqlar bittaga, apostrof variantlari (`’ ‘ ʻ ʼ ` ´`) oddiy `'` ga.
    /// Excel avtomatik tuzatishi `Yo'nalish` ni `Yo’nalish` ga aylantirib qo'yishi mumkin —
    /// shundan keyin ham ustun topilishi kerak.
    /// </summary>
    public static string NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasSpace = false;

        foreach (var ch in value.Trim())
        {
            var normalized = ch switch
            {
                '’' or '‘' or 'ʻ' or 'ʼ' or '`' or '´' => '\'',
                _ => ch,
            };

            if (char.IsWhiteSpace(normalized))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                }

                previousWasSpace = true;
                continue;
            }

            previousWasSpace = false;
            builder.Append(char.ToLower(normalized, CultureInfo.InvariantCulture));
        }

        return builder.ToString().Trim();
    }
}
