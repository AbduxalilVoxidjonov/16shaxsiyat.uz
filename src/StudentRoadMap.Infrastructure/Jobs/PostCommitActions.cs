using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Jobs;

/// <summary>
/// <see cref="IPostCommitActions"/> implementatsiyasi — P18-R1 (`prompts/18`). `Scoped` bo'lib
/// ro'yxatga olinadi (`DependencyInjection.AddInfrastructure`) — har bir HTTP so'rov/MediatR
/// pipeline'i o'z instansiyasiga ega.
/// </summary>
internal sealed class PostCommitActions : IPostCommitActions
{
    private readonly List<Func<CancellationToken, Task>> _actions = [];

    public void Enqueue(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_actions.Count == 0)
        {
            return;
        }

        // Nusxa olib tozalaymiz — `RunAsync` ikki marta chaqirilsa (bo'lmasligi kerak, lekin
        // himoya sifatida) amallar ikki marta bajarilmasin.
        var actionsToRun = _actions.ToArray();
        _actions.Clear();

        foreach (var action in actionsToRun)
        {
            await action(cancellationToken).ConfigureAwait(false);
        }
    }
}
