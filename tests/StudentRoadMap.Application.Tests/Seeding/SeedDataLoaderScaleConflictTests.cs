using FluentAssertions;
using StudentRoadMap.Application.Seeding;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Tests.Seeding;

/// <summary>
/// `SeedDataLoader.DetectScaleConflicts` — BR-8 himoyasining sof (DB'siz) qismi: seed faylida
/// mavjud savolning `scale`/`direction`/`weight`i o'zgargan bo'lsa aniqlanadi (`prompts/04`,
/// 2-band va `CLAUDE.md` 9a-qoida: "Scale/Direction/Weight o'zgarmaydi").
/// </summary>
public sealed class SeedDataLoaderScaleConflictTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static TestDefinition BuildExisting(string questionCode = "Q1", string scale = "EI", int direction = 1, decimal weight = 1.0m)
    {
        var testDefinitionId = Guid.NewGuid();
        var question = Question.Create(
            Guid.NewGuid(), testDefinitionId, questionCode, 1, "Savol matni",
            QuestionType.Likert5, scale, direction, weight, isSystem: true);

        return TestDefinition.CreateSystemPublished(
            testDefinitionId, "MBTI16", "16 tipli shaxsiyat modeli", null, 1, 9, false, 10, "MBTI16",
            [question], Now);
    }

    private static TestDefinitionSeedDto BuildIncoming(string questionCode = "Q1", string scale = "EI", int direction = 1, decimal weight = 1.0m) => new()
    {
        Code = "MBTI16",
        NameUz = "16 tipli shaxsiyat modeli",
        DisplayOrder = 1,
        EstimatedMinutes = 9,
        PageSize = 10,
        Questions =
        [
            new QuestionSeedDto { Code = questionCode, Order = 1, TextUz = "Savol matni (yangilangan)", Type = "Likert5", Scale = scale, Direction = direction, Weight = weight },
        ],
    };

    [Fact]
    public void DetectScaleConflicts_WhenNothingChanged_ReturnsEmpty()
    {
        var existing = BuildExisting();
        var incoming = BuildIncoming();

        var conflicts = SeedDataLoader.DetectScaleConflicts(existing, incoming);

        conflicts.Should().BeEmpty();
    }

    [Fact]
    public void DetectScaleConflicts_WhenOnlyTextChanged_ReturnsEmpty()
    {
        // Matn/tartib o'zgarishi konflikt emas — faqat scale/direction/weight himoyalangan (BR-8).
        var existing = BuildExisting();
        var incoming = BuildIncoming();
        incoming = incoming with
        {
            Questions = [incoming.Questions[0] with { TextUz = "Butunlay boshqa matn", Order = 5 }],
        };

        var conflicts = SeedDataLoader.DetectScaleConflicts(existing, incoming);

        conflicts.Should().BeEmpty();
    }

    [Fact]
    public void DetectScaleConflicts_WhenScaleChanged_ReturnsConflict()
    {
        var existing = BuildExisting(scale: "EI");
        var incoming = BuildIncoming(scale: "SN");

        var conflicts = SeedDataLoader.DetectScaleConflicts(existing, incoming);

        conflicts.Should().ContainSingle();
        conflicts[0].QuestionCode.Should().Be("Q1");
        conflicts[0].ExistingScale.Should().Be("EI");
        conflicts[0].IncomingScale.Should().Be("SN");
    }

    [Fact]
    public void DetectScaleConflicts_WhenDirectionChanged_ReturnsConflict()
    {
        var existing = BuildExisting(direction: 1);
        var incoming = BuildIncoming(direction: -1);

        var conflicts = SeedDataLoader.DetectScaleConflicts(existing, incoming);

        conflicts.Should().ContainSingle();
        conflicts[0].ExistingDirection.Should().Be(1);
        conflicts[0].IncomingDirection.Should().Be(-1);
    }

    [Fact]
    public void DetectScaleConflicts_WhenWeightChanged_ReturnsConflict()
    {
        // S7 (QA): og'irlik o'zgarishi ham BR-8 buzilishi — jimgina yo'qolmasligi kerak.
        var existing = BuildExisting(weight: 1.0m);
        var incoming = BuildIncoming(weight: 2.0m);

        var conflicts = SeedDataLoader.DetectScaleConflicts(existing, incoming);

        conflicts.Should().ContainSingle();
        conflicts[0].ExistingWeight.Should().Be(1.0m);
        conflicts[0].IncomingWeight.Should().Be(2.0m);
    }

    [Fact]
    public void DetectScaleConflicts_WhenIncomingQuestionCodeIsNew_DoesNotCountAsConflict()
    {
        var existing = BuildExisting(questionCode: "Q1");
        var incoming = BuildIncoming(questionCode: "Q2");

        var conflicts = SeedDataLoader.DetectScaleConflicts(existing, incoming);

        conflicts.Should().BeEmpty("yangi kodli savol konflikt emas — DbSeeder buni alohida ogohlantirish sifatida ko'radi");
    }
}
