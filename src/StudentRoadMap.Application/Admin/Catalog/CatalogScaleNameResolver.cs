using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Admin.Catalog;

/// <summary>
/// Savol qatoridagi shkala kodiga (`Question.Scale`) o'zbekcha nom topadi —
/// `CatalogQuestionItemDto.ScaleNameUz` uchun YAGONA joy.
///
/// <para>
/// <b>Aniqlash tartibi</b> (`docs/07` §3.4):
/// <list type="number">
///   <item><description>
///     <see cref="TestScale.NameUz"/> — anketaning O'Z shkalasi bo'lsa. Bu faqat `Custom`
///     anketalarda uchraydi (`TestDefinition.Scales` tizim metodikasida bo'sh), lekin qoida
///     "`Custom` bo'lsa" emas, "o'z shkalasi bo'lsa" — ya'ni bazadagi haqiqat har doim ustun.
///   </description></item>
///   <item><description>
///     <see cref="SystemScaleCatalog"/> — tizim metodikasi bo'lsa, `ScoringStrategyCode`
///     bo'yicha (`docs/03`).
///   </description></item>
///   <item><description>
///     <c>null</c> — noma'lum kod. Bu XATO EMAS, degradatsiya: frontend faqat kodni ko'rsatadi.
///     Taxminiy nom berilsa, noto'g'ri nom kodning o'zidan yomonroq bo'lardi — u ishonchli
///     ko'rinadi va jimgina yolg'on gapiradi.
///   </description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class CatalogScaleNameResolver
{
    private static readonly IReadOnlyDictionary<string, TestScale> NoOwnScaleEntities =
        new Dictionary<string, TestScale>(StringComparer.Ordinal);

    private readonly string? _scoringStrategyCode;
    private readonly IReadOnlyDictionary<string, TestScale> _ownScales;

    private CatalogScaleNameResolver(string? scoringStrategyCode, IReadOnlyDictionary<string, TestScale> ownScales)
    {
        _scoringStrategyCode = scoringStrategyCode;
        _ownScales = ownScales;
    }

    /// <summary>Hech qanday nom bermaydigan resolver — `ScaleNameUz` har doim <c>null</c>.</summary>
    public static CatalogScaleNameResolver None { get; } = new(null, NoOwnScaleEntities);

    /// <summary>
    /// Anketa agregatidan quradi. Chaqiruvchi `Scales` kolleksiyasi TO'LDIRILGANIGA ishonch
    /// hosil qilishi kerak (`CatalogMapping.LoadTrackedAsync` uni yuklaydi) — aks holda
    /// `Custom` anketa nomsiz qolardi.
    /// </summary>
    public static CatalogScaleNameResolver ForTest(TestDefinition test)
    {
        ArgumentNullException.ThrowIfNull(test);

        return Create(test.ScoringStrategyCode, test.Scales);
    }

    /// <summary>Anketa atributlari va alohida so'ralgan shkalalardan quradi (agregat yuklanmagan holat).</summary>
    public static CatalogScaleNameResolver Create(string? scoringStrategyCode, IEnumerable<TestScale> scales)
    {
        ArgumentNullException.ThrowIfNull(scales);

        var ownScales = new Dictionary<string, TestScale>(StringComparer.Ordinal);
        foreach (var scale in scales)
        {
            ownScales[scale.Code] = scale;
        }

        return new CatalogScaleNameResolver(scoringStrategyCode, ownScales);
    }

    /// <summary>Shkala kodining o'zbekcha nomi yoki <c>null</c> (noma'lum kod).</summary>
    public string? Resolve(string? scaleCode)
    {
        if (string.IsNullOrWhiteSpace(scaleCode))
        {
            return null;
        }

        if (_ownScales.TryGetValue(scaleCode, out var ownScale))
        {
            return ownScale.NameUz;
        }

        return SystemScaleCatalog.FindNameUz(_scoringStrategyCode, scaleCode);
    }

    /// <summary>
    /// Shkalaning qisqa izohi yoki <c>null</c>. Manba NOM bilan BIR XIL bo'lishi shart —
    /// aks holda bitta shkala bir manbadan nom, boshqasidan tavsif olib, chalkash juftlik
    /// hosil qilardi.
    /// </summary>
    public string? ResolveDescription(string? scaleCode)
    {
        if (string.IsNullOrWhiteSpace(scaleCode))
        {
            return null;
        }

        if (_ownScales.TryGetValue(scaleCode, out var ownScale))
        {
            return ownScale.DescriptionUz;
        }

        return SystemScaleCatalog.Find(_scoringStrategyCode, scaleCode)?.DescriptionUz;
    }
}
