using System.Security.Cryptography;
using System.Text;

namespace StudentRoadMap.Domain.Common;

/// <summary>
/// Xom (raw) tokenni DB'da saqlash uchun bir tomonlama xeshlaydi — SHA-256, kichik harfli hex
/// (64 belgi). `docs/08-auth-va-xavfsizlik.md` 2-bo'lim: "DB'da faqat SHA-256 xeshi saqlanadi".
///
/// Algoritm `Application/Identity/Common/RefreshTokenHash` bilan AYNAN bir xil
/// (`Convert.ToHexString(SHA256.HashData(UTF8(raw))).ToLowerInvariant()`) — u yerdagi izohda
/// aytilganidek, tuzli/qimmat (parolga xos) xeshlash SHART EMAS: token o'zi yuqori entropiyali
/// tasodifiy qiymat, solishtirish esa unikal indeks bo'yicha oddiy tenglik orqali ketadi.
///
/// Bu yordamchi DOMEN qatlamida turadi, chunki uni ikkala token oqimi ham ishlatadi:
/// ommaviy sessiya tokeni (`Assessment.SessionTokenHash`) va ommaviy refresh token
/// (`PublicUsers.PublicRefreshToken.TokenHash`). Domen tozaligi buzilmaydi — faqat BCL
/// (`System.Security.Cryptography`) ishlatiladi, hech qanday NuGet paketi yo'q.
///
/// Postgres ekvivalenti (migratsiyadagi backfill uchun): `encode(sha256(t::bytea), 'hex')`.
/// </summary>
public static class TokenHash
{
    /// <summary>Xeshning hex ko'rinishidagi uzunligi — DB ustuni shu uzunlikka moslanadi.</summary>
    public const int HexLength = 64;

    public static string Compute(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("Xeshlanadigan token bo'sh bo'lishi mumkin emas.", nameof(rawToken));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
    }
}
