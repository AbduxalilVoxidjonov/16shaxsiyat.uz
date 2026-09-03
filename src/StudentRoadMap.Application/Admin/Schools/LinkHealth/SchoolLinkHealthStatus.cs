namespace StudentRoadMap.Application.Admin.Schools.LinkHealth;

/// <summary>
/// Maktab havolasining "ishlaydimi" holati — 2026-09-03 jonli hodisasidan keyin qo'shildi
/// (yagona dastur o'chirilgan edi, barcha maktab havolasi jimgina o'lik bo'lib qoldi, panelda
/// esa hech qanday belgi yo'q edi).
///
/// **Bloklovchi holatlar** (<see cref="NoProgramsAtAll"/>, <see cref="NoProgramAssigned"/>,
/// <see cref="ProgramsDeactivated"/>) — o'quvchi havolani ochsa `GET /api/public/schools/{slug}`
/// `409 NO_PROGRAM_AVAILABLE` qaytaradi. Bu ekvivalentlik `PublicSchoolInfoNoProgramEndpointTests`
/// da qulflangan: mezon ajralib ketsa test yiqiladi.
///
/// <see cref="ProgramsWithoutTests"/> — ommaviy javob hali `200`, LEKIN sessiyada bironta test
/// bo'lmaydi (`StartSessionCommandHandler` savol soni `0` bo'lgan testni o'tkazib yuboradi),
/// ya'ni o'quvchi bo'sh testga tushadi. Shu sabab bu ham "havola ishlamaydi" deb belgilanadi.
/// </summary>
public enum SchoolLinkHealthStatus
{
    /// <summary>Kamida bitta mavjud dastur bor va unda kamida bitta yaroqli test bor.</summary>
    Ok = 0,

    /// <summary>Tizimda umuman dastur yo'q.</summary>
    NoProgramsAtAll = 1,

    /// <summary>Dasturlar bor, lekin hech biri `Public` emas va bu maktabga biriktirilmagan.</summary>
    NoProgramAssigned = 2,

    /// <summary>
    /// Bu maktabga ko'rinishi kerak bo'lgan dastur(lar) bor (`Public` yoki biriktirilgan),
    /// lekin ular `Published`/`IsActive` emas — ya'ni o'chirilgan yoki arxivlangan.
    /// </summary>
    ProgramsDeactivated = 3,

    /// <summary>Mavjud dastur bor, lekin unda nashr qilingan/faol va savoli bor test yo'q.</summary>
    ProgramsWithoutTests = 4,
}
