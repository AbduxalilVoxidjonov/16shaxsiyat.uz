namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>
/// Bo'sh shablon (`GET /api/admin/catalog/import-template.xlsx`) mazmuni — ATAYLAB oddiy
/// <see cref="CatalogExcelTestDto"/>, ya'ni eksport bilan BIR XIL quruvchidan o'tadi. Shu
/// sabab shablon "boshqacha ko'rinadigan" fayl emas: uni yuklab olib, to'ldirib, darhol
/// qaytarib yuklash mumkin (aylanma test buni qulflaydi).
/// </summary>
public static class CatalogExcelTemplate
{
    public const string FileName = "shaxsiyat-anketa-shablon.xlsx";

    /// <summary>Bitta namunaviy shkala (uchta to'g'ri oraliq bilan) va bitta namunaviy savol.</summary>
    public static CatalogExcelTestDto Sample { get; } = new(
        Code: "NAMUNA",
        NameUz: "Namunaviy anketa",
        DescriptionUz: "Bu qatorni o'z anketangiz ma'lumotlari bilan almashtiring.",
        EstimatedMinutes: 6,
        PageSize: 10,
        ScoringMode: "Scored",
        Scales:
        [
            new CatalogExcelScaleDto(
                "STRESS",
                "Stressga munosabat",
                "Namunaviy shkala — o'z shkalangiz bilan almashtiring.",
                [
                    new InterpretationBandDto(0, 33, "Past"),
                    new InterpretationBandDto(34, 66, "O'rtacha"),
                    new InterpretationBandDto(67, 100, "Yuqori"),
                ]),
        ],
        Questions:
        [
            new CatalogExcelQuestionDto(
                Code: "NAMUNA-Q01",
                Order: 1,
                TextUz: "Kutilmagan vaziyatlarda ham xotirjam qola olaman.",
                Type: "Likert5",
                Scale: "STRESS",
                Direction: 1,
                Weight: 1.0m,
                IsRequired: true),
        ]);
}
