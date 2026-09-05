using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Common;

public sealed class TokenHashTests
{
    [Fact]
    public void Compute_Sha256HexKichikHarfda_64Belgi()
    {
        var hash = TokenHash.Compute("session-token-abc");

        hash.Should().HaveLength(TokenHash.HexLength);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Compute_ApplicationQatlamidagiRefreshTokenHashBilanBirXil()
    {
        // `Application/Identity/Common/RefreshTokenHash` bilan AYNAN bir xil algoritm —
        // ikkalasi ajralib ketmasligi shu yerda qulflanadi (u sinf `internal`, shu sabab
        // formulani takrorlab solishtiramiz).
        const string raw = "some-random-refresh-token";
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

        TokenHash.Compute(raw).Should().Be(expected);
    }

    [Fact]
    public void Compute_Deterministik()
    {
        TokenHash.Compute("abc").Should().Be(TokenHash.Compute("abc"));
        TokenHash.Compute("abc").Should().NotBe(TokenHash.Compute("abd"));
    }

    [Fact]
    public void Compute_MalumQiymat_PostgresSha256BilanMosKeladi()
    {
        // `encode(sha256(convert_to('abc','UTF8')),'hex')` — migratsiyadagi backfill formulasi
        // AYNAN shu qiymatni beradi (SHA-256("abc") standart sinov vektori).
        TokenHash.Compute("abc").Should()
            .Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }

    [Fact]
    public void Compute_BoshQiymat_ArgumentExceptionOtadi()
    {
        var act = () => TokenHash.Compute("   ");

        act.Should().Throw<ArgumentException>();
    }
}
