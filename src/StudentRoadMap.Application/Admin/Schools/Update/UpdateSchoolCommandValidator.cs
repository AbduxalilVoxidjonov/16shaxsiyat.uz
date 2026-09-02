using FluentValidation;

namespace StudentRoadMap.Application.Admin.Schools.Update;

/// <summary>`public` — `CreateSchoolCommandValidator` izohidagi sabab bilan bir xil.</summary>
public sealed class UpdateSchoolCommandValidator : AbstractValidator<UpdateSchoolCommand>
{
    public UpdateSchoolCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Region).NotEmpty().MaximumLength(100);
        RuleFor(x => x.District).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SchoolNumber).MaximumLength(20);
        RuleFor(x => x.ContactPerson).MaximumLength(150);
        RuleFor(x => x.ContactPhone).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(1000);

        RuleFor(x => x.AccessCode)
            .Matches("^[0-9]{6}$")
            .When(x => !string.IsNullOrEmpty(x.AccessCode))
            .WithMessage("Kirish kodi 6 ta raqamdan iborat bo'lishi kerak.");

        RuleFor(x => x.DailyRegistrationLimit)
            .GreaterThan(0)
            .When(x => x.DailyRegistrationLimit.HasValue)
            .WithMessage("Kunlik ro'yxatdan o'tish limiti musbat bo'lishi kerak.");
    }
}
