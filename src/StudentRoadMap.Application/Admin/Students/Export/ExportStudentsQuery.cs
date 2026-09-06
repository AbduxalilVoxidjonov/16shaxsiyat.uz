using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Students.Export;

/// <summary>
/// `GET /api/admin/students/export?<filtrlar>` — `docs/07-api-shartnoma.md` 3.2-bo'lim: "`.xlsx`
/// (filtr saqlanadi)". Filtr maydonlari `ListStudentsQuery` bilan AYNAN bir xil (sahifalash/
/// saralash YO'Q — eksport FILTRGA mos BARCHA qatorni chiqaradi, `prompts/27` MAXSUS DIQQAT #1).
/// `AdminUserId`/`IpAddress`/`UserAgent` — audit uchun (`docs/08` §8: "kim, qachon, qaysi
/// filtr, nechta qator").
/// </summary>
public sealed record ExportStudentsQuery(
    Guid? SchoolId,
    int? Grade,
    string? Status,
    bool? NeedsAttention,
    string? PersonalityType,
    string? ActivityLevel,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    Guid? AdminUserId,
    string? IpAddress,
    string? UserAgent,
    // MANBA filtri — `ListStudentsQuery.Source` bilan AYNAN bir xil ma'noda.
    string? Source = null) : IRequest<Result<AdminStudentsExportResult>>;

/// <summary>Tayyor `.xlsx` fayl baytlari + nomi — controller shundan `File(...)` qaytaradi.</summary>
public sealed record AdminStudentsExportResult(byte[] Content, string FileName, string ContentType);
