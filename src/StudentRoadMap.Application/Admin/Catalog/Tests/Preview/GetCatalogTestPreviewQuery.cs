using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Preview;

/// <summary>`GET /api/admin/catalog/tests/{id}/preview` — `docs/07` §3.4: "O'quvchi ko'radigan ko'rinish (savollar + shkala yorliqlari)". Nashr qilishdan OLDIN ham ishlaydi (`Draft` testda ham) — admin tekshirib chiqishi uchun (FR-6.8).</summary>
public sealed record GetCatalogTestPreviewQuery(Guid Id) : IRequest<Result<CatalogTestPreviewDto>>;
