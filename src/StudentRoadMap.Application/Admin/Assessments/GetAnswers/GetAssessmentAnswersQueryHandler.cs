using MediatR;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Admin.Assessments.GetAnswers;

/// <summary>
/// `docs/07` 3.3-bo'lim, `prompts/15` MAXSUS DIQQAT #5. Read-only — `AsNoTracking`.
///
/// <para>
/// <b>Nima o'zgardi (2026-09-03, egasining talabi).</b> Avval bu handler faqat XOM javobni
/// (`rawValue = 5`) qaytarardi; javobning MA'NOSI — qaysi shkalaga tegishli, teskari savolmi,
/// shkalaga qanday tushgani — yo'q edi. Teskari savolga berilgan `5` shkalaga `1` bo'lib
/// tushadi, ya'ni xom qiymatni ko'rgan psixolog javobni BUTUNLAY teskari o'qirdi. Endi har
/// qatorda `Scale`/`ScaleNameUz`/`ScaleDirection`/`Weight`/`EffectiveValue` bor.
/// </para>
/// <para>
/// <b>Formula qayta yozilmagan.</b> `EffectiveValue` — <see cref="ScoringMath.ApplyDirection"/>,
/// diapazon — <see cref="ScoringMath.GetLikertBounds"/>; ikkalasi ham `BigFiveStrategy`/
/// `ActivityStrategy`/`SumStrategy` ishlatadigan AYNAN O'SHA domen funksiyalari. Xronologik
/// tartib — <see cref="ReliabilityInputBuilder"/> (P12-R1), ya'ni straight-lining seriyasi
/// `ReliabilityCalculator` ko'radigan ketma-ketlikning O'ZIDA qidiriladi. Chegaralar —
/// <see cref="ScoringConstants"/>, javobda mijozga uzatiladi.
/// </para>
/// <para>
/// Bog'langan hajm (≤ ~190 javob/sessiya, `docs/05` §5) — barcha lug'atlar (savollar,
/// variantlar, test kodlari, shkalalar) SAHIFA emas, BUTUN sessiya doirasida, lekin har biri
/// kamida bitta (sikl ichida so'rovsiz) batch so'rov bilan yuklanadi.
/// </para>
/// </summary>
internal sealed class GetAssessmentAnswersQueryHandler : IRequestHandler<GetAssessmentAnswersQuery, Result<AdminAssessmentAnswersDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetAssessmentAnswersQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    /// <summary>`ScoringConstants` dagi chegaralar — mijozda takrorlanmasin uchun javobga qo'shiladi.</summary>
    private static readonly AdminAnswerThresholdsDto Thresholds = new(
        ScoringConstants.FastAnswerDurationThresholdMs,
        ScoringConstants.StraightLiningMinRunLength,
        ScoringConstants.ShortSessionThresholdMinutes);

    /// <summary>Bitta savolning javob tahlili uchun kerakli xom ma'lumoti.</summary>
    private sealed record AnswerRow(
        Guid QuestionId,
        Guid AssessmentTestId,
        string TestCode,
        int SessionOrder,
        int QuestionOrder,
        string QuestionCode,
        string QuestionText,
        QuestionType QuestionType,
        string Scale,
        string? ScaleNameUz,
        int ScaleDirection,
        decimal Weight,
        int RawValue,
        string? SelectedOptionText,
        int DurationMs,
        int RevisionCount,
        DateTimeOffset AnsweredAt,
        bool IsScored);

    public async Task<Result<AdminAssessmentAnswersDto>> Handle(GetAssessmentAnswersQuery request, CancellationToken cancellationToken)
    {
        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments)
                .Where(a => a.Id == request.Id)
                .Select(a => new { a.Id, a.TotalDurationSeconds, a.ReliabilityScore, a.ReliabilityFlag }),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<AdminAssessmentAnswersDto>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        var sessionDuration = TimeSpan.FromSeconds(assessment.TotalDurationSeconds ?? 0);
        var emptySignals = new AdminAnswerSessionSignalsDto(
            0, 0, 0, AllSameAnswer: false,
            ShortSession: false,
            assessment.TotalDurationSeconds,
            assessment.ReliabilityScore,
            assessment.ReliabilityFlag?.ToString());

        var rows = await LoadRowsAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return Result.Success(new AdminAssessmentAnswersDto([], emptySignals, [], Thresholds));
        }

        // --- Sessiya darajasidagi signallar: FAQAT ballanadigan (`Scored`) bloklar ---
        // `RecalculateAssessmentScoresCommandHandler` ham `Survey` bloklarini
        // `ReliabilityCalculator`dan chiqarib tashlaydi (ballanmagan javobda teskari savol
        // tushunchasi yo'q) — ikki joy bir xil to'plamda ishlashi shart.
        var scoredRows = rows.Where(r => r.IsScored).ToList();

        var orderedScored = OrderChronologically(scoredRows, sessionDuration);
        var values = orderedScored.Select(r => r.RawValue).ToList();

        var allSame = values.Count > 0 && values.TrueForAll(v => v == values[0]);

        // `docs/03` §7.1 band 1: "hammasi bir xil" va straight-lining bir-birini istisno
        // qiladi — UI ham shu bilan izchil, aks holda bitta xatti-harakat ikki marta
        // belgilangandek ko'rinardi.
        var blockIndexByQuestionId = allSame
            ? new Dictionary<Guid, int>()
            : MarkStraightLiningBlocks(orderedScored);

        var fastCount = scoredRows.Count(r => r.DurationMs < ScoringConstants.FastAnswerDurationThresholdMs);

        var signals = new AdminAnswerSessionSignalsDto(
            scoredRows.Count,
            fastCount,
            blockIndexByQuestionId.Count == 0 ? 0 : blockIndexByQuestionId.Values.Max(),
            allSame,
            sessionDuration.TotalMinutes < ScoringConstants.ShortSessionThresholdMinutes,
            assessment.TotalDurationSeconds,
            assessment.ReliabilityScore,
            assessment.ReliabilityFlag?.ToString());

        var scaleSignals = BuildScaleSignals(scoredRows);

        // --- Qatorlar: `testCode` filtri FAQAT shu yerda qo'llanadi ---
        var visibleRows = string.IsNullOrWhiteSpace(request.TestCode)
            ? rows
            : rows.Where(r => string.Equals(r.TestCode, request.TestCode, StringComparison.OrdinalIgnoreCase)).ToList();

        var answers = visibleRows
            // Audit uchun tushunarli tartib: avval sessiya ICHIDAGI test tartibi, so'ng
            // TEST ICHIDAGI savol tartibi (ikkalasi ham `int` — SQLite `ORDER BY` cheklovi
            // faqat `DateTimeOffset`ga tegishli, bu yerda muammo yo'q).
            .OrderBy(r => r.SessionOrder)
            .ThenBy(r => r.QuestionOrder)
            .Select(r => new AdminAssessmentAnswerDto(
                r.QuestionId,
                r.QuestionCode,
                r.TestCode,
                r.QuestionText,
                r.RawValue,
                r.SelectedOptionText,
                r.DurationMs,
                r.RevisionCount,
                r.AnsweredAt,
                r.QuestionType.ToString(),
                r.Scale,
                r.ScaleNameUz,
                r.ScaleDirection,
                r.Weight,
                EffectiveValueOf(r),
                r.DurationMs < ScoringConstants.FastAnswerDurationThresholdMs,
                blockIndexByQuestionId.TryGetValue(r.QuestionId, out var blockIndex) ? blockIndex : null))
            .ToList();

        return Result.Success(new AdminAssessmentAnswersDto(answers, signals, scaleSignals, Thresholds));
    }

    /// <summary>
    /// Teskari tuzatilgan qiymat — <see cref="ScoringMath.ApplyDirection"/> (`docs/03` §1).
    /// Formula bu yerda TAKRORLANMAYDI: diapazon ham domen funksiyasidan olinadi, shu sabab
    /// `Likert7` savollar (superadmin anketalari) ham avtomatik to'g'ri hisoblanadi.
    /// </summary>
    private static int EffectiveValueOf(AnswerRow row)
    {
        var (min, max) = ScoringMath.GetLikertBounds(row.QuestionType);
        return ScoringMath.ApplyDirection(row.RawValue, row.ScaleDirection, min, max);
    }

    /// <summary>
    /// ⚠️ P12-R1: xronologik tartib <see cref="ReliabilityInputBuilder"/> orqali quriladi —
    /// butun ro'yxatga bittalikda `.OrderBy(q => q.DisplayOrder)` QO'LLANMAYDI (`DisplayOrder`
    /// har testda `1..N` dan qayta boshlanadi va 4 blokni aralashtirib yuborardi). Shu bilan
    /// bu yerdagi straight-lining seriyasi `ReliabilityCalculator` ko'radigan ketma-ketlikning
    /// AYNAN O'ZI bo'ladi.
    /// </summary>
    private static List<AnswerRow> OrderChronologically(IReadOnlyList<AnswerRow> scoredRows, TimeSpan sessionDuration)
    {
        var rowByQuestionId = scoredRows.ToDictionary(r => r.QuestionId);
        var answers = scoredRows.ToDictionary(r => r.QuestionId, r => r.RawValue);
        var durations = scoredRows.ToDictionary(r => r.QuestionId, r => r.DurationMs);

        var blocks = scoredRows
            .GroupBy(r => r.AssessmentTestId)
            .Select(g => new ReliabilityInputBuilder.TestBlock(
                g.First().SessionOrder,
                g.Select(r => new QuestionMeta(
                    r.QuestionId, r.QuestionCode, r.Scale, r.ScaleDirection, r.Weight, r.QuestionType, r.QuestionOrder))
                    .ToList()))
            .ToList();

        var input = ReliabilityInputBuilder.Build(blocks, answers, durations, sessionDuration);

        return input.Questions.Select(q => rowByQuestionId[q.QuestionId]).ToList();
    }

    /// <summary>
    /// Har TO'LIQ <see cref="ScoringConstants.StraightLiningMinRunLength"/> talik ketma-ket bir
    /// xil qiymat alohida blok (`docs/03` §7.1 band 2 va `ReliabilityCalculator.
    /// CalculateStraightLiningPenalty` bilan bir xil sanoq: 36 ta ketma-ket → 3 blok).
    /// Qaytadi: `QuestionId → blok tartib raqami (1 dan)`; blokka tushmagan javob ro'yxatda yo'q.
    /// </summary>
    private static Dictionary<Guid, int> MarkStraightLiningBlocks(IReadOnlyList<AnswerRow> ordered)
    {
        var result = new Dictionary<Guid, int>();
        var blockCount = 0;
        var runStart = 0;

        for (var i = 1; i <= ordered.Count; i++)
        {
            var continuesRun = i < ordered.Count && ordered[i].RawValue == ordered[i - 1].RawValue;
            if (continuesRun)
            {
                continue;
            }

            var runLength = i - runStart;
            var blocksInRun = runLength / ScoringConstants.StraightLiningMinRunLength;
            for (var block = 0; block < blocksInRun; block++)
            {
                blockCount++;
                var from = runStart + (block * ScoringConstants.StraightLiningMinRunLength);
                for (var offset = 0; offset < ScoringConstants.StraightLiningMinRunLength; offset++)
                {
                    result[ordered[from + offset].QuestionId] = blockCount;
                }
            }

            runStart = i;
        }

        return result;
    }

    /// <summary>
    /// Shkala darajasidagi teskari savol ziddiyati (`docs/03` §7.1 band 4): musbat
    /// yo'nalishli savollarning normallashgan o'rtachasi ↔ teskari TUZATILGAN manfiylarning
    /// o'rtachasi. Tuzatish <see cref="ScoringMath.ApplyDirection"/> orqali; normallashtirish
    /// (`(v − min) / (max − min)`) — bu yerdagi ko'rsatish hisobi, jarima EMAS (jarima
    /// `ReliabilityCalculator` da, `d × 25`). Faqat IKKALA yo'nalish ham bor shkalalar
    /// qaytadi — bittasi yetishmasa `d` aniqlanmaydi (teskari savoli yo'q `RIASEC` kabi).
    /// </summary>
    private static List<AdminAnswerScaleSignalDto> BuildScaleSignals(IReadOnlyList<AnswerRow> scoredRows)
    {
        var signals = new List<AdminAnswerScaleSignalDto>();

        foreach (var group in scoredRows
            .GroupBy(r => r.Scale, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var forward = new List<double>();
            var reverse = new List<double>();
            string? scaleNameUz = null;

            foreach (var row in group)
            {
                scaleNameUz ??= row.ScaleNameUz;

                var (min, max) = ScoringMath.GetLikertBounds(row.QuestionType);
                // `GetLikertBounds` doim ijobiy diapazon qaytaradi (Likert5: 4, Likert7: 6) —
                // nol bilan bo'lish holati mumkin emas.
                var normalized = (EffectiveValueOf(row) - min) / (double)(max - min);

                if (row.ScaleDirection > 0)
                {
                    forward.Add(normalized);
                }
                else
                {
                    reverse.Add(normalized);
                }
            }

            if (forward.Count == 0 || reverse.Count == 0)
            {
                continue;
            }

            var forwardAvg = forward.Average();
            var reverseAvg = reverse.Average();

            signals.Add(new AdminAnswerScaleSignalDto(
                group.Key,
                scaleNameUz,
                forward.Count,
                reverse.Count,
                Math.Round(forwardAvg * 100.0, ScoringConstants.RoundingDecimals),
                Math.Round(reverseAvg * 100.0, ScoringConstants.RoundingDecimals),
                Math.Round(Math.Abs(forwardAvg - reverseAvg) * 100.0, ScoringConstants.RoundingDecimals)));
        }

        return signals;
    }

    /// <summary>Sessiyaning BARCHA javoblarini metama'lumoti bilan yuklaydi (filtrsiz).</summary>
    private async Task<List<AnswerRow>> LoadRowsAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var assessmentTests = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AssessmentTests)
                .Where(t => t.AssessmentId == assessmentId)
                .Select(t => new { t.Id, t.TestDefinitionId, t.DisplayOrder }),
            cancellationToken).ConfigureAwait(false);

        if (assessmentTests.Count == 0)
        {
            return [];
        }

        var testDefinitionIds = assessmentTests.Select(t => t.TestDefinitionId).Distinct().ToList();
        var testDefinitions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestDefinitions)
                .Where(t => testDefinitionIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Code, t.ScoringStrategyCode, t.ScoringMode }),
            cancellationToken).ConfigureAwait(false);
        var testDefinitionById = testDefinitions.ToDictionary(t => t.Id);

        // Shkala nomi — `CatalogScaleNameResolver` (`docs/07` §3.4): anketaning O'Z shkalasi
        // (`TestScale.NameUz`, faqat `Custom`) → `SystemScaleCatalog` (tizim metodikasi,
        // `docs/03`) → `null`. Ikkinchi nusxa YOZILMAYDI — admin katalogi bilan bitta manba.
        var testScales = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestScales).Where(s => testDefinitionIds.Contains(s.TestDefinitionId)),
            cancellationToken).ConfigureAwait(false);
        var scalesByTestDefinitionId = testScales
            .GroupBy(s => s.TestDefinitionId)
            .ToDictionary(g => g.Key, g => (IEnumerable<TestScale>)g.ToList());

        var resolverByTestDefinitionId = testDefinitionIds.ToDictionary(
            id => id,
            id => CatalogScaleNameResolver.Create(
                testDefinitionById.GetValueOrDefault(id)?.ScoringStrategyCode,
                scalesByTestDefinitionId.GetValueOrDefault(id, [])));

        var assessmentTestIds = assessmentTests.Select(t => t.Id).ToList();
        var answers = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Answers).Where(a => assessmentTestIds.Contains(a.AssessmentTestId)),
            cancellationToken).ConfigureAwait(false);

        if (answers.Count == 0)
        {
            return [];
        }

        var questionIds = answers.Select(a => a.QuestionId).Distinct().ToList();
        var questions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.Questions)
                .Where(q => questionIds.Contains(q.Id))
                .Select(q => new
                {
                    q.Id,
                    q.Code,
                    q.TextUz,
                    q.DisplayOrder,
                    q.QuestionType,
                    q.Scale,
                    q.ScaleDirection,
                    q.Weight,
                }),
            cancellationToken).ConfigureAwait(false);
        var questionById = questions.ToDictionary(q => q.Id);

        var optionIds = answers.Where(a => a.SelectedOptionId.HasValue).Select(a => a.SelectedOptionId!.Value).Distinct().ToList();
        var options = optionIds.Count == 0
            ? []
            : await _executor.ToListAsync(
                _context.AsNoTracking(_context.AnswerOptions).Where(o => optionIds.Contains(o.Id)).Select(o => new { o.Id, o.TextUz }),
                cancellationToken).ConfigureAwait(false);
        var optionTextById = options.ToDictionary(o => o.Id, o => o.TextUz);

        var testByAssessmentTestId = assessmentTests.ToDictionary(t => t.Id);

        var rows = new List<AnswerRow>(answers.Count);
        foreach (var answer in answers)
        {
            if (!questionById.TryGetValue(answer.QuestionId, out var question)
                || !testByAssessmentTestId.TryGetValue(answer.AssessmentTestId, out var assessmentTest))
            {
                continue;
            }

            var definition = testDefinitionById.GetValueOrDefault(assessmentTest.TestDefinitionId);
            var resolver = resolverByTestDefinitionId.GetValueOrDefault(assessmentTest.TestDefinitionId, CatalogScaleNameResolver.None);

            rows.Add(new AnswerRow(
                answer.QuestionId,
                answer.AssessmentTestId,
                definition?.Code ?? string.Empty,
                assessmentTest.DisplayOrder,
                question.DisplayOrder,
                question.Code,
                question.TextUz,
                question.QuestionType,
                question.Scale,
                resolver.Resolve(question.Scale),
                question.ScaleDirection,
                question.Weight,
                // `docs/18` §2.7: `Answer.RawValue` endi `int?` (matn/ko'p tanlov javoblari uchun
                // `null`). Bu qator faqat `IsScored` (Scored) bloklar uchun signal/tahlil
                // hisoblashda ishlatiladi (pastda `scoredRows`/`orderedScored` filtri) — u yerda
                // `RawValue` har doim to'ldirilgan (B-1). `Survey` javoblarining matn/tanlov
                // shaklini shu admin jadvalida ko'rsatish P52 A2 (Application) vazifasi doirasida;
                // hozircha `0` — faqat ko'rinish uchun, hisoblashga ta'sir qilmaydi.
                answer.RawValue ?? 0,
                answer.SelectedOptionId.HasValue ? optionTextById.GetValueOrDefault(answer.SelectedOptionId.Value) : null,
                answer.DurationMs,
                answer.RevisionCount,
                answer.AnsweredAt,
                definition?.ScoringMode != TestScoringMode.Survey));
        }

        return rows;
    }
}
