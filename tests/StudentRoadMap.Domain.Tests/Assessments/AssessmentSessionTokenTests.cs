using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Assessments;

/// <summary>
/// Sessiya tokenining xeshlanishi — P47. Ilgari token bazada OCHIQ saqlanardi (refresh
/// tokenlardan farqli), endi domen xeshni O'ZI hisoblaydi.
/// </summary>
public sealed class AssessmentSessionTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static Assessment CreateDraft(string token = "session-token-abc") => Assessment.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        token,
        "uz",
        Guid.NewGuid(),
        Now,
        Now.AddDays(7),
        Now);

    [Fact]
    public void Create_XeshniAvtomatikHisoblaydi()
    {
        var assessment = CreateDraft();

        assessment.SessionTokenHash.Should().Be(TokenHash.Compute("session-token-abc"));
        assessment.SessionTokenHash.Should().HaveLength(TokenHash.HexLength);
    }

    [Fact]
    public void Create_TurliTokenlar_TurliXesh()
    {
        CreateDraft("token-1").SessionTokenHash.Should().NotBe(CreateDraft("token-2").SessionTokenHash);
    }

    [Fact]
    public void RotateSessionToken_TokenVaXeshniBirgaAlmashtiradi()
    {
        var assessment = CreateDraft();
        var oldHash = assessment.SessionTokenHash;

        assessment.RotateSessionToken("yangi-token", Now.AddHours(1));

        assessment.SessionToken.Should().Be("yangi-token");
        assessment.SessionTokenHash.Should().Be(TokenHash.Compute("yangi-token"));
        assessment.SessionTokenHash.Should().NotBe(oldHash);
        assessment.UpdatedAt.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public void RotateSessionToken_BoshToken_ArgumentExceptionOtadi()
    {
        var assessment = CreateDraft();

        var act = () => assessment.RotateSessionToken("  ", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RotateSessionToken_TashlabKetilganSessiyada_DomainExceptionOtadi()
    {
        var assessment = CreateDraft();
        assessment.MarkAbandoned(Now.AddDays(8));

        var act = () => assessment.RotateSessionToken("yangi-token", Now.AddDays(8));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("ASSESSMENT_INVALID_TRANSITION");
    }
}
