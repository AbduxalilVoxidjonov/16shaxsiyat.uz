using System.Diagnostics;
using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
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

        var message = health.IsHealthy ? SuccessMessage : MessageForError(health.ErrorKind);

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

    /// <summary>
    /// Xato TURIga qarab oldindan yozilgan o'zbekcha xabar — provayder javobining xom matni
    /// (kalit, so'rov tanasi) HECH QACHON ishlatilmaydi. Har bir xabar adminni ANIQ keyingi
    /// harakatga yo'naltiradi (`prompts/28` MAXSUS DIQQAT #3).
    /// </summary>
    private static string MessageForError(AiErrorKind kind) => kind switch
    {
        AiErrorKind.Auth => "API kaliti noto'g'ri yoki bekor qilingan — kalitni qayta kiriting.",
        AiErrorKind.RateLimit => "Provayder kvotasi/limiti tugagan — birozdan keyin urinib ko'ring yoki tarifni tekshiring.",
        AiErrorKind.ModelNotFound => "Model topilmadi — model nomini tekshiring.",
        AiErrorKind.Network => "Provayderga ulanib bo'lmadi — internet aloqasini tekshiring.",
        AiErrorKind.Timeout or AiErrorKind.Server => "Provayder javob bermadi — birozdan keyin qayta urinib ko'ring.",
        AiErrorKind.Schema => "Provayder kutilgan shakldagi javob qaytarmadi.",
        AiErrorKind.BadRequest => "So'rov shakli noto'g'ri — bu bizning xatomiz, jurnalga qarang.",
        _ => "Noma'lum xato yuz berdi.",
    };
}
