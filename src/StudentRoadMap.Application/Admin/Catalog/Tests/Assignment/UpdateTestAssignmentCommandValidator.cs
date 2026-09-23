using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

public sealed class UpdateTestAssignmentCommandValidator : AbstractValidator<UpdateTestAssignmentCommand>
{
    /// <summary>Bir so'rovda biriktiriladigan maktablar chegarasi — suiiste'molga qarshi, amalda maktablar soni undan ancha kam.</summary>
    public const int MaxSchoolIds = 1000;

    public UpdateTestAssignmentCommandValidator()
    {
        RuleFor(x => x.RegistrationMode)
            .Must(v => !string.IsNullOrWhiteSpace(v)
                && !int.TryParse(v, out _)
                && Enum.TryParse<Domain.Catalog.RegistrationMode>(v, ignoreCase: true, out var mode)
                && Enum.IsDefined(mode))
            .WithMessage("Ro'yxatdan o'tish rejimi 'Full' yoki 'None' bo'lishi kerak.");

        RuleFor(x => x.SchoolIds)
            .NotNull().WithMessage("Maktablar ro'yxati berilishi kerak (bo'sh bo'lishi mumkin).");

        RuleFor(x => x.SchoolIds)
            .Must(ids => ids.Count <= MaxSchoolIds)
            .WithMessage($"Bir so'rovda ko'pi bilan {MaxSchoolIds} ta maktab.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("Maktab identifikatori bo'sh bo'lishi mumkin emas.")
            .When(x => x.SchoolIds is not null);
    }
}
