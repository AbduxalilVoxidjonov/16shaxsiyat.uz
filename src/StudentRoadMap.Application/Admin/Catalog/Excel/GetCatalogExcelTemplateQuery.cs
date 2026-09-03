using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>`GET /api/admin/catalog/import-template.xlsx` — bo'sh shablon (ko'rsatma varag'i + bitta namunaviy qator).</summary>
public sealed record GetCatalogExcelTemplateQuery : IRequest<Result<CatalogExcelFileDto>>;
