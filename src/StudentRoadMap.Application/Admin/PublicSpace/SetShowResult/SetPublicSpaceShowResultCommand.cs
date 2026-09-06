using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.SetShowResult;

/// <summary>
/// `PUT /api/admin/public-space/show-result` — ommaviy makonda foydalanuvchi O'Z natijasini
/// ko'radimi (`School.ShowResultToStudent`). Ilgari bu bayroq FAQAT bazadan qo'lda
/// o'zgartirilardi (`School.SetShowResultToStudent` domen metodi tayyor turgan bo'lsa ham).
///
/// <para>
/// Bayroq global "avariya rubilnigi" (`App:ShowResultToStudent`) BILAN BIRGA ishlaydi:
/// `ShowResultPolicy` ikkalasini `&amp;&amp;` bilan birlashtiradi. Ya'ni global bayroq yopiq
/// bo'lsa, bu yerda `true` qilish natijani OCHMAYDI — UI shuni aniq aytishi kerak.
/// </para>
/// <para>
/// Toggle EMAS, ANIQ qiymat (`Enabled`) — ikki admin bir vaqtda bosganda "teskarisiga
/// o'tkazish" oxirgi holatni oldindan aytib bo'lmaydigan qilardi.
/// </para>
/// </summary>
public sealed record SetPublicSpaceShowResultCommand(
    bool Enabled,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminPublicSpaceDto>>;
