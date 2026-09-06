using System.ComponentModel.DataAnnotations;

namespace StudentRoadMap.Api.Contracts.Admin.PublicSpace;

/// <summary>
/// `PUT /api/admin/public-space/show-result` tanasi.
///
/// `Enabled` — ANIQ qiymat (toggle emas): `SetPublicSpaceShowResultCommand` izohiga qarang.
/// `[Required]` — `bool` uchun ham majburiy, chunki tanada maydon berilmasa `false` (standart
/// qiymat) jimgina qo'llanib, natijani BEXOSDAN yopib qo'yardi.
/// </summary>
public sealed record SetPublicSpaceShowResultRequest
{
    [Required]
    public required bool Enabled { get; init; }
}
