using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Tests.Public;

/// <summary>
/// ⚠️ P12-R2 (MAJBURIY, `prompts/12`) — `tests/StudentRoadMap.Domain.Tests/Scoring/RealQuestionBankTests.cs`
/// dagi `ReliabilityCalculator_RealSeedSession_StraightLinedTestBlockIsDetected` testining
/// **Application darajasidagi** nusxasi: xuddi shu 190 savolli haqiqiy sessiya (`mbti16→big5→
/// riasec→activity`, BIG5 bloki to'liq bir xil javob bilan to'ldirilgan) endi qo'lda tuzilgan
/// `ReliabilityInput` o'rniga **`CompleteSessionCommandHandler` chaqiradigan aynan o'sha
/// production kodi** — `ReliabilityInputBuilder.Build` (P12-R1 implementatsiyasi) — orqali
/// quriladi. Agar handler/builder butun ro'yxatga bittalikda `.OrderBy(q => q.DisplayOrder)`
/// qo'llasa (P12-R1 taqiqlagan xato), BIG5 bloki 4 ta test orasida aralashib ketadi va
/// straight-lining signali yo'qoladi — aynan shu test qizaradi.
///
/// Regressiyani yanada qattiqroq tekshirish uchun test bloklari `ReliabilityInputBuilder.Build`ga
/// ATAYLAB SESSIYA tartibida EMAS (aralashtirilgan holda) uzatiladi — `Build` o'zi
/// `DisplayOrderInSession` bo'yicha to'g'ri saralashi shart (chaqiruvchi oldindan saralab
/// berishga arzimaydi).
/// </summary>
public sealed class ReliabilityInputBuilderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Build_RealSeedSession_StraightLinedBig5BlockIsDetectedRegardlessOfInputBlockOrder()
    {
        var mbti = LoadSeedDefinition("mbti16.json");
        var big5 = LoadSeedDefinition("big5.json");
        var riasec = LoadSeedDefinition("riasec.json");
        var activity = LoadSeedDefinition("activity.json");

        var answers = new Dictionary<Guid, int>();

        // Sessiya ICHIDAGI (haqiqiy) tartib: mbti16=1, big5=2, riasec=3, activity=4
        // (`docs/03` §9, seed JSON `displayOrder`lari bilan bir xil).
        var mbtiBlock = BuildBlock(mbti, 1, answers, constantValue: 3);
        var big5Block = BuildBlock(big5, 2, answers, constantValue: 4); // BIG5 — to'liq bir xil javob (straight-lining maqsadli signali).
        var riasecBlock = BuildBlock(riasec, 3, answers, constantValue: 3);
        var activityBlock = BuildBlock(activity, 4, answers, constantValue: 3);

        // ATAYLAB aralashtirilgan tartibda uzatiladi (2,4,1,3) — `Build` chaqiruvchining
        // ro'yxat tartibiga emas, `DisplayOrderInSession`ga tayanishi shart.
        var testBlocks = new List<ReliabilityInputBuilder.TestBlock> { big5Block, activityBlock, mbtiBlock, riasecBlock };

        var durations = answers.Keys.ToDictionary(id => id, _ => 2000);

        var input = ReliabilityInputBuilder.Build(testBlocks, answers, durations, TimeSpan.FromMinutes(25));
        var result = ReliabilityCalculator.Calculate(input);

        // Qiymatlar `RealQuestionBankTests.ReliabilityCalculator_RealSeedSession_StraightLinedTestBlockIsDetected`
        // bilan AYNAN bir xil ssenariy — bir xil natija kutiladi (qo'lda hisob shu testda izohlangan).
        result.Reasons.Should().Contain("StraightLining", "P12-R2: handler/builder tartibni buzsa BIG5 bloki aralashib ketadi va straight-lining signali yo'qoladi");
        result.Score.Should().Be(65.19);
        result.Flag.Should().Be(ReliabilityFlag.Questionable);
    }

    [Fact]
    public void Build_QuestionOrder_IsSessionDisplayOrderThenInTestDisplayOrder_NotAFlatSort()
    {
        // P12-R1 QAT'IY TAQIQI ning kichik, izolyatsiya qilingan isboti: ikkita test bloki,
        // ikkalasida ham DisplayOrder 1..3 (haqiqiy seed'dagidek qayta boshlanadi). Butun
        // ro'yxatga bittalikda `.OrderBy(q => q.DisplayOrder)` qo'llansa ikkala blok
        // aralashib ketardi (A1,B1,A2,B2,A3,B3); to'g'ri (ikki bosqichli) natija A blokini
        // TO'LIQ B blokidan oldin beradi (A1,A2,A3,B1,B2,B3).
        var testA = new[]
        {
            new QuestionMeta(Guid.NewGuid(), "A1", "S", 1, 1.0m, QuestionType.Likert5, 1),
            new QuestionMeta(Guid.NewGuid(), "A2", "S", 1, 1.0m, QuestionType.Likert5, 2),
            new QuestionMeta(Guid.NewGuid(), "A3", "S", 1, 1.0m, QuestionType.Likert5, 3),
        };
        var testB = new[]
        {
            new QuestionMeta(Guid.NewGuid(), "B1", "S", 1, 1.0m, QuestionType.Likert5, 1),
            new QuestionMeta(Guid.NewGuid(), "B2", "S", 1, 1.0m, QuestionType.Likert5, 2),
            new QuestionMeta(Guid.NewGuid(), "B3", "S", 1, 1.0m, QuestionType.Likert5, 3),
        };

        // Sessiyada B (DisplayOrderInSession=2) A (=1) dan KEYIN keladi, lekin ro'yxatga B
        // birinchi uzatiladi — `Build` baribir to'g'ri (A to'liq, keyin B to'liq) tartib berishi shart.
        var blocks = new List<ReliabilityInputBuilder.TestBlock>
        {
            new(2, testB),
            new(1, testA),
        };

        var answers = testA.Concat(testB).ToDictionary(q => q.QuestionId, _ => 3);
        var durations = testA.Concat(testB).ToDictionary(q => q.QuestionId, _ => 2000);

        var input = ReliabilityInputBuilder.Build(blocks, answers, durations, TimeSpan.FromMinutes(10));

        input.Questions.Select(q => q.Code).Should().Equal("A1", "A2", "A3", "B1", "B2", "B3");
    }

    private static ReliabilityInputBuilder.TestBlock BuildBlock(
        SeedTestDefinition definition,
        int displayOrderInSession,
        Dictionary<Guid, int> answers,
        int constantValue)
    {
        var questions = new List<QuestionMeta>(definition.Questions.Count);

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

        return new ReliabilityInputBuilder.TestBlock(displayOrderInSession, questions);
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
    }
}
