using System.Globalization;
using System.Text;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Export;

/// <summary>
/// `IPdfExporter` — `QuestPDF` orqali (`docs/06`da ko'rsatilgan kutubxona, `prompts/27` vazifa #2).
///
/// **Litsenziya (`prompts/27` MAXSUS DIQQAT #7 — ATAYLAB ESLATMA):** QuestPDF'ning bepul
/// "Community" litsenziyasi uchun `QuestPDF.Settings.License` MAJBURIY o'rnatilishi kerak —
/// aks holda birinchi hujjat generatsiyasida `QuestPDF.Infrastructure.RequiredSettingsException`
/// otiladi. Bu klassning STATIK konstruktorida (DI ro'yxatidan mustaqil, ilova qanday
/// composition root ishlatishidan qat'i nazar bir marta ishga tushadi) o'rnatiladi — shu yerda
/// shrift registratsiyasi bilan birga.
///
/// **Shrift (`prompts/27` vazifa #2, MAXSUS DIQQAT #7):** `DejaVu Sans` (Bitstream Vera
/// litsenziyasi — erkin joylashtirish uchun ochiq) `.ttf` sifatida `Export/Resources/`da
/// EMBEDDED RESURS qilib saqlangan (regular + bold) — o'zbek lotin alifbosi (standart lotin +
/// apostrof) va Docker/production konteynerida shrift o'rnatilmagan bo'lsa ham matn TO'G'RI
/// chiqishi uchun (host OS shriftlariga bog'liq bo'lmaydi).
/// </summary>
internal sealed class PdfExporter : IPdfExporter
{
    private const string FontFamilyName = "DejaVu Sans";
    private const string PlatformName = "Shaxsiyat"; // `CLAUDE.md` "Brend" bo'limi — foydalanuvchiga ko'rinadigan nom.

    private static readonly string[] Mbti16AxisOrder = ["EI", "SN", "TF", "JP"];
    private static readonly string[] Big5FactorOrder = ["O", "C", "E", "A", "N"];
    private static readonly string[] ActivityScaleOrder = ["MOT", "SELF", "SOCA", "ENG"];
    private static readonly string[] RiasecTypeOrder = ["R", "I", "A", "S", "E", "C"];

    private static readonly Color PrimaryColor = Color.FromHex("#1F3A5F");
    private static readonly Color MutedColor = Color.FromHex("#6B7280");
    private static readonly Color TextColor = Color.FromHex("#1F2937");
    private static readonly Color DividerColor = Color.FromHex("#D8DEE9");
    private static readonly Color CardBackground = Color.FromHex("#F5F7FA");
    private static readonly Color BarColor = Color.FromHex("#2E6F9E");
    private static readonly Color TrackColor = Color.FromHex("#E4E9F0");
    private static readonly Color WarningBackground = Color.FromHex("#FFF4E5");
    private static readonly Color WarningColor = Color.FromHex("#92400E");
    private static readonly Color NoteBackground = Color.FromHex("#EEF2F7");

    private static readonly TextStyle BaseStyle = TextStyle.Default.FontFamily([FontFamilyName]).FontColor(TextColor).FontSize(9.5f);
    private static readonly TextStyle TitleStyle = BaseStyle.FontSize(20).Bold().FontColor(PrimaryColor);
    private static readonly TextStyle StudentNameStyle = BaseStyle.FontSize(13).SemiBold();
    private static readonly TextStyle SectionHeadingStyle = BaseStyle.FontSize(13).Bold().FontColor(PrimaryColor);
    private static readonly TextStyle SubHeadingStyle = BaseStyle.FontSize(10.5f).Bold();
    private static readonly TextStyle LabelStyle = BaseStyle.FontSize(8.5f).FontColor(MutedColor);
    private static readonly TextStyle ValueStyle = BaseStyle.FontSize(10.5f).SemiBold();
    private static readonly TextStyle CardValueStyle = BaseStyle.FontSize(14).Bold().FontColor(PrimaryColor);
    private static readonly TextStyle BodyStyle = BaseStyle.FontSize(9.5f).LineHeight(1.35f);
    private static readonly TextStyle SmallMutedStyle = BaseStyle.FontSize(8).FontColor(MutedColor);
    private static readonly TextStyle WarningTextStyle = BaseStyle.FontSize(9.5f).FontColor(WarningColor);
    private static readonly TextStyle WarningHeadingStyle = BaseStyle.FontSize(10.5f).Bold().FontColor(WarningColor);
    private static readonly TextStyle BrandStyle = BaseStyle.FontSize(13).Bold().FontColor(PrimaryColor);

    static PdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        FontManager.RegisterFontFromEmbeddedResource("StudentRoadMap.Infrastructure.Export.Resources.DejaVuSans.ttf");
        FontManager.RegisterFontFromEmbeddedResource("StudentRoadMap.Infrastructure.Export.Resources.DejaVuSans-Bold.ttf");
    }

    public byte[] GenerateAssessmentReport(AssessmentReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32, Unit.Point);
                page.DefaultTextStyle(BaseStyle);
                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, AssessmentReportData data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(PlatformName).Style(BrandStyle);
                row.AutoItem().AlignRight().Text(
                    $"ID: {data.AssessmentId.ToString("N", CultureInfo.InvariantCulture)[..8]} · {data.GeneratedAt:yyyy-MM-dd}").Style(SmallMutedStyle);
            });
            column.Item().PaddingTop(4).Height(1, Unit.Point).Background(DividerColor);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"{PlatformName} — individual profil hisoboti").Style(SmallMutedStyle);
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(SmallMutedStyle);
                text.Span("Sahifa ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static void ComposeContent(IContainer container, AssessmentReportData data)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSummaryPage(c, data));

            var hasAnyResults = data.Results.Mbti16 is not null || data.Results.Big5 is not null
                || data.Results.Riasec is not null || data.Results.Activity is not null;

            // "Ma'lumot yo'q bo'lgan bo'limlar PDF'da bo'sh joy qoldirmasin" (`prompts/27`
            // cheklovi) — hech qanday test natijasi yo'q bo'lsa, "Diagrammalar" sahifasi
            // butunlay o'tkazib yuboriladi (bo'sh sahifa qoldirilmaydi).
            if (hasAnyResults)
            {
                column.Item().PageBreak();
                column.Item().Element(c => ComposeChartsPage(c, data));
            }

            column.Item().PageBreak();
            column.Item().Element(c => ComposeAiSection(c, data));

            column.Item().PageBreak();
            column.Item().Element(ComposeDisclaimerPage);
        });
    }

    // ---------------------------------------------------------------- 1-sahifa: sarlavha + umumiy ma'lumot

    private static void ComposeSummaryPage(IContainer container, AssessmentReportData data)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Individual profil hisoboti").Style(TitleStyle);
            column.Item().Text(data.StudentFullName).Style(StudentNameStyle);

            column.Item().Element(c => ComposeInfoGrid(c, data));

            column.Item().PaddingTop(4).Text("Yig'ma natijalar").Style(SectionHeadingStyle);
            column.Item().Element(c => ComposeSummaryCards(c, data));
        });
    }

    private static void ComposeInfoGrid(IContainer container, AssessmentReportData data)
    {
        container.Border(1, Unit.Point).BorderColor(DividerColor).Background(CardBackground).Padding(12).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Spacing(5);
                InfoItem(col, "Maktab", data.SchoolName);
                InfoItem(col, "Sinf", data.ClassLetter is null ? data.Grade.ToString(CultureInfo.InvariantCulture) : $"{data.Grade}-{data.ClassLetter}");
                InfoItem(col, "Yosh", $"{data.Age} yosh");
                InfoItem(col, "Jins", TranslateGender(data.Gender));
            });
            row.RelativeItem().Column(col =>
            {
                col.Spacing(5);
                InfoItem(col, "Boshlangan", data.StartedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                InfoItem(col, "Yakunlangan", data.CompletedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "—");
                InfoItem(col, "Ishonchlilik", BuildReliabilityText(data.ReliabilityFlag, data.ReliabilityScore));
            });
        });
    }

    private static void InfoItem(ColumnDescriptor column, string label, string value)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(90, Unit.Point).Text(label).Style(LabelStyle);
            row.RelativeItem().Text(value).Style(ValueStyle);
        });
    }

    private static void ComposeSummaryCards(IContainer container, AssessmentReportData data)
    {
        var cards = new List<(string Label, string Value)>();

        if (data.Results.Mbti16 is { } mbti && !string.IsNullOrEmpty(mbti.ResultCode))
        {
            cards.Add(("Shaxsiyat tipi", string.IsNullOrEmpty(mbti.TypeName) ? mbti.ResultCode : $"{mbti.ResultCode} — {mbti.TypeName}"));
        }

        if (data.Results.Big5?.MaturityIndex is { } maturityIndex)
        {
            cards.Add(("Yetuklik indeksi", $"{maturityIndex:0.#}% ({data.Results.Big5.MaturityLevel ?? "—"})"));
        }

        if (data.Results.Activity?.ActivityIndex is { } activityIndex)
        {
            cards.Add(("Aktivlik indeksi", $"{activityIndex:0.#}% ({data.Results.Activity.ActivityLevel ?? "—"})"));
        }

        if (data.Results.Riasec is { } riasec && !string.IsNullOrEmpty(riasec.ResultCode))
        {
            cards.Add(("Holland kodi", riasec.ResultCode));
        }

        if (cards.Count == 0)
        {
            container.Text("Natijalar hali mavjud emas.").Style(SmallMutedStyle);
            return;
        }

        container.Row(row =>
        {
            row.Spacing(8);
            foreach (var (label, value) in cards)
            {
                row.RelativeItem().Border(1, Unit.Point).BorderColor(DividerColor).Background(CardBackground).Padding(10).Column(col =>
                {
                    col.Item().Text(label).Style(LabelStyle);
                    col.Item().PaddingTop(3).Text(value).Style(CardValueStyle);
                });
            }
        });
    }

    // ---------------------------------------------------------------- 2-sahifa: diagrammalar

    private static void ComposeChartsPage(IContainer container, AssessmentReportData data)
    {
        container.Column(column =>
        {
            column.Spacing(14);
            column.Item().Text("Diagrammalar").Style(SectionHeadingStyle);

            if (data.Results.Mbti16 is { } mbti)
            {
                column.Item().Element(c => ComposeBarChartSection(
                    c,
                    "MBTI-16 o'lchamlari",
                    Mbti16AxisOrder
                        .Where(axis => mbti.Axes.ContainsKey(axis))
                        .Select(axis => ($"{axis} ({mbti.Axes[axis].Letter})", mbti.Axes[axis].Pct))));
            }

            if (data.Results.Big5 is { } big5)
            {
                column.Item().Element(c => ComposeBarChartSection(
                    c,
                    "Katta beshlik (Big Five)",
                    Big5FactorOrder.Where(f => big5.Factors.ContainsKey(f)).Select(f => (f, big5.Factors[f].Pct))));
            }

            if (data.Results.Riasec is { } riasec)
            {
                column.Item().Element(c => ComposeRiasecSection(c, riasec));
            }

            if (data.Results.Activity is { } activity)
            {
                column.Item().Element(c => ComposeBarChartSection(
                    c,
                    "Aktivlik shkalalari",
                    ActivityScaleOrder.Where(s => activity.Scales.ContainsKey(s)).Select(s => (s, activity.Scales[s]))));
            }
        });
    }

    private static void ComposeBarChartSection(IContainer container, string title, IEnumerable<(string Label, double Pct)> items)
    {
        container.Column(column =>
        {
            column.Spacing(3);
            column.Item().Text(title).Style(SubHeadingStyle);
            foreach (var (label, pct) in items)
            {
                Bar(column, label, pct);
            }
        });
    }

    /// <summary>RIASEC — bar (6 tip) + radar/hexagon SVG yonma-yon (`prompts/27` "bar va radar").</summary>
    private static void ComposeRiasecSection(IContainer container, AdminRiasecDto riasec)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text($"RIASEC (Holland kodi: {riasec.ResultCode})").Style(SubHeadingStyle);
            column.Item().Row(row =>
            {
                row.RelativeItem(3).Column(col =>
                {
                    col.Spacing(3);
                    foreach (var type in RiasecTypeOrder)
                    {
                        if (riasec.Types.TryGetValue(type, out var pct))
                        {
                            Bar(col, type, pct);
                        }
                    }
                });
                row.ConstantItem(150, Unit.Point).AlignMiddle().Svg(BuildRadarSvg(riasec.Types));
            });
        });
    }

    /// <summary>
    /// Proporsional oddiy chiziq diagramma — QuestPDF'ning `RelativeItem(weight)` xususiyatidan
    /// foydalanadi (pct/(100-pct) og'irlik nisbati = aniq foizli to'ldirish), alohida rasm/Canvas
    /// kerak emas — bu esa shrift/render muhitiga bog'liq bo'lmagan eng ishonchli yo'l.
    /// </summary>
    private static void Bar(ColumnDescriptor column, string label, double pctValue)
    {
        var clamped = (float)Math.Clamp(pctValue, 0, 100);
        column.Item().Row(row =>
        {
            row.ConstantItem(72, Unit.Point).AlignMiddle().Text(label).Style(LabelStyle);
            row.RelativeItem().Height(11, Unit.Point).Background(TrackColor).Row(barRow =>
            {
                if (clamped > 0)
                {
                    barRow.RelativeItem(clamped).Background(BarColor);
                }

                if (clamped < 100)
                {
                    barRow.RelativeItem(100 - clamped);
                }
            });
            row.ConstantItem(34, Unit.Point).AlignMiddle().AlignRight().Text($"{clamped:0}%").Style(SmallMutedStyle);
        });
    }

    /// <summary>
    /// RIASEC oltiburchak (hexagon) radar diagrammasi — xom SVG sifatida quriladi va
    /// `SvgExtensions.Svg` bilan chiziladi (`prompts/27`: "SkiaSharp/QuestPDF vositalari bilan
    /// chiziladi — bar va radar"; QuestPDF o'zining ichki Skia bog'lovchisi orqali SVG'ni
    /// to'g'ridan-to'g'ri render qiladi). Matn SVG ICHIGA solinmaydi (shrift kafolati faqat
    /// QuestPDF matn dvijogida ishlaydi) — belgilar tashqarida oddiy bar'lar orqali beriladi.
    /// </summary>
    private static string BuildRadarSvg(IReadOnlyDictionary<string, double> types)
    {
        const double size = 140;
        const double center = size / 2;
        const double maxRadius = size / 2 - 14;
        var axisCount = RiasecTypeOrder.Length;

        var valuePoints = new (double X, double Y)[axisCount];
        var gridPoints = new (double X, double Y)[axisCount];
        for (var i = 0; i < axisCount; i++)
        {
            var angle = -Math.PI / 2 + (i * (2 * Math.PI / axisCount));
            var value = types.TryGetValue(RiasecTypeOrder[i], out var pct) ? Math.Clamp(pct, 0, 100) : 0;
            var radius = maxRadius * (value / 100.0);
            valuePoints[i] = (center + (radius * Math.Cos(angle)), center + (radius * Math.Sin(angle)));
            gridPoints[i] = (center + (maxRadius * Math.Cos(angle)), center + (maxRadius * Math.Sin(angle)));
        }

        string ToPointsAttribute(IEnumerable<(double X, double Y)> points) =>
            string.Join(" ", points.Select(p => FormattableString.Invariant($"{p.X:0.##},{p.Y:0.##}")));

        var svg = new StringBuilder();
        svg.Append(FormattableString.Invariant(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{size}\" height=\"{size}\" viewBox=\"0 0 {size} {size}\">"));

        foreach (var point in gridPoints)
        {
            svg.Append(FormattableString.Invariant(
                $"<line x1=\"{center:0.##}\" y1=\"{center:0.##}\" x2=\"{point.X:0.##}\" y2=\"{point.Y:0.##}\" stroke=\"#D0D5DD\" stroke-width=\"1\" />"));
        }

        svg.Append($"<polygon points=\"{ToPointsAttribute(gridPoints)}\" fill=\"none\" stroke=\"#D0D5DD\" stroke-width=\"1.5\" />");
        svg.Append($"<polygon points=\"{ToPointsAttribute(valuePoints)}\" fill=\"#2E6F9E\" fill-opacity=\"0.28\" stroke=\"#2E6F9E\" stroke-width=\"2\" />");

        foreach (var point in valuePoints)
        {
            svg.Append(FormattableString.Invariant($"<circle cx=\"{point.X:0.##}\" cy=\"{point.Y:0.##}\" r=\"2.5\" fill=\"#2E6F9E\" />"));
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    // ---------------------------------------------------------------- 3+ sahifa: AI tahlili

    private static void ComposeAiSection(IContainer container, AssessmentReportData data)
    {
        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Text("AI tahlili").Style(SectionHeadingStyle);

            if (data.AiAnalysis is null)
            {
                // `prompts/27` MAXSUS DIQQAT #4: AI hali yo'q bo'lishi mumkin — bo'sh joy
                // qoldirilmaydi, tushunarli izoh chiqadi, hujjat YIQILMAYDI.
                column.Item().Background(NoteBackground).Padding(10).Text(
                    "AI tahlili hali tayyor emas. Ushbu bo'lim AI tahlil yakunlangach avtomatik to'ldiriladi.").Style(BodyStyle);
                return;
            }

            var ai = data.AiAnalysis;
            column.Item().Text(
                $"Provayder: {ai.Provider} · Model: {ai.Model} · Prompt versiyasi: {ai.PromptVersion} · {ai.CreatedAt:yyyy-MM-dd}").Style(SmallMutedStyle);

            AddParagraphIfPresent(column, "Umumiy xulosa", ai.Summary);
            AddParagraphIfPresent(column, "Shaxsiyat portreti", ai.PersonalityPortrait);
            AddBulletsIfPresent(column, "Kuchli tomonlar", ai.Strengths);
            AddBulletsIfPresent(column, "O'sish yo'nalishlari", ai.GrowthAreas);
            AddCareerSuggestionsIfPresent(column, ai.CareerSuggestions);
            AddBulletsIfPresent(column, "O'quvchiga tavsiyalar", ai.StudentRecommendations);
            AddParagraphIfPresent(column, "O'qituvchiga izoh", ai.TeacherNotes);
            AddParagraphIfPresent(column, "Ota-onaga izoh", ai.ParentNotes);
            AddAttentionFlagsIfPresent(column, ai.AttentionFlags);
        });
    }

    private static void AddParagraphIfPresent(ColumnDescriptor column, string heading, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        column.Item().PaddingTop(3).Text(heading).Style(SubHeadingStyle);
        column.Item().Text(text).Style(BodyStyle);
    }

    private static void AddBulletsIfPresent(ColumnDescriptor column, string heading, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(3).Text(heading).Style(SubHeadingStyle);
        foreach (var item in items)
        {
            column.Item().Text($"•  {item}").Style(BodyStyle);
        }
    }

    private static void AddCareerSuggestionsIfPresent(ColumnDescriptor column, IReadOnlyList<AdminCareerSuggestionDto> suggestions)
    {
        if (suggestions.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(3).Text("Kasb tavsiyalari").Style(SubHeadingStyle);
        foreach (var suggestion in suggestions)
        {
            column.Item().Text(suggestion.Field).Style(ValueStyle);
            if (!string.IsNullOrWhiteSpace(suggestion.Why))
            {
                column.Item().Text(suggestion.Why).Style(BodyStyle);
            }

            foreach (var step in suggestion.NextSteps)
            {
                column.Item().Text($"  →  {step}").Style(SmallMutedStyle);
            }
        }
    }

    private static void AddAttentionFlagsIfPresent(ColumnDescriptor column, IReadOnlyList<string> flags)
    {
        if (flags.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(6).Background(WarningBackground).Padding(8).Column(col =>
        {
            col.Item().Text("Diqqat talab qiladigan holatlar").Style(WarningHeadingStyle);
            foreach (var flag in flags)
            {
                col.Item().Text($"•  {flag}").Style(WarningTextStyle);
            }
        });
    }

    // ---------------------------------------------------------------- oxirgi sahifa: disclaimer

    private const string DisclaimerText =
        "Ushbu hisobot psixologik-pedagogik test natijalari va (mavjud bo'lsa) sun'iy intellekt " +
        "(AI) tomonidan tayyorlangan dastlabki tahlil asosida shakllantirilgan. Hisobot tibbiy " +
        "yoki psixologik TASHXIS emas — u o'quvchining qiziqishlari, shaxsiyat xususiyatlari va " +
        "aktivlik darajasi bo'yicha yo'naltiruvchi ma'lumot beradi. AI tomonidan tayyorlangan " +
        "bo'limlar bo'yicha yakuniy xulosa chiqarishdan oldin malakali mutaxassis (psixolog yoki " +
        "pedagog) ko'rigi tavsiya etiladi.";

    private static void ComposeDisclaimerPage(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Muhim eslatma").Style(SectionHeadingStyle);
            column.Item().Background(WarningBackground).Padding(12).Text(DisclaimerText).Style(WarningTextStyle);
        });
    }

    // ---------------------------------------------------------------- kichik yordamchilar

    private static string TranslateGender(string rawGender) => rawGender switch
    {
        "Male" => "Erkak",
        "Female" => "Ayol",
        _ => "Ko'rsatilmagan",
    };

    private static string TranslateReliabilityFlag(string rawFlag) => rawFlag switch
    {
        "Reliable" => "Ishonchli",
        "Questionable" => "Shubhali",
        "Unreliable" => "Ishonchsiz",
        _ => rawFlag,
    };

    private static string BuildReliabilityText(string? reliabilityFlag, double? reliabilityScore)
    {
        if (reliabilityFlag is null)
        {
            return "—";
        }

        var label = TranslateReliabilityFlag(reliabilityFlag);
        return reliabilityScore.HasValue ? $"{label} ({reliabilityScore.Value:0.#}%)" : label;
    }
}
