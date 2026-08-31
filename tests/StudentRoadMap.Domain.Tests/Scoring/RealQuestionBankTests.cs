using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// Haqiqiy savol banki (`Infrastructure/Persistence/SeedData/test-definitions/*.json`, P05–P08
/// natijasi, 190 savol) ustida to'liq sessiya hisoblaydi. Shkala nomlari, `direction` va savol
/// soni mosligini, shuningdek strategiya real ma'lumot bilan ishlashini tekshiradi
/// (`prompts/09-scoring-engine.md`).
/// </summary>
public sealed class RealQuestionBankTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Mbti16_RealSeed_Has60QuestionsFifteenPerAxis()
    {
        var definition = LoadSeedDefinition("mbti16.json");

        definition.Questions.Should().HaveCount(60);
        foreach (var axis in new[] { "EI", "SN", "TF", "JP" })
        {
            definition.Questions.Count(q => q.Scale == axis).Should().Be(15, $"'{axis}' o'qida 15 savol bo'lishi kerak (docs/03 §2.1)");
        }
    }

    [Fact]
    public void Mbti16_RealSeed_AllNeutralAnswers_ProducesFiftyPercentOnEveryAxisAndTieBreakCode()
    {
        // Har o'qda pos/neg nisbati teng bo'lmasa ham, v=3 => axisPct=50 (umumiy isbot: Scoring/Mbti16Strategy
        // golden testidagi "barcha javob 3" holatiga qarang). Tie-break harflari EI->I,SN->S,TF->T,JP->J.
        var definition = LoadSeedDefinition("mbti16.json");
        var input = BuildInputAllNeutral(definition);

        var result = new Mbti16Strategy().Score(input);

        result.ResultCode.Should().Be("ISTJ");
        result.NormalizedScores.Values.Should().OnlyContain(pct => pct == 50.0);
        result.Flags.Should().BeEquivalentTo(["Borderline:EI", "Borderline:SN", "Borderline:TF", "Borderline:JP"]);
    }

    [Fact]
    public void BigFive_RealSeed_Has50QuestionsTenPerFactor()
    {
        var definition = LoadSeedDefinition("big5.json");

        definition.Questions.Should().HaveCount(50);
        foreach (var factor in new[] { "O", "C", "E", "A", "N" })
        {
            definition.Questions.Count(q => q.Scale == factor).Should().Be(10, $"'{factor}' omilida 10 savol bo'lishi kerak (docs/03 §3.1)");
        }
    }

    [Fact]
    public void BigFive_RealSeed_AllNeutralAnswers_ProducesFiftyPercentOnEveryFactor()
    {
        var definition = LoadSeedDefinition("big5.json");
        var input = BuildInputAllNeutral(definition);

        var result = new BigFiveStrategy().Score(input);

        result.NormalizedScores.Values.Should().OnlyContain(pct => pct == 50.0);
        result.Levels.Values.Should().OnlyContain(level => level == "O'rtacha");
    }

    [Fact]
    public void Riasec_RealSeed_Has48QuestionsEightPerType()
    {
        var definition = LoadSeedDefinition("riasec.json");

        definition.Questions.Should().HaveCount(48);
        foreach (var type in new[] { "R", "I", "ART", "SOC", "ENT", "CONV" })
        {
            definition.Questions.Count(q => q.Scale == type).Should().Be(8, $"'{type}' tipida 8 savol bo'lishi kerak (docs/03 §4.1)");
        }

        definition.Questions.Should().OnlyContain(q => q.Direction == 1, "docs/03 §4.1: RIASEC'da teskari savol yo'q");
    }

    [Fact]
    public void Riasec_RealSeed_AllNeutralAnswers_ProducesFiftyPercentOnEveryType()
    {
        var definition = LoadSeedDefinition("riasec.json");
        var input = BuildInputAllNeutral(definition);

        var result = new RiasecStrategy().Score(input);

        result.NormalizedScores["R"].Should().Be(50.0);
        result.NormalizedScores["I"].Should().Be(50.0);
        result.NormalizedScores["ART"].Should().Be(50.0);
        result.NormalizedScores["SOC"].Should().Be(50.0);
        result.NormalizedScores["ENT"].Should().Be(50.0);
        result.NormalizedScores["CONV"].Should().Be(50.0);
        result.NormalizedScores["DIFFERENTIATION"].Should().Be(0.0);
        result.Flags.Should().Contain("LowDifferentiation");
    }

    [Fact]
    public void Activity_RealSeed_Has32QuestionsEightPerScale()
    {
        var definition = LoadSeedDefinition("activity.json");

        definition.Questions.Should().HaveCount(32);
        foreach (var scale in new[] { "MOT", "SELF", "SOCA", "ENG" })
        {
            definition.Questions.Count(q => q.Scale == scale).Should().Be(8, $"'{scale}' shkalasida 8 savol bo'lishi kerak (docs/03 §5.1)");
        }
    }

    [Fact]
    public void Activity_RealSeed_AllNeutralAnswers_ProducesFiftyPercentActivityIndexAndLowActiveLevel()
    {
        // v=3 => har shkalada pct=50 (yo'nalish nisbatidan qat'i nazar, 6-3=3). ActivityIndex=50.0
        // <=50 (ActivityLevelLowActiveMax) => "LowActive", NeedsAttention=false (50 emas <31).
        var definition = LoadSeedDefinition("activity.json");
        var input = BuildInputAllNeutral(definition);

        var result = new ActivityStrategy().Score(input);

        result.NormalizedScores.Values.Should().OnlyContain(pct => pct == 50.0);
        result.CompositeIndex.Should().Be(50.0);
        result.Levels["ACTIVITY"].Should().Be("LowActive");
        result.Flags.Should().BeEmpty();
    }

    [Fact]
    public void AllFourSystemTests_RealSeed_TotalQuestionCountIs190()
    {
        var total = new[] { "mbti16.json", "big5.json", "riasec.json", "activity.json" }
            .Sum(file => LoadSeedDefinition(file).Questions.Count);

        total.Should().Be(190, "docs/03 §9: jami savol soni 190");
    }

    [Fact]
    public void AllFourSystemTests_RealSeed_UseOnlyDocumentedScaleCodes()
    {
        var documentedScales = new HashSet<string>(StringComparer.Ordinal)
        {
            "EI", "SN", "TF", "JP",
            "O", "C", "E", "A", "N",
            "R", "I", "ART", "SOC", "ENT", "CONV",
            "MOT", "SELF", "SOCA", "ENG",
        };

        foreach (var file in new[] { "mbti16.json", "big5.json", "riasec.json", "activity.json" })
        {
            var definition = LoadSeedDefinition(file);
            definition.Questions.Should().OnlyContain(q => documentedScales.Contains(q.Scale), $"'{file}' — docs/03 §1 dagi yagona shkala ro'yxatidan tashqari kod ishlatilmasligi kerak");
        }
    }

    [Fact]
    public void ReliabilityCalculator_RealSeedSession_StraightLinedTestBlockIsDetected()
    {
        // QA Bloklovchi-2 reproduksiyasi: 190 savolli haqiqiy sessiyada (mbti16->big5->riasec->
        // activity xronologik tartibda) BIG5 bloki (50 savol) to'liq bir xil javob (4) bilan
        // to'ldirilgan, qolgan 3 blok (MBTI16/ACTIVITY: yo'nalishga qarab simmetrik 3,
        // RIASEC: teskari savolsiz, 3) bilan. Avvalgi (xato) versiya `DisplayOrder` bo'yicha
        // saralar edi — mbti16 (1..60), big5 (1..50), riasec (1..48), activity (1..32)
        // DisplayOrder'lari bir-biriga mos kelib, BIG5 blokini butunlay chalkashtirib
        // yuborardi va straight-lining SIGNAL UMUMAN YO'QOLARDI. Tuzatilgan versiyada
        // `ReliabilityInput.Questions` xronologik ro'yxat sifatida ishlatiladi (qayta
        // tartiblanmaydi) — blok TO'LIQ ANIQLANADI.
        var mbti = LoadSeedDefinition("mbti16.json");
        var big5 = LoadSeedDefinition("big5.json");
        var riasec = LoadSeedDefinition("riasec.json");
        var activity = LoadSeedDefinition("activity.json");

        var questions = new List<QuestionMeta>();
        var answers = new Dictionary<Guid, int>();

        // MBTI16/ACTIVITY: yo'nalishdan qat'i nazar barcha javob = 3 (simmetrik — musbat va
        // tuzatilgan-manfiy normallashgan qiymat ikkalasi ham 0.5, ReverseConflict = 0).
        AppendConstantBlock(mbti, questions, answers, 3);
        // BIG5: BARCHA javob = 4 (yo'nalishdan qat'i nazar) — 50 ta ketma-ket bir xil qiymat,
        // bu blokning o'zi maqsadli straight-lining signali.
        AppendConstantBlock(big5, questions, answers, 4);
        // RIASEC: teskari savol yo'q (docs/03 §4.1), qiymat ReverseConflict'ga ta'sir qilmaydi.
        AppendConstantBlock(riasec, questions, answers, 3);
        AppendConstantBlock(activity, questions, answers, 3);

        var durations = questions.ToDictionary(q => q.QuestionId, _ => 2000);
        var input = new ReliabilityInput(questions, answers, durations, TimeSpan.FromMinutes(25));

        var result = ReliabilityCalculator.Calculate(input);

        // Qo'lda hisob:
        // StraightLining: BIG5'ning 50 ta ketma-ket "4"si — floor(50/12)=4 blok => min(30,40)=30
        //   (MBTI/RIASEC/ACTIVITY'ning "3" bloklari ham o'zaro qo'shni bo'lib merge bo'ladi va
        //   ular ham straight-lining hosil qiladi, lekin jarima allaqachon 30ga to'lgan — cap).
        // ReverseConflict: MBTI (4 o'q) va ACTIVITY (4 shkala) uchun musbat/manfiy o'rtachasi
        //   ikkalasi ham (3-1)/4=0.5 => mismatch=0 (8 ta shkala, hammasi 0). BIG5 (5 omil,
        //   har birida 5 musbat + 5 manfiy, hammasi "4"): posAvg=(4-1)/4=0.75,
        //   negAvgCorrected=(6-4-1)/4=0.25 => mismatch=0.5 (5 ta omil). RIASEC reverse-conflikt
        //   hisobiga kirmaydi (teskari savol yo'q). d = (5*0.5)/(4+5+4) = 2.5/13 ≈ 0.192308.
        //   penalty = d*25 ≈ 4.8077.
        // ShortSession/FastAnswers: 0 (25 daqiqa, barcha javob 2000ms).
        // totalPenalty ≈ 30 + 4.8077 = 34.8077 => score ≈ 65.19 (2dp).
        result.Reasons.Should().Contain("StraightLining", "QA Bloklovchi-2: real 190 savolli sessiyada bitta test bloki to'liq bir xil javob bo'lsa aniqlanishi SHART");
        result.Score.Should().Be(65.19);
        result.Flag.Should().Be(ReliabilityFlag.Questionable);
    }

    private static ScoringInput BuildInputAllNeutral(SeedTestDefinition definition)
    {
        var questions = definition.Questions
            .Select(q => new QuestionMeta(
                DeterministicGuid(q.Code),
                q.Code,
                q.Scale,
                q.Direction,
                (decimal)q.Weight,
                Enum.Parse<QuestionType>(q.Type, ignoreCase: true),
                q.Order))
            .ToList();

        var answers = questions.ToDictionary(q => q.QuestionId, _ => 3);

        return new ScoringInput(questions, answers, new Dictionary<Guid, int>(), new StudentContext(null, null, null));
    }

    private static void AppendConstantBlock(SeedTestDefinition definition, List<QuestionMeta> questions, Dictionary<Guid, int> answers, int constantValue)
    {
        foreach (var q in definition.Questions.OrderBy(q => q.Order))
        {
            var meta = new QuestionMeta(
                DeterministicGuid($"{definition.Code}-{q.Code}"),
                q.Code,
                q.Scale,
                q.Direction,
                (decimal)q.Weight,
                Enum.Parse<QuestionType>(q.Type, ignoreCase: true),
                q.Order);

            questions.Add(meta);
            answers[meta.QuestionId] = constantValue;
        }
    }

    private static Guid DeterministicGuid(string seed)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }

    private static SeedTestDefinition LoadSeedDefinition(string fileName)
    {
        var path = Path.Combine(FindSeedDataDirectory(), fileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SeedTestDefinition>(json, JsonOptions)
            ?? throw new InvalidOperationException($"'{path}' bo'sh yoki noto'g'ri formatda.");
    }

    private static string FindSeedDataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "StudentRoadMap.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Loyiha ildizi (StudentRoadMap.sln) topilmadi.");
        }

        var seedDirectory = Path.Combine(directory.FullName, "src", "StudentRoadMap.Infrastructure", "Persistence", "SeedData", "test-definitions");
        Directory.Exists(seedDirectory).Should().BeTrue($"'{seedDirectory}' mavjud bo'lishi kerak");

        return seedDirectory;
    }

    private sealed class SeedTestDefinition
    {
        public string Code { get; set; } = "";

        public List<SeedQuestion> Questions { get; set; } = [];
    }

    private sealed class SeedQuestion
    {
        public string Code { get; set; } = "";

        public int Order { get; set; }

        public string Type { get; set; } = "Likert5";

        public string Scale { get; set; } = "";

        public int Direction { get; set; } = 1;

        public double Weight { get; set; } = 1.0;

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; } = true;
    }
}
