using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

/// <summary>`AdminTestAssignmentDto` qurish uchun YAGONA joy (`GET` va `PUT` bir xil javob qaytaradi).</summary>
internal static class TestAssignmentMapping
{
    public static async Task<AdminTestAssignmentDto> BuildAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        TestDefinition test,
        AssessmentProgram? program,
        CancellationToken cancellationToken)
    {
        if (program is null)
        {
            return new AdminTestAssignmentDto(
                test.Id,
                test.Status.ToString(),
                test.IsActive,
                IsConfigured: false,
                IsPublic: false,
                SchoolIds: [],
                IsInPublicSpace: false,
                RegistrationMode: RegistrationMode.Full.ToString(),
                State: null,
                IsAvailable: false,
                HasPersonalityBattery: test.IsPersonalityBattery,
                SessionCount: 0);
        }

        // Makon TURI bilan birga (bitta `JOIN`) — maktablar ro'yxatiga ommaviy makon kirmaydi.
        var links = await executor.ToListAsync(
            context.AsNoTracking(context.SchoolPrograms)
                .Where(sp => sp.ProgramId == program.Id)
                .Join(
                    context.AsNoTracking(context.Schools),
                    sp => sp.SchoolId,
                    s => s.Id,
                    (sp, s) => new { sp.SchoolId, s.Kind, sp.CreatedAt })
                .OrderBy(x => x.CreatedAt),
            cancellationToken).ConfigureAwait(false);

        var schoolIds = links.Where(l => l.Kind == SchoolKind.School).Select(l => l.SchoolId).ToList();
        var isInPublicSpace = links.Any(l => l.Kind == SchoolKind.PublicSpace);
        var isPublic = program.Visibility == ProgramVisibility.Public;

        var sessionCount = await executor.CountAsync(
            context.AsNoTracking(context.Assessments).Where(a => a.ProgramId == program.Id),
            cancellationToken).ConfigureAwait(false);

        var state = program.State;
        var isAvailable = state == ProgramState.Active && (isPublic || schoolIds.Count > 0 || isInPublicSpace);

        return new AdminTestAssignmentDto(
            test.Id,
            test.Status.ToString(),
            test.IsActive,
            IsConfigured: true,
            isPublic,
            schoolIds,
            isInPublicSpace,
            program.RegistrationMode.ToString(),
            state.ToString(),
            isAvailable,
            test.IsPersonalityBattery,
            sessionCount);
    }
}
