namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>
/// `.xlsx` anketa fayli — QURISH va O'QISH bitta abstraksiyada. Ataylab bitta interfeys:
/// varaq nomlari va ustun sarlavhalari ikkala tomon uchun ham BITTA manbadan kelsin, aks holda
/// eksport bilan import vaqt o'tib jimgina bir-biridan uzilib ketardi (`docs/12` "aylanma
/// test" talabi). Amalga oshirish — `Infrastructure/Excel/CatalogExcelWorkbook` (`ClosedXML`);
/// `Application` qatlami `ClosedXML` paketiga bog'lanmaydi (`docs/06` §3).
/// </summary>
public interface ICatalogExcelWorkbook
{
    /// <summary>Modeldan `.xlsx` bayt massivi quradi (`Anketa`, `Shkalalar`, `Oraliqlar`, `Savollar`, `Ko'rsatma`).</summary>
    byte[] Build(CatalogExcelTestDto test);

    /// <summary>
    /// `.xlsx` baytlarini o'qiydi. HECH QACHON istisno uloqtirmaydi (buzuq fayl ham natija
    /// bilan qaytadi) — chaqiruvchi `500` bermasligi uchun.
    /// </summary>
    CatalogExcelParseOutcome Parse(byte[] content);
}
