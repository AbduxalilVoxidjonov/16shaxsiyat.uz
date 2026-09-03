using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Catalog.Excel;

/// <summary>
/// Read-only. Shkalalar ikki manbadan kelishi mumkin: `Custom` anketaning O'Z `TestScale`lari
/// (oraliqlari bilan) yoki tizim metodikasi — u yerda `TestScale` jadvali BO'SH, shu sabab
/// shkalalar savollardagi kodlardan yig'iladi va nomi `SystemScaleCatalog` dan olinadi
/// (`CatalogScaleNameResolver` — katalog sahifasidagi "Shkalalar (faqat ko'rish uchun)" bloki
/// bilan AYNAN bir xil manba).
/// </summary>
internal sealed class ExportCatalogTestExcelQueryHandler : IRequestHandler<ExportCatalogTestExcelQuery, Result<CatalogExcelFileDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly ICatalogExcelWorkbook _workbook;

    public ExportCatalogTestExcelQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, ICatalogExcelWorkbook workbook)
    {
        _context = context;
        _executor = executor;
        _workbook = workbook;
    }

    public async Task<Result<CatalogExcelFileDto>> Handle(ExportCatalogTestExcelQuery request, CancellationToken cancellationToken)
    {
        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure<CatalogExcelFileDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions)
                .Where(q => q.TestDefinitionId == test.Id)
                .OrderBy(q => q.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var scales = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestScales)
                .Where(s => s.TestDefinitionId == test.Id)
                .OrderBy(s => s.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var resolver = CatalogScaleNameResolver.Create(test.ScoringStrategyCode, scales);

        var scaleDtos = scales.Count > 0
            ? scales
                .Select(s => new CatalogExcelScaleDto(
                    s.Code,
                    s.NameUz,
                    s.DescriptionUz,
                    s.InterpretationBands
                        .OrderBy(b => b.MinInclusive)
                        .Select(b => new InterpretationBandDto(b.MinInclusive, b.MaxInclusive, b.Level))
                        .ToList()))
                .ToList()
            : questions
                .Select(q => q.Scale)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.Ordinal)
                .Select(code => new CatalogExcelScaleDto(
                    code,
                    resolver.Resolve(code) ?? code,
                    resolver.ResolveDescription(code),
                    []))
                .ToList();

        var questionDtos = questions
            .Select(q => new CatalogExcelQuestionDto(
                q.Code,
                q.DisplayOrder,
                q.TextUz,
                q.QuestionType.ToString(),
                q.Scale,
                q.ScaleDirection,
                q.Weight,
                q.IsRequired))
            .ToList();

        var dto = new CatalogExcelTestDto(
            test.Code,
            test.NameUz,
            test.DescriptionUz,
            test.EstimatedMinutes,
            test.PageSize,
            test.ScoringMode.ToString(),
            scaleDtos,
            questionDtos);

        var content = _workbook.Build(dto);

        return Result.Success(new CatalogExcelFileDto(
            content,
            $"anketa-{SafeFileNamePart(test.Code)}.xlsx",
            CatalogExcelLimits.ExcelContentType));
    }

    /// <summary>Fayl nomiga faqat xavfsiz belgilar tushadi (kod odatda `^[A-Z0-9_-]+$`, lekin nom sarlavhasiga ishonilmaydi).</summary>
    private static string SafeFileNamePart(string code) =>
        string.Concat(code.Where(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_')) is { Length: > 0 } safe
            ? safe
            : "anketa";
}
