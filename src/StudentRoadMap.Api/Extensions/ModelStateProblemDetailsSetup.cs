using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Extensions;

/// <summary>
/// Model-binding va so'rov tanasini (JSON) o'qish bosqichidagi xatolarni loyihaning YAGONA
/// `ProblemDetails` shakliga keltiradi — `code` maydoni bilan (`docs/06-arxitektura.md`
/// 6-bo'lim, `CLAUDE.md` 11-qoida).
///
/// **Nima uchun kerak edi (P31 topilmasi).** `[ApiController]` atributi 400 javobini o'zi
/// yasaydi va standart holatda ASP.NET Core'ning `ValidationProblemDetails`iga tushadi:
/// unda `code` maydoni YO'Q, `title` inglizcha ("One or more validation errors occurred.")
/// va JSON parser xabari (`'{' is invalid after a value. Path: $ | LineNumber: 0 |
/// BytePositionInLine: 8.`) to'g'ridan-to'g'ri mijozga uzatiladi. Ya'ni buzuq JSON tanasi
/// (`{"name":`) yoki noto'g'ri tur (`"age":"abc"`) yuborilganda javob shakli
/// `ValidationBehavior` chiqaradigan (`ExceptionHandlingMiddleware` orqali) shakldan butunlay
/// boshqacha bo'lardi va klient xatoni ajrata olmasdi.
///
/// **Yagona manba.** Bu yerdagi javob `ExceptionHandlingMiddleware`ning
/// `ApplicationValidationException` uchun chiqaradigan javobi bilan AYNAN bir xil:
/// `status=400`, bir xil `title`, `type`, `code=VALIDATION_ERROR`, `traceId` va camelCase
/// kalitli `errors` lug'ati. Ikki xil format hosil qilinmaydi — faqat xatolar boshqa
/// bosqichda (pipeline'ga kirishdan OLDIN) topiladi.
///
/// **Ichki tafsilot sizmasligi.** `ModelError.Exception` mavjud bo'lsa (JSON deserializatsiya
/// istisnosi) uning xabari HECH QACHON javobga ko'chirilmaydi — o'rniga o'zbekcha umumiy
/// xabar beriladi. Framework'ning inglizcha model-binding xabarlari esa
/// `ModelBindingMessageProvider` orqali o'zbekchaga almashtiriladi (`CLAUDE.md` 1-qoida).
/// </summary>
public static class ModelStateProblemDetailsSetup
{
    /// <summary>
    /// Butun so'rov tanasiga (ma'lum bir maydonga emas) tegishli xato kaliti. System.Text.Json
    /// bunday xatoni `$` yo'li bilan beradi — `$` mijoz uchun ma'nosiz, shu sabab `body`ga
    /// normallashtiriladi.
    /// </summary>
    internal const string BodyErrorKey = "body";

    private const string InvalidValueMessage = "Qiymat noto'g'ri formatda.";
    private const string InvalidBodyMessage = "So'rov tanasi (JSON) noto'g'ri formatda.";
    private const string MissingValueMessage = "Majburiy maydon berilmagan.";
    private const string NumberExpectedMessage = "Qiymat son bo'lishi kerak.";
    private const string NotNullMessage = "Qiymat bo'sh bo'lishi mumkin emas.";

    /// <summary>`ExceptionHandlingMiddleware`dagi `ApplicationValidationException` sarlavhasi bilan bir xil.</summary>
    private const string Title = "Kiritilgan ma'lumotlar noto'g'ri.";

    /// <summary>
    /// MVC nullable bo'lmagan (NRT) xususiyat/parametr uchun yashirin `RequiredAttribute`
    /// qo'shadi va uning xabari framework'ning INGLIZCHA standarti bo'ladi ("The Slug field
    /// is required.") — `ModelBindingMessageProvider` bu xabarni qamramaydi (u DataAnnotations
    /// yo'li, model-binding yo'li emas). Shu shablon bilan solishtirib, xabar o'zbekchaga
    /// almashtiriladi (`CLAUDE.md` 1-qoida). Loyihaning DTO'larida qo'lda qo'yilgan
    /// DataAnnotations atributlari YO'Q (tekshirilgan) — barcha biznes validatsiyasi
    /// FluentValidation'da va u `ModelState`ga umuman tushmaydi, shu sabab bu solishtiruv
    /// hech qanday o'z xabarimizni ustidan yozib yubormaydi.
    /// </summary>
    private static readonly RequiredAttribute ImplicitRequired = new();

    public static IServiceCollection AddModelStateProblemDetails(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = BuildProblemResult;
        });

        // System.Text.Json istisno xabarlari MIJOZGA UZATILMAYDI. Standart holatda ular
        // `ModelState`ga XABAR sifatida tushadi (istisno obyekti sifatida emas) va ichki
        // tafsilotni oshkor qiladi — jonli tekshiruvda javobda `StudentRoadMap.Api.Contracts.
        // Public.StartSessionRequest`, `Path: $.grade`, `LineNumber`, `BytePositionInLine`
        // ko'rindi (P31 topilmasi: ichki tafsilot sizishi). `false` bilan formatter xom
        // istisnoni yozadi va `ModelError.Exception` to'ldiriladi — quyidagi `CollectErrors`
        // uni o'zbekcha umumiy xabarga almashtiradi.
        services.Configure<JsonOptions>(options => options.AllowInputFormatterExceptionMessages = false);

        // Framework'ning inglizcha standart xabarlari o'rniga o'zbekcha (`CLAUDE.md` 1-qoida).
        // Bular query/route parametrlari va `[BindRequired]` uchun ishlaydi; tanadagi JSON
        // xatolari `ModelError.Exception` orqali keladi va pastda alohida qayta yoziladi.
        services.Configure<MvcOptions>(options =>
        {
            var messages = options.ModelBindingMessageProvider;
            messages.SetValueIsInvalidAccessor(_ => InvalidValueMessage);
            messages.SetAttemptedValueIsInvalidAccessor((_, _) => InvalidValueMessage);
            messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => InvalidValueMessage);
            messages.SetUnknownValueIsInvalidAccessor(_ => InvalidValueMessage);
            messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => InvalidValueMessage);
            messages.SetValueMustNotBeNullAccessor(_ => NotNullMessage);
            messages.SetMissingBindRequiredValueAccessor(_ => MissingValueMessage);
            messages.SetMissingKeyOrValueAccessor(() => MissingValueMessage);
            messages.SetMissingRequestBodyRequiredValueAccessor(() => InvalidBodyMessage);
            messages.SetValueMustBeANumberAccessor(_ => NumberExpectedMessage);
            messages.SetNonPropertyValueMustBeANumberAccessor(() => NumberExpectedMessage);
        });

        return services;
    }

    private static IActionResult BuildProblemResult(ActionContext context)
    {
        var errors = CollectErrors(context, BodyParameterNames(context));

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = Title,
            Type = $"https://studentroadmap/errors/{ProblemCodes.ValidationError.ToLowerInvariant().Replace('_', '-')}",
            Instance = context.HttpContext.Request.Path,
        };
        problem.Extensions["code"] = ProblemCodes.ValidationError;
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        problem.Extensions["errors"] = errors;

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
    }

    /// <summary>
    /// `[FromBody]` parametrining C# nomlarini qaytaradi (`request`, `command`…). So'rov
    /// tanasi umuman bog'lanmaganda (buzuq/bo'sh JSON) `ModelState` kaliti aynan shu nom
    /// bo'ladi — u mijoz uchun ma'nosiz va endpointdan endpointga o'zgaradi, shu sabab
    /// yagona `body` kalitiga normallashtiriladi.
    /// </summary>
    private static HashSet<string> BodyParameterNames(ActionContext context)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (parameter.BindingInfo?.BindingSource == BindingSource.Body)
            {
                names.Add(parameter.Name);
            }
        }

        return names;
    }

    private static Dictionary<string, string[]> CollectErrors(ActionContext context, HashSet<string> bodyParameterNames)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var (rawKey, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            var isBodyKey = bodyParameterNames.Contains(rawKey);

            foreach (var error in entry.Errors)
            {
                var key = isBodyKey || string.Equals(error.ErrorMessage, InvalidBodyMessage, StringComparison.Ordinal)
                    ? BodyErrorKey
                    : NormalizeKey(rawKey);

                // Istisno mavjud bo'lsa — bu JSON deserializatsiya xatosi. Uning xabari
                // (yo'l, satr/bayt raqami, kutilgan CLR tipi) ichki tafsilot hisoblanadi va
                // javobga CHIQARILMAYDI.
                var message = error.Exception is not null || string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? (key == BodyErrorKey ? InvalidBodyMessage : InvalidValueMessage)
                    : Localize(rawKey, key, error.ErrorMessage);

                if (!errors.TryGetValue(key, out var list))
                {
                    list = [];
                    errors[key] = list;
                }

                if (!list.Contains(message, StringComparer.Ordinal))
                {
                    list.Add(message);
                }
            }
        }

        if (errors.Count == 0)
        {
            errors[BodyErrorKey] = [InvalidBodyMessage];
        }

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Framework'ning yashirin `RequiredAttribute` inglizcha xabarini o'zbekchaga almashtiradi;
    /// boshqa xabarlar (barchasi `ModelBindingMessageProvider` orqali allaqachon o'zbekcha)
    /// o'zgarishsiz qoladi.
    /// </summary>
    private static string Localize(string rawKey, string normalizedKey, string message)
    {
        var displayName = LastSegmentIdentifier(rawKey);

        if (displayName.Length > 0
            && string.Equals(message, ImplicitRequired.FormatErrorMessage(displayName), StringComparison.Ordinal))
        {
            return normalizedKey == BodyErrorKey ? InvalidBodyMessage : MissingValueMessage;
        }

        return message;
    }

    /// <summary>`Answers[0].QuestionId` → `QuestionId` (yashirin `RequiredAttribute` shu nomni ishlatadi).</summary>
    private static string LastSegmentIdentifier(string rawKey)
    {
        var segment = rawKey.Split('.')[^1];
        var bracketIndex = segment.IndexOf('[', StringComparison.Ordinal);
        return bracketIndex < 0 ? segment : segment[..bracketIndex];
    }

    /// <summary>
    /// `ModelState` kalitini `errors` lug'atining camelCase kalitiga o'giradi.
    ///
    /// System.Text.Json JSON tanasidagi xato uchun JSONPath beradi: `$` (butun tana),
    /// `$.age`, `$.answers[0].questionId`. Boshlang'ich `$`/`$.` olib tashlanadi.
    /// Query/route parametrlari uchun kalit C# nomi bo'ladi (`PageSize`) — u
    /// `Application/Common/Exceptions/ValidationException`dagi bilan BIR XIL qoida bo'yicha
    /// camelCase qilinadi (har bo'lak alohida, `[0]` indeksiga tegilmaydi). Ikki joyda bir xil
    /// mantiq takrorlanishi ataylab: `Application` qatlami ASP.NET Core'ning `ModelState`ini
    /// bilmaydi (`docs/06` 3-bo'lim), shu sabab umumiy yordamchi qatlamlar orasida bo'lisha
    /// olmaydi. Ikkalasi o'zgarsa birga o'zgaradi.
    /// </summary>
    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return BodyErrorKey;
        }

        var path = key;
        if (path[0] == '$')
        {
            path = path.Length > 1 && path[1] == '.' ? path[2..] : path[1..];
        }

        if (path.Length == 0)
        {
            return BodyErrorKey;
        }

        var segments = path.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = ToCamelCaseSegment(segments[i]);
        }

        return string.Join('.', segments);
    }

    private static string ToCamelCaseSegment(string segment)
    {
        if (segment.Length == 0)
        {
            return segment;
        }

        var bracketIndex = segment.IndexOf('[', StringComparison.Ordinal);
        if (bracketIndex < 0)
        {
            return JsonNamingPolicy.CamelCase.ConvertName(segment);
        }

        var identifier = segment[..bracketIndex];
        var indexer = segment[bracketIndex..];
        return identifier.Length == 0
            ? indexer
            : JsonNamingPolicy.CamelCase.ConvertName(identifier) + indexer;
    }
}
