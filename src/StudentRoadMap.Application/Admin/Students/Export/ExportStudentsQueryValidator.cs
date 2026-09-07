using FluentValidation;
using StudentRoadMap.Application.Admin.Students.List;

namespace StudentRoadMap.Application.Admin.Students.Export;

/// <summary>
/// `ListStudentsQueryValidator` bilan AYNAN bir xil qoidalar (`AdminStudentFilterRules`) —
/// ro'yxat sahifasida qabul qilingan filtr eksportda ham qabul qilinadi va aksincha.
/// </summary>
public sealed class ExportStudentsQueryValidator : AbstractValidator<ExportStudentsQuery>
{
    public ExportStudentsQueryValidator()
    {
        RuleFor(x => x.Gender)
            .Must(AdminStudentFilterRules.IsValidGender)
            .WithMessage(AdminStudentFilterRules.GenderMessage);

        RuleFor(x => x.AgeMin)
            .Must(AdminStudentFilterRules.IsValidAge)
            .WithMessage(AdminStudentFilterRules.AgeRangeMessage);

        RuleFor(x => x.AgeMax)
            .Must(AdminStudentFilterRules.IsValidAge)
            .WithMessage(AdminStudentFilterRules.AgeRangeMessage);

        RuleFor(x => x)
            .Must(x => AdminStudentFilterRules.IsValidAgeOrder(x.AgeMin, x.AgeMax))
            .WithName(nameof(ExportStudentsQuery.AgeMin))
            .WithMessage(AdminStudentFilterRules.AgeOrderMessage);
    }
}
