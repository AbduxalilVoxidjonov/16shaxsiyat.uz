using FluentAssertions;
using StudentRoadMap.Infrastructure.Jobs;

namespace StudentRoadMap.Infrastructure.Tests.Jobs;

/// <summary>
/// `PostCommitActions` — P18-R1 (`prompts/18`, MAJBURIY): `TransactionBehavior` bu klassni
/// FAQAT commit muvaffaqiyatli bo'lgandan keyin ishga tushiradi. Rollback (istisno) holatida
/// `RunAsync` UMUMAN chaqirilmaydi — shu sabab bu yerda faqat "chaqirilsa ishlaydi"/"ikki marta
/// bajarilmaydi" tekshiriladi; "rollback'da chaqirilmasligi" `CompleteSessionCommandHandler`
/// integratsiya sinovida (P18-R2) tekshiriladi, chunki bu klassning o'zi tranzaksiya haqida
/// hech narsa bilmaydi — qaror butunlay `TransactionBehavior`da.
/// </summary>
public sealed class PostCommitActionsTests
{
    [Fact]
    public async Task RunAsync_NoActionsEnqueued_DoesNothing()
    {
        var postCommitActions = new PostCommitActions();

        await postCommitActions.RunAsync(CancellationToken.None);
        // Istisno chiqmasligi — muvaffaqiyat mezoni.
    }

    [Fact]
    public async Task RunAsync_RunsEnqueuedActionsInOrder()
    {
        var postCommitActions = new PostCommitActions();
        var order = new List<int>();

        postCommitActions.Enqueue(_ => { order.Add(1); return Task.CompletedTask; });
        postCommitActions.Enqueue(_ => { order.Add(2); return Task.CompletedTask; });

        await postCommitActions.RunAsync(CancellationToken.None);

        order.Should().ContainInOrder(1, 2);
    }

    [Fact]
    public async Task RunAsync_CalledTwice_DoesNotRerunAlreadyExecutedActions()
    {
        var postCommitActions = new PostCommitActions();
        var callCount = 0;
        postCommitActions.Enqueue(_ => { callCount++; return Task.CompletedTask; });

        await postCommitActions.RunAsync(CancellationToken.None);
        await postCommitActions.RunAsync(CancellationToken.None);

        callCount.Should().Be(1);
    }

    [Fact]
    public void Enqueue_NullAction_ThrowsArgumentNullException()
    {
        var postCommitActions = new PostCommitActions();

        var act = () => postCommitActions.Enqueue(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
