using FluentAssertions;
using Microsoft.Data.Sqlite;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Persistence;

/// <summary>
/// `IAppDbContext.TryMarkTotpBackupCodeUsedAsync` — atomik shartli `UPDATE ... WHERE used_at
/// IS NULL` (QA topilmasi, `docs/13-auth-va-jwt.md` MAJOR #1: zaxira kod poyga holati).
///
/// Haqiqiy `Task.WhenAll` bilan parallel chaqiruv bu yerda ATAYLAB ishlatilmaydi — SQLite
/// (sinov muhiti) bitta ulanish/bitta yozuvchi qulfi tufayli ikkala chaqiruvni real vaqtda
/// bir-biriga solishtirish o'rniga oddiy ketma-ketlashtirib qo'yardi (natija SQLite qulf
/// tartibiga bog'liq, Postgres'dagi haqiqiy MVCC xatti-harakatini aks ettirmaydi — `docs/06`
/// §8dagi shu turdagi boshqa eslatmalarga mos). Buning o'rniga SQL'ning o'zi — shartli
/// `UPDATE ... WHERE used_at IS NULL` — natijani QATORLAR SONI darajasida isbotlaydi: ikkinchi
/// chaqiruv birinchisi allaqachon `used_at`ni to'ldirgan qatorga mos kelmay, 0 qator qaytaradi.
/// Bu — aynan shartli yangilash mantig'ining o'zi, real parallel ijrodan qat'i nazar bir xil
/// ishlaydi (Postgres'da ikkita HAQIQIY parallel `UPDATE` ham xuddi shu sabab bilan — bittasi
/// qatorni qulflab oladi, ikkinchisi kutib, keyin `WHERE used_at IS NULL` shartiga mos
/// kelmagani uchun 0 qator bilan qaytadi).
/// </summary>
public sealed class AdminTotpBackupCodeConcurrencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    [Fact]
    public async Task TryMarkTotpBackupCodeUsedAsync_IkkiMartaChaqirilsa_FaqatBirinchisiTrueQaytaradi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        Guid backupCodeId;

        await using (var setupContext = NewContext(connection))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var admin = AdminUser.Create(Guid.NewGuid(), "totp-race-admin", "totp-race@salohiyat.uz", "hash", Now);
            setupContext.AdminUsers.Add(admin);

            var backupCode = AdminTotpBackupCode.Create(Guid.NewGuid(), admin.Id, "hashed-code", Now);
            setupContext.AdminTotpBackupCodes.Add(backupCode);

            await setupContext.SaveChangesAsync();
            backupCodeId = backupCode.Id;
        }

        // Ikkita MUSTAQIL kontekst — xuddi ikkita bir vaqtdagi so'rov handler'i kabi, ikkalasi
        // ham "kod hali ishlatilmagan" deb o'ylab shu metodni chaqiradi.
        await using var contextA = NewContext(connection);
        await using var contextB = NewContext(connection);

        var firstClaim = await ((IAppDbContext)contextA).TryMarkTotpBackupCodeUsedAsync(backupCodeId, Now, CancellationToken.None);
        var secondClaim = await ((IAppDbContext)contextB).TryMarkTotpBackupCodeUsedAsync(backupCodeId, Now.AddSeconds(1), CancellationToken.None);

        firstClaim.Should().BeTrue("birinchi so'rov kodni muvaffaqiyatli 'ishlatilgan' deb belgilashi kerak");
        secondClaim.Should().BeFalse("kod ALLAQACHON ishlatilgan — ikkinchi so'rov g'olib bo'lmasligi kerak (poyga holati yopilgan)");

        // DB'dagi yakuniy holat — `used_at` BIRINCHI chaqiruv vaqtiga o'rnatilgan (ikkinchisi
        // hech narsani o'zgartirmagan, 0 qator ta'sirlangan).
        await using var verifyContext = NewContext(connection);
        var savedCode = verifyContext.AdminTotpBackupCodes.Single(c => c.Id == backupCodeId);
        savedCode.UsedAt.Should().Be(Now);
    }

    [Fact]
    public async Task TryMarkTotpBackupCodeUsedAsync_MavjudBolmaganIdBilan_FalseQaytaradi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using (var setupContext = NewContext(connection))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        await using var context = NewContext(connection);

        var claimed = await ((IAppDbContext)context).TryMarkTotpBackupCodeUsedAsync(Guid.NewGuid(), Now, CancellationToken.None);

        claimed.Should().BeFalse();
    }
}
