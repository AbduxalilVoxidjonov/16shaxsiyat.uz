namespace StudentRoadMap.Application.Admin.PublicSpace;

/// <summary>
/// Ommaviy makon bo'limining audit harakatlari.
///
/// <para>
/// ⚠️ **Nima uchun `Domain.Identity.AuditActions` da emas:** shu vazifada `Domain` qatlami
/// MUZLATILGAN (topshiriq: "TEGMA: `src/StudentRoadMap.Domain/**`"). `AuditLog.Create` harakat
/// nomini oddiy `string` sifatida qabul qiladi, shu sabab qiymat bu yerda ham xavfsiz
/// e'lon qilinadi. **PM'ga savol:** keyingi ish davomida `AuditActions` ga
/// `PublicSpaceShowResultChanged` sifatida ko'chirilsinmi (registr bitta joyda qolishi uchun)?
/// </para>
/// <para>
/// Dastur biriktirish/olib tashlash bu yerda YO'Q — ular mavjud `AuditActions.ProgramAssigned`/
/// `ProgramUnassigned` yozuvlarini beradi (`AssignProgramSchoolCommandHandler` qayta
/// ishlatilgani uchun), ya'ni audit jurnalida ikki oqim bir xil ko'rinadi.
/// </para>
/// </summary>
internal static class PublicSpaceAuditActions
{
    /// <summary>`PUT /api/admin/public-space/show-result` — natijani foydalanuvchiga ko'rsatish bayrog'i.</summary>
    public const string ShowResultChanged = "PublicSpace.ShowResultChanged";
}
