using FluentValidation;

namespace StudentRoadMap.Application.Admin.Programs.ReorderTests;

/// <summary>`public` — `CreateSchoolCommandValidator` izohidagi sabab bilan bir xil (`AssemblyScanner`).</summary>
public sealed class ReorderProgramTestsCommandValidator : AbstractValidator<ReorderProgramTestsCommand>
{
    public ReorderProgramTestsCommandValidator()
    {
        RuleFor(x => x.TestDefinitionIds).NotEmpty();
    }
}
