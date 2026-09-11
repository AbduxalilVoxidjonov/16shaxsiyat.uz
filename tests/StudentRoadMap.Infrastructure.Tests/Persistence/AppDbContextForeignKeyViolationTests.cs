using FluentAssertions;
using Microsoft.Data.Sqlite;
using StudentRoadMap.Application.Common.Exceptions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Persistence;

/// <summary>
/// `AppDbContext.SaveChangesAsync` — tashqi kalit (FK) cheklovi buzilishi, P52 (2026-09-11 QA
/// topilmasi): egasi javobi bor savolni o'chirmoqchi bo'lgan va `fk_answers_questions_question_id`
/// (`23503`) jimgina `500 INTERNAL_ERROR` bo'lib chiqqan (aniq holatlar endi
/// `DeleteTestQuestionCommandHandler`da OLDINDAN tekshiriladi — bu test ularni CHETLAB o'tgan
/// ZAXIRA yo'lni sinaydi, `UniqueConstraintViolationException`ni sinovchi test yo'qligi sababli
/// bu — shu naqshning BIRINCHI amaliy sinovi).
///
/// Soddaroq FK buzilishi tanlangan (`AnswerOption.QuestionId` mavjud bo'lmagan savolga
/// ishora qiladi) — `Answer` zanjiri (`Assessment`/`Student`/`School`) to'liq qurishni talab
/// qilmaydi, xuddi shu `23503` mexanizmini sinaydi.
/// </summary>
public sealed class AppDbContextForeignKeyViolationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    [Fact]
    public async Task SaveChangesAsync_ForeignKeyViolation_ThrowsForeignKeyViolationException()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        await using (var setupContext = NewContext(connection))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        await using var context = NewContext(connection);

        // `QuestionId` ataylab BAZADA MAVJUD EMAS — tashqi kalit `fk_answer_options_questions_question_id`
        // (`23503`) shu yerda buziladi.
        var orphanOption = AnswerOption.Create(Guid.NewGuid(), Guid.NewGuid(), "Variant", 1, 1);
        context.Add(orphanOption);

        var act = async () => await context.SaveChangesAsync();

        var ex = (await act.Should().ThrowAsync<ForeignKeyViolationException>()).Which;
        ex.Code.Should().Be("REFERENCED_RECORD_EXISTS");
    }

    [Fact]
    public async Task SaveChangesAsync_ForeignKeyViolation_XabarDbTafsilotiniOshkorQilmaydi()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();

        await using (var setupContext = NewContext(connection))
        {
            await setupContext.Database.EnsureCreatedAsync();
        }

        await using var context = NewContext(connection);

        var orphanOption = AnswerOption.Create(Guid.NewGuid(), Guid.NewGuid(), "Variant", 1, 1);
        context.Add(orphanOption);

        var act = async () => await context.SaveChangesAsync();

        var ex = (await act.Should().ThrowAsync<ForeignKeyViolationException>()).Which;

        // P31: DB jadval/cheklov nomi yoki kutubxona istisno matni javobga sizib chiqmasligi kerak.
        string[] leaks = ["fk_", "FOREIGN KEY", "SQLite Error", "constraint", "answer_options"];
        foreach (var leak in leaks)
        {
            ex.Message.Should().NotContain(leak);
        }
    }
}
