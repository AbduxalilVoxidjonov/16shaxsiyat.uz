using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>Bazaga umuman tegmaydi — shablon statik namunadan (`CatalogExcelTemplate.Sample`) quriladi.</summary>
internal sealed class GetCatalogExcelTemplateQueryHandler : IRequestHandler<GetCatalogExcelTemplateQuery, Result<CatalogExcelFileDto>>
{
    private readonly ICatalogExcelWorkbook _workbook;

    public GetCatalogExcelTemplateQueryHandler(ICatalogExcelWorkbook workbook)
    {
        _workbook = workbook;
    }

    public Task<Result<CatalogExcelFileDto>> Handle(GetCatalogExcelTemplateQuery request, CancellationToken cancellationToken)
    {
        var content = _workbook.Build(CatalogExcelTemplate.Sample);

        return Task.FromResult(Result.Success(new CatalogExcelFileDto(
            content,
            CatalogExcelTemplate.FileName,
            CatalogExcelLimits.ExcelContentType)));
    }
}
