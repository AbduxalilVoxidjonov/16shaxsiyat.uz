using FluentValidation;

namespace StudentRoadMap.Application.Admin.Programs.AddTest;

/// <summary>`public` — `CreateSchoolCommandValidator` izohidagi sabab bilan bir xil (`AssemblyScanner`).</summary>
public sealed class AddProgramTestCommandValidator : AbstractValidator<AddProgramTestCommand>
{
    public AddProgramTestCommandValidator()
    {
        RuleFor(x => x.TestDefinitionId).NotEmpty();
        RuleFor(x => x.DisplayOrder).GreaterThan(0);
    }
}
