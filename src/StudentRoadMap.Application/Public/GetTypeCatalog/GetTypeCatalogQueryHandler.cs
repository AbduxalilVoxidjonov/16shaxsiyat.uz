using MediatR;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetTypeCatalog;

/// <summary>
/// `docs/07` 1.10-bo'lim. Read-only, autentifikatsiyasiz.
///
/// Butun ish `PublicCatalogCache.GetTypeCatalogAsync` da: katalog (16 qator) bir soatga
/// keshlanadi, chunki u FAQAT seed orqali o'zgaradi. Handler kesh proyeksiyasini javob
/// DTO'siga o'giradi — hech qanday matn bu yerda yozilmaydi yoki o'zgartirilmaydi.
///
/// Katalog bo'sh bo'lsa (seed qilinmagan baza) xato EMAS, bo'sh ro'yxat qaytadi: bu ommaviy
/// marketing bo'limi, mijoz uni "bo'sh holat" bilan ko'rsatadi va sahifa yiqilmaydi.
/// </summary>
internal sealed class GetTypeCatalogQueryHandler : IRequestHandler<GetTypeCatalogQuery, Result<GetTypeCatalogResult>>
{
    private readonly PublicCatalogCache _catalogCache;

    public GetTypeCatalogQueryHandler(PublicCatalogCache catalogCache)
    {
        _catalogCache = catalogCache;
    }

    public async Task<Result<GetTypeCatalogResult>> Handle(GetTypeCatalogQuery request, CancellationToken cancellationToken)
    {
        var entries = await _catalogCache.GetTypeCatalogAsync(cancellationToken).ConfigureAwait(false);

        var types = entries
            .Select(t => new PublicTypeCatalogItemDto(
                t.Code,
                t.Name,
                t.ShortDescription,
                t.LongDescription,
                t.Strengths,
                t.GrowthAreas,
                t.CareerHints))
            .ToList();

        return Result.Success(new GetTypeCatalogResult(types));
    }
}
