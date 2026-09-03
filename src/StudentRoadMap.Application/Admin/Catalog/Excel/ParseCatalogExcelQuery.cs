using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>
/// `POST /api/admin/catalog/import/parse-excel` — faylni O'QIYDI, hech narsa SAQLAMAYDI
/// (shu sabab `Command` emas, `Query`: bazaga yozuv yo'q, audit yozuvi ham yo'q). Natijadagi
/// obyektni frontend mavjud JSON import yo'liga (oldindan ko'rish → yaratish) beradi.
/// </summary>
public sealed record ParseCatalogExcelQuery(byte[] Content) : IRequest<Result<ParseCatalogExcelResultDto>>;
