using Microsoft.AspNetCore.Mvc;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Extensions;

/// <summary>
/// `ProblemDetails` xizmatining yagona sozlamasi (`docs/06-arxitektura.md` 6-bo'lim,
/// `CLAUDE.md` 11-qoida: "Barcha xatolar `ProblemDetails` formatida, `code` maydoni bilan").
///
/// **Nima uchun `code` uchun zaxira xarita kerak (P31 topilmasi).** Loyihada `code` uchta
/// joyda ANIQ qo'yiladi: `ExceptionHandlingMiddleware` (istisnolar), `ControllerResultExtensions`
/// (`Result` xatolari), autentifikatsiya handlerlari (401/403/410). Lekin javob HECH BIR
/// handler'ga yetib bormasdan, TRANSPORT darajasida ham tugashi mumkin — marshrut topilmasa
/// (404), metod noto'g'ri bo'lsa (405), `Content-Type` qo'llab-quvvatlanmasa (415). Bunday
/// javobni `app.UseStatusCodePages()` `IProblemDetailsService` orqali yozadi va unda `code`
/// bo'lmasdi. `CustomizeProblemDetails` — bu oqimning YAGONA umumiy nuqtasi, shu sabab zaxira
/// qiymat shu yerda beriladi. Yuqoridagi uch manba `WriteAsJsonAsync`/`ObjectResult` bilan
/// to'g'ridan-to'g'ri yozadi va bu yerdan O'TMAYDI — ya'ni ularning aniq kodlari hech qachon
/// ustidan yozilmaydi (ikki xil format hosil bo'lmaydi).
/// </summary>
public static class ProblemDetailsSetup
{
    /// <summary>`405` — marshrut bor, lekin HTTP metodi qo'llab-quvvatlanmaydi (transport darajasi).</summary>
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";

    /// <summary>`413` — so'rov tanasi Kestrel chegarasidan katta (`docs/08` 6-bo'lim: fayl hajmi tekshiruvi).</summary>
    public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";

    /// <summary>`415` — `Content-Type` qo'llab-quvvatlanmaydi (masalan `text/plain` bilan JSON endpointga).</summary>
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";

    private static readonly IReadOnlyDictionary<int, string> CodeByStatus = new Dictionary<int, string>
    {
        [StatusCodes.Status400BadRequest] = ProblemCodes.ValidationError,
        [StatusCodes.Status401Unauthorized] = ProblemCodes.Unauthorized,
        [StatusCodes.Status403Forbidden] = ProblemCodes.Forbidden,
        [StatusCodes.Status404NotFound] = ProblemCodes.NotFound,
        [StatusCodes.Status405MethodNotAllowed] = MethodNotAllowed,
        [StatusCodes.Status413PayloadTooLarge] = PayloadTooLarge,
        [StatusCodes.Status415UnsupportedMediaType] = UnsupportedMediaType,
        [StatusCodes.Status429TooManyRequests] = ProblemCodes.RateLimited,
    };

    public static IServiceCollection AddConfiguredProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);

                if (context.ProblemDetails.Extensions.ContainsKey("code"))
                {
                    return;
                }

                var status = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                context.ProblemDetails.Extensions["code"] = ResolveCode(status);
            };
        });

        return services;
    }

    /// <summary>
    /// Status kodidan `code`ni topadi. Ro'yxatda bo'lmagan 5xx uchun `INTERNAL_ERROR` —
    /// mijozga ichki tafsilot bermaydigan, umumiy javob. Boshqa (kutilmagan) 4xx uchun ham
    /// `VALIDATION_ERROR` emas, `NOT_FOUND` emas — aniq bo'lmagani uchun `INTERNAL_ERROR`
    /// bermaymiz; bu holat amalda yuzaga kelmaydi, lekin `code` maydoni HAR DOIM bo'lishi
    /// shart (`CLAUDE.md` 11-qoida), shu sabab umumiy zaxira sifatida `INTERNAL_ERROR`.
    /// </summary>
    internal static string ResolveCode(int status) =>
        CodeByStatus.TryGetValue(status, out var code) ? code : ProblemCodes.InternalError;
}
