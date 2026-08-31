using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Domain.Tests.Scoring;

/// <summary>
/// `docs/12-testlash-strategiyasi.md` §2 dagi barcha oltin holatlarni
/// `Scoring/GoldenCases/*.json` fayllaridan o'qib, mos strategiya ustida ishga tushiradi.
/// Har bir JSON faylidagi holat qo'lda hisoblangan kirish/chiqishni o'z ichiga oladi — hisob-kitob
/// izohi shu holatning `name`/`_comment` maydonida, batafsili esa PR hisobotida keltiriladi.
/// </summary>
public sealed class GoldenCaseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IEnumerable<object[]> CaseLocations()
    {
        var directory = FindGoldenCasesDirectory();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            var cases = LoadFile(file);
            for (var i = 0; i < cases.Count; i++)
            {
                yield return [Path.GetFileName(file), i];
            }
        }
    }

    [Theory]
    [MemberData(nameof(CaseLocations))]
    public void GoldenCase_ProducesHandComputedResult(string fileName, int caseIndex)
    {
        var directory = FindGoldenCasesDirectory();
        var testCase = LoadFile(Path.Combine(directory, fileName))[caseIndex];

        var strategy = ResolveStrategy(testCase.StrategyCode);
        var input = BuildInput(testCase);

        var result = strategy.Score(input);

        AssertMatches(testCase, result);
    }

    private static void AssertMatches(GoldenCaseFile testCase, ScoringResult result)
    {
        var because = $"'{testCase.Name}' oltin holati";
        var tolerance = testCase.Tolerance;
        var expected = testCase.Expected;

        if (expected.ResultCode is not null)
        {
            result.ResultCode.Should().Be(expected.ResultCode, because);
        }

        if (expected.NormalizedScores is not null)
        {
            result.NormalizedScores.Keys.Should().BeEquivalentTo(expected.NormalizedScores.Keys, because);
            foreach (var (key, value) in expected.NormalizedScores)
            {
                result.NormalizedScores[key].Should().BeApproximately(value, tolerance, $"{because}: '{key}' normalized score");
            }
        }

        if (expected.RawScores is not null)
        {
            result.RawScores.Keys.Should().BeEquivalentTo(expected.RawScores.Keys, because);
            foreach (var (key, value) in expected.RawScores)
            {
                result.RawScores[key].Should().BeApproximately(value, tolerance, $"{because}: '{key}' raw score");
            }
        }

        if (expected.Levels is not null)
        {
            result.Levels.Should().BeEquivalentTo(expected.Levels, because);
        }

        if (expected.CompositeIndex is not null)
        {
            result.CompositeIndex.Should().NotBeNull(because);
            result.CompositeIndex!.Value.Should().BeApproximately(expected.CompositeIndex.Value, tolerance, because);
        }

        if (expected.Flags is not null)
        {
            result.Flags.Should().BeEquivalentTo(expected.Flags, because);
        }

        if (expected.InterpretationKey is not null)
        {
            result.InterpretationKey.Should().Be(expected.InterpretationKey, because);
        }
    }

    private static IScoringStrategy ResolveStrategy(string strategyCode) => strategyCode switch
    {
        "MBTI16" => new Mbti16Strategy(),
        "BIG5" => new BigFiveStrategy(),
        "RIASEC" => new RiasecStrategy(),
        "ACTIVITY" => new ActivityStrategy(),
        "SUM" => new SumStrategy(),
        _ => throw new InvalidOperationException($"Noma'lum strategiya kodi: '{strategyCode}'."),
    };

    private static ScoringInput BuildInput(GoldenCaseFile testCase)
    {
        var questions = testCase.Questions
            .Select(q => new QuestionMeta(
                DeterministicGuid(q.Code),
                q.Code,
                q.Scale,
                q.Direction,
                (decimal)q.Weight,
                Enum.Parse<QuestionType>(q.QuestionType, ignoreCase: true),
                q.DisplayOrder))
            .ToList();

        var answers = testCase.Questions.ToDictionary(q => DeterministicGuid(q.Code), q => q.Answer);
        var durations = new Dictionary<Guid, int>();

        IReadOnlyDictionary<string, IReadOnlyList<InterpretationBand>>? scaleBands = testCase.ScaleBands?.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<InterpretationBand>)kv.Value.Select(b => new InterpretationBand(b.Min, b.Max, b.Level)).ToList());

        return new ScoringInput(questions, answers, durations, new StudentContext(null, null, null), scaleBands);
    }

    private static List<GoldenCaseFile> LoadFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<GoldenCaseFile>>(json, JsonOptions)
            ?? throw new InvalidOperationException($"'{path}' bo'sh yoki noto'g'ri formatda.");
    }

    /// <summary>Savol kodidan barqaror (deterministik) `Guid` — test JSON'lari xom `Guid` yozmasligi uchun.</summary>
    private static Guid DeterministicGuid(string seed)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }

    private static string FindGoldenCasesDirectory()
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

        var casesDirectory = Path.Combine(directory.FullName, "tests", "StudentRoadMap.Domain.Tests", "Scoring", "GoldenCases");
        Directory.Exists(casesDirectory).Should().BeTrue($"'{casesDirectory}' mavjud bo'lishi kerak");

        return casesDirectory;
    }
}
