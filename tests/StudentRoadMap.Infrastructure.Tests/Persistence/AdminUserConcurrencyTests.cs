using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudentRoadMap.Application.Common.Exceptions;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Persistence;

/// <summary>
/// Optimistik konkurentlik (`AdminUser.ConcurrencyStamp`) — QA topilmasi
/// (`docs/13-auth-va-jwt.md` MAJOR #1): TOTP asosiy kod poyga holati (ikki bir vaqtdagi so'rov
/// bir xil kodni ikkalasi ham qabul qilishi mumkin edi).
///
/// **Haqiqiy `Task.WhenAll` parallelizm ATAYLAB ishlatilmaydi**: SQLite (sinov muhiti) bitta
/// yozuvchi qulfini o'zi boshqaradi — ikkinchi parallel yozuvchi ilova darajasidagi
/// `ConcurrencyStamp` tekshiruviga yetib bormasdan turib SQLite'ning "database is locked"/qulf
/// kutish vaqtiga bog'lanib qolardi (natija SQLite qulf tartibiga, ya'ni CI mashinasining
/// tezligiga bog'liq — beqaror/flaky sinov). Buning o'rniga poyganing MOHIYATI deterministik
/// qayta ishlab chiqariladi: ikkita mustaqil `DbContext` bir xil qatorni ("eski" holatni)
/// YUKLAYDI — bu aynan poyga sharti — so'ng KETMA-KET saqlanadi. Natija HAQIQIY parallel
/// ijrodagi bilan bir xil: birinchi saqlash g'alaba qozonadi, ikkinchisi `ConcurrencyStamp`
/// eskirganini ko'radi va rad etiladi.
/// </summary>
public sealed class AdminUserConcurrencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    [Fact]
    public async Task IkkiKontekst_BirXilAdminUserniYuklabOzgartiradi_FaqatBirinchiSaqlashMuvaffaqiyatli()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        Guid adminId;

        await using (var setupContext = NewContext(connection))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var admin = AdminUser.Create(Guid.NewGuid(), "concurrency-admin", "concurrency@salohiyat.uz", "hash", Now);
            setupContext.AdminUsers.Add(admin);
            await setupContext.SaveChangesAsync();
            adminId = admin.Id;
        }

        // Ikkalasi ham bir xil ("eski") `ConcurrencyStamp` qiymatini o'qiydi — poyganing sharti.
        await using var contextA = NewContext(connection);
        await using var contextB = NewContext(connection);

        var userA = await contextA.AdminUsers.SingleAsync(u => u.Id == adminId);
        var userB = await contextB.AdminUsers.SingleAsync(u => u.Id == adminId);

        userA.RegisterTotpStepUsed(100, Now);
        userB.RegisterTotpStepUsed(100, Now);

        // Birinchi "so'rov" — muvaffaqiyatli, DB'dagi `ConcurrencyStamp` yangilanadi.
        await contextA.SaveChangesAsync();

        // Ikkinchisi — o'zi yuklagan ESKI `ConcurrencyStamp` bilan solishtiradi (EF standart
        // xatti-harakati: UPDATE'ning WHERE qismi original qiymatni ishlatadi), DB'dagi
        // qiymat endi boshqa — konflikt, `Application` qatlamiga portativ istisno sifatida chiqadi.
        var act = async () => await contextB.SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyConflictException>();

        // Yakuniy holat — faqat BIRINCHI so'rovning qadami saqlangan (ikkinchisi yo'qolmagan/
        // "yutib olinmagan" — lost update yo'q).
        await using var verifyContext = NewContext(connection);
        var finalUser = await verifyContext.AdminUsers.SingleAsync(u => u.Id == adminId);
        finalUser.TotpLastUsedStep.Should().Be(100);
    }

    [Fact]
    public async Task IkkiKontekst_TurliQadamlarBilan_IkkinchiHamKonfliktOladi()
    {
        // Konflikt `TotpLastUsedStep` qiymatiga emas, `ConcurrencyStamp`ga bog'liq — hatto
        // ikkinchi so'rov BOSHQA qadam bilan kelsa ham (masalan, keyingi 30 soniyalik oynadagi
        // kod), birinchisi allaqachon saqlangan bo'lsa, ikkinchisi baribir rad etilishi kerak.
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        Guid adminId;

        await using (var setupContext = NewContext(connection))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var admin = AdminUser.Create(Guid.NewGuid(), "concurrency-admin-2", "concurrency2@salohiyat.uz", "hash", Now);
            setupContext.AdminUsers.Add(admin);
            await setupContext.SaveChangesAsync();
            adminId = admin.Id;
        }

        await using var contextA = NewContext(connection);
        await using var contextB = NewContext(connection);

        var userA = await contextA.AdminUsers.SingleAsync(u => u.Id == adminId);
        var userB = await contextB.AdminUsers.SingleAsync(u => u.Id == adminId);

        userA.RegisterTotpStepUsed(100, Now);
        userB.RegisterTotpStepUsed(101, Now);

        await contextA.SaveChangesAsync();

        var act = async () => await contextB.SaveChangesAsync();

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
