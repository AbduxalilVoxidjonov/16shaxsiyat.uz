using MediatR;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>
/// Faylni parserga beradi va natijani `Result`ga o'giradi. Fayl darajasidagi xato
/// (zip emas, buzilgan, `.xls`/`.xlsm`, chegaradan katta) → `400 IMPORT_FILE_INVALID`
/// oldindan yozilgan o'zbekcha xabar bilan; mazmun darajasidagi xatolar (`issues`) esa
/// MUVAFFAQIYATLI `200` javob ichida qaytadi — superadmin oldindan ko'rish bilan BIRGA
/// nimani tuzatishni ko'radi.
/// </summary>
internal sealed class ParseCatalogExcelQueryHandler : IRequestHandler<ParseCatalogExcelQuery, Result<ParseCatalogExcelResultDto>>
{
    private readonly ICatalogExcelWorkbook _workbook;

    public ParseCatalogExcelQueryHandler(ICatalogExcelWorkbook workbook)
    {
        _workbook = workbook;
    }

    public Task<Result<ParseCatalogExcelResultDto>> Handle(ParseCatalogExcelQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outcome = _workbook.Parse(request.Content);

        if (outcome.FileErrorMessage is not null)
        {
            return Task.FromResult(Result.Failure<ParseCatalogExcelResultDto>(
                new Error(ProblemCodes.ImportFileInvalid, outcome.FileErrorMessage)));
        }

        return Task.FromResult(Result.Success(new ParseCatalogExcelResultDto(outcome.Data, outcome.Issues)));
    }
}
