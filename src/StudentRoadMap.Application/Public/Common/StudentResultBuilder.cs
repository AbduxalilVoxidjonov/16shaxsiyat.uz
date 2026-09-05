using StudentRoadMap.Application.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.GetStudentResult;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// Yakunlangan sessiyadan o'quvchiga ko'rsatiladigan **qisqartirilgan** natijani quradi
/// (`docs/07` 1.9-bo'lim shakli). Mantiq ilgari `GetStudentResultQueryHandler` ichida edi —
/// P47da AYNAN SHU proyeksiya ikki joyda kerak bo'lgani uchun shu yerga ko'chirildi:
///
/// • `GET /api/public/sessions/result` — egalik `X-Session-Token` bilan isbotlanadi (maktab oqimi);
/// • `GET /api/me/assessments/{id}/result` — egalik ommaviy foydalanuvchi JWT'si bilan.
///
/// Ikkalasi bir xil natijani berishi SHART: nusxa ko'chirilgan mantiq vaqt o'tib bir-biridan
/// ajralib ketardi (masalan `TypeCatalog` moslashtirish qoidasi faqat bir joyda tuzatilardi).
/// Ruxsat (`ShowResultToStudent`), holat (`Analyzed`) va muddat tekshiruvlari BU YERDA EMAS —
/// ular chaqiruvchi handler'ning zimmasida, chunki ikki oqimda ATAYLAB farq qiladi
/// (`GetMyAssessmentResultQueryHandler` izohiga qarang).
/// </summary>
internal sealed class StudentResultBuilder
{
    private const string NoteUz = "Bu natija tashxis emas — hozirgi holatingiz surati.";
    private const int MaxTopStrengths = 3;
    private const int MaxCareerFields = 3;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public StudentResultBuilder(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<GetStudentResultResult> BuildAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var testResults = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestResults).Where(r => r.AssessmentId == assessmentId),
            cancellationToken).ConfigureAwait(false);

        // ⚠️ Natija QAYSI test blokidan olinishi metodika KODI bilan aniqlanmaydi (`docs/06`
        // 8-bo'lim, 2026-09-02 "dastur" qarori). Ilgari bu yerda `TestCode == "MBTI16"` /
        // `"RIASEC"` satr solishtiruvi turardi: `Custom` dastur boshqa kodli metodika ishlatsa
        // (yoki kod versiyalansa) ekran JIMGINA noto'g'ri holatga tushardi — `MBTI16` kodli
        // superadmin anketasi shaxsiyat tipi o'rniga o'tib ketardi, hech qanday xato ko'rinmasdi.
        // Mezon — `PersonalityBattery.RoleOf` domen qoidasi (`PersonalityBatteryRoles` orqali).
        var rolesByAssessmentTestId = await PersonalityBatteryRoles.LoadByAssessmentTestIdAsync(
            _context, _executor, assessmentId, cancellationToken).ConfigureAwait(false);

        var personalityTypeCode = PersonalityBatteryRoles
            .FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.PersonalityType)?.ResultCode;
        var careerInterestCode = PersonalityBatteryRoles
            .FindByRole(testResults, rolesByAssessmentTestId, PersonalityBatteryRole.CareerInterest)?.ResultCode;

        string typeName = "";
        string shortDescription = "";
        IReadOnlyList<string> topStrengths = [];

        if (!string.IsNullOrEmpty(personalityTypeCode))
        {
            var typeCatalogEntry = await _executor.FirstOrDefaultAsync(
                _context.AsNoTracking(_context.TypeCatalog).Where(t => t.Code == personalityTypeCode),
                cancellationToken).ConfigureAwait(false);

            if (typeCatalogEntry is not null)
            {
                typeName = typeCatalogEntry.NameUz;
                shortDescription = typeCatalogEntry.ShortDescriptionUz;
                topStrengths = typeCatalogEntry.Strengths.Take(MaxTopStrengths).ToList();
            }
        }

        var careerFields = await ResolveCareerFieldsAsync(careerInterestCode, cancellationToken).ConfigureAwait(false);

        return new GetStudentResultResult(
            PersonalityType: personalityTypeCode ?? "",
            TypeName: typeName,
            ShortDescription: shortDescription,
            TopStrengths: topStrengths,
            CareerFields: careerFields,
            Note: NoteUz);
    }

    /// <summary>
    /// `docs/03` §4.3: `CareerMap.HollandCode` 2 harfli kalit. O'quvchining 3 harfli Holland
    /// kodi (`docs/03` §4.2 misoli: `IRA`) 15 mumkin bo'lgan juftlikdan faqat ba'zilarini
    /// qamraydi (seed'da 18 ta — barcha tartib bo'yicha 9 juftlik) — shu sabab ANIQ mos
    /// kelmasligi mumkin bo'lgan ikkinchi/uchinchi harf juftligini emas, o'quvchining TOP-3
    /// harfining IKKALASI ham (tartibsiz) shu 2 harfli kodda uchraydigan yozuvlarni tanlaydi
    /// (`RelevanceOrder` bo'yicha, 3 tagacha). Kod topilmasa (masalan `RIASEC` bu sessiyada
    /// yo'q) — bo'sh ro'yxat, xato emas (kasb yo'nalishlari ixtiyoriy ma'lumot).
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolveCareerFieldsAsync(string? hollandCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(hollandCode))
        {
            return [];
        }

        var codeLetters = hollandCode.ToCharArray();

        var careerMapEntries = await _executor.ToListAsync(
            _context.AsNoTracking(_context.CareerMap).OrderBy(c => c.RelevanceOrder),
            cancellationToken).ConfigureAwait(false);

        return careerMapEntries
            .Where(c => c.HollandCode.All(letter => codeLetters.Contains(letter)))
            .OrderBy(c => c.RelevanceOrder)
            .Select(c => c.FieldNameUz)
            .Distinct()
            .Take(MaxCareerFields)
            .ToList();
    }
}
