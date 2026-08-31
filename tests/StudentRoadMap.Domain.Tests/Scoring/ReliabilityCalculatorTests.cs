using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// `ReliabilityCalculator` — `docs/03-psixologik-metodikalar.md` §7, har bir jarima signali
/// uchun alohida, qo'lda hisoblangan test.
/// </summary>
public sealed class ReliabilityCalculatorTests
{
    [Fact]
    public void Calculate_CleanSession_ReturnsScore100AndReliable()
    {
        // 20 savol, qiymatlar 1..5 aylanma tartibda (uzunligi 1 dan oshmaydigan "run"lar) —
        // hech qanday signal ishga tushmaydi: to'liq bir xil emas, straight-lining yo'q,
        // barcha javob 2000ms (tez emas), 10 daqiqalik sessiya (qisqa emas).
        var (questions, answers) = BuildCyclingAnswers(20);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10)));

        result.Score.Should().Be(100.0);
        result.Flag.Should().Be(ReliabilityFlag.Reliable);
        result.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_AllAnswersIdentical_AppliesFlat50PenaltyAndExcludesStraightLining()
    {
        // docs/03 §7: "To'liq bir xil javob (barchasi 3)" => jarima 50, score=100-50=50.
        // Bu "kuchliroq" signal bo'lgani uchun straight-lining alohida qo'shilmaydi (aks holda
        // bitta xatti-harakat ikki marta jarimalanadi).
        var (questions, answers) = BuildQuestions(20, _ => 3);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10)));

        result.Score.Should().Be(50.0);
        result.Flag.Should().Be(ReliabilityFlag.Questionable);
        result.Reasons.Should().Equal("AllSameAnswer");
    }

    [Fact]
    public void Calculate_TwentyConsecutiveIdenticalAnswers_AppliesSingleStraightLiningBlock()
    {
        // 20 ta ketma-ket bir xil (2), keyin 5 ta turli qiymat — sessiya to'liq bir xil EMAS
        // (oxirgi 5 tasi har xil), shuning uchun faqat straight-lining (1 blok >= 12 => 10 jarima).
        var values = Enumerable.Repeat(2, 20).Concat([1, 3, 4, 5, 2]).ToArray();
        var (questions, answers) = BuildQuestions(values.Length, i => values[i]);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10)));

        result.Score.Should().Be(90.0);
        result.Flag.Should().Be(ReliabilityFlag.Reliable);
        result.Reasons.Should().Equal("StraightLining");
    }

    [Fact]
    public void Calculate_ThreeSeparateStraightLiningBlocks_CapsPenaltyAt30()
    {
        // 3 ta alohida >=12 uzunlikdagi bir xil bloki, orasida bittadan farqli ajratuvchi qiymat —
        // har biri 10 dan jarima beradi, lekin cap=30 dan oshmaydi (docs/03 §7).
        var block = Enumerable.Repeat(1, 12);
        var separator = new[] { 5 };
        var values = block.Concat(separator).Concat(block).Concat(separator).Concat(block).ToArray();
        var (questions, answers) = BuildQuestions(values.Length, i => values[i]);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10)));

        result.Score.Should().Be(70.0);
        result.Flag.Should().Be(ReliabilityFlag.Reliable);
        result.Reasons.Should().Equal("StraightLining");
    }

    [Fact]
    public void Calculate_AllAnswersFast_AppliesCappedFastAnswerPenalty()
    {
        // Barcha javob 900ms dan tez => p=1.0, jarima=min(40, 1.0*100*0.8)=40 => score=60.
        var (questions, answers) = BuildCyclingAnswers(20);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 500);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10)));

        result.Score.Should().Be(60.0);
        result.Flag.Should().Be(ReliabilityFlag.Questionable);
        result.Reasons.Should().Equal("FastAnswers");
    }

    [Fact]
    public void Calculate_SomeAnswersFast_AppliesProportionalPenalty()
    {
        // 20 tadan 6 tasi tez (p=0.3) => jarima=0.3*100*0.8=24.0 => score=76.0.
        var (questions, answers) = BuildCyclingAnswers(20);
        const int fastCount = 6;
        var durations = questions.Select((q, i) => (q.QuestionId, ms: i < fastCount ? 500 : 2000))
            .ToDictionary(x => x.QuestionId, x => x.ms);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10)));

        result.Score.Should().Be(76.0);
        result.Reasons.Should().Equal("FastAnswers");
    }

    [Fact]
    public void Calculate_ShortSession_AppliesPenalty20()
    {
        var (questions, answers) = BuildCyclingAnswers(20);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(5)));

        result.Score.Should().Be(80.0);
        result.Reasons.Should().Equal("ShortSession");
    }

    [Fact]
    public void Calculate_ReverseQuestionMismatch_AppliesProportionalPenalty()
    {
        // Bitta shkalada 2 ijobiy (v=5,5) va 2 teskari (v=5,3). Normallashgan qiymat
        // (v-1)/4: posAvg=((5-1)/4+(5-1)/4)/2=1.0.
        // Teskari tuzatilgan (6-v): Q3=6-5=1=>norm 0; Q4=6-3=3=>norm 0.5 => negAvgCorrected=0.25.
        // mismatch=d=|1.0-0.25|=0.75 (bitta shkala) => jarima=0.75*25=18.75 => score=81.25.
        // Javoblar (5,5,5,3) bir xil EMAS, shuning uchun "AllSameAnswer" ishga tushmaydi.
        var questions = new List<QuestionMeta>
        {
            new(Guid.NewGuid(), "Q1", "SCALE", 1, 1.0m, QuestionType.Likert5, 0),
            new(Guid.NewGuid(), "Q2", "SCALE", 1, 1.0m, QuestionType.Likert5, 1),
            new(Guid.NewGuid(), "Q3", "SCALE", -1, 1.0m, QuestionType.Likert5, 2),
            new(Guid.NewGuid(), "Q4", "SCALE", -1, 1.0m, QuestionType.Likert5, 3),
        };
        var values = new[] { 5, 5, 5, 3 };
        var answers = questions.Select((q, i) => (q.QuestionId, v: values[i])).ToDictionary(x => x.QuestionId, x => x.v);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10)));

        result.Score.Should().Be(81.25);
        result.Flag.Should().Be(ReliabilityFlag.Reliable);
        result.Reasons.Should().Equal("ReverseConflict");
    }

    [Theory]
    // Faqat "tez javoblar" signalidan foydalanib, `Reliable`/`Questionable` chegarasini (70) aniq
    // sinaydi: jarima = p*100*0.8 = p*80. p=0.375 (30/80 savol) => jarima=30 => score=70.
    // p=0.3875 (31/80) => jarima=31 => score=69.
    [InlineData(30, 70.0, ReliabilityFlag.Reliable)]
    [InlineData(31, 69.0, ReliabilityFlag.Questionable)]
    public void Calculate_FastAnswerPenalty_MatchesReliableQuestionableBoundary(int fastCount, double expectedScore, ReliabilityFlag expectedFlag)
    {
        var (questions, answers) = BuildCyclingAnswers(80);
        var durations = questions.Select((q, i) => (q.QuestionId, ms: i < fastCount ? 500 : 2000))
            .ToDictionary(x => x.QuestionId, x => x.ms);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(10)));

        result.Score.Should().Be(expectedScore);
        result.Flag.Should().Be(expectedFlag);
        result.Reasons.Should().Equal("FastAnswers");
    }

    [Fact]
    public void Calculate_FastCapPlusShortSession_ReturnsQuestionableAtLowerBoundary()
    {
        // FastAnswers cap (barchasi tez, p=1.0 => min(40,80)=40) + ShortSession (20) = 60 => score=40.
        // 40 — `docs/03` §7 jadvalidagi "Questionable" pastki chegarasi (40–69).
        var (questions, answers) = BuildCyclingAnswers(20);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 500);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(5)));

        result.Score.Should().Be(40.0);
        result.Flag.Should().Be(ReliabilityFlag.Questionable);
        result.Reasons.Should().Equal("FastAnswers", "ShortSession");
    }

    [Fact]
    public void Calculate_AllSamePlusShortSession_ReturnsUnreliable()
    {
        // AllSameAnswer (50) + ShortSession (20) = 70 => score=30 < 40 => Unreliable.
        var (questions, answers) = BuildQuestions(20, _ => 3);
        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);

        var result = ReliabilityCalculator.Calculate(new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(5)));

        result.Score.Should().Be(30.0);
        result.Flag.Should().Be(ReliabilityFlag.Unreliable);
        result.Reasons.Should().Equal("AllSameAnswer", "ShortSession");
    }

    [Fact]
    public void Calculate_IsDeterministic_SameInputProducesSameResultEveryTime()
    {
        var (questions, answers) = BuildCyclingAnswers(40);
        var durations = questions.Select((q, i) => (q.QuestionId, ms: i % 3 == 0 ? 500 : 2000))
            .ToDictionary(x => x.QuestionId, x => x.ms);
        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(7));

        var first = ReliabilityCalculator.Calculate(input);
        for (var i = 0; i < 50; i++)
        {
            var repeated = ReliabilityCalculator.Calculate(input);
            repeated.Score.Should().Be(first.Score);
            repeated.Flag.Should().Be(first.Flag);
            repeated.Reasons.Should().Equal(first.Reasons);
        }
    }

    private static (List<QuestionMeta> Questions, Dictionary<Guid, int> Answers) BuildQuestions(int count, Func<int, int> valueSelector)
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

    /// <summary>1..5 aylanma qiymatlar (hech qanday >=2 uzunlikdagi ketma-ket bir xillik yo'q).</summary>
    private static (List<QuestionMeta> Questions, Dictionary<Guid, int> Answers) BuildCyclingAnswers(int count) =>
        BuildQuestions(count, i => 1 + (i % 5));
}
