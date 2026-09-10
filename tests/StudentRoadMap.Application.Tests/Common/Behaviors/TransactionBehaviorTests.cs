using System.Linq.Expressions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Application.Common.Behaviors;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Jobs;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Common.Behaviors;

/// <summary>
/// ⚠️ P18-R1/P18-R2 (`prompts/18-ai-navbat-va-orkestratsiya.md`, MAJBURIY):
/// <list type="bullet">
/// <item>P18-R1: `IPostCommitActions.RunAsync` tranzaksiya MUVAFFAQIYATLI commit bo'lgandan
/// KEYIN, va faqat o'shanda, chaqirilishi kerak.</item>
/// <item>P18-R2: tranzaksiya ROLLBACK bo'lganda (handler istisno otganda) `RunAsync`
/// UMUMAN chaqirilmasligi kerak — navbatga hech narsa qo'yilmaydi.</item>
/// </list>
/// Soxta `IAppDbContext`/`IAppDbContextTransaction`/`IPostCommitActions` bilan — DB'siz,
/// to'g'ridan-to'g'ri `TransactionBehavior.Handle` pipeline segmentini sinaydi.
/// </summary>
public sealed class TransactionBehaviorTests
{
    private sealed record FakeCommand : IRequest<string>;

    private sealed class FakeTransaction : IAppDbContextTransaction
    {
        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public bool Disposed { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>`TransactionBehavior` faqat `BeginTransactionAsync`ni chaqiradi — qolgan hammasi shu sabab `NotSupportedException` otadi (chaqirilsa test SINIQ ekanini bildiradi).</summary>
    private sealed class FakeAppDbContext : IAppDbContext
    {
        public FakeTransaction? LastTransaction { get; private set; }

        public Task<IAppDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            LastTransaction = new FakeTransaction();
            return Task.FromResult<IAppDbContextTransaction>(LastTransaction);
        }

        private static NotSupportedException Unsupported() => new("TransactionBehaviorTests: bu a'zo chaqirilmasligi kerak edi.");

        public IQueryable<School> Schools => throw Unsupported();

        public IQueryable<Student> Students => throw Unsupported();

        public IQueryable<Assessment> Assessments => throw Unsupported();

        public IQueryable<AssessmentTest> AssessmentTests => throw Unsupported();

        public IQueryable<Answer> Answers => throw Unsupported();

        public IQueryable<TestResult> TestResults => throw Unsupported();

        public IQueryable<TestDefinition> TestDefinitions => throw Unsupported();

        public IQueryable<Question> Questions => throw Unsupported();

        public IQueryable<TestScale> TestScales => throw Unsupported();

        public IQueryable<QuestionSection> QuestionSections => throw Unsupported();

        public IQueryable<AnalysisJob> AnalysisJobs => throw Unsupported();

        public IQueryable<PublicUser> PublicUsers => throw Unsupported();

        public IQueryable<PublicRefreshToken> PublicRefreshTokens => throw Unsupported();

        public IQueryable<AssessmentProgram> AssessmentPrograms => throw Unsupported();

        public IQueryable<ProgramTest> ProgramTests => throw Unsupported();

        public IQueryable<SchoolProgram> SchoolPrograms => throw Unsupported();

        public IQueryable<AnswerOption> AnswerOptions => throw Unsupported();

        public IQueryable<TypeCatalogEntry> TypeCatalog => throw Unsupported();

        public IQueryable<CareerMapEntry> CareerMap => throw Unsupported();

        public IQueryable<AiProviderConfig> AiProviderConfigs => throw Unsupported();

        public IQueryable<AiAnalysis> AiAnalyses => throw Unsupported();

        public IQueryable<PromptTemplate> PromptTemplates => throw Unsupported();

        public IQueryable<AdminUser> AdminUsers => throw Unsupported();

        public IQueryable<RefreshToken> RefreshTokens => throw Unsupported();

        public IQueryable<AuditLog> AuditLogs => throw Unsupported();

        public IQueryable<AdminTotpBackupCode> AdminTotpBackupCodes => throw Unsupported();

        public IQueryable<RegistrationCounter> RegistrationCounters => throw Unsupported();

        public IQueryable<SchoolLinkView> SchoolLinkViews => throw Unsupported();

        public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

        public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> query) where TEntity : class => query;

        public void Add<TEntity>(TEntity entity) where TEntity : class => throw Unsupported();

        public void Remove<TEntity>(TEntity entity) where TEntity : class => throw Unsupported();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw Unsupported();

        public Task<int> IncrementRegistrationCounterAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw Unsupported();

        public Task<int> IncrementSchoolLinkViewAsync(Guid schoolId, DateOnly dateUtc, CancellationToken cancellationToken = default) => throw Unsupported();

        public Task<bool> TryMarkTotpBackupCodeUsedAsync(Guid backupCodeId, DateTimeOffset usedAt, CancellationToken cancellationToken = default) => throw Unsupported();
    }

    private sealed class SpyPostCommitActions : IPostCommitActions
    {
        private readonly List<Func<CancellationToken, Task>> _actions = [];

        public int RunAsyncCallCount { get; private set; }

        public void Enqueue(Func<CancellationToken, Task> action) => _actions.Add(action);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            RunAsyncCallCount++;
            foreach (var action in _actions)
            {
                await action(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    [Fact]
    public async Task Handle_CommandSucceeds_CommitsThenRunsPostCommitActions()
    {
        var context = new FakeAppDbContext();
        var postCommitActions = new SpyPostCommitActions();
        var enqueuedActionRan = false;
        var behavior = new TransactionBehavior<FakeCommand, string>(context, postCommitActions, NullLogger<TransactionBehavior<FakeCommand, string>>.Instance);

        var result = await behavior.Handle(
            new FakeCommand(),
            _ =>
            {
                // Handler ICHIDA navbatga qo'yish — P18-R1: darhol emas, `Enqueue` orqali.
                postCommitActions.Enqueue(_ => { enqueuedActionRan = true; return Task.CompletedTask; });
                return Task.FromResult("ok");
            },
            CancellationToken.None);

        result.Should().Be("ok");
        context.LastTransaction!.Committed.Should().BeTrue();
        context.LastTransaction.RolledBack.Should().BeFalse();
        postCommitActions.RunAsyncCallCount.Should().Be(1, "commit muvaffaqiyatli bo'lgandan keyin FAQAT bir marta ishga tushishi kerak");
        enqueuedActionRan.Should().BeTrue("navbatga qo'yilgan amal commit'dan keyin bajarilishi shart");
    }

    /// <summary>P18-R2 (MAJBURIY): rollback bo'lganda navbatga hech narsa qo'yilmaydi.</summary>
    [Fact]
    public async Task Handle_HandlerThrows_RollsBackAndNeverRunsPostCommitActions()
    {
        var context = new FakeAppDbContext();
        var postCommitActions = new SpyPostCommitActions();
        var enqueuedActionRan = false;
        var behavior = new TransactionBehavior<FakeCommand, string>(context, postCommitActions, NullLogger<TransactionBehavior<FakeCommand, string>>.Instance);

        Func<Task> act = async () => await behavior.Handle(
            new FakeCommand(),
            _ =>
            {
                // Handler AI navbatiga qo'yishga ULGURGAN, keyin xato beradi (masalan keyingi
                // domen qoidasi buzilgan) — bu AYNAN P18-R1 muammosining ssenariysi.
                postCommitActions.Enqueue(_ => { enqueuedActionRan = true; return Task.CompletedTask; });
                throw new InvalidOperationException("Handler ichidagi kutilmagan xato.");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        context.LastTransaction!.RolledBack.Should().BeTrue();
        context.LastTransaction.Committed.Should().BeFalse();
        postCommitActions.RunAsyncCallCount.Should().Be(0, "rollback bo'lganda RunAsync UMUMAN chaqirilmasligi kerak");
        enqueuedActionRan.Should().BeFalse("navbatga qo'yilgan (lekin commit bo'lmagan) amal HECH QACHON bajarilmasligi kerak — P18-R2");
    }

    [Fact]
    public async Task Handle_QueryRequest_SkipsTransactionAndPostCommitActionsEntirely()
    {
        var context = new FakeAppDbContext();
        var postCommitActions = new SpyPostCommitActions();
        var behavior = new TransactionBehavior<FakeQuery, string>(context, postCommitActions, NullLogger<TransactionBehavior<FakeQuery, string>>.Instance);

        var result = await behavior.Handle(new FakeQuery(), _ => Task.FromResult("q-ok"), CancellationToken.None);

        result.Should().Be("q-ok");
        context.LastTransaction.Should().BeNull("`*Query` so'rovlar tranzaksiyaga o'ralmaydi (`docs/06` 4-bo'lim)");
        postCommitActions.RunAsyncCallCount.Should().Be(0);
    }

    private sealed record FakeQuery : IRequest<string>;
}
