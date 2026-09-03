using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Ai;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Infrastructure.Ai;

namespace StudentRoadMap.Infrastructure.Jobs;

/// <inheritdoc cref="IAnalysisOrchestrator"/>
/// <summary>
/// `docs/09-ai-analiz-moduli.md` 1 va 7-bo'lim. Fallback zanjiri va retry siyosati:
/// <code>
/// urinish 1: default/so'ralgan provider
///    ├─ Timeout/RateLimit/Server/Unknown → 2s, 6s, 15s kutib qayta urinish (shu providerda, max 3 marta)
///    ├─ Validatsiya (schema/til/PII)      → tuzatuvchi eslatma bilan 1 marta qayta so'raladi (shu providerda)
///    ├─ Auth                              → retry YO'Q, darhol keyingi provider
///    └─ BadRequest                        → retry YO'Q (bir xil so'rov yana rad etiladi), keyingi provider
/// urinish 2, 3, ...: fallback zanjiridagi keyingi providerlar (`FallbackOrder`)
/// hammasi muvaffaqiyatsiz → Assessment.AnalysisFailed + zaxira shablon hisobot
/// </code>
/// Har HAQIQIY HTTP urinish (tarmoq darajasida) alohida `AiAnalysis` yozuvi (`AttemptNumber`
/// bilan) — validatsiya-qayta-so'rash ham shu hisoblanadi (yangi HTTP chaqiruvi).
/// </summary>
public sealed class AnalysisOrchestrator : IAnalysisOrchestrator
{
    /// <summary>`docs/09` 7-bo'lim: "2 s, 6 s, 15 s kutib 3 martagacha qayta urinish".</summary>
    private static readonly TimeSpan[] NetworkRetryBackoffs =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(6),
        TimeSpan.FromSeconds(15),
    ];

    /// <summary>Validatsiya (schema/til/PII) muammosida shu providerda ruxsat etilgan JAMI urinishlar soni (`docs/09` 7-bo'lim: "2 martagacha").</summary>
    private const int MaxContentValidationAttemptsPerProvider = 2;

    private const string BadRequestAggregateMessage =
        "So'rov shakli noto'g'ri — barcha faol AI provayderlar so'rovni rad etdi. Administrator so'rov shaklini (prompt/JSON sxema) tekshirishi kerak.";

    private const string NoProviderMessage = "Faol AI provayder sozlanmagan (kalit kiritilmagan yoki hammasi nofaol).";

    private const int MaxErrorMessageLength = 2000;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IAiProviderResolver _providerResolver;
    private readonly IAiResponseValidator _validator;
    private readonly IAiCostCalculator _costCalculator;
    private readonly IEnumerable<IAiAnalysisProvider> _directProviders;
    private readonly ILogger<AnalysisOrchestrator> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public AnalysisOrchestrator(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IPromptBuilder promptBuilder,
        IAiProviderResolver providerResolver,
        IAiResponseValidator validator,
        IAiCostCalculator costCalculator,
        IEnumerable<IAiAnalysisProvider> directProviders,
        ILogger<AnalysisOrchestrator> logger,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _promptBuilder = promptBuilder;
        _providerResolver = providerResolver;
        _validator = validator;
        _costCalculator = costCalculator;
        // `delay` — DI'da HECH QACHON ro'yxatdan o'tkazilmaydi (production real `Task.Delay`
        // ishlatadi); faqat testlarda 2s/6s/15s kutishning HAQIQIY o'rniga darhol qaytadigan
        // funksiya berish uchun (`AnalysisOrchestratorTests` — retry mantig'ini soniyalab
        // kutmasdan tekshirish).
        _delay = delay ?? Task.Delay;
        // `directProviders` — DI'da to'g'ridan-to'g'ri ro'yxatdan o'tkazilgan `IAiAnalysisProvider`
        // (production'da BO'SH; Dev/Test'da `MockAiProvider`, `Api/Program.cs`). `AiProviderConfig`
        // bazada HECH narsa sozlanmaganda (masalan integratsiya sinovlarida — haqiqiy kalit yo'q,
        // `CLAUDE.md` qat'iy qoidasi) ZAXIRA zanjir sifatida ishlatiladi — shu bilan butun
        // navbat/orkestratsiya oqimi tarmoqsiz, uchidan-uchigacha sinaladi.
        _directProviders = directProviders;
        _logger = logger;
    }

    public async Task RunAsync(Guid assessmentId, AiProvider? requestedProvider, string? requestedPromptVersion, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.Assessments.Where(a => a.Id == assessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            _logger.LogWarning("AnalysisOrchestrator: sessiya topilmadi {AssessmentId}", assessmentId);
            return;
        }

        // Idempotentlik (`prompts/18` vazifa #2): faqat `Analyzing` holatida davom etadi —
        // boshqa ishchi allaqachon yakunlagan yoki hech qachon to'g'ri navbatga qo'yilmagan bo'lsa jimgina chiqadi.
        if (assessment.Status != AssessmentStatus.Analyzing)
        {
            _logger.LogInformation(
                "AnalysisOrchestrator: sessiya '{Status}' holatida (Analyzing emas) — o'tkazib yuborildi {AssessmentId}",
                assessment.Status, assessmentId);
            return;
        }

        var student = await _executor.FirstOrDefaultAsync(
            _context.Students.Where(s => s.Id == assessment.StudentId),
            cancellationToken).ConfigureAwait(false);

        if (student is null)
        {
            // Nazariy jihatdan bo'lmasligi kerak (FK) — himoya: tahlilsiz muvaffaqiyatsizlik.
            _logger.LogError("AnalysisOrchestrator: o'quvchi topilmadi {AssessmentId}/{StudentId}", assessmentId, assessment.StudentId);
            assessment.MarkAnalysisFailed(now);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var testResults = await _executor.ToListAsync(
            _context.TestResults.Where(r => r.AssessmentId == assessmentId),
            cancellationToken).ConfigureAwait(false);

        var promptBuild = await _promptBuilder.BuildAsync(student, assessment, testResults, now, cancellationToken).ConfigureAwait(false);
        var promptVersion = requestedPromptVersion ?? promptBuild.PromptVersion;

        var piiTokens = BuildPiiTokens(student.FullName);

        var existingCurrent = await _executor.FirstOrDefaultAsync(
            _context.AiAnalyses.Where(a => a.AssessmentId == assessmentId && a.IsCurrent),
            cancellationToken).ConfigureAwait(false);

        var providerConfigs = await LoadProviderConfigsAsync(cancellationToken).ConfigureAwait(false);
        var providers = await BuildProviderChainAsync(requestedProvider, cancellationToken).ConfigureAwait(false);

        var attemptNumber = await GetNextAttemptNumberAsync(assessmentId, cancellationToken).ConfigureAwait(false);
        var allFinalOutcomesAreBadRequest = providers.Count > 0;
        AiAnalysis? lastFailedAttempt = null;

        using var jsonSchemaDocument = System.Text.Json.JsonDocument.Parse(AnalysisJsonSchema.RawJson);

        foreach (var provider in providers)
        {
            var outcome = await TryProviderWithRetriesAsync(provider).ConfigureAwait(false);

            if (outcome.Succeeded)
            {
                existingCurrent?.MarkNotCurrent();
                assessment.MarkAnalyzed(now);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "AI tahlil muvaffaqiyatli: {AssessmentId}, provider={Provider}, attempt={Attempt}",
                    assessmentId, provider.Kind, outcome.SucceededAnalysis!.AttemptNumber);
                return;
            }

            if (outcome.FinalErrorKind != AiErrorKind.BadRequest)
            {
                allFinalOutcomesAreBadRequest = false;
            }

            lastFailedAttempt = outcome.LastAnalysis;
        }

        // Hammasi muvaffaqiyatsiz — `docs/09` 7-bo'lim.
        assessment.MarkAnalysisFailed(now);

        // CLAUDE.md MAXSUS DIQQAT #1: hamma provider `BadRequest` bersa — umumiy "AI ishlamadi"
        // emas, aniq "so'rov shakli noto'g'ri" xabari oxirgi urinish yozuviga yoziladi.
        if (allFinalOutcomesAreBadRequest && lastFailedAttempt is not null)
        {
            lastFailedAttempt.OverrideErrorMessage(BadRequestAggregateMessage);
        }

        var fallbackContent = await FallbackReportBuilder.BuildAsync(assessmentId, testResults, _context, _executor, cancellationToken).ConfigureAwait(false);
        var fallbackProvider = providers.Count > 0 ? providers[0].Kind : AiProvider.Gemini;
        var fallbackReport = AiAnalysis.CreateFallbackReport(
            Guid.NewGuid(),
            assessmentId,
            fallbackProvider,
            promptVersion,
            attemptNumber,
            fallbackContent.Summary,
            fallbackContent.PersonalityPortrait,
            fallbackContent.StrengthsJson,
            fallbackContent.GrowthAreasJson,
            fallbackContent.CareerSuggestionsJson,
            fallbackContent.TeacherNotes,
            fallbackContent.ParentNotes,
            fallbackContent.AttentionFlagsJson,
            now);

        // Eski (haqiqiy) muvaffaqiyatli tahlil bo'lsa — shablon bilan YASHIRILMAYDI (`docs/09`
        // 11-bo'lim ruhi: shablon faqat HECH NARSA bo'lmaganda ko'rsatiladi).
        if (existingCurrent is null)
        {
            fallbackReport.MarkCurrent();
        }

        _context.Add(fallbackReport);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogError(
            "AI tahlil muvaffaqiyatsiz (barcha provayderlar): {AssessmentId}, zaxira shablon hisobot yaratildi",
            assessmentId);

        // --- Lokal funksiyalar -------------------------------------------------------------

        async Task<ProviderOutcome> TryProviderWithRetriesAsync(IAiAnalysisProvider provider)
        {
            var (model, maxOutputTokens, temperature) = ResolveRequestSettings(provider.Kind, providerConfigs);

            var networkRetryCount = 0;
            var contentAttempt = 1;
            string? correctiveNote = null;
            AiErrorKind lastErrorKind = AiErrorKind.Unknown;
            AiAnalysis? lastAnalysis = null;

            while (true)
            {
                var userText = correctiveNote is null ? promptBuild.UserText : $"{promptBuild.UserText}\n\n{correctiveNote}";

                var analysis = AiAnalysis.Create(Guid.NewGuid(), assessmentId, provider.Kind, model, promptVersion, now, attemptNumber, promptBuild.AnalysisInputJson);
                attemptNumber++;
                _context.Add(analysis);
                analysis.Start();

                var request = new AiCompletionRequest(promptBuild.SystemText, userText, jsonSchemaDocument, model, maxOutputTokens, temperature);

                AiCompletionResult result;
                try
                {
                    result = await provider.CompleteJsonAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Provayder o'zi HTTP darajasidagi xatolarni ushlaydi (`AiHttpExecutor`) —
                    // bu faqat kutilmagan (masalan konfiguratsiya) xatolarga qarshi himoya.
                    result = new AiCompletionResult(false, null, null, null, 0, ex.Message, AiErrorKind.Unknown);
                }

                if (!result.Success)
                {
                    analysis.Fail(Truncate(result.ErrorMessage ?? "AI provayderdan noma'lum xato."), now, result.DurationMs);
                    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    lastErrorKind = result.ErrorKind;
                    lastAnalysis = analysis;

                    if (result.ErrorKind is AiErrorKind.Auth or AiErrorKind.BadRequest or AiErrorKind.ModelNotFound)
                    {
                        // Retry YO'Q — darhol keyingi providerga (`docs/09` 7-bo'lim, `AiErrorKind` izohi).
                        // `ModelNotFound` ham shu guruhda: model nomi urinishlar orasida o'zgarmaydi.
                        return ProviderOutcome.Failed(lastErrorKind, lastAnalysis);
                    }

                    // RateLimit/Server/Timeout/Network/Unknown → backoff bilan qayta urinish.
                    if (networkRetryCount < NetworkRetryBackoffs.Length)
                    {
                        await _delay(NetworkRetryBackoffs[networkRetryCount], cancellationToken).ConfigureAwait(false);
                        networkRetryCount++;
                        continue;
                    }

                    return ProviderOutcome.Failed(lastErrorKind, lastAnalysis);
                }

                var validation = _validator.Validate(result.RawJson, piiTokens, contentAttempt);

                if (validation.Outcome == ValidationOutcome.Retry)
                {
                    validation.ParsedJson?.Dispose();
                    analysis.Fail(Truncate($"Validatsiya muvaffaqiyatsiz ({validation.FailedStage}): {validation.Message}"), now, result.DurationMs);
                    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    lastErrorKind = AiErrorKind.Schema;
                    lastAnalysis = analysis;

                    if (contentAttempt < MaxContentValidationAttemptsPerProvider)
                    {
                        correctiveNote = $"MUHIM: oldingi javobingiz rad etildi — sabab: {validation.Message} " +
                            "Iltimos, faqat berilgan JSON sxemasiga to'liq mos, yuqoridagi barcha qoidalarga rioya qilgan holda qayta javob ber.";
                        contentAttempt++;
                        continue;
                    }

                    return ProviderOutcome.Failed(lastErrorKind, lastAnalysis);
                }

                // Ok yoki Moderated — ikkalasi ham saqlanadi (`docs/09` 6-bo'lim, 3-band).
                using (validation.ParsedJson)
                {
                    var mapped = AiAnalysisResponseMapper.Map(validation.ParsedJson!.RootElement);
                    var cost = _costCalculator.EstimateCostUsd(provider.Kind, model, result.InputTokens, result.OutputTokens);

                    var attentionFlagsJson = validation.Outcome == ValidationOutcome.Moderated
                        ? AppendModerationFlag(mapped.AttentionFlagsJson)
                        : mapped.AttentionFlagsJson;

                    analysis.Succeed(
                        result.RawJson!,
                        mapped.Summary,
                        mapped.PersonalityPortrait,
                        now,
                        mapped.StrengthsJson,
                        mapped.GrowthAreasJson,
                        mapped.RecommendationsJson,
                        mapped.CareerSuggestionsJson,
                        mapped.TeacherNotes,
                        mapped.ParentNotes,
                        attentionFlagsJson,
                        result.InputTokens,
                        result.OutputTokens,
                        cost,
                        result.DurationMs);
                }

                // ⚠️ Eski `IsCurrent = true` yozuv shu YERDA, YANGISI bilan BIR XIL `SaveChangesAsync`
                // chaqiruvida `NotCurrent`ga o'tkaziladi — `ux_ai_analyses_current` filtrlangan
                // unique indeksi bir vaqtning o'zida ikkita `IsCurrent = true` yozuvga yo'l
                // qo'ymaydi (agar bu ikkalikni keyinga — chaqiruvchi metodga — qoldirsak, aynan
                // shu oraliqda `SaveChangesAsync` chaqirilib, unique cheklov buziladi).
                existingCurrent?.MarkNotCurrent();

                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return ProviderOutcome.Success(analysis);
            }
        }
    }

    /// <summary>`docs/09` §3: promptga ism yuborilmaydi, lekin validator baribir sizib ketmaganini tekshiradi — sizga tegishli so'zlar shu yerda ajratiladi.</summary>
    private static IReadOnlyList<string> BuildPiiTokens(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private async Task<int> GetNextAttemptNumberAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var existingAttemptNumbers = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AiAnalyses).Where(a => a.AssessmentId == assessmentId).Select(a => a.AttemptNumber),
            cancellationToken).ConfigureAwait(false);

        return existingAttemptNumbers.Count == 0 ? 1 : existingAttemptNumbers.Max() + 1;
    }

    private async Task<IReadOnlyDictionary<AiProvider, AiProviderConfig>> LoadProviderConfigsAsync(CancellationToken cancellationToken)
    {
        var configs = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AiProviderConfigs).Where(c => c.IsActive && c.ApiKeyEncrypted != null),
            cancellationToken).ConfigureAwait(false);

        return configs.ToDictionary(c => c.Provider);
    }

    private async Task<IReadOnlyList<IAiAnalysisProvider>> BuildProviderChainAsync(AiProvider? requestedProvider, CancellationToken cancellationToken)
    {
        try
        {
            var primary = await _providerResolver.ResolveAsync(requestedProvider, cancellationToken).ConfigureAwait(false);
            var fallback = await _providerResolver.GetFallbackChainAsync(primary.Kind, cancellationToken).ConfigureAwait(false);

            var chain = new List<IAiAnalysisProvider> { primary };
            chain.AddRange(fallback);
            return chain;
        }
        catch (InvalidOperationException)
        {
            // Bazada (`AiProviderConfig`) faol/kaliti bor provider YO'Q. Production'da bu —
            // haqiqatan ham "hech narsa sozlanmagan" holati. Dev/Test'da esa `_directProviders`
            // (`MockAiProvider`) orqali navbat/orkestratsiya oqimini TARMOQSIZ, HAQIQIY kalitsiz
            // uchidan-uchigacha sinash imkoni beriladi (`Api/Program.cs` izohi bilan bir xil naqsh).
            var direct = _directProviders.ToList();
            if (direct.Count > 0)
            {
                _logger.LogDebug("AnalysisOrchestrator: bazada faol AI provider yo'q — to'g'ridan-to'g'ri ro'yxatdan o'tgan provider(lar) ishlatiladi ({Count})", direct.Count);
            }
            else
            {
                _logger.LogWarning("AnalysisOrchestrator: {Message}", NoProviderMessage);
            }

            return direct;
        }
    }

    private static (string Model, int MaxOutputTokens, double Temperature) ResolveRequestSettings(
        AiProvider provider, IReadOnlyDictionary<AiProvider, AiProviderConfig> configs)
    {
        if (configs.TryGetValue(provider, out var config))
        {
            return (config.Model, config.MaxOutputTokens, (double)config.Temperature);
        }

        // Bazada sozlama yo'q (masalan `_directProviders` — Dev/Test) — `AiProviderConfig.Create`
        // standart qiymatlari bilan bir xil taxminiy sozlama.
        var defaultModel = provider switch
        {
            AiProvider.Gemini => RecommendedAiModels.Gemini,
            AiProvider.OpenAi => RecommendedAiModels.OpenAi,
            AiProvider.Anthropic => RecommendedAiModels.Anthropic,
            _ => "unknown",
        };

        return (defaultModel, 4096, 0.4);
    }

    private static string Truncate(string message) =>
        message.Length <= MaxErrorMessageLength ? message : string.Concat(message.AsSpan(0, MaxErrorMessageLength - 1), "…");

    private static string AppendModerationFlag(string attentionFlagsJson)
    {
        var flags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(attentionFlagsJson) ?? [];
        flags.Add("MODERATION_REQUIRED: taqiqlangan atama ikkinchi urinishda ham topildi.");
        return System.Text.Json.JsonSerializer.Serialize(flags);
    }

    private readonly record struct ProviderOutcome(bool Succeeded, AiErrorKind FinalErrorKind, AiAnalysis? SucceededAnalysis, AiAnalysis? LastAnalysis)
    {
        public static ProviderOutcome Success(AiAnalysis analysis) => new(true, AiErrorKind.None, analysis, analysis);

        public static ProviderOutcome Failed(AiErrorKind errorKind, AiAnalysis? lastAnalysis) => new(false, errorKind, null, lastAnalysis);
    }
}
