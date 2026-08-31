using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// Mustaqil QA sharhi (FAIL verdikti) bo'yicha tuzatilgan ikkita bloklovchi xato va PM
/// tomonidan tasdiqlangan/talab qilingan qo'shimcha tuzatishlar uchun regressiya testlari.
/// </summary>
public sealed class QaFollowUpFixesTests
{
    // ============================================================
    // BLOKLOVCHI 1 — Mbti16Strategy borderline chegarasi IEEE-754 xatosi.
    // ============================================================

    [Fact]
    public void Mbti16_RealShapedFifteenQuestionAxis_AxisRaw48_FlagsBorderlineDespiteFloatingPointDrift()
    {
        // QA aynan shu holatni keltirdi: 15 ta savolli o'q, hammasi `direction=+1`
        // (axisMin=15*1=15, axisMax=15*5=75, range=60). axisRaw=48 => (48-15)/60*100
        // IEEE-754'da aynan 55.0 emas, balki 55.00000000000001 bo'lib chiqadi (Python bilan
        // tasdiqlangan: `(48-15)/(75-15)*100 == 55.00000000000001`). Tuzatishdan oldingi
        // kod xom qiymatni solishtirar edi (`axisPctRaw <= 55.0` => False) — bayroq
        // QO'YILMAS EDI. Endi solishtiruv `ScorePercent.FromClamped` bilan yaxlitlangan
        // qiymat ustida bajariladi.
        var questions = BuildSingleAxis("EI", direction: 1, count: 15);
        // 12 ta javob = 3, 3 ta javob = 4 => yig'indi = 12*3 + 3*4 = 36 + 12 = 48.
        var values = Enumerable.Repeat(3, 12).Concat(Enumerable.Repeat(4, 3)).ToArray();
        var answers = questions.Select((q, i) => (q.QuestionId, v: values[i])).ToDictionary(x => x.QuestionId, x => x.v);
        var input = BuildFullMbtiInputWithOneRealAxis("EI", questions, answers);

        var result = new Mbti16Strategy().Score(input);

        result.NormalizedScores["EI"].Should().Be(55.0);
        result.Flags.Should().Contain("Borderline:EI");
    }

    [Fact]
    public void Mbti16_RealShapedFifteenQuestionAxis_AxisRaw42_FlagsBorderlineAtLowerBound()
    {
        // Xuddi shu o'q shakli: axisRaw=42 => (42-15)/60*100=45.0 — pastki chegara.
        var questions = BuildSingleAxis("EI", direction: 1, count: 15);
        // 12 ta javob = 3, 3 ta javob = 2 => yig'indi = 12*3 + 3*2 = 36 + 6 = 42.
        var values = Enumerable.Repeat(3, 12).Concat(Enumerable.Repeat(2, 3)).ToArray();
        var answers = questions.Select((q, i) => (q.QuestionId, v: values[i])).ToDictionary(x => x.QuestionId, x => x.v);
        var input = BuildFullMbtiInputWithOneRealAxis("EI", questions, answers);

        var result = new Mbti16Strategy().Score(input);

        result.NormalizedScores["EI"].Should().Be(45.0);
        result.Flags.Should().Contain("Borderline:EI");
    }

    [Fact]
    public void Mbti16_RealShapedEightPosSevenNegAxis_AxisRaw6_FlagsBorderlineDespiteFloatingPointDrift()
    {
        // Haqiqiy seed shakliga yaqinroq: 8 musbat + 7 manfiy (masalan haqiqiy `EI` o'qi).
        // axisMin=8*1+7*(-5)=-27, axisMax=8*5+7*(-1)=33, range=60.
        // axisRaw=6 => (6-(-27))/60*100 IEEE-754'da 55.00000000000001 (Python bilan
        // tasdiqlangan).
        var pos = Enumerable.Range(0, 8)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"EI-P{i}", "EI", 1, 1.0m, QuestionType.Likert5, i));
        var neg = Enumerable.Range(0, 7)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"EI-N{i}", "EI", -1, 1.0m, QuestionType.Likert5, 8 + i));
        var questions = pos.Concat(neg).ToList();

        // Musbat: 8 ta javob = 3 => yig'indi=24. Manfiy: to'rttasi=3, uchtasi=2 => yig'indi=18.
        // axisRaw = 24 - 18 = 6.
        var answers = new Dictionary<Guid, int>();
        foreach (var q in questions.Take(8))
        {
            answers[q.QuestionId] = 3;
        }

        var negValues = new[] { 3, 3, 3, 3, 2, 2, 2 };
        for (var i = 0; i < 7; i++)
        {
            answers[questions[8 + i].QuestionId] = negValues[i];
        }

        var input = BuildFullMbtiInputWithOneRealAxis("EI", questions, answers);

        var result = new Mbti16Strategy().Score(input);

        result.NormalizedScores["EI"].Should().Be(55.0);
        result.Flags.Should().Contain("Borderline:EI");
    }

    private static List<QuestionMeta> BuildSingleAxis(string axis, int direction, int count) =>
        Enumerable.Range(0, count)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"{axis}-{i}", axis, direction, 1.0m, QuestionType.Likert5, i))
            .ToList();

    /// <summary>Bitta o'q haqiqiy (15 savolli) shaklda, qolgan 3 o'q sodda (4 savol, tie-break'siz).</summary>
    private static ScoringInput BuildFullMbtiInputWithOneRealAxis(string realAxis, List<QuestionMeta> realAxisQuestions, Dictionary<Guid, int> realAxisAnswers)
    {
        var questions = new List<QuestionMeta>(realAxisQuestions);
        var answers = new Dictionary<Guid, int>(realAxisAnswers);

        foreach (var axis in new[] { "SN", "TF", "JP" })
        {
            if (axis == realAxis)
            {
                continue;
            }

            for (var i = 0; i < 4; i++)
            {
                var direction = i % 2 == 0 ? 1 : -1;
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{axis}-{i}", axis, direction, 1.0m, QuestionType.Likert5, i));
                answers[id] = 4; // aniq qutb tomon, borderline emas.
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    // ============================================================
    // BLOKLOVCHI 2 — ReliabilityCalculator sessiya darajasida test bloklarini aralashtirishi.
    // (Haqiqiy 190 savolli bank bilan regressiya — `RealQuestionBankTests.
    // ReliabilityCalculator_RealSeedSession_StraightLinedTestBlockIsDetected`.)
    // ============================================================

    [Fact]
    public void ReliabilityCalculator_QaReproduction_TwoBlocksWithOverlappingDisplayOrder_BothDetected()
    {
        // QA reproduksiyasi so'zma-so'z: "12 ta ketma-ket 5, keyin 12 ta ketma-ket 1 (haqiqatda
        // 2 blok = 20 jarima) → oldingi versiyada score=100, Reliable, reasons=[]".
        // Ikkala "test" blokining `DisplayOrder`i ATAYLAB bir xil (1..12) — haqiqiy sessiyada
        // MBTI16 (1..60) va BIG5 (1..50) kabi navbatdosh testlarning DisplayOrder'i mos kelib
        // qolishini simulyatsiya qiladi. `ReliabilityInput.Questions` — sessiya (xronologik)
        // tartibi: test1 to'liq, keyin test2 to'liq.
        var block1 = Enumerable.Range(1, 12)
            .Select(order => new QuestionMeta(Guid.NewGuid(), $"T1-Q{order}", "SCALE", 1, 1.0m, QuestionType.Likert5, order))
            .ToList();
        var block2 = Enumerable.Range(1, 12)
            .Select(order => new QuestionMeta(Guid.NewGuid(), $"T2-Q{order}", "SCALE", 1, 1.0m, QuestionType.Likert5, order))
            .ToList();

        var questions = block1.Concat(block2).ToList();
        var answers = new Dictionary<Guid, int>();
        foreach (var q in block1)
        {
            answers[q.QuestionId] = 5;
        }

        foreach (var q in block2)
        {
            answers[q.QuestionId] = 1;
        }

        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);
        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10));

        var result = ReliabilityCalculator.Calculate(input);

        // 2 ta alohida to'liq 12talik blok => floor(12/12)*2 = 2 blok => 2*10 = 20 jarima.
        result.Score.Should().Be(80.0);
        result.Flag.Should().Be(ReliabilityFlag.Reliable);
        result.Reasons.Should().Equal("StraightLining");
    }

    [Fact]
    public void ReliabilityCalculator_QuestionsListOrder_IsNotReSortedByDisplayOrder()
    {
        // To'g'ridan-to'g'ri kontraktni tekshiradi: `Questions` ro'yxatining o'zi (kirish
        // tartibi) ishlatiladi, `DisplayOrder` bo'yicha QAYTA TARTIBLANMAYDI. Kirish ro'yxatida
        // DisplayOrder KAMAYISH tartibida (5,4,3,...) berilsa ham, straight-lining kirish
        // RO'YXATI tartibiga qarab hisoblanishi kerak.
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        // Ro'yxat tartibi: 12 ta "5" (DisplayOrder kamayib boradi), keyin 12 ta "1".
        for (var i = 0; i < 12; i++)
        {
            var id = Guid.NewGuid();
            questions.Add(new QuestionMeta(id, $"A{i}", "SCALE", 1, 1.0m, QuestionType.Likert5, 12 - i));
            answers[id] = 5;
        }

        for (var i = 0; i < 12; i++)
        {
            var id = Guid.NewGuid();
            questions.Add(new QuestionMeta(id, $"B{i}", "SCALE", 1, 1.0m, QuestionType.Likert5, 12 - i));
            answers[id] = 1;
        }

        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);
        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10));

        var result = ReliabilityCalculator.Calculate(input);

        // Agar `DisplayOrder` bo'yicha qayta saralansa, "5" va "1" guruhlari bir-biriga
        // aralashib ketardi (ikkalasi ham DisplayOrder 1..12 oralig'ida) va straight-lining
        // YO'QOLARDI. Ro'yxat tartibiga ishonilsa — 2 blok, 20 jarima, score=80 to'g'ri chiqadi.
        result.Score.Should().Be(80.0);
        result.Reasons.Should().Equal("StraightLining");
    }

    // ============================================================
    // PM QARORI — ketma-ket bir xil javob bloklarini sanash: har to'liq 12talik seriya
    // alohida blok (masalan 36 → 3 blok → 30, eski talqinda 1 blok → 10 edi).
    // ============================================================

    [Fact]
    public void ReliabilityCalculator_ThirtySixConsecutiveIdenticalAnswers_CountsAsThreeBlocksNotOne()
    {
        // 36 ta ketma-ket "5", keyin qisqa xilma-xil "quyruq" — sessiya to'liq bir xil bo'lib
        // qolmasligi uchun (aks holda "AllSameAnswer" ishga tushadi, straight-lining emas).
        var values = Enumerable.Repeat(5, 36).Concat([1, 2, 3, 4, 5]).ToArray();
        var (questions, answers) = BuildSequential(values.Length, i => values[i]);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);
        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10));

        var result = ReliabilityCalculator.Calculate(input);

        // floor(36/12) = 3 blok => 3*10 = 30 jarima (yangi cap'ga teng qiymatga to'g'ri keladi).
        result.Score.Should().Be(70.0);
        result.Reasons.Should().Equal("StraightLining");
    }

    [Fact]
    public void ReliabilityCalculator_ThirteenConsecutiveIdenticalAnswers_StillCountsAsOneBlock()
    {
        // 13 — 12dan bitta ortiq, lekin ikkinchi TO'LIQ 12talikni hosil qilmaydi
        // (floor(13/12)=1) — hali ham 1 blok, 10 jarima. Qisqa xilma-xil "quyruq" —
        // sessiya to'liq bir xil bo'lib qolmasligi uchun.
        var values = Enumerable.Repeat(5, 13).Concat([1, 2, 3, 4, 5]).ToArray();
        var (questions, answers) = BuildSequential(values.Length, i => values[i]);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);
        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10));

        var result = ReliabilityCalculator.Calculate(input);

        result.Score.Should().Be(90.0);
        result.Reasons.Should().Equal("StraightLining");
    }

    private static (List<QuestionMeta> Questions, Dictionary<Guid, int> Answers) BuildSequential(int count, Func<int, int> valueSelector)
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            questions.Add(new QuestionMeta(id, $"Q{i}", "SCALE", 1, 1.0m, QuestionType.Likert5, i));
            answers[id] = valueSelector(i);
        }

        return (questions, answers);
    }

    // ============================================================
    // M1 — javob diapazoni validatsiyasi.
    // ============================================================

    [Theory]
    [InlineData(9)]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(6)]
    public void Mbti16_WithAnswerOutsideLikert5Range_ThrowsDomainException(int badValue)
    {
        var questions = BuildSingleAxis("EI", 1, 4).Concat(BuildAxes(["SN", "TF", "JP"])).ToList();
        var answers = questions.ToDictionary(q => q.QuestionId, _ => 3);
        answers[questions[0].QuestionId] = badValue;

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new Mbti16Strategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_ANSWER_OUT_OF_RANGE");
    }

    [Fact]
    public void Activity_WithAnswerAboveRange_ThrowsDomainException()
    {
        // QA misoli: "ACTIVITY: MOT=200" — bitta savolga 200 kelsa (jismonan mumkin bo'lmagan,
        // lekin ommaviy API'dan buzilgan JSON kelishi mumkin) endi aniq xato beriladi.
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var scale in new[] { "MOT", "SELF", "SOCA", "ENG" })
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{scale}-{i}", scale, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = scale == "MOT" && i == 0 ? 200 : 3;
            }
        }

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new ActivityStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_ANSWER_OUT_OF_RANGE");
    }

    [Fact]
    public void Sum_WithAnswerOutsideLikert7Range_ThrowsDomainException()
    {
        var questions = Enumerable.Range(0, 4)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"Q{i}", "SCALE", 1, 1.0m, QuestionType.Likert7, i))
            .ToList();
        var answers = questions.ToDictionary(q => q.QuestionId, _ => 4);
        answers[questions[0].QuestionId] = 8; // Likert7 diapazoni [1,7].

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new SumStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_ANSWER_OUT_OF_RANGE");
    }

    private static List<QuestionMeta> BuildAxes(IEnumerable<string> axes) =>
        axes.SelectMany(axis => Enumerable.Range(0, 4)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"{axis}-{i}", axis, i % 2 == 0 ? 1 : -1, 1.0m, QuestionType.Likert5, i)))
            .ToList();

    // ============================================================
    // M4 — FastAnswerPenalty maxraji javoblar soni bo'lishi kerak (Durations.Count emas).
    // ============================================================

    [Fact]
    public void ReliabilityCalculator_PartialDurations_DenominatorIsAnswerCountNotDurationCount()
    {
        // 20 ta javob, lekin faqat 1 tasining (tez) davomiyligi ma'lum. Eski (xato) versiyada
        // maxraj `durations.Count`=1 bo'lib, p=1/1=100% => 40 jarima chiqar edi. To'g'ri
        // versiyada maxraj `answers.Count`=20 => p=1/20=0.05 => jarima=0.05*100*0.8=4.0.
        var (questions, answers) = BuildSequential(20, i => 1 + i % 5);
        var fastId = questions[0].QuestionId;
        var durations = new Dictionary<Guid, int> { [fastId] = 500 };

        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10));

        var result = ReliabilityCalculator.Calculate(input);

        result.Score.Should().Be(96.0);
        result.Reasons.Should().Equal("FastAnswers");
    }

    [Fact]
    public void ReliabilityCalculator_AllAnswersHaveDurations_BehavesAsBefore()
    {
        // Regressiya: barcha javobning davomiyligi bo'lsa (odatiy holat), natija o'zgarmaydi.
        var (questions, answers) = BuildSequential(20, i => 1 + i % 5);
        const int fastCount = 6;
        var durations = questions.Select((q, i) => (q.QuestionId, ms: i < fastCount ? 500 : 2000))
            .ToDictionary(x => x.QuestionId, x => x.ms);

        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10));

        var result = ReliabilityCalculator.Calculate(input);

        result.Score.Should().Be(76.0);
    }

    // ============================================================
    // M5 — SumStrategy: barcha Weight=0 bo'lsa DomainException (DivideByZeroException emas).
    // ============================================================

    [Fact]
    public void Sum_AllWeightsZeroInScale_ThrowsDomainExceptionNotDivideByZero()
    {
        var questions = Enumerable.Range(0, 4)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"Q{i}", "SCALE", 1, 0.0m, QuestionType.Likert5, i))
            .ToList();
        var answers = questions.ToDictionary(q => q.QuestionId, _ => 3);

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new SumStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SUM_SCALE_WEIGHT_ZERO");
    }

    // ============================================================
    // M7 — Weight tizim metodikalarida (MBTI16/BIG5/RIASEC/ACTIVITY) e'tiborsiz qoldirilmaydi.
    // ============================================================

    [Fact]
    public void Mbti16_WithNonUnitWeight_ThrowsDomainException()
    {
        var questions = BuildSingleAxis("EI", 1, 4).Concat(BuildAxes(["SN", "TF", "JP"])).ToList();
        questions[0] = questions[0] with { Weight = 2.0m };
        var answers = questions.ToDictionary(q => q.QuestionId, _ => 3);

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new Mbti16Strategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_UNEXPECTED_WEIGHT");
    }

    [Fact]
    public void BigFive_WithNonUnitWeight_ThrowsDomainException()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var factor in new[] { "O", "C", "E", "A", "N" })
        {
            for (var i = 0; i < 10; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{factor}-{i}", factor, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        questions[0] = questions[0] with { Weight = 0.5m };

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new BigFiveStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_UNEXPECTED_WEIGHT");
    }

    [Fact]
    public void Riasec_WithNonUnitWeight_ThrowsDomainException()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var type in new[] { "R", "I", "ART", "SOC", "ENT", "CONV" })
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{type}-{i}", type, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        questions[0] = questions[0] with { Weight = 3.0m };

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new RiasecStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_UNEXPECTED_WEIGHT");
    }

    [Fact]
    public void Activity_WithNonUnitWeight_ThrowsDomainException()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var scale in new[] { "MOT", "SELF", "SOCA", "ENG" })
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{scale}-{i}", scale, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        questions[0] = questions[0] with { Weight = 1.5m };

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new ActivityStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_UNEXPECTED_WEIGHT");
    }

    // ============================================================
    // M8 — RiasecStrategy: direction != 1 bo'lsa aniq xato (docs/03: "teskari savol yo'q").
    // ============================================================

    [Fact]
    public void Riasec_WithReverseDirectionQuestion_ThrowsDomainException()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var type in new[] { "R", "I", "ART", "SOC", "ENT", "CONV" })
        {
            for (var i = 0; i < 8; i++)
            {
                var direction = type == "R" && i == 0 ? -1 : 1;
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{type}-{i}", type, direction, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new RiasecStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SCORING_UNEXPECTED_DIRECTION");
    }

    // ============================================================
    // M9 — SumStrategy: har shkalada kamida 4 savol (docs/03 §6.3).
    // ============================================================

    [Fact]
    public void Sum_ScaleWithFewerThanFourQuestions_ThrowsDomainException()
    {
        var questions = Enumerable.Range(0, 3)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"Q{i}", "SCALE", 1, 1.0m, QuestionType.Likert5, i))
            .ToList();
        var answers = questions.ToDictionary(q => q.QuestionId, _ => 3);

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));

        var act = () => new SumStrategy().Score(input);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("SUM_SCALE_TOO_FEW_QUESTIONS");
    }

    [Fact]
    public void Sum_ScaleWithExactlyFourQuestions_Succeeds()
    {
        var questions = Enumerable.Range(0, 4)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"Q{i}", "SCALE", 1, 1.0m, QuestionType.Likert5, i))
            .ToList();
        var answers = questions.ToDictionary(q => q.QuestionId, _ => 3);
        IReadOnlyDictionary<string, IReadOnlyList<InterpretationBand>> bands = new Dictionary<string, IReadOnlyList<InterpretationBand>>
        {
            ["SCALE"] = [new InterpretationBand(0, 100, "O'rtacha")],
        };

        var input = new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null), bands);

        var act = () => new SumStrategy().Score(input);

        act.Should().NotThrow();
    }

    // ============================================================
    // M11 — har bir ScoringResult `ScoringVersion`ni tashiydi.
    // ============================================================

    [Fact]
    public void AllStrategies_ReturnCurrentScoringVersion()
    {
        var mbti = new Mbti16Strategy().Score(BuildAxesInput());
        var big5 = new BigFiveStrategy().Score(BuildBig5Input());
        var riasec = new RiasecStrategy().Score(BuildRiasecInput());
        var activity = new ActivityStrategy().Score(BuildActivityInput());
        var sum = new SumStrategy().Score(BuildSumInput());

        foreach (var result in new[] { mbti, big5, riasec, activity, sum })
        {
            result.ScoringVersion.Should().Be(ScoringConstants.CurrentScoringVersion);
        }

        ScoringConstants.CurrentScoringVersion.Should().Be(1);
    }

    private static ScoringInput BuildAxesInput()
    {
        var questions = BuildAxes(["EI", "SN", "TF", "JP"]);
        var answers = questions.ToDictionary(q => q.QuestionId, _ => 3);
        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    private static ScoringInput BuildBig5Input()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var factor in new[] { "O", "C", "E", "A", "N" })
        {
            for (var i = 0; i < 10; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{factor}-{i}", factor, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    private static ScoringInput BuildRiasecInput()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var type in new[] { "R", "I", "ART", "SOC", "ENT", "CONV" })
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{type}-{i}", type, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    private static ScoringInput BuildActivityInput()
    {
        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();
        foreach (var scale in new[] { "MOT", "SELF", "SOCA", "ENG" })
        {
            for (var i = 0; i < 8; i++)
            {
                var id = Guid.NewGuid();
                questions.Add(new QuestionMeta(id, $"{scale}-{i}", scale, 1, 1.0m, QuestionType.Likert5, questions.Count));
                answers[id] = 3;
            }
        }

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    private static ScoringInput BuildSumInput()
    {
        var questions = Enumerable.Range(0, 4)
            .Select(i => new QuestionMeta(Guid.NewGuid(), $"Q{i}", "SCALE", 1, 1.0m, QuestionType.Likert5, i))
            .ToList();
        var answers = questions.ToDictionary(q => q.QuestionId, _ => 3);
        IReadOnlyDictionary<string, IReadOnlyList<InterpretationBand>> bands = new Dictionary<string, IReadOnlyList<InterpretationBand>>
        {
            ["SCALE"] = [new InterpretationBand(0, 100, "O'rtacha")],
        };

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null), bands);
    }
}
