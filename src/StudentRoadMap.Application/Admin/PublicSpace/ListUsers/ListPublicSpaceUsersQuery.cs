using MediatR;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.ListUsers;

/// <summary>
/// `GET /api/admin/public-space/users?search=&amp;status=&amp;page=&amp;pageSize=&amp;sort=` —
/// ommaviy makonda ro'yxatdan o'tgan foydalanuvchilar (`docs/07` 3.7-bo'lim, 2026-09-07).
///
/// <para>
/// `Status` — OXIRGI sessiya bo'yicha: `all` (standart) · `never_started` (birorta sessiya
/// yo'q — anketa to'ldirilmagan bo'lsa ham) · `in_progress` (oxirgi sessiya yakunlanmagan —
/// `Draft`/`InProgress`/`Abandoned`) · `completed` (oxirgi sessiya yakunlangan —
/// `CompletedAt != null`). Qiymatlar `PublicUserStatusFilter` da.
/// </para>
/// <para>
/// `Search` — F.I.Sh. (anketa), Telegram ism/familiya/username bo'yicha (katta-kichik
/// harf farqsiz). `Sort` — `registeredAt` (standart `-registeredAt`) yoki `lastLoginAt`.
/// </para>
/// </summary>
public sealed record ListPublicSpaceUsersQuery(
    string? Search,
    string? Status,
    int Page,
    int PageSize,
    string? Sort) : IRequest<Result<PagedResult<AdminPublicUserListItemDto>>>;

/// <summary>
/// `?status=` qiymatlari — `const string` (JSON/query'da satr, `AdminSourceFilter` bilan bir
/// xil sabab). `Parse` noma'lum qiymatni `null` (filtr yo'q) qaytarMAYDI — validator rad
/// etadi (`400`), chunki bu yerda "hammasi" uchun ANIQ `all` qiymati bor.
/// </summary>
public static class PublicUserStatusFilter
{
    public const string All = "all";
    public const string NeverStarted = "never_started";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";

    public static readonly IReadOnlyList<string> Values = [All, NeverStarted, InProgress, Completed];

    /// <summary>Bo'sh → `All`; noma'lum → `null` (validator `400` beradi).</summary>
    public static string? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return All;
        }

        var trimmed = raw.Trim();
        return Values.FirstOrDefault(v => string.Equals(v, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
