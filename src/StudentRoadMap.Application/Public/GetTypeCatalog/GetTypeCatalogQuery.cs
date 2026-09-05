using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetTypeCatalog;

/// <summary>
/// `GET /api/public/type-catalog` — `docs/07-api-shartnoma.md` 1.10-bo'lim.
///
/// Parametrsiz: bu OCHIQ marketing kontenti (`/metodika` sahifasidagi 16 tip bo'limi va
/// `/metodika/:kod` sahifalari), sessiyaga ham, maktabga ham bog'liq emas. Aynan shu sabab
/// so'rovda `X-Session-Token` ham, hech qanday ID ham YO'Q — `CLAUDE.md` 8-qoidasi (IDOR)
/// buzilmaydi, chunki qaytadigan ma'lumot hech kimga tegishli emas.
/// </summary>
public sealed record GetTypeCatalogQuery : IRequest<Result<GetTypeCatalogResult>>;
