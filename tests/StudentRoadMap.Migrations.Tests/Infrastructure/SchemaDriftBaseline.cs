using FluentAssertions;

namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>
/// ⚠️ MA'LUM (hali TUZATILMAGAN) sxema farqlari — "baseline".
///
/// `MigratsiyaSxemasi_ModeldanQurilganSxemaBilanBirXil` testi migratsiyalar bilan qurilgan
/// HAQIQIY sxemani modeldan qurilgan sxema bilan solishtiradi. Bugun mavjud bo'lgan farqlar
/// shu yerda ANIQ ro'yxatga olingan: ular testni qizil qilmaydi (chunki tuzatish YANGI
/// migratsiya talab qiladi — alohida qaror, `CLAUDE.md` 7-qoida: qo'llangan migratsiya
/// tahrirlanmaydi), lekin YANGI farq paydo bo'lsa test darhol yiqiladi.
///
/// Ro'yxatdagi har bir yozuv — OCHIQ TEXNIK QARZ. Farq tuzatilgach yozuv ham o'chirilishi
/// shart: <see cref="NormalizeColumns"/> ro'yxatda bor, lekin bazada YO'Q farqni topsa,
/// "baseline eskirgan" deb yiqiladi.
/// </summary>
internal static class SchemaDriftBaseline
{
    /// <param name="ColumnKey">`jadval.ustun`.</param>
    /// <param name="MigratedDefault">Migratsiya natijasidagi `column_default`.</param>
    /// <param name="ModelDefault">Modeldan qurilgan sxemadagi `column_default` (`-` — yo'q).</param>
    /// <param name="Reason">Nima uchun farq bor va nima qilish kerak.</param>
    internal sealed record ColumnDefaultDrift(string ColumnKey, string MigratedDefault, string ModelDefault, string Reason);

    /// <summary>
    /// Hozircha BO'SH — ustun `DEFAULT`lari bo'yicha ochiq texnik qarz YO'Q.
    ///
    /// Oxirgi yozuv (`admin_users.concurrency_stamp`, `DEFAULT '000…0'::uuid`)
    /// `20260902194217_DropConcurrencyStampDefault` migratsiyasi bilan TUZATILDI va shu
    /// yerdan o'chirildi (<see cref="NormalizeColumns"/> ro'yxatda bor, lekin bazada yo'q
    /// farqni topsa "baseline eskirgan" deb yiqilgan bo'lardi). Endi migratsiya bilan
    /// qurilgan sxemadagi HAR QANDAY yangi `DEFAULT` farqi
    /// `MigratsiyaSxemasi_ModeldanQurilganSxemaBilanBirXil` testini darhol qizil qiladi.
    /// </summary>
    internal static readonly IReadOnlyList<ColumnDefaultDrift> KnownColumnDefaultDrifts = [];

    /// <param name="IndexName">Indeks nomi.</param>
    /// <param name="MigratedSuffix">Migratsiya natijasidagi `indexdef` oxiri.</param>
    /// <param name="ModelSuffix">Modeldan qurilgan sxemadagi `indexdef` oxiri.</param>
    /// <param name="Reason">Nima uchun farq bor.</param>
    internal sealed record IndexDefinitionDrift(string IndexName, string MigratedSuffix, string ModelSuffix, string Reason);

    internal static readonly IReadOnlyList<IndexDefinitionDrift> KnownIndexDrifts =
    [
        new(
            IndexName: "ix_students_last_at",
            MigratedSuffix: "(last_assessment_at DESC NULLS LAST)",
            ModelSuffix: "(last_assessment_at DESC)",
            Reason:
                "ATAYLAB qilingan farq (`docs/05` §2/§5, `StudentConfiguration` izohi): EF fluent "
                + "API'da `NULLS` tartibi ifodalanmaydi, shu sabab indeks `InitialCreate` da xom "
                + "SQL bilan `NULLS LAST` sifatida yaratiladi; modelda esa oddiy `DESC` bo'lib "
                + "qoladi (snapshot mosligi uchun). Ya'ni `EnsureCreated()` bilan qurilgan barcha "
                + "mavjud testlar production'dagidan BOSHQA indeks ustida ishlaydi — bu farq "
                + "bilib qilingan, ammo shu yerda qayd etilgan. TUZATISH SHART EMAS; agar "
                + "kelajakda EF `NULLS` tartibini qo'llasa, yozuv o'chiriladi."),
    ];

    /// <summary>
    /// Migratsiya bilan qurilgan bazadan olingan ustunlar ro'yxatini "model ko'rinishiga"
    /// keltiradi — faqat YUQORIDA ro'yxatga olingan farqlar bo'yicha.
    /// </summary>
    public static IReadOnlyList<string> NormalizeColumns(IReadOnlyList<string> migratedColumns)
    {
        var normalized = migratedColumns.ToList();

        foreach (var drift in KnownColumnDefaultDrifts)
        {
            var from = $"{drift.ColumnKey} ";
            var migratedSuffix = $" default={drift.MigratedDefault}";
            var index = normalized.FindIndex(line =>
                line.StartsWith(from, StringComparison.Ordinal)
                && line.EndsWith(migratedSuffix, StringComparison.Ordinal));

            index.Should().BeGreaterThanOrEqualTo(0,
                "sxema farqlari 'baseline'i eskirgan: `{0}` uchun kutilgan farq bazada topilmadi. "
                + "Farq tuzatilgan bo'lsa, `SchemaDriftBaseline` dan yozuvni o'chiring.",
                drift.ColumnKey);

            normalized[index] = string.Concat(
                normalized[index].AsSpan(0, normalized[index].Length - migratedSuffix.Length),
                $" default={drift.ModelDefault}");
        }

        return normalized;
    }

    /// <summary>
    /// Indeks ta'riflarini "model ko'rinishiga" keltiradi — faqat ro'yxatga olingan farqlar bo'yicha.
    /// </summary>
    public static IReadOnlyList<string> NormalizeIndexes(IReadOnlyList<string> migratedIndexes)
    {
        var normalized = migratedIndexes.ToList();

        foreach (var drift in KnownIndexDrifts)
        {
            var index = normalized.FindIndex(line =>
                line.Contains($" {drift.IndexName} ", StringComparison.Ordinal)
                && line.EndsWith(drift.MigratedSuffix, StringComparison.Ordinal));

            index.Should().BeGreaterThanOrEqualTo(0,
                "sxema farqlari 'baseline'i eskirgan: `{0}` indeksi uchun kutilgan farq bazada topilmadi. "
                + "Farq yo'qolgan bo'lsa, `SchemaDriftBaseline` dan yozuvni o'chiring.",
                drift.IndexName);

            normalized[index] = string.Concat(
                normalized[index].AsSpan(0, normalized[index].Length - drift.MigratedSuffix.Length),
                drift.ModelSuffix);
        }

        return normalized;
    }
}
