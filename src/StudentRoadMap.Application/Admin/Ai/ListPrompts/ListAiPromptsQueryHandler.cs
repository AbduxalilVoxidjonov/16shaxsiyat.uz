using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.ListPrompts;

internal sealed class ListAiPromptsQueryHandler : IRequestHandler<ListAiPromptsQuery, Result<IReadOnlyList<AdminPromptTemplateDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public ListAiPromptsQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<IReadOnlyList<AdminPromptTemplateDto>>> Handle(ListAiPromptsQuery request, CancellationToken cancellationToken)
    {
        var templates = await _executor.ToListAsync(
            _context.AsNoTracking(_context.PromptTemplates).OrderByDescending(t => t.CreatedAt),
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AdminPromptTemplateDto> dtos = templates
            .Select(t => new AdminPromptTemplateDto(t.Id, t.Key, t.Version, t.SystemText, t.UserText, t.JsonSchema, t.IsActive, t.CreatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}
