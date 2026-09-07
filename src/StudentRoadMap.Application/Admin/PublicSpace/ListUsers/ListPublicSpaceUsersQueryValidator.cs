using FluentValidation;

namespace StudentRoadMap.Application.Admin.PublicSpace.ListUsers;

/// <summary>
/// `public` — `AssemblyScanner` faqat ochiq validatorlarni topadi. Sahifalash bu yerda
/// tekshirilMAYDI — `AdminPagingOptions.Normalize` jimgina to'g'irlaydi (`docs/07` 4-bo'lim,
/// boshqa admin ro'yxatlari bilan bir xil).
/// </summary>
public sealed class ListPublicSpaceUsersQueryValidator : AbstractValidator<ListPublicSpaceUsersQuery>
{
    public const int MaxSearchLength = 200;

    public ListPublicSpaceUsersQueryValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .WithMessage($"Qidiruv matni {MaxSearchLength} belgidan oshmasligi kerak.");

        RuleFor(x => x.Status)
            .Must(status => PublicUserStatusFilter.Parse(status) is not null)
            .WithMessage($"Holat filtri quyidagilardan biri bo'lishi kerak: {string.Join(", ", PublicUserStatusFilter.Values)}.");
    }
}
