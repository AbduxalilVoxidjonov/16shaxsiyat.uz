using System.Diagnostics;
using MediatR;
using StudentRoadMap.Application.Ai;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.TestProvider;

/// <summary>
/// `docs/07` §3.5. `IAiAnalysisProvider.CheckHealthAsync` chaqiradi (`ResolveForTestAsync` —
/// `IsActive`ni TEKSHIRMAYDI, faollashtirishdan OLDIN sinash mumkin bo'lishi uchun).
/// <para>
/// ⚠️ **Xavfsizlik/UX (koordinator ko'rsatmasi, 2026-09-02):** frontend `message`ni O'ZGARTIRMASDAN
/// to'g'ridan-to'g'ri adminga ko'rsatadi. Shu sabab: (1) `AiHealthResult.Message`dagi PROVIDERNING
/// XOM javob matni HECH QACHON ishlatilmaydi — faqat `AiErrorKind` asosida qurilgan, oldindan
/// yozilgan, aniq-harakatga-yo'naltiruvchi o'zbekcha xabar; (2) shu bilan kalit yoki ichki
/// tafsilotlarning tasodifan sizib chiqishi imkonsiz (`AiHealthErrorMessageTests` bunga kafolat beradi).
/// </para>
/// <para>
/// 2026-09-23 (Gemini 503 "model overloaded" timeout bilan bir xil xabar berardi — chalg'ituvchi):
/// xabar endi status kodini ham hisobga oladi (503/529 → "serverlari band", boshqa 5xx →
/// "Provayder xatosi (kod N)", timeout → "(timeout)") va oxiriga `AiHealthResult.ProviderDetail`
/// ("Tafsilot: ...") qo'shiladi. Bu XOM tana EMAS — faqat `error.message` maydoni, Infrastructure'da
/// aniq kalit bo'yicha, bu yerda yana kalit SHAKLI bo'yicha (`AiErrorDetailSanitizer`) tozalangan
/// va 200 belgigacha qisqartirilgan. `AiHealthResult.Message` (xom xabar) hamon ISHLATILMAYDI.
/// </para>
/// </summary>
internal sealed class TestAiProviderCommandHandler : IRequestHandler<TestAiProviderCommand, Result<AdminAiProviderTestResultDto>>
{
    private const string SuccessMessage = "Ulanish muvaffaqiyatli — provayder javob berdi.";

    private readonly IAiProviderResolver _providerResolver;
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public TestAiProviderCommandHandler(IAiProviderResolver providerResolver, IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _providerResolver = providerResolver;
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    public async Task<Result<AdminAiProviderTestResultDto>> Handle(TestAiProviderCommand request, CancellationToken cancellationToken)
    {
        IAiAnalysisProvider provider;
        try
        {
            provider = await _providerResolver.ResolveForTestAsync(request.Provider, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<AdminAiProviderTestResultDto>(new Error(
                ProblemCodes.NotFound,
                $"'{request.Provider}' uchun API kaliti kiritilmagan — avval kalitni saqlang."));
        }

        var stopwatch = Stopwatch.StartNew();
        var health = await provider.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var message = health.IsHealthy ? SuccessMessage : BuildFailureMessage(request.Provider, health);

        var now = _dateTime.UtcNow;
        var config = await _executor.FirstOrDefaultAsync(
            _context.AiProviderConfigs.Where(c => c.Provider == request.Provider),
            cancellationToken).ConfigureAwait(false);

        if (config is not null)
        {
            config.RecordConnectionCheck(health.IsHealthy ? "ok" : health.ErrorKind.ToString(), now);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(new AdminAiProviderTestResultDto(health.IsHealthy, (int)stopwatch.ElapsedMilliseconds, message));
    }

    private static string BuildFailureMessage(AiProvider provider, AiHealthResult health)
    {
        var message = MessageForError(provider, health.ErrorKind, health.StatusCode, health.Model);
        var detail = AiErrorDetailSanitizer.Sanitize(health.ProviderDetail);
        return detail is null ? message : $"{message} Tafsilot: {detail}";
    }

    /// <summary>
    /// Xato TURI (+ status kodi) ga qarab oldindan yozilgan o'zbekcha xabar — provayder javobining
    /// xom matni (kalit, so'rov tanasi) HECH QACHON ishlatilmaydi. Har bir xabar adminni ANIQ
    /// keyingi harakatga yo'naltiradi (`prompts/28` MAXSUS DIQQAT #3).
    /// </summary>
    private static string MessageForError(AiProvider provider, AiErrorKind kind, int? statusCode, string? model) => kind switch
    {
        AiErrorKind.Auth => "API kaliti noto'g'ri yoki bekor qilingan — kalitni qayta kiriting.",
        AiErrorKind.RateLimit => "Provayder kvotasi/limiti tugagan — birozdan keyin urinib ko'ring yoki tarifni tekshiring.",
        AiErrorKind.ModelNotFound => string.IsNullOrWhiteSpace(model)
            ? "Model topilmadi — model nomini tekshiring."
            : $"'{Truncate(model.Trim(), 100)}' modeli topilmadi — model nomini tekshiring.",
        AiErrorKind.Network => "Provayderga ulanib bo'lmadi — internet aloqasini tekshiring.",
        AiErrorKind.Timeout => "Provayder belgilangan vaqt ichida javob bermadi (timeout) — birozdan keyin qayta urinib ko'ring.",
        AiErrorKind.Server when statusCode is 503 or 529 =>
            $"{VendorName(provider)} serverlari hozir band ({statusCode}) — bir necha daqiqadan so'ng qayta urinib ko'ring yoki boshqa modelni tanlang.",
        AiErrorKind.Server when statusCode is not null =>
            $"Provayder xatosi (kod {statusCode}) — birozdan keyin qayta urinib ko'ring.",
        AiErrorKind.Server => "Provayder xatosi — birozdan keyin qayta urinib ko'ring.",
        AiErrorKind.Schema => "Provayder kutilgan shakldagi javob qaytarmadi.",
        AiErrorKind.BadRequest => "So'rov shakli noto'g'ri — bu bizning xatomiz, jurnalga qarang.",
        _ => "Noma'lum xato yuz berdi.",
    };

    private static string VendorName(AiProvider provider) => provider switch
    {
        AiProvider.Gemini => "Google",
        AiProvider.OpenAi => "OpenAI",
        AiProvider.Anthropic => "Anthropic",
        _ => "Provayder",
    };

    private static string Truncate(string text, int maxLength) =>
        text.Length > maxLength ? string.Concat(text.AsSpan(0, maxLength), "…") : text;
}
