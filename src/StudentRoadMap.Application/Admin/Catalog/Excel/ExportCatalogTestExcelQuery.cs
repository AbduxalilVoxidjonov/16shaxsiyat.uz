using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>
/// `GET /api/admin/catalog/tests/{id}/export.xlsx` — mavjud anketani Excel'ga chiqaradi.
///
/// <para>
/// Tizim metodikasini (`IsSystem = true`) eksport qilish RUXSAT ETILADI: bu O'QISH amali,
/// BR-8 (`CLAUDE.md` 9a) faqat tizim testini O'ZGARTIRISHNI qulflaydi. Aynan shu eng yaxshi
/// namuna: superadmin 60 savol, shkala va og'irliklar bilan to'ldirilgan HAQIQIY fayl ko'radi
/// va uni nusxalab o'zinikini yozadi.
/// </para>
/// </summary>
public sealed record ExportCatalogTestExcelQuery(Guid Id) : IRequest<Result<CatalogExcelFileDto>>;
